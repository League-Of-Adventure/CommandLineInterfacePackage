using Cysharp.Threading.Tasks;
using Louis.CustomPackages.CommandLineInterface.Core.Exceptions;
using Louis.CustomPackages.CommandLineInterface.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using ILogDispatcher = Louis.CustomPackages.CommandLineInterface.Logging.ILogDispatcher;
using LogDispatcher = Louis.CustomPackages.CommandLineInterface.Logging.LogDispatcher;
[assembly: InternalsVisibleTo("com.Louis.CommandLineInterface.Tests")]

namespace Louis.CustomPackages.CommandLineInterface.Core {
    [AddComponentMenu("Command Line Interface/Command Handler")]
    [RequireComponent(typeof(LogDispatcher))]
    [RequireComponent(typeof(CommandRegistry))]
    [RequireComponent(typeof(CommandCompiler))]
    public class CommandHandler : MonoBehaviour, ICommandHandler {
        CancellationTokenSource _cts = new();
        readonly Queue<Command> _commandQueue = new();
        ILogDispatcher _logger;
        ICommandRegistry _registry;
        ICommandCompiler _compiler;

        private void Awake() {
            TryGetComponent(out _logger);
            TryGetComponent(out _registry);
            TryGetComponent(out _compiler);
        }

        private void OnEnable() {
            _registry.RegisterCommand(
                "cancelAll",
                new CommandSchema()
                    .WithDescription("Clears the current execution queue of any pending or executing tasks")
                    .ExecutesImmediately(true),
                CancelAll);
            _registry.RegisterCommand(
                "help",
                new CommandSchema()
                    .WithDescription("Outputs a list of functions and how to use them")
                    .Optional("functionName", 0, "", "A specific function you want to know the use of")
                    .ExecutesImmediately(true),
                ShowHelpText
                );
        }

        public void Start() {
            ProcessCommands(_cts.Token);
        }

        public void OnDestroy() {
            _cts.Cancel();
        }

        async void ProcessCommands(CancellationToken token) {
            _logger.Log("CommandManager", $"Command Processing Started: {_commandQueue.Count} unhandled commands pushed", LogLevel.Success);
            while(true) {
                if(_commandQueue.Count > 0) {
                    try {
                        await RunCommand(_commandQueue.Dequeue(), token);
                    } catch(Exception e) {
                        _logger.Log("CommandManager", e.Message, LogLevel.Error);
                    }
                } else {
                    await UniTask.Yield();
                }

                if(token.IsCancellationRequested) {
                    _logger.Log("CommandManager", $"Cancellation Requested: {_commandQueue.Count} unhandled commands remaining", LogLevel.Warning);
                    return;
                }
            }
        }

        async UniTask RunCommand(Command command, CancellationToken token) {
            if(!_registry.Schemas.ContainsKey(command.CommandName)) throw new CommandException($"Internal Error, not function bound for command {command.CommandName}");
            var callback = _registry.Callbacks[command.CommandName];
            try {
                await callback.Invoke(command.BoundArgs, token);
            } catch(Exception e) {
                _logger.Log("CommandManager", e.Message, LogLevel.Error);
            }
        }

        public void PushCommand(string keyword, params string[] args) {
            try {
                // Get our command for this keyword and try to compile it (compilation may fail if it relies on a command that hasn't been registered yet, e.g. in a scene about to be loaded
                Command command = _compiler.CreateCommand(keyword, args);
                var result = _compiler.TryCompile(command);
                // Some commands are meant to skip the queue
                if(result.succesful && command.ExecuteImmediately) {
                    RunCommand(command, gameObject.GetCancellationTokenOnDestroy()).Forget();
                } else {
                    _commandQueue.Enqueue(command);
                }
            } catch(Exception e) {
                _logger.Log("CommandManager", e.Message, LogLevel.Error);
            }
        }

        public void PushCommand(string raw) {
            try {
                Command command = _compiler.CreateCommand(raw);
                string keyword = command.CommandName;
                CommandSchema schema = _registry.Schemas[keyword];
                if(schema.ExecuteImmediately) {

                }
                _commandQueue.Enqueue(command);
            } catch(Exception e) {
                _logger.Log("CommandManager", e.Message, LogLevel.Error);
            }
        }

        public void PushCommand(Command command) {
            if(command.ExecuteImmediately) {
                RunCommand(command, gameObject.GetCancellationTokenOnDestroy()).Forget();
            } else {
                _commandQueue.Enqueue(command);
            }
        }

        async UniTask CancelAll(BoundArgs args, CancellationToken cancellationToken) {
            _logger.Log("CommandManager", "Cancelling all current actions and clearing command buffer", LogLevel.Warning);
            _cts.Cancel();
            _cts = new();
            _commandQueue.Clear();
            await UniTask.Yield();
            await UniTask.Delay(250);
            ProcessCommands(_cts.Token);
        }

        UniTask ShowHelpText(BoundArgs args, CancellationToken cancellationToken) {
            string functionName = args.Get<string>("functionName");
            string helpText;
            if(string.IsNullOrWhiteSpace(functionName)) {
                helpText = HelpGenerator.GenerateHelpString(_registry.Schemas);
            } else {
                if(!_registry.Schemas.TryGetValue(functionName, out var schema)) throw new CommandArgumentException($"Function {functionName} is not a registered function");
                helpText = HelpGenerator.GenerateUsage(functionName, schema);
            }
            _logger.Log($"CommandManager", helpText);
            return UniTask.CompletedTask;
        }
    }
}