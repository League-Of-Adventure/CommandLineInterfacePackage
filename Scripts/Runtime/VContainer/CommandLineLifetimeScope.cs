using com.Louis.CommandLineInterface.Commands;
using Louis.CustomPackages.CommandLineInterface.CommandConfiguration;
using Louis.CustomPackages.CommandLineInterface.Core;
using Louis.CustomPackages.CommandLineInterface.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Louis.CustomPackages.CommandLineInterface.Logging;

#if USE_VCONTAINER
namespace com.Louis.CommandLineInterface.VContainer {
    public class CommandLineLifetimeScope : LifetimeScope {

        [Header("Command Line References")]
        [SerializeField] CommandHandler _commandHandler;
        [SerializeField] CommandRegistry _commandRegistry;
        [SerializeField] CommandCompiler _commandCompiler;
        [SerializeField] LogDispatcher _commandLogger;
        [SerializeField] Console _console;

        [SerializeField] InputProvider _inputProvider;

        protected override void Configure(IContainerBuilder builder) {
            // Core Components
            builder.RegisterComponent(_commandHandler).AsImplementedInterfaces();
            builder.RegisterComponent(_commandRegistry).AsImplementedInterfaces();
            builder.RegisterComponent(_commandCompiler).AsImplementedInterfaces();
            builder.RegisterComponent(_commandLogger).AsImplementedInterfaces();
            builder.RegisterBuildCallback(resolver => {
                LogDispatch.Init(resolver.Resolve<ILogDispatcher>());
            });

            // Input Provider for the Command Input Mapper
            if(_inputProvider != null)
                builder.RegisterComponent(_inputProvider).AsImplementedInterfaces();

            if(_console != null)
                builder.RegisterComponent(_console).AsImplementedInterfaces();

            // Command Runners
            builder.RegisterEntryPoint<RunRepeatedCommand>().AsSelf();
        }
    }
}
#endif
