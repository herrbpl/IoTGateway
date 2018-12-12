using Autofac;
using DeviceReader.Agents;
using DeviceReader.Configuration;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using DeviceReader.Extensions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
namespace DeviceReader.Tests.Devices
{
    public class DevicesTests
    {
        LoggingConfig lg;
        ILogger logger;

        private static global::System.Resources.ResourceManager resourceMan;
        private readonly ITestOutputHelper output;
        private IContainer Container;


        public DevicesTests(ITestOutputHelper output)
        {


            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;

            Logger.DefaultLoggerFactory.AddProvider(new XUnitLoggerProvider(output));
            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);



            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var confbuilder = new ConfigurationBuilder()
                //.SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
            //.AddEnvironmentVariables();

            var Configuration = confbuilder.Build();

            var builder = new ContainerBuilder();
            builder.RegisterInstance(lg).As<ILoggingConfig>().SingleInstance();
            builder.RegisterInstance(logger).As<ILogger>();
            builder.RegisterDeviceReaderServices(Configuration);


            Container = builder.Build();

        }

       
    }
}
