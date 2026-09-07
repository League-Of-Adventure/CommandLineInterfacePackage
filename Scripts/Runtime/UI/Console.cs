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
#if USE_VCONTAINER
using VContainer;
#endif
using ILogDispatcher = Louis.CustomPackages.CommandLineInterface.Logging.ILogDispatcher;

namespace Louis.CustomPackages.CommandLineInterface.UI {
    public interface IConsole {
        event Action<bool> onCommandLineVisibilityStateChanged;
        FontAsset CurrentFont { get; set; }
    }

    public class Console : MonoBehaviour, IConsole, IOutput, IInputBlocker {
        public event Action<bool> onCommandLineVisibilityStateChanged = delegate { };
        public bool IsInputBlocked => _inputVisible || Time.frameCount == _frameClosed;
        int _frameClosed = -1;

#if USE_VCONTAINER
        [Inject] readonly IOutputRegistry _outputProvider;
        [Inject] readonly ICommandRegistry _commandRegistry;
        [Inject] readonly ICommandHandler _commandHandler;
#else
        [Header("References")]
        [SerializeField] CommandRegistry _commandRegistry;
        [SerializeField] CommandHandler _commandHandler;
        [SerializeField] CommandLogger _outputProvider;
#endif

        IOutputRegistry OutputProvider => _outputProvider;
        ICommandHandler CommandHandler => _commandHandler;
        ICommandRegistry CommandRegistry => _commandRegistry;

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

        private void Awake() {
            // 1. Configure the UI Document at runtime
            UIDocument uiDoc = gameObject.AddComponent<UIDocument>();
            uiDoc.panelSettings = _panelSettings;
            uiDoc.visualTreeAsset = _consoleLayout;
            uiDoc.sortingOrder = sortOrder;

            // 2. Query elements
            var root = uiDoc.rootVisualElement;
            _rootContainer = root.Q<VisualElement>("console-container");
            _outputScroll = root.Q<ScrollView>("output-box");
            _inputField = root.Q<TextField>("input-field");

            // 3. Setup Scrolling on the output box
            _outputScroll.pickingMode = PickingMode.Position;
            _outputScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;

            // 4. Setup Events
            _inputField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            // 5. Setup Initial State: Hidden
            CurrentFont = _consoleFont;
            SetInputVisibility(false);
            SetOutputVisibility(false);
        }

        private void OnEnable() {
            OutputProvider.AttachOutput(this);
            CommandRegistry.RegisterCommand(
                "echo",
                new CommandSchema()
                    .WithDescription("Outputs some text to the console")
                    .Required<string>("output", 0, "The text you want to output to the console"),
                Echo);
            CommandRegistry.RegisterCommand(
                "clear",
                new CommandSchema()
                    .WithDescription("Clears the Console"),
                Clear);
            CommandRegistry.RegisterCommand(
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
        }

        private void OnDisable() {
            OutputProvider?.DetachOutput(this);
            CommandRegistry?.UnregisterCommand("echo");
            CommandRegistry?.UnregisterCommand("clear");
            CommandRegistry?.UnregisterCommand("setConsoleMode");
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

            // Scrolling through previous commands using up and down arrow keys
            if (evt.keyCode == KeyCode.UpArrow) {
                _commandHistoryIndex--;
                if(_commandHistoryIndex == -2) {
                    _commandHistoryIndex = _commandHistory.Count - 1;
                }
                _inputField.value = _commandHistoryIndex == -1 ? string.Empty : _commandHistory[_commandHistoryIndex];
            } else if (evt.keyCode == KeyCode.DownArrow) {
                _commandHistoryIndex++;
                if (_commandHistoryIndex >= _commandHistory.Count) {
                    _commandHistoryIndex = -1;
                }
                _inputField.value = _commandHistoryIndex == -1 ? string.Empty : _commandHistory[_commandHistoryIndex];
            }

            if(evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) {
                // 1. Get the value
                string command = _inputField.value;

                // 2. Only process if it's not empty (prevents spamming empty enters)
                if(!string.IsNullOrWhiteSpace(command)) {
                    CommandHandler?.PushCommand(command);
                    _commandHistory.Add(command);
                    if (_commandHistory.Count > _maxCommandHistory) {
                        _commandHistory.RemoveAt(0); // Keep command history manageable
                    }
                }

                // 3. Hide Command Line and stop event propagation
                SetInputVisibility(false);
                evt.StopImmediatePropagation();
                _inputField.value = "";
            }
        }

        void SetOutputVisibility(bool visible) {
            // Check current mode for visibility override rules
            if(Mode == ConsoleMode.AlwaysOpen) visible = true;
            else if(Mode == ConsoleMode.AlwaysClosed) visible = false;

            if(visible) _hideTimer = _timeBeforeHideOutput;
            _outputVisible = visible;
            _outputScroll.EnableInClassList("hidden", !visible);
        }

        void SetInputVisibility(bool visible) {
            if (_inputVisible && !visible) {
                _frameClosed = Time.frameCount;
            }
            _inputField.EnableInClassList("hidden", !visible);
            _inputVisible = visible;

            _commandHistoryIndex = -1;
            if(visible) {
                _inputField.Focus();
                _inputField.schedule.Execute(() => _inputField.value = string.Empty).ExecuteLater(1);
            } else {
                _inputField.Blur();
            }
            onCommandLineVisibilityStateChanged(visible);
        }

        public void Write(Log log) {
            string output = log.Formatted;
            Write(output);
        }

        async void Write(string output) {
            // 0. Set the Output log to visible if it isn't already
            SetOutputVisibility(true);

            // 1. Create the element
            Label newEntry = new ($"{output}");
            newEntry.AddToClassList("log-entry");

            // 2. Add to the scroll view
            _outputScroll.Add(newEntry);

            // 3. Keep log size under control
            if(_outputScroll.childCount > _maxLogEntries) {
                _outputScroll.RemoveAt(0); // Remove the oldest entry
            }

            // 4. Scroll to the bottom to show the new message (scheduled to allow for layout update)
            await UniTask.Yield();
            var scroller = _outputScroll.verticalScroller;
            _outputScroll.scrollOffset = new Vector2(0, scroller.highValue);
        }

        #region Command Line Functions
        UniTask Echo(BoundArgs args, CancellationToken token) {
            Write($"> {args.Get<string>("output")}");
            return UniTask.CompletedTask;
        }

        UniTask Clear(BoundArgs args, CancellationToken token) {
            SetOutputVisibility(true);
            _outputScroll.Clear();
            return UniTask.CompletedTask;
        }

        UniTask SetConsoleMode(BoundArgs args, CancellationToken token) {
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