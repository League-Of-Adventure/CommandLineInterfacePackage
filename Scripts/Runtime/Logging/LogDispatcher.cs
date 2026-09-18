using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Louis.CustomPackages.CommandLineInterface.Logging {
    public interface ILogDispatcher {
        void Log(Object sender, string message, LogLevel level = LogLevel.Info);
        void Log(object sender, string message, LogLevel level = LogLevel.Info);
        void Log(string message, LogLevel level = LogLevel.Info);
    }

    public interface IOutput {
        void Write(Log log);
    }

    public interface IOutputRegistry {
        void AttachOutput(IOutput output);
        void DetachOutput(IOutput output);
    }

    [AddComponentMenu("Command Line Interface/Logger")]
    public class LogDispatcher : IOutputRegistry, ILogDispatcher, IOutput {
        readonly HashSet<IOutput> _outputSet = new();

        public void AttachOutput(IOutput output) {
            _outputSet.Add(output);
        }

        public void DetachOutput(IOutput output) {
            _outputSet.Remove(output);
        }

        public void Log(Object sender, string message, LogLevel level = LogLevel.Info) => Output(new Log(level, sender.name, message));
        public void Log(object sender, string message, LogLevel level = LogLevel.Info) => Output(new Log(level, sender.ToString(), message));
        public void Log(string message, LogLevel level = LogLevel.Info) => Output(new Log(level, message));

        void Output(Log log) {
            Write(log);
            if(_outputSet == null) return;
            foreach(var outputChannel in _outputSet) {
                outputChannel.Write(log);
            }
        }

        public void Write(Log log) {
            Debug.Log(log.Raw);
        }
    }

    [Serializable]
    public struct Log {
        public DateTime timestamp;
        public LogLevel level;
        public string sender;
        public string message;

        public Log(LogLevel level, string senderName, string message) {
            this.level = level;
            this.message = message;
            sender = senderName;
            timestamp = DateTime.UtcNow;
        }

        public Log(LogLevel level, string message) {
            this.level = level;
            this.message = message;
            sender = "";
            timestamp = DateTime.UtcNow;
        }

        public readonly string Raw {
            get {
                if(string.IsNullOrWhiteSpace(sender)) {
                    return $"{timestamp:HH:mm:ss} -  [{level}]: {message}";
                } else {
                    return $"{timestamp:HH:mm:ss} - [{level}] - [{sender}]: {message}";
                }
            }
        } 

        public readonly string Formatted {
            get {
                string color = level switch {
                    LogLevel.Debug => "pink",
                    LogLevel.Info => "white",
                    LogLevel.Analytics => "blue",
                    LogLevel.Success => "green",
                    LogLevel.Warning => "yellow",
                    LogLevel.Error => "red",
                    _ => "white"
                };
                string prefix = $"{timestamp:HH:mm:ss} - [{level}]".PadRight(20, ' ');
                string sender = string.IsNullOrWhiteSpace(this.sender) ? "" : $"[{this.sender}]: ";
                return $"<b><color={color}>{prefix} - {sender}</color></b>\n<indent=100px>{message}</indent>";
            }
        }
    }

    [Flags]
    public enum LogLevel {
        Debug       = 0,
        Info        = 1,
        Analytics   = 2,
        Success     = 4,
        Warning     = 8,
        Error       = 16
    }
}