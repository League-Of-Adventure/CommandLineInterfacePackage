using Cysharp.Threading.Tasks;
using Louis.CustomPackages.CommandLineInterface.Core;
using Louis.CustomPackages.CommandLineInterface.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using VContainer;

namespace Louis.CustomPackages.CommandLineInterface.UI {
    public interface IConsole {
        event Action<bool> onCommandLineVisibilityStateChanged;
        FontAsset CurrentFont { get; set; }
    }

    public class Console : MonoBehaviour, IConsole, IOutput, IInputBlocker {
        public event Action<bool> onCommandLineVisibilityStateChanged = delegate { };
        public bool IsInputBlocked => _inputVisible || Time.frameCount == _frameClosed;
        int _frameClosed = -1;

        [Inject] readonly IOutputRegistry _outputProvider;
        [Inject] readonly ICommandRegistry _commandRegistry;
        [Inject] readonly ICommandHandler _commandHandler;

        [Header("UI Settings")]
        [SerializeField] VisualTreeAsset _consoleLayout;
        [SerializeField] PanelSettings _panelSettings;
        [SerializeField] int sortOrder = 1000;
        [Space(10)]
        [SerializeField] FontAsset _consoleFont;

        [Header("Settings")]
        [SerializeField] int _maxLogEntries = 100;
        [SerializeField] int _maxCommandHistory = 10;
        [SerializeField] float _timeBeforeHideOutput = 3f;
        [SerializeField] ConsoleMode _defaultMode = ConsoleMode.OpenOnMessage;

        int _commandHistoryIndex = -1;
        readonly List<string> _commandHistory = new();
        VisualElement _rootContainer;
        ScrollView _outputScroll;
        TextField _inputField;
        VisualElement _inputRow;
        VisualElement _filterMenu;
        Button _filterButton;
        readonly List<(LogLevel level, string output)> _logHistory = new();
        LogLevel _enabledLogLevels = LogLevel.All;
        readonly Dictionary<LogLevel, Toggle> _levelToggles = new();

        ConsoleMode _mode;
        ConsoleMode Mode {
            get => _mode;
            set {
                _mode = value;
                if(_mode == ConsoleMode.AlwaysOpen) {
                    SetOutputVisibility(true);
                } else if(_mode == ConsoleMode.AlwaysClosed) {
                    SetOutputVisibility(false);
                }
            }
        }

        FontAsset _currentFont;
        public FontAsset CurrentFont {
            get => _currentFont;
            set {
                if(value == null) return;
                _currentFont = value;
                if(CurrentFont != null && _rootContainer != null) {
                    FontDefinition fontDef = FontDefinition.FromSDFFont(CurrentFont);
                    _rootContainer.style.unityFontDefinition = fontDef;
                }
            }
        }

        float _hideTimer;
        bool _outputVisible;
        bool _inputVisible;
        bool _isInitialized;

        void InitializeUIToolkitElements() {
            // 1. Configure the UI Document at runtime
            UIDocument uiDoc = gameObject.AddComponent<UIDocument>();
            uiDoc.sortingOrder = sortOrder;
            uiDoc.panelSettings = _panelSettings;
            uiDoc.visualTreeAsset = _consoleLayout;

            // 2. Query elements
            var root = uiDoc.rootVisualElement;
            _rootContainer = root.Q<VisualElement>("console-container");
            _outputScroll = root.Q<ScrollView>("output-box");
            _inputField = root.Q<TextField>("input-field");
            _inputRow = root.Q<VisualElement>("input-row");
            _filterButton = root.Q<Button>("filter-button");
            _filterMenu = root.Q<VisualElement>("filter-menu");
            _levelToggles.Clear();
            foreach(LogLevel level in Enum.GetValues(typeof(LogLevel))) {
                if(level == LogLevel.None || level == LogLevel.All) continue;
                var toggle = new Toggle(level.ToString());
                toggle.AddToClassList("filter-toggle");
                toggle.SetValueWithoutNotify(IsLogLevelEnabled(level));
                toggle.RegisterValueChangedCallback(evt => {
                    if(evt.newValue) _enabledLogLevels |= level;
                    else _enabledLogLevels &= ~level;
                    RefreshFilter();
                });
                _levelToggles.Add(level, toggle);
                _filterMenu.Add(toggle);
            }
            _filterButton.clicked += () => _filterMenu.ToggleInClassList("filter-menu-closed");
            root.RegisterCallback<PointerDownEvent>(evt => {
                var target = evt.target as VisualElement;
                if(target != null && !_filterMenu.Contains(target) && !_filterButton.Contains(target)
                    && target != _filterMenu && target != _filterButton) {
                    _filterMenu.AddToClassList("filter-menu-closed");
                }
            }, TrickleDown.TrickleDown);

            // 3. Setup Scrolling on the output box
            _outputScroll.pickingMode = PickingMode.Position;
            _outputScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;

            // 4. Setup Events
            _inputField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            // 5. Setup Initial State: Hidden
            CurrentFont = _consoleFont;
            _isInitialized = true;
            SetInputVisibility(false);
            SetOutputVisibility(false);
            RefreshFilter();
        }

