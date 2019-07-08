using Autofac;
using DeviceReader.Agents;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using DeviceReader.Extensions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Runtime.CompilerServices;

namespace DeviceReader.Tests.Devices
{
    public class TestClass {
        public int ValueScalar { get; set; } = 10;
        public List<String> ValueObject { get; set; }
    }

    public class DeviceManagerTests
    {
        private readonly ITestOutputHelper output;
        LoggingConfig lg;
        ILogger logger;

        private IContainer Container; // { get; set; }        

        public DeviceManagerTests(ITestOutputHelper output)
        {

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var confbuilder = new ConfigurationBuilder()
                //.SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
            //.AddEnvironmentVariables();

            var Configuration = confbuilder.Build();

            var builder = new ContainerBuilder();


            lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;
            this.output = output;
            Logger.DefaultLoggerFactory.AddProvider(new XUnitLoggerProvider(output));
            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

            SetupCustomRules(builder, Configuration);
            Container = builder.Build();

        }

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
            // need some validation for configuration
            DeviceManagerConfig dmConfig = new DeviceManagerConfig();
            configurationRoot.GetSection("DeviceManager").Bind(dmConfig);
            builder.RegisterInstance(dmConfig).As<DeviceManagerConfig>().AsSelf().SingleInstance();

            // now register different stuff
            builder.RegisterDeviceReaderServices(configurationRoot);

            DeviceReaderExtensions.RegisterProtocolReaders(builder);
            DeviceReaderExtensions.RegisterFormatParsers(builder);
            DeviceReaderExtensions.RegisterRouterFactory(builder);

            DeviceReaderExtensions.RegisterDeviceManager(builder);
            DeviceReaderExtensions.RegisterAgentFactory(builder);
            builder.RegisterType<DeviceManager>().As<DeviceManager>().SingleInstance();
        }

        [Fact]
        public void DeviceManager_CacheTests()
        {
            var dm = Container.Resolve<DeviceManager>();

            // primitive tests
            int i = 10;
            dm.SetCacheValue<int>("test1", i, new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3)
            }).Wait();

            int o;
            o = dm.GetCacheValue<int>("test1").Result;
            Assert.Equal(i, o);

            var n = dm.GetCacheValue<int>("notexisting").Result;
            Assert.Equal(0, n);

            Thread.Sleep(4000);
            o = dm.GetCacheValue<int>("test1").Result;
            Assert.Equal(0, o);

            // object
            var to = new TestClass();
            to.ValueObject = new List<string>() { "a", "b " };
            to.ValueScalar = 25;

            dm.SetCacheValue<TestClass>("test2", to, new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3)
            }).Wait();

            TestClass x, y;

            x = dm.GetCacheValue<TestClass>("test2").Result;
            Assert.Equal(to, x);

            Assert.Equal(25, x.ValueScalar);
            Assert.Equal(to.ValueObject, x.ValueObject);

            // test if changing original object changes value in cache
            to.ValueScalar = 2;
            to.ValueObject = new List<string>() { "c", "d" };

            y = dm.GetCacheValue<TestClass>("test2").Result;
            Assert.Equal(2, y.ValueScalar);


            // timing tests
            dm.SetCacheValue<TestClass>("Test3", to, new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            }).Wait();

            // overwrite 
            dm.SetCacheValue<TestClass>("Test3", to, new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3)
            }).Wait();

            // wait 4 seconds
            Thread.Sleep(4000);
            x = dm.GetCacheValue<TestClass>("Test3").Result;
            Assert.Null(x);

            var to2 = new TestClass()
            {
                ValueObject = null,
                ValueScalar = -1
        };

            dm.SetCacheValue<TestClass>("Test3", to, new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3)
            }).Wait();

            // overwrite 
            dm.SetCacheValue<TestClass>("Test3", to2, new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            }).Wait();

            Thread.Sleep(4000);
            x = dm.GetCacheValue<TestClass>("Test3").Result;
            Assert.Equal(to2, x);
        }
    }
}
