using Cysharp.Threading.Tasks;
using Louis.CustomPackages.CommandLineInterface.Core;
using Louis.CustomPackages.CommandLineInterface.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using ILogDispatcher = Louis.CustomPackages.CommandLineInterface.Logging.ILogDispatcher;

namespace com.Louis.CommandLineInterface.Commands {
#if USE_VCONTAINER
    public class RunRepeatedCommand : IDisposable, IStartable {
#else
    public class RunRepeatedCommand : IDisposable
#endif


        readonly CancellationTokenSource _cts = new();
        readonly Dictionary<string, CancellationTokenSource> _runningOperations = new();
        readonly ICommandRegistry _commandRegistry;
        readonly ICommandCompiler _commandCompiler;
        readonly ICommandHandler _commandHandler;

#if USE_VCONTAINER
        [Inject]
#endif
        public RunRepeatedCommand(ICommandRegistry commandRegistry, ICommandCompiler commandCompiler, ICommandHandler commandHandler) {
            _commandRegistry = commandRegistry;
            _commandHandler = commandHandler;
            _commandCompiler = commandCompiler;
#if !USE_VCONTAINER
            Start();
#endif
        }

        public void Start() {
            _commandRegistry.RegisterCommand("runRepeated",
                new CommandSchema()
                    .WithDescription("Runs a given command repeatedly at regular intervals")
                    .Required<string>("command", 0, "The command to run")
                    .Required<float>("interval", 1, "How many seconds to wait between executions")
                    .Optional<string>("key", 2, "", "The key to give this operation such that it can be stopped later on"),
                RunRepeated);
            _commandRegistry.RegisterCommand("stopRepeated",
                new CommandSchema()
                    .WithDescription("Stops running a command that has been started using it's user-defined key")
                    .Required<string>("key", 0, "The name of the operation you want to stop"),
                StopRepeated);
        }

        public void Dispose() {
            _commandRegistry.UnregisterCommand("runRepeated");
            _commandRegistry.UnregisterCommand("stopRepeated");
            _cts?.Cancel();
            _cts?.Dispose();
        }

        UniTask RunRepeated(ILogDispatcher logger, BoundArgs args, CancellationToken _) {
            string command = args.Get<string>("command");
            float interval = args.Get<float>("interval");
            string key = args.Get<string>("key");

            if(string.IsNullOrWhiteSpace(key))
                key = Guid.NewGuid().ToString();

            if(interval < 0) {
                logger.Log(this, $"Cannot run a command with interval < 0", LogLevel.Error);
                return UniTask.CompletedTask;
            }
            if(_runningOperations.ContainsKey(key)) {
                logger.Log(this, $"Cannot run a looping command with key={{{key}}} because one already exists with that key");
                return UniTask.CompletedTask;
            }

            CancellationTokenSource cts = new();
            _runningOperations.Add(key, cts);
            CancellationToken cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, _cts.Token).Token;
            logger.Log(this, $"Started looping command with key={{{key}}}");
            RunRepeated(command, interval, key, cancellationToken).Forget();
            return UniTask.CompletedTask;
        }

        UniTask StopRepeated(ILogDispatcher logger, BoundArgs args, CancellationToken cancellationToken) {
            string key = args.Get<string>("key");
            if(_runningOperations.TryGetValue(key, out var cts)) {
                cts.Cancel();
                cts.Dispose(); // Clean up memory allocation
                _runningOperations.Remove(key);
                logger.Log(this, $"Cancelled looping command with key={{{key}}}");
            } else {
                logger.Log(this, $"Cannot cancel operation with key={{{key}}}. A looping operation with that key does not exist");
            }
            return UniTask.CompletedTask;
        }

        async UniTaskVoid RunRepeated(string rawCommand, float interval, string key, CancellationToken cancellationToken) {
            Command command = _commandCompiler.CreateCommand(rawCommand);

            try {
                while(!cancellationToken.IsCancellationRequested) {
                    _commandHandler.PushCommand(command);
                    await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: cancellationToken);
                }
            } catch(OperationCanceledException) { } finally {
                if(_runningOperations.ContainsKey(key) && cancellationToken.IsCancellationRequested) {
                    _runningOperations.Remove(key);
                }
            }
        }

        public override string ToString() => "RunRepeatedCommand";
    }
}
