using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Autofac;
using DeviceReader.Extensions;
using DeviceReader.Diagnostics;
using System.Diagnostics;
using DeviceReader.Tests;

namespace DeviceReader.Configuration.Tests
{
    public class ConfigurationProviderTests

    {
        private readonly ITestOutputHelper output;

        Dictionary<string, string> dict = new Dictionary<string, string>

        {
            {"DeviceConfigurationProvider:NamePlaceholder", "#NONAME#"},
            {"DeviceConfigurationProvider:DefaultConfig", "{ 'configtype': 'dummy', 'name': '#NONAME#' }"}
        };
        ILogger logger;
        public ConfigurationProviderTests(ITestOutputHelper output)
        {
            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;

            Logger.DefaultLoggerFactory.AddProvider(new XUnitLoggerProvider(output));
            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);
            

        }

        [Fact]
        public void DummyConfigurationProvider_Factory_Tests()
        {
            var builder = new ContainerBuilder();

            var configurationRoot = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();

            builder.RegisterInstance<IConfigurationRoot>(configurationRoot).Keyed<IConfigurationRoot>(DeviceReaderExtensions.KEY_GLOBAL_APP_CONFIG).SingleInstance();
            builder.RegisterDeviceConfigurationProviders();
            
            builder.RegisterInstance(logger).As<ILogger>();

            var container = builder.Build();

            var configproviderFactory = container.Resolve<IDeviceConfigurationProviderFactory>();

            Assert.NotNull(configproviderFactory);

            // create with global options
            var dummyprovider = configproviderFactory.Get("dummy");
            Assert.NotNull(dummyprovider);

            var conf = dummyprovider.GetConfigurationAsync<string, string>("deviceId").Result;
            logger.Info($"Config got: {conf}", () => { });
            Assert.NotEqual<string>("", conf);

        }
    }
}
