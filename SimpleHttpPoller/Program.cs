using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Authentication;
using DeviceReader.Diagnostics;
using DeviceReader.Extensions;
using Autofac;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using DeviceReader.Protocols;
using System.Collections.Generic;

namespace SimpleHttpPoller
{

    class Options
    {

        [Option(Required = true, HelpText = "URL to test")]
        public string url { get; set; }

        [Option(Required = false, HelpText = "username for basic authentication")]
        public string username { get; set; }

        [Option(Required = false, HelpText = "passdword for basic authentication")]
        public string password { get; set; }       
    }


    class Program
    {

        static ILogger logger;
        //static IDeviceAgentRunnerFactory runnerFactory;

        private static IContainer Container; // { get; set; }

        static string AgentConfigTemplate = $@"
{{
    'name': 'httpagenttest',
    'executables': {{ 
        'reader': {{            
            'format':'dummy',
            'protocol':'http',
            'protocol_config': #CONFIG#,
            'frequency': 1           
        }},
        'writer': {{            
            'frequency': 10000
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
        public const string KEY_AGENT_EXECUTABLE_ROOT = "executables:reader";
        public const string KEY_AGENT_PROTOCOL_CONFIG = KEY_AGENT_EXECUTABLE_ROOT + ":protocol_config";

        static void Main(string[] args)
        {

            Options options = new Options();
            string configString = "";
                  

            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(opts =>
            {

                options.url = opts.url;
                options.username = opts.username;
                options.password = opts.password;

                string configString2 = $@"{{
    'Url': '{opts.url}',
    'Username': '{opts.username}',
    'Password': '{opts.password}',
    'NoSSLValidation': 'true'
}}";

                configString = AgentConfigTemplate.Replace("#CONFIG#", configString2);

                
            }).WithNotParsed<Options>((errors) => {
                Console.WriteLine("Errors while running:");
                foreach(var err in errors)
                {
                    Console.WriteLine(err.ToString());
                }
            });


            Console.WriteLine(configString);


            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var confbuilder = new ConfigurationBuilder()
                .AddJsonString(configString);
           

            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;
            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

            var builder = new ContainerBuilder();

            // logger
            builder.RegisterInstance(lg).As<ILoggingConfig>().SingleInstance().ExternallyOwned();
            builder.RegisterInstance(logger).As<ILogger>().SingleInstance().ExternallyOwned();

            // register protocol readers
            builder.RegisterProtocolReaders();

            // create container
            Container = builder.Build();

            // Protocol reader factory
            IProtocolReaderFactory prf = Container.Resolve<IProtocolReaderFactory>();

            // DeviceConfig 
            var configurationRoot = confbuilder.Build();

            IProtocolReader pr =  prf.GetProtocolReader("http", KEY_AGENT_PROTOCOL_CONFIG, configurationRoot);

            Dictionary<string, string> queryParams = new Dictionary<string, string>();

            

            // Add timespan 1 hour, with stop = now and start = now - 1h
            // is Vaisala using UTC or local time zone?
            DateTime dt = DateTime.UtcNow;
            queryParams.Add("start", dt.AddHours(-1).ToString("s"));
            queryParams.Add("stop", dt.ToString("s"));            
            queryParams.Add("returnHierarchy", "true");

            var response = pr.ReadAsync(queryParams, CancellationToken.None).Result;

            Console.WriteLine(response);

            Console.ReadLine();
        }

        static async Task<string> PollStation(string url, string username, string password)
        {
            var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", username, password));

            return "";
        }

    }
}
