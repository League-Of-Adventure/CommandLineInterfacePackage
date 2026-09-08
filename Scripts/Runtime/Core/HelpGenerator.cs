using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Louis.CustomPackages.CommandLineInterface.Core {
    internal static class HelpGenerator {
        public static string GenerateHelpString(IReadOnlyDictionary<string, CommandSchema> commands) {
            var sb = new StringBuilder();
            sb.AppendLine($"Available Commands:");

            foreach(var command in commands.Keys.OrderBy(c => c)) {
                sb.AppendLine($"<indent=5%>- {command}</indent>");
                if(!string.IsNullOrWhiteSpace((commands[command].Description)))
                    sb.AppendLine($"<indent=10%>{commands[command].Description}</indent>");
            }
            return sb.ToString();
        }

        public static string GenerateUsage(string commandName, CommandSchema schema) {
            var sb = new StringBuilder();
            sb.AppendLine($"Function Name: {commandName}");
            sb.AppendLine($"<indent=5%>Purpose : {schema.Description}</indent>");
            sb.AppendLine($"<indent=5%>Usage   : {commandName} {(schema.Args.Count > 0 ? "[args]" : "")} {(schema.Flags.Count > 0 || schema.FlagChoices.Count > 0 ? "[flags]" : "")}</indent>");

            // Arguments
            if(schema.Args.Count > 0) {
                sb.AppendLine("\n<indent=5%>Arguments:</indent>");
                foreach(var arg in schema.Args) {
                    string req = arg.Required ? "(required)" : $"(optional, default={arg.DefaultValue})";
                    sb.AppendLine($"<indent=10%>{arg.Name} [{arg.Type.Name}] {req} {arg.Description}</indent>");
                    if(arg.Type.IsEnum) sb.AppendLine($"<indent=15%>values: {string.Join("/", Enum.GetNames(arg.Type))}</indent>");
                }
            }

            // Boolean flags
            if(schema.Flags.Count > 0) {
                sb.AppendLine("\n<indent=5%>Flags:</indent>");
                foreach(var flag in schema.Flags) {
                    sb.AppendLine($"<indent=10%>--{flag.Name} {(string.IsNullOrWhiteSpace(flag.Description) ? "" : $": {flag.Description})")}</indent>");
                }
            }

            // Flag choices
            if(schema.FlagChoices.Count > 0) {
                sb.AppendLine("\n<indent=5%>Flag choices:</indent>");
                foreach(var choice in schema.FlagChoices) {
                    string opts = string.Join("\n", choice.Options.Select(o => $"<indent=10%>--{o.Flag} ({o.Value})</indent>"));
                    sb.AppendLine($"<indent=10%>{choice.Name} [{choice.Type.Name}] {(string.IsNullOrWhiteSpace(choice.DefaultValue.ToString()) ? "" : $"default={choice.DefaultValue}")}:</indent>");
                    sb.AppendLine($"<indent=15%>{opts}</indent>");
                    sb.AppendLine($"<indent=10%>{choice.Description}</indent>");
                }
            }

            // Shorthands
            if(schema.Shorthands.Count > 0) {
                sb.AppendLine("\n<indent=5%>Shorthands:</indent>");
                foreach(var kv in schema.Shorthands)
                    sb.AppendLine($"<indent=10%>-{kv.Key} → --{kv.Value}</indent>");
            }

            return sb.ToString();
        }
    }
}