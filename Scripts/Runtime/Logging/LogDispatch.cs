using UnityEngine;
using VContainer.Unity;

namespace Louis.CustomPackages.CommandLineInterface.Logging {
    public class LogDispatcherInitializationBridge : IInitializable {
        readonly ILogDispatcher _logDispatcher;
        public LogDispatcherInitializationBridge(ILogDispatcher logDispatcher) => _logDispatcher = logDispatcher;
        public void Initialize() => LogDispatch.Init(_logDispatcher);
    }


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
