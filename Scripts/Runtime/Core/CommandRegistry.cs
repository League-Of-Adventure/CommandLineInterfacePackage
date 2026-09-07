using Cysharp.Threading.Tasks;
using Louis.CustomPackages.CommandLineInterface.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Louis.CustomPackages.CommandLineInterface.Core {
    [AddComponentMenu("Command Line Interface/Command Registry")]
    public class CommandRegistry : MonoBehaviour, ICommandRegistry {
        readonly Dictionary<string, CommandSchema> _schemaMap = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Func<BoundArgs, CancellationToken, UniTask>> _funcMap = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, CommandSchema> Schemas => _schemaMap;
        public IReadOnlyDictionary<string, Func<BoundArgs, CancellationToken, UniTask>> Callbacks => _funcMap;

        public void RegisterCommand(string command, CommandSchema schema, Func<BoundArgs, CancellationToken, UniTask> callback) {
            if(_schemaMap.ContainsKey(command)) throw new KeywordAlreadyExistsException($"Cannot register {command}. It is already registered");
            _schemaMap[command] = schema ?? throw new NullReferenceException($"Cannot register null as a schema for command {command}");
            _funcMap[command] = callback ?? throw new NullReferenceException($"Cannot register null as a function to call for command {command}");
        }

        public void UnregisterCommand(string command) {
            if(_schemaMap.ContainsKey(command)) _schemaMap.Remove(command);
            if(_funcMap.ContainsKey(command)) _funcMap.Remove(command);
        }
    }
}