        private void OnEnable() {
            _outputProvider.AttachOutput(this);
            _commandRegistry.RegisterCommand(
                "echo",
                new CommandSchema()
                    .WithDescription("Outputs some text to the console")
                    .Required<string>("output", 0, "The text you want to output to the console"),
                Echo);
            _commandRegistry.RegisterCommand(
                "clear",
                new CommandSchema()
                    .WithDescription("Clears the Console"),
                Clear);
            _commandRegistry.RegisterCommand(
                "setConsoleMode",
                new CommandSchema()
                    .WithDescription("Set the Console to be Visible, Hidden, or to Appear when a message is sent")
                    .FlagChoice(
                        name: "mode",
                        defaultValue: _defaultMode,
                        options: new[] {
                            ("alwaysOpen", ConsoleMode.AlwaysOpen),
                            ("alwaysClosed", ConsoleMode.AlwaysClosed),
                            ("openOnMessage", ConsoleMode.OpenOnMessage)
                        },
                        description: "Which mode the console should be set to")
                    .FlagShorthands(
                        ("o", "alwaysOpen"),
                        ("c", "alwaysClosed"),
                        ("m", "openOnMessage")),
                SetConsoleMode);
            _commandRegistry.RegisterCommand(
                "setConsoleFilter",
                new CommandSchema()
                    .WithDescription("Show only the selected log levels without clearing log history")
                    .Required<LogLevel>("levels", 0, "Comma-separated log levels, all, or none (e.g. Warning,Error)"),
                SetConsoleFilter);

            InitializeUIToolkitElements();
        }

        private void OnDisable() {
            _outputProvider?.DetachOutput(this);
            _commandRegistry?.UnregisterCommand("echo");
            _commandRegistry?.UnregisterCommand("clear");
            _commandRegistry?.UnregisterCommand("setConsoleMode");
            _commandRegistry?.UnregisterCommand("setConsoleFilter");
            _isInitialized = false;
        }

        private void Update() {
            if(Keyboard.current.backquoteKey.wasPressedThisFrame) {
                // Show the command line
                SetInputVisibility(!_inputVisible);
            }

            if(Keyboard.current.escapeKey.wasPressedThisFrame) {
                // Hide the command line
                SetInputVisibility(false);
            }

            if(_outputVisible && _hideTimer > 0f && Mode == ConsoleMode.OpenOnMessage) {
                _hideTimer -= Time.deltaTime;
                if(_hideTimer <= 0f) {
                    SetOutputVisibility(false);
                }
            }
        }

        void OnKeyDown(KeyDownEvent evt) {
            // Prevent the backtick/tilde key from printing "`" into the field when toggling
            if(evt.keyCode == KeyCode.BackQuote) {
                evt.StopImmediatePropagation();
                return;
            }

            if(!_inputVisible || !_isInitialized) return;

            // Scrolling with Keyboard
            if(evt.keyCode == KeyCode.PageUp) {
                _outputScroll.scrollOffset -= new Vector2(0, 50);
                evt.StopPropagation();
                return;
            }
            if(evt.keyCode == KeyCode.PageDown) {
                _outputScroll.scrollOffset += new Vector2(0, 50);
                evt.StopPropagation();
                return;
            }

            // Command History Navigation
            if(evt.keyCode == KeyCode.UpArrow) {
                _commandHistoryIndex--;
                if(_commandHistoryIndex == -2) {
                    _commandHistoryIndex = _commandHistory.Count - 1;
                }
                _inputField.value = _commandHistoryIndex == -1 ? string.Empty : _commandHistory[_commandHistoryIndex];
            } else if(evt.keyCode == KeyCode.DownArrow) {
                _commandHistoryIndex++;
                if(_commandHistoryIndex >= _commandHistory.Count) {
                    _commandHistoryIndex = -1;
                }
                _inputField.value = _commandHistoryIndex == -1 ? string.Empty : _commandHistory[_commandHistoryIndex];
            }

            // Submit Command
            if(evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) {
                string command = _inputField.value;

                if(!string.IsNullOrWhiteSpace(command)) {
                    _commandHandler?.PushCommand(command);
                    _commandHistory.Add(command);
                    if(_commandHistory.Count > _maxCommandHistory) {
                        _commandHistory.RemoveAt(0);
                    }
                }

                SetInputVisibility(false);
                evt.StopImmediatePropagation();
                _inputField.value = "";
            }
        }

