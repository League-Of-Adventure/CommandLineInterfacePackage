using System;
using System.Collections.Generic;
using System.Text;

namespace Louis.CustomPackages.CommandLineInterface.Core {
    internal static class CommandTokenizer {
        /// <summary>
        /// Tokenizes a command string into tokens. Supports:
        /// - Quoted strings
        /// - Multiline commands
        /// - Arrays ([...]) and objects ({...}) as single tokens
        /// - Nested arrays/objects
        /// - Normalizes internal whitespace in arrays/objects
        /// </summary>
        /// <param name="input">The input command string</param>
        /// <returns>Array of tokens</returns>
        /// <exception cref="CommandParseException"></exception>
        public static string[] Tokenize(string input) {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            var stack = new Stack<char>(); // Track [ ] and { }

            for(int i = 0; i < input.Length; i++) {
                char c = input[i];

                if(c == '"' && !IsEscaped(input, i)) {
                    sb.Append(c);
                    inQuotes = !inQuotes;
                    continue;
                }

                // Track nested brackets/braces
                if(!inQuotes) {
                    if(c == '[' || c == '{') {
                        stack.Push(c);
                        sb.Append(c);
                        continue;
                    }
                    if(c == ']' || c == '}') {
                        if(stack.Count == 0)
                            throw new CommandParseException($"Unmatched closing {c}");

                        char open = stack.Pop();
                        if((open == '[' && c != ']') || (open == '{' && c != '}'))
                            throw new CommandParseException($"Mismatched {open} and {c}");

                        sb.Append(c);
                        continue;
                    }

                    // Token separator: whitespace only if not in quotes or nested structure
                    if(char.IsWhiteSpace(c) && stack.Count == 0) {
                        if(sb.Length > 0) {
                            tokens.Add(NormalizeToken(sb.ToString()));
                            sb.Clear();
                        }

                        // Skip consecutive whitespace/newlines
                        while(i + 1 < input.Length && char.IsWhiteSpace(input[i + 1]))
                            i++;

                        continue;
                    }
                }

                sb.Append(c);
            }

            if(sb.Length > 0)
                tokens.Add(NormalizeToken(sb.ToString()));

            if(inQuotes)
                throw new CommandParseException("Unterminated quote");
            if(stack.Count > 0)
                throw new CommandParseException("Unterminated bracket or brace");

            return tokens.ToArray();
        }

        /// <summary>
        /// Normalizes a token by removing extra whitespace and line breaks
        /// inside arrays/objects, but preserves quoted strings and outer structure
        /// </summary>
        internal static bool IsEscaped(string text, int index) {
            int backslashes = 0;
            for(int i = index - 1; i >= 0 && text[i] == '\\'; i--)
                backslashes++;
            return (backslashes & 1) != 0;
        }

        internal static string UnescapeQuotedString(string text) {
            var result = new StringBuilder(text.Length);
            for(int i = 0; i < text.Length; i++) {
                if(text[i] == '\\' && i + 1 < text.Length &&
                    (text[i + 1] == '"' || text[i + 1] == '\\')) {
                    result.Append(text[++i]);
                } else {
                    result.Append(text[i]);
                }
            }
            return result.ToString();
        }
        private static string NormalizeToken(string token) {
            int start = token.IndexOfAny(new[] { '{', '[' });
            if(start == -1)
                return token;

            string prefix = string.Empty;
            string body = token; // Preserve quote state even when brackets occur inside text.

            var sb = new StringBuilder();
            bool inQuotes = false;
            bool pendingCommaSpace = false;

            for(int i = 0; i < body.Length; i++) {
                char c = body[i];
                if(c == '"' && !IsEscaped(body, i)) {
                    if(pendingCommaSpace) {
                        sb.Append(' ');
                        pendingCommaSpace = false;
                    }

                    sb.Append(c);
                    inQuotes = !inQuotes;
                    continue;
                }

                if(inQuotes) {
                    sb.Append(c);
                    continue;
                }

                switch(c) {
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        // discard all whitespace outside quotes
                        break;

                    case ',':
                        sb.Append(',');
                        pendingCommaSpace = true;
                        break;

                    case ':':
                        sb.Append(':');
                        break;

                    case ']':
                    case '}':
                        // never allow space before closing bracket/brace
                        pendingCommaSpace = false;
                        sb.Append(c);
                        break;

                    default:
                        if(pendingCommaSpace) {
                            sb.Append(' ');
                            pendingCommaSpace = false;
                        }
                        sb.Append(c);
                        break;
                }
            }

            return prefix + sb.ToString();
        }
    }

    public class CommandArgumentException : Exception {
        public CommandArgumentException() : base() { }
        public CommandArgumentException(string message) : base(message) { }
    }


    public class CommandParseException : Exception {
        public CommandParseException() : base() { }
        public CommandParseException(string message) : base(message) { }
    }
}