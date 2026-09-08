using com.Louis.CommandLineInterface.Commands;
using Louis.CustomPackages.CommandLineInterface.Core;
using Louis.CustomPackages.CommandLineInterface.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Louis.CustomPackages.CommandLineInterface.Logging;

namespace com.Louis.CommandLineInterface.VContainer {
    public class CommandLineLifetimeScope : LifetimeScope {

        [Header("Command Line References")]
        [SerializeField] LogDispatcher _commandLogger;
        [SerializeField] Console _console;

        protected override void Configure(IContainerBuilder builder) {
            // Core Components
            builder.Register<ICommandRegistry, CommandRegistry>(Lifetime.Singleton);
            builder.Register<ICommandCompiler, CommandCompiler>(Lifetime.Singleton);
            builder.RegisterEntryPoint<CommandHandler>().As<ICommandHandler>();

            // Log Dispatcher
            builder.Register<LogDispatcher>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterEntryPoint<LogDispatcherInitializationBridge>();

            if(_console != null)
                builder.RegisterComponent(_console).AsImplementedInterfaces();

            // Command Runners
            builder.RegisterEntryPoint<RunRepeatedCommand>();
        }
    }
}