        void SetOutputVisibility(bool visible) {
            if(!_isInitialized) return;
            // Check current mode for visibility override rules
            if(Mode == ConsoleMode.AlwaysOpen) visible = true;
            else if(Mode == ConsoleMode.AlwaysClosed) visible = false;

            if(visible) _hideTimer = _timeBeforeHideOutput;
            _outputVisible = visible;
            _outputScroll.EnableInClassList("hidden", !visible);
        }

        async void SetInputVisibility(bool visible) {
            if(!_isInitialized) return;
            if (_inputVisible && !visible) {
                _frameClosed = Time.frameCount;
            }
            _inputRow.EnableInClassList("hidden", !visible);
            if(!visible) _filterMenu.AddToClassList("filter-menu-closed");
            _inputVisible = visible;

            _commandHistoryIndex = -1;
            if(visible) {
                await UniTask.Yield();
                _inputField.Focus();
                await UniTask.Yield();
                _inputField.value = string.Empty;
            } else {
                _inputField.Blur();
            }
            onCommandLineVisibilityStateChanged(visible);
        }

        public void Write(Log log) {
            Write(log.Formatted, log.level);
        }

        void Write(string output, LogLevel level = LogLevel.Info) {
            _logHistory.Add((level, output));
            while(_logHistory.Count > Math.Max(0, _maxLogEntries)) {
                _logHistory.RemoveAt(0);
            }
            if(!_isInitialized) return;
            RebuildOutput();
            if(IsLogLevelEnabled(level)) {
                SetOutputVisibility(true);
                ScrollToBottom();
            }
        }

        void RebuildOutput() {
            _outputScroll.Clear();
            foreach(var entry in _logHistory) {
                if(!IsLogLevelEnabled(entry.level)) continue;
                var label = new Label(entry.output);
                label.AddToClassList("log-entry");
                _outputScroll.Add(label);
            }
        }

        bool IsLogLevelEnabled(LogLevel level) => (_enabledLogLevels & level) != LogLevel.None;

        void RefreshFilter() {
            int enabledCount = 0;
            foreach(var pair in _levelToggles) {
                bool enabled = IsLogLevelEnabled(pair.Key);
                pair.Value.SetValueWithoutNotify(enabled);
                if(enabled) enabledCount++;
            }
            _filterButton.text = $"Levels ({enabledCount}/{_levelToggles.Count})";
            RebuildOutput();
            ScrollToBottom();
        }

        async void ScrollToBottom() {
            await UniTask.Yield();
            await UniTask.Yield();
            await UniTask.Yield();
            if(!_isInitialized) return;
            var scroller = _outputScroll.verticalScroller;
            _outputScroll.scrollOffset = new Vector2(0, scroller.highValue);
        }

        #region Command Line Functions
        UniTask Echo(BoundArgs args, CancellationToken token) {
            if(!_isInitialized) return UniTask.CompletedTask;
            Write($"> {args.Get<string>("output")}");
            return UniTask.CompletedTask;
        }

        UniTask Clear(BoundArgs args, CancellationToken token) {
            if(!_isInitialized) return UniTask.CompletedTask;
            SetOutputVisibility(true);
            _logHistory.Clear();
            _outputScroll.Clear();
            return UniTask.CompletedTask;
        }

        UniTask SetConsoleFilter(BoundArgs args, CancellationToken token) {
            var levels = args.Get<LogLevel>("levels");
            if((levels & ~LogLevel.All) != LogLevel.None) {
                throw new CommandArgumentException($"Unknown log levels '{levels}'. Use {string.Join(", ", Enum.GetNames(typeof(LogLevel)))}.");
            }
            _enabledLogLevels = levels;
            if(_isInitialized) {
                RefreshFilter();
                SetOutputVisibility(true);
            }
            return UniTask.CompletedTask;
        }

        UniTask SetConsoleMode(BoundArgs args, CancellationToken token) {
            if(!_isInitialized) return UniTask.CompletedTask;
            var mode = args.Get<ConsoleMode>("mode");
            Mode = mode;
            LogDispatch.Log(this, $"Set Console Mode to {Mode}");
            return UniTask.CompletedTask;
        }
        #endregion
    }

    public enum ConsoleMode {
        AlwaysOpen,
        AlwaysClosed,
        OpenOnMessage
    }
}
