using System;
using System.Collections.Generic;
using UnityEngine;

namespace Louis.CustomPackages.CommandLineInterface.Core {
    internal static class CommandBinder {
        [Serializable]
        sealed class JsonArrayWrapper<T> {
            public T[] items;
        }

        static object DeserializeArray(string json, Type arrayType) {
            if(arrayType.GetArrayRank() != 1)
                throw new NotSupportedException("Only one-dimensional arrays are supported.");

            string trimmed = json.Trim();
            if(!trimmed.StartsWith("[") || !trimmed.EndsWith("]"))
                throw new FormatException("Expected a JSON array enclosed in [ and ].");

            Type wrapperType = typeof(JsonArrayWrapper<>)
                .MakeGenericType(arrayType.GetElementType());
            object wrapper = JsonUtility.FromJson(
                "{\"items\":" + trimmed + "}", wrapperType);

            return wrapperType.GetField(nameof(JsonArrayWrapper<int>.items))
                .GetValue(wrapper)
                ?? throw new FormatException("Could not deserialize the JSON array.");
        }
        public static BoundArgs Bind(CommandSchema schema, ParsedTokens tokens) {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // 1️. Bind regular arguments (named or positional)
            foreach(var arg in schema.Args) {
                string raw = null;

                // Named argument takes precedence
                if(tokens.Named.TryGetValue(arg.Name, out var named)) {
                    raw = named;
                }
                // Positional fallback
                else if(arg.Position < tokens.Positional.Count) {
                    raw = tokens.Positional[arg.Position];
                }

                // Missing value handling
                if(raw == null) {
                    if(arg.Required)
                        throw new CommandArgumentException(
                            $"Missing required argument '{arg.Name}'");

                    values[arg.Name] = arg.DefaultValue;
                    continue;
                }

                if (raw.StartsWith('"') && raw.EndsWith('"')) {
                    raw = raw[1..^1];
                    if(arg.Type == typeof(string))
                        raw = CommandTokenizer.UnescapeQuotedString(raw);
                }

                // Type conversion
                object converted;
                try {
                    if(arg.Type.IsEnum) {
                        converted = Enum.Parse(arg.Type, raw, ignoreCase: true);
                    } else if(arg.Type.IsArray) {
                        // JsonUtility requires an object at the root.
                        converted = DeserializeArray(raw, arg.Type);
                    } else if(arg.Type != typeof(string) && !arg.Type.IsPrimitive) {
                        // JSON object -> Custom class
                        converted = JsonUtility.FromJson(raw, arg.Type);
                    } else {
                        // Fallback for primitives
                        converted = Convert.ChangeType(raw, arg.Type);
                    }
                } catch(Exception ex) {
                    throw new CommandArgumentException($"Invalid value '{raw}' for argument '{arg.Name}'. Expected type {arg.Type}. {ex.Message}");
                }
                if(converted != null) values[arg.Name] = converted;
            }

            // 2️. Collect boolean flags
            var flags = new HashSet<string>(tokens.Flags, StringComparer.OrdinalIgnoreCase);

            foreach(var flag in schema.Flags) {
                // Default to false if not present
                values[flag.Name] = flags.Contains(flag.Name);
            }

            // 3️. Bind mutually exclusive flag choices
            foreach(var choice in schema.FlagChoices) {
                object selectedValue = choice.DefaultValue;
                string matchedFlag = null;

                foreach(var (flag, value) in choice.Options) {
                    if(tokens.Flags.Contains(flag)) {
                        if(matchedFlag != null) {
                            throw new CommandArgumentException(
                                $"Flags '--{matchedFlag}' and '--{flag}' are mutually exclusive");
                        }

                        matchedFlag = flag;
                        selectedValue = value;
                    }
                }

                values[choice.Name] = selectedValue;
            }

            return new BoundArgs(values, flags);
        }
    }
}