using System;
using System.Collections.Generic;
using System.Globalization;

namespace Louis.CustomPackages.CommandLineInterface.Core {
    internal readonly struct ParsedTokens {
        public readonly Dictionary<string, string> Named;
        public readonly HashSet<string> Flags;
        public readonly List<string> Positional;

        public ParsedTokens(string[] tokens, IReadOnlyDictionary<string, string> shorthands) {
            Named = new(StringComparer.OrdinalIgnoreCase);
            Flags = new(StringComparer.OrdinalIgnoreCase);
            Positional = new();

            foreach (var token in tokens) {
                // A leading minus belongs to a numeric value, not a flag name.
                if(token.StartsWith("-") && double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    Positional.Add(token);
                else if(token.StartsWith("--") && token.Length > 2)
                    Flags.Add(token[2..]);
                else if(token.StartsWith("-") && token.Length > 1) { 
                    var key = token[1..];
                    if(shorthands != null && shorthands.TryGetValue(key, out var longFlag))
                        Flags.Add(longFlag);
                    else
                        Flags.Add(key);
                }
                else {
                    int eq = token.IndexOf('=');
                    if (eq > 0) {
                        Named[token[..eq]] = token[(eq + 1)..];
                    } else {
                        Positional.Add(token);
                    }
                }
            }
        }
    }
}