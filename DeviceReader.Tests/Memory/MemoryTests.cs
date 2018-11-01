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

namespace DeviceReader.Tests.Memory
{
    public static class MemoryTests
    {
        private static IContainer Container; // { get; set; }        

        static public void Main()
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

            // Register DeviceReader Services.
            SetupCustomRules(builder, Configuration);

            Container = builder.Build();
            //IAgentFactory agentFactory = Container.Resolve<IAgentFactory>();
            
            Console.WriteLine("Snap..");
            Console.ReadLine();

            RunMany(10, 20);
            //RunMultiple();
            /*
            List<IAgent> agents = new List<IAgent>();
            for (var i = 0; i < 20; i++)
            {
                agents.Add(CreateAgent($"TEST{i}"));
            }
            Console.WriteLine("Snap..");
            Console.ReadLine();
            foreach (var item in agents)
            {
                item.Dispose();
            }
            agents.Clear();
            */
            Console.WriteLine("Snap..");
            Console.ReadLine();


            //Console.ReadLine();

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
            builder.RegisterInstance(dmConfig).As<DeviceManagerConfig>().SingleInstance();

            // now register different stuff

            DeviceReaderExtensions.RegisterProtocolReaders(builder);
            DeviceReaderExtensions.RegisterFormatParsers(builder);
            DeviceReaderExtensions.RegisterRouterFactory(builder);
          
            DeviceReaderExtensions.RegisterDeviceManager(builder);
            DeviceReaderExtensions.RegisterAgentFactory(builder);


        }


        static string AgentConfigTemplate = $@"
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
        public const string KEY_AGENT_EXECUTABLE_ROOT = "executables:reader";
        public const string KEY_AGENT_PROTOCOL_CONFIG = KEY_AGENT_EXECUTABLE_ROOT + ":protocol_config";



        private static IAgent CreateAgent(string name)
        {
            string configString2 = $@"{{
    'HostName': '127.0.0.1',
    'Port': 5000,
    'Timeout': 10,    
    'Debug': 'false'
}}";

            var configString = AgentConfigTemplate.Replace("#CONFIG#", configString2).Replace("#NAME#", name);

            return CreateAgentTemplate(configString);
        }

        private static IAgent CreateAgentTemplate(string jsontemplate)
        {
            IAgentFactory af = Container.Resolve<IAgentFactory>();
            return af.CreateAgent(jsontemplate);
        }


        

        static void RunMany(int howmany, int stopafterseconds)
        {
            var logger = Container.Resolve<ILogger>();
            IDictionary<string, IAgent> agents = new Dictionary<string, IAgent>();
            CancellationTokenSource stopall = new CancellationTokenSource();

            // create agents
            for (int i = 0; i < howmany; i++)
            {
                IAgent agent = CreateAgent(i.ToString());
                agents.Add(i.ToString(), agent);
                agent.StartAsync(stopall.Token);
            }

            char ch;
            Console.WriteLine($"Press C to quit, stopping automatically after {stopafterseconds} seconds");
            if (stopafterseconds >= 0) stopall.CancelAfter(stopafterseconds * 1000);
            while (true && !stopall.Token.IsCancellationRequested)
            {
                Console.WriteLine("Total Agents: {0}", agents.Count);
                Console.WriteLine("Press C to quit");
                ch = Console.ReadKey().KeyChar;
                if (ch == 'C' || ch == 'c')
                {
                    // stop all.
                    logger.Info("Request stop for all agents", () => { });
                    List<Task> tasks = new List<Task>();

                    foreach (var agent in agents)
                    {
                        if (agent.Value.IsRunning) tasks.Add(agent.Value.ExecutingTask);
                        //agent.Value.StopAsync(stopall.Token);
                    }
                    if (!stopall.Token.IsCancellationRequested) stopall.Cancel();
                    logger.Info("Waiting for agents to stop", () => { });

                    Task.WaitAll(tasks.ToArray());



                    foreach (var agent in agents)
                    {
                        Console.WriteLine("{0}, {1}, {2}, {3}ms", agent.Value.Name, agent.Value.StopStartTime, agent.Value.StopStopTime, agent.Value.StoppingTime);
                        agent.Value.Dispose();
                    }
                    agents.Clear();

                    break;
                }

            }
        }

        static void RunMultiple()
        {
            var logger = Container.Resolve<ILogger>();
            IDictionary<string, IAgent> agents = new Dictionary<string, IAgent>();
            CancellationTokenSource stopall = new CancellationTokenSource();
            char ch;
            Console.WriteLine("Press C to quit, S to stop agent, X to start agent");
            while (true)
            {
                Console.WriteLine("Total Agents: {0}", agents.Count);

                foreach (var agent in agents)
                {
                    Console.WriteLine("Agent {0}: running: {1}", agent.Value.Name, agent.Value.IsRunning);
                }

                ch = Console.ReadKey().KeyChar;
                byte bb = Convert.ToByte(ch);
                string sch = Convert.ToString(ch);
                if (bb >= Convert.ToByte('0') && bb <= Convert.ToByte('9'))
                {
                    if (agents.ContainsKey(sch))
                    {
                        var agent = agents[sch];
                        if (agent.IsRunning)
                        {
                            agent.StopAsync(stopall.Token).Wait();
                            agent.Dispose();
                            agent = null;
                            agents.Remove(sch);
                        }
                        else
                        {
                            agent.StartAsync(stopall.Token).Wait();
                        }
                        agent = null;
                    }
                    else
                    {
                        //Device device = new Device(sch);

                        ///if (sch == "0" || sch == "1" | sch == "2") device.Config.ProtocolReader = "dummy";


                        // new agent creating. This should be done by device manager service in future.                        
                        IAgent agent = CreateAgent(sch);
                        agents.Add(sch, agent);
                        agent.StartAsync(stopall.Token);
                        agent = null;
                    }

                }
                else if (ch == 'C' || ch == 'c')
                {
                    // stop all.
                    logger.Info("Request stop for all agents", () => { });
                    List<Task> tasks = new List<Task>();

                    foreach (var agent in agents)
                    {
                        tasks.Add(agent.Value.ExecutingTask);
                        agent.Value.StopAsync(stopall.Token);                        
                    }
                    agents.Clear();
                    logger.Info("Waiting for agents to stop", () => { });
                    Task.WaitAll(tasks.ToArray());
                    foreach (var agent in agents)
                    {
                        agent.Value.Dispose();
                    }
                    agents.Clear();
                    break;
                }

                {
                    Console.WriteLine("Press C to quit, 0 .. 9 to toggle an agent");
                }


            }


        }

       
    }
}
