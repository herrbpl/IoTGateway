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

namespace DeviceReader.Tests.Agents
{
    public class AgentFactoryTests
    {
        LoggingConfig lg;
        ILogger logger;

        private static global::System.Resources.ResourceManager resourceMan;
        private readonly ITestOutputHelper output;
        private IContainer Container;

        private string AgentConfigBaseTemplate = $@"
{{
    'name': '#NAME#',
    'executables': {{ 
        'reader': {{            
            'format':'me14',
            'protocol':'me14',
            'protocol_config': #CONFIG#,
            'frequency': 1005,
            'type': 'reader'
        }},
        'writer': {{            
            'frequency': 1000,
            'type': 'zeroagent'
        }}
    }},
    'routes': {{
        'reader': {{ 
            'writer': {{
                'target': 'writer',
                'evaluator': ''
            }}
        }}                         
    }}
}}
";
        private string AgentConfigMe14Template = $@"{{
    'HostName': '127.0.0.1',
    'Port': 5000,
    'Timeout': 10,    
    'Debug': 'false'
}}";

        public AgentFactoryTests(ITestOutputHelper output)
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

        [Fact]
        public void Simple_Agent_Create_Test()
        {
            var configString = AgentConfigBaseTemplate.Replace("#CONFIG#", AgentConfigMe14Template).Replace("#NAME#", "BasicME14Agent");

            IAgentFactory af = Container.Resolve<IAgentFactory>();
            var agent = af.CreateAgent(configString);

            const string KEY_AGENT_EXECUTABLE_ROOT = "executables:reader";
            const string KEY_AGENT_PROTOCOL_CONFIG = KEY_AGENT_EXECUTABLE_ROOT + ":protocol_config";

            Assert.NotNull(agent);
            Assert.Equal("BasicME14Agent", agent.Name);
            var hostname = agent.Configuration.GetValue<string>(KEY_AGENT_PROTOCOL_CONFIG + ":HostName", "127.0.0.1");
            Assert.Equal("127.0.0.1", hostname);
        }

    }


}
