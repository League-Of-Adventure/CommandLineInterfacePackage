using Cysharp.Threading.Tasks;
using Louis.CustomPackages.CommandLineInterface.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;

namespace Louis.CustomPackages.CommandLineInterface.Core {
    [AddComponentMenu("Command Line Interface/Command Registry")]
    public class CommandRegistry : ICommandEntryRegistry {
        readonly Dictionary<string, ICommandEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, CommandSchema> _schemaMap = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Func<BoundArgs, CancellationToken, UniTask>> _callbackMap = new (StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, CommandSchema> Schemas { get; }
        public IReadOnlyDictionary<string, Func<BoundArgs, CancellationToken, UniTask>> Callbacks { get; }

        public CommandRegistry() {
            // These remain live views as commands are added and removed.
            Schemas = new ReadOnlyDictionary<string, CommandSchema>(_schemaMap);
            Callbacks = new ReadOnlyDictionary<string, Func<BoundArgs, CancellationToken, UniTask>>(_callbackMap);
        }

        public void RegisterCommand(ICommandEntry entry) {
            if(entry == null) throw new ArgumentNullException(nameof(entry));

            string keyword = entry.Command;
            if(keyword == null) throw new ArgumentNullException(nameof(entry.Command));

            if(_entries.ContainsKey(keyword)) throw new KeywordAlreadyExistsException($"Cannot register {keyword}. It is already registered");

            CommandSchema schema = entry.Schema ?? throw new NullReferenceException($"Cannot register null as a schema for command {keyword}");

            Func<BoundArgs, CancellationToken, UniTask> callback = entry.ExecuteAsync;

            _entries.Add(keyword, entry);
            _schemaMap.Add(keyword, schema);
            _callbackMap.Add(keyword, callback);
        }

        public void RegisterCommand(string keyword, CommandSchema schema, Func<BoundArgs, CancellationToken, UniTask> callback) {
            if(keyword == null) throw new ArgumentNullException(nameof(keyword));
            if(_entries.ContainsKey(keyword)) throw new KeywordAlreadyExistsException($"Cannot register {keyword}. It is already registered");

            if(schema == null) throw new NullReferenceException($"Cannot register null as a schema for command {keyword}");
            if(callback == null) throw new NullReferenceException($"Cannot register null as a function to call fro comamnd {keyword}");
            RegisterCommand(new CallbackCommandEntry(keyword, schema, callback));
        }

        public void UnregisterCommand(ICommandEntry entry) {
            if(entry == null) throw new ArgumentNullException(nameof(entry));
            if(_entries.TryGetValue(entry.Command, out var registered) && ReferenceEquals(registered, entry)) UnregisterCommand(entry.Command);
        }

        public void UnregisterCommand(string keyword) {
            _entries.Remove(keyword);
            _schemaMap.Remove(keyword);
            _callbackMap.Remove(keyword);
        }

        public bool TryGetEntry(string keyword, out ICommandEntry entry) {
            return _entries.TryGetValue(keyword, out entry);
        }

        sealed class CallbackCommandEntry : ICommandEntry {
            readonly Func<BoundArgs, CancellationToken, UniTask> _callback;

            public string Command { get; }
            public CommandSchema Schema { get; }

            public CallbackCommandEntry(
                string command,
                CommandSchema schema,
                Func<BoundArgs, CancellationToken, UniTask> callback) {
                Command = command;
                Schema = schema;
                _callback = callback;
            }

            public UniTask ExecuteAsync(
                BoundArgs args,
                CancellationToken token) {
                return _callback(args, token);
            }
        }
    }
}