using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DeviceReader.Diagnostics;
using DeviceReader.Devices;
using DeviceReader.Protocols;
using DeviceReader.Parsers;
using DeviceReader.Extensions;
using DeviceReader.Models;
using DeviceReader.Agents;
using Microsoft.Extensions.Configuration;
using Autofac;
using Autofac.Core;
using System.Linq;
using DeviceReader.Router;
using System.Configuration;

namespace DeviceReader
{
    // http://foreverframe.net/asp-net-core-resolving-proper-implementation-in-runtime-using-autofac/

    class Program
    {

        static ILogger logger;
        //static IDeviceAgentRunnerFactory runnerFactory;

        private static IContainer Container; // { get; set; }

        static  void Main(string[] args)
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

            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;

            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

            // logger
            builder.RegisterInstance(lg).As<ILoggingConfig>().SingleInstance().ExternallyOwned();
            builder.RegisterInstance(logger).As<ILogger>().SingleInstance().ExternallyOwned();

            // Register DeviceReader Services.
            builder.RegisterDeviceReaderServices(Configuration);                                           

            Container = builder.Build();


            //RunMultiple();
            //RoutesTest();

            // problem is with stopping multiple tasks. It takes afwul amount of time. 
            //RunMany(10, 30);
            RunDeviceManager();
            //TestAgentFactory("test123");
            //RunOne();
            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        
        }
        
        static void RunMultiple()
        {
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
                        } else
                        {
                            agent.StartAsync(stopall.Token).Wait();
                        }
                    } else
                    {
                        //Device device = new Device(sch);
                        
                        ///if (sch == "0" || sch == "1" | sch == "2") device.Config.ProtocolReader = "dummy";


                        // new agent creating. This should be done by device manager service in future.                        
                        IAgent agent = CreateAgent(sch);
                        agents.Add(sch, agent);
                        agent.StartAsync(stopall.Token);
                    }

                } else if (ch == 'C' ||ch == 'c')
                {
                    // stop all.
                    logger.Info("Request stop for all agents", () => { });
                    List<Task> tasks = new List<Task>();
                    
                    foreach (var agent in agents)
                    {
                        tasks.Add(agent.Value.ExecutingTask);
                        agent.Value.StopAsync(stopall.Token);
                    }
                    logger.Info("Waiting for agents to stop", () => { });
                    Task.WaitAll(tasks.ToArray());
                    break;
                }

                {
                    Console.WriteLine("Press C to quit, 0 .. 9 to toggle an agent");
                }

                
            }


        }

        private static IAgent CreateAgent(string name)
        {
            string jsontemplate = $@"
{{
    'name': '{name}',
    'executables': {{ 
        'reader': {{            
            'format':'dummy',
            'protocol':'dummy',
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
            
            return CreateAgentTemplate(jsontemplate);
        }

        private static IAgent CreateAgentTemplate(string jsontemplate)
        {
            IAgentFactory af = Container.Resolve<IAgentFactory>();
            return af.CreateAgent(jsontemplate);
        }

        static void RunMany(int howmany, int stopafterseconds)
        {
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
                        Console.WriteLine("{0}, {1}, {2}, {3}ms", agent.Value.Name, agent.Value.StopStartTime, agent.Value.StopStopTime,  agent.Value.StoppingTime);
                    }

                    break;
                }                                                

            }
        }

        static void TestAgentFactory(string name)
        {
            CreateAgent(name);
        }

        static void RunDeviceManager()
        {
                        
            IDeviceManager dm = Container.Resolve<IDeviceManager>();
            
            // Get list of devices            
            var dlist = dm.GetDeviceListAsync().Result;
            foreach (var item in dlist)
            {
                Console.WriteLine($"{item.Key} = {item.Value}");
            }
            var device = dm.GetDevice(dlist.First().Key).Result;
            //var device = dm.GetDevice("TestDevice").Result;
            device.SendData("doivjoijvier");
           

        }
    }
}
