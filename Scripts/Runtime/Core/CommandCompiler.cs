using System;
using UnityEngine;

namespace Louis.CustomPackages.CommandLineInterface.Core {
    [AddComponentMenu("Command Line Interface/Command Compiler")]
    [RequireComponent(typeof(ICommandRegistry))]
    public class CommandCompiler : ICommandCompiler {
        readonly ICommandRegistry _registry;

        public CommandCompiler(ICommandRegistry registry) {
            _registry = registry;
        }

        public Command CreateCommand(string keyword, string[] args) {
            if(string.IsNullOrWhiteSpace(keyword)) throw new ArgumentException("Cannot have empty command");
            Command command = new(keyword, args);
            TryCompile(command);
            return command;
        }

        public Command CreateCommand(string rawCommand) {
            if(string.IsNullOrWhiteSpace(rawCommand)) throw new ArgumentException("Cannot have empty command");
            // Step 0: Tokenize Input
            string[] tokens = CommandTokenizer.Tokenize(rawCommand);
            string keyword = tokens[0];
            string[] args = tokens[1..];
            Command command = new Command(keyword, args);
            TryCompile(command);
            return command;
        }

        public CompilationResult TryCompile(Command command) {
            if(!_registry.Schemas.ContainsKey(command.CommandName)) return new(false, $"Command {command.CommandName} not recognised");
            if(!_registry.Callbacks.ContainsKey(command.CommandName)) return new(false, $"Internal Error, not function bound for command {command.CommandName}");
            if(command.IsCompiled) return new(true, "Command is already compiled");

            // Step 1: Identify which function we are compiling for
            var schema = _registry.Schemas[command.CommandName];

            // Step 2: Parse Tokens according to function schema
            var parsedTokens = new ParsedTokens(command.RawTokens, schema.Shorthands);

            // Step 3: Bind the tokens as arguments to the schema
            var boundArgs = CommandBinder.Bind(schema, parsedTokens);

            // Step 4: Return compiled command with keyword and bound arguments
            command.SetBoundArgs(boundArgs);

            // Step 5: Set command to execute normally or bypass the normal command queue
            command.ExecuteImmediately = schema.ExecuteImmediately;
            return new(true, "Compilation Successful");
        }
    }

    [Serializable]
    public class CommandCacheEntry {
        public Command command;
        public int lifetime;
    }


    [Serializable]
    public struct CompilationResult {
        public bool succesful;
        public string message;

        public CompilationResult(bool succesful, string message) {
            this.succesful = succesful;
            this.message = message;
        }
    }
}