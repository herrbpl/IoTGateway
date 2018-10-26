using Autofac;
using Autofac.Extensions.DependencyInjection;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using DeviceReader.Extensions;
using DeviceReader.WebService.Exeptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DeviceReader.WebService
{
    public static class DependencyResolution
    {
        
        /// <summary>
        /// Autofac configuration. Find more information here:
        /// @see http://docs.autofac.org/en/latest/integration/aspnetcore.html
        /// </summary>
        public static IContainer Setup(IServiceCollection services, IConfiguration configurationRoot)
        {
            var builder = new ContainerBuilder();

            builder.Populate(services);
            
            AutowireAssemblies(builder);
            SetupCustomRules(builder, configurationRoot);

            var container = builder.Build();            

            return container;
        }

        /// <summary>
        /// Autowire interfaces to classes from all the assemblies, to avoid
        /// manual configuration. Note that autowiring works only for interfaces
        /// with just one implementation.
        /// @see http://autofac.readthedocs.io/en/latest/register/scanning.html
        /// </summary>
        private static void AutowireAssemblies(ContainerBuilder builder)
        {
            var assembly = Assembly.GetEntryAssembly();
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

            /*
            // Auto-wire Services.DLL
            assembly = typeof(IServicesConfig).GetTypeInfo().Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

            // Auto-wire SimulationAgent.DLL
            assembly = typeof(ISimulation).GetTypeInfo().Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            */
        }

        /// <summary>
        /// Setup custom rules overriding autowired ones, for example in cases
        /// where an interface has multiple implementations, and cases where
        /// a singleton is preferred to new instances.
        /// </summary>
        private static void SetupCustomRules(ContainerBuilder builder, IConfiguration configurationRoot)
        {

            LoggingConfig lg = new LoggingConfig();
            try
            {
                lg.LogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), configurationRoot.GetValue<string>("LogLevel", "Debug"));
            }
            catch (Exception e)
            {
                lg.LogLevel = LogLevel.Debug;
            }

            var logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

            // logger
            builder.RegisterInstance(lg).As<ILoggingConfig>().SingleInstance();
            builder.RegisterInstance(logger).As<ILogger>();

            // register Devicemanager config instance
            // need some validation for configuration. 
            // nb, services configuration gives opportunity to configure options according to config and deliver config using DI.
            DeviceManagerConfig dmConfig = new DeviceManagerConfig();
            configurationRoot.GetSection("DeviceManager").Bind(dmConfig);
            builder.RegisterInstance(dmConfig).As<DeviceManagerConfig>().SingleInstance();

            // DeviceConfigProvider 
            // decide on type which device config provider to load.
            // This looks ugly.
            // TODO: get possible candidates by reflection from assembly, all classes providing implementation of interface?
            // Later, now time  now..
            var deviceConfigProviderType = configurationRoot.GetSection("DeviceConfigProvider").GetValue<string>("Type", "AzureTableProvider");
            if (deviceConfigProviderType == "AzureTableProvider")
            {
                DeviceConfigurationAzureTableProviderOptions options = new DeviceConfigurationAzureTableProviderOptions();
                configurationRoot.GetSection("DeviceConfigProvider").GetSection("Config").Bind(options);
                builder.RegisterInstance(options).As<DeviceConfigurationAzureTableProviderOptions>().SingleInstance();

                builder.RegisterType<DeviceConfigurationAzureTableProvider>().As<IDeviceConfigurationProvider<TwinCollection>>();
            } else
            {
                throw new InvalidDeviceConfigurationProviderType();
            }


            // now register different stuff

            DeviceReaderExtensions.RegisterProtocolReaders(builder);
            DeviceReaderExtensions.RegisterFormatParsers(builder);
            DeviceReaderExtensions.RegisterRouterFactory(builder);

            
            DeviceReaderExtensions.RegisterDeviceManager(builder);
            DeviceReaderExtensions.RegisterAgentFactory(builder);


            
        }
    }
}
