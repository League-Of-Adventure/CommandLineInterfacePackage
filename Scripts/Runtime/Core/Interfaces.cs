using Cysharp.Threading.Tasks;
using Louis.CustomPackages.CommandLineInterface.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Louis.CustomPackages.CommandLineInterface.Core {
    public interface ICommandCompiler {
        Command CreateCommand(string command, string[] args);
        Command CreateCommand(string rawCommand);
        CompilationResult TryCompile(Command command);
    }

    public interface ICommandRegistry {
        void RegisterCommand(string keyword, CommandSchema schema, Func<ILogDispatcher, BoundArgs, CancellationToken, UniTask> callback);
        void UnregisterCommand(string keyword);
        public IReadOnlyDictionary<string, CommandSchema> Schemas { get; }
        public IReadOnlyDictionary<string, Func<ILogDispatcher, BoundArgs, CancellationToken, UniTask>> Callbacks { get; }
    }

    public interface ICommandHandler {
        void PushCommand(Command command);
        void PushCommand(string command);
        void PushCommand(string keyword, params string[] args);
    }
}