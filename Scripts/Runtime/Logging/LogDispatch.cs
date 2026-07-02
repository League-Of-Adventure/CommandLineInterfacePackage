using UnityEngine;

namespace Louis.CustomPackages.CommandLineInterface.Logging {
    public static class LogDispatch {
        public static ILogDispatcher _logger;
        public static void Init(ILogDispatcher logger) {
            _logger = logger;
        }
        public static void Log(Object sender, string message, LogLevel level = LogLevel.Info) => _logger?.Log(sender, message, level); 
        public static void Log(object sender, string message, LogLevel level = LogLevel.Info) => _logger?.Log(sender, message, level);
        public static void Log(string message, LogLevel level = LogLevel.Info) => _logger?.Log(message, level);
    }
}
