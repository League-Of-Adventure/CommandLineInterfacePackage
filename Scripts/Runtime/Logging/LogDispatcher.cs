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
    public class LogDispatcher : MonoBehaviour, IOutputRegistry, ILogDispatcher, IOutput {
        [Header("Settings")]
        [SerializeField] bool _outputToConsole = true;
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
            if(!_outputToConsole) return;
            Debug.Log(log.Raw);
        }
    }

    [Serializable]
    public struct Log {
        public LogLevel level;
        public string sender;
        public string message;

        public Log(LogLevel level, string senderName, string message) {
            this.level = level;
            this.message = message;
            sender = senderName;
        }

        public Log(LogLevel level, string message) {
            this.level = level;
            this.message = message;
            sender = "";
        }

        public readonly string Raw {
            get {
                if(string.IsNullOrWhiteSpace(sender)) {
                    return $"[{level}]: {message}";
                } else {
                    return $"[{level}] - [{sender}]: {message}";
                }
            }
        } 

        public readonly string Formatted {
            get {
                string color = level switch {
                    LogLevel.Info => "white",
                    LogLevel.Success => "green",
                    LogLevel.Warning => "yellow",
                    LogLevel.Error => "red",
                    _ => "white"
                };
                string prefix = $"[{level}]".PadRight(10, ' ');
                string sender = string.IsNullOrWhiteSpace(this.sender) ? "" : $"[{this.sender}]: ".PadRight(25, ' ');
                return $"<b><color={color}>{prefix}</color> - {sender}</b>{message}";
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