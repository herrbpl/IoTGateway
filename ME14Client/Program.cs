using System;

namespace ME14Client
{

    using CommandLine;
   
    using System.Threading.Tasks;
   
    using DeviceReader.Diagnostics;
    using Autofac;
    using System.Diagnostics;
    using DeviceReader.Protocols;
    using DeviceReader.Extensions;
   
    using Microsoft.Extensions.Configuration;
    using System.Threading;
    using DeviceReader.Parsers;
    using DeviceReader.Models;
    using Newtonsoft.Json;

    enum RetOptions
    {
        MES14 = 1,
        HIST
    }

    class Options
    {

        


        [Option(Required = false, HelpText = "Server address to use, 127.0.0.1 default", Default = "127.0.0.1")]
        public string serveraddress { get; set; }

        [Option(Required = false, HelpText = "Server port to use, 5000 default", Default = 5000)]
        public int serverport { get; set; }
        /*
        [Option(Required = false, HelpText = "Use SSL for authentication")]
        public bool usessl { get; set; }

        [Option(Required = false, HelpText = "PFX File Path")]
        public string pfxfile { get; set; }

        [Option(Required = false, HelpText = "PFX File password")]
        public string pfxpassword { get; set; }
        */
        [Option(Required = false, HelpText = "What to retrieve { MES14 | HIST } ", Default = RetOptions.MES14)]
        public RetOptions retrieve { get; set; }

        [Option(Required = false, HelpText = "Debug protocol", Default = false)]
        public bool debug { get; set; }

        [Option(Required = false, HelpText = "Timeout (in seconds) for whole operation", Default = 5)]
        public int timeout { get; set; }

    }
    /// <summary>
    /// Example program
    /// TODO: Check how to use logging frameworks and integrate so logging framework can be selected.
    /// </summary>
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


       
        static async Task RunClientAsync(string[] args)
        {
            bool exitfromopts = false;
           
            Options options = new Options();
            string configString = "";


            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(opts =>
            {              

                string configString2 = $@"{{
    'HostName': '{opts.serveraddress}',
    'Port': {opts.serverport},
    'Timeout': {opts.timeout},    
    'Debug': '{opts.debug}'
}}";

                configString = AgentConfigTemplate.Replace("#CONFIG#", configString2);


            }).WithNotParsed<Options>((errors) => {
                Console.WriteLine("Invalid program arguments:");
                foreach (var err in errors)
                {
                    Console.WriteLine(err.ToString());
                }
                exitfromopts = true;
            });

            if (exitfromopts) return;

            Console.WriteLine(configString);



            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var confbuilder = new ConfigurationBuilder()
                .AddJsonString(configString);


            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = DeviceReader.Diagnostics.LogLevel.Debug;
            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

            var builder = new ContainerBuilder();

            // logger
            builder.RegisterInstance(lg).As<ILoggingConfig>().SingleInstance().ExternallyOwned();

            //builder.RegisterInstance(logger).As<ILogger>().SingleInstance().ExternallyOwned();

            builder.RegisterType<Logger>().As<ILogger>().WithParameter(
                new NamedParameter("processId", Process.GetCurrentProcess().Id.ToString())
                ).WithParameter(
                new NamedParameter("config", lg)
                );
            //.SingleInstance().ExternallyOwned();

            // register protocol readers
            builder.RegisterProtocolReaders();

            builder.RegisterFormatParsers();

            // create container
            Container = builder.Build();

            // Protocol reader factory
            IProtocolReaderFactory prf = Container.Resolve<IProtocolReaderFactory>();

            // DeviceConfig 
            var configurationRoot = confbuilder.Build();

            IProtocolReader pr = prf.GetProtocolReader("me14", KEY_AGENT_PROTOCOL_CONFIG, configurationRoot);

            var formatParserFactory = Container.Resolve<IFormatParserFactory<string, Observation>>();
            var formatParser = formatParserFactory.GetFormatParser("me14");

            try
            {
                var response = pr.ReadAsync(CancellationToken.None).Result;
                Console.WriteLine(response);

                var observations = formatParser.ParseAsync(response, CancellationToken.None).Result;
                var jsonstring = JsonConvert.SerializeObject(observations, Formatting.Indented);
                logger.Debug($"{jsonstring}", () => { });

            } catch (AggregateException e) { }
            catch (Exception e)
            {
                logger.Error(e.ToString(), () => { });
            }

           
            

            Console.ReadLine();

        }

        static void Main(string[] args) => RunClientAsync(args).Wait();
    }
}
