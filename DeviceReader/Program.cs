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

            var builder = new ContainerBuilder();

            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Info;

            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

            // logger
            builder.RegisterInstance(lg).As<ILoggingConfig>().SingleInstance().ExternallyOwned();
            builder.RegisterInstance(logger).As<ILogger>().SingleInstance().ExternallyOwned();

            // Register DeviceReader Services.
            builder.RegisterDeviceReaderServices();                                  

            builder.RegisterType<DeviceAgentReader>().Keyed<IAgentExecutable>("reader");
            builder.RegisterType<DeviceAgentWriter>().Keyed<IAgentExecutable>("writer");


            // TODO: build filtering and routing.

            Container = builder.Build();


            RunMultiple();
            //RoutesTest();

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
                        Device device = new Device(sch, "Device " + sch);

                        if (sch == "0" || sch == "1" | sch == "2") device.Config.ProtocolReader = "dummy";

                        // create configuration from json string..
                        IConfigurationBuilder cb = new ConfigurationBuilder();

                        string jsontemplate = $@"
{{
    'name': '{sch}',
    'executables': {{ 
        'reader': {{
            'format':'dummy',
            'protocol':'{device.Config.ProtocolReader}'
        }}
    }}
}}
";
                        Console.WriteLine(jsontemplate.Replace("'", "\""));

                        cb.AddJsonString(jsontemplate);
                        var cbc = cb.Build();

                                                
                        IAgent agent = new Agent(Container.Resolve<ILogger>(), cbc, 
                            
                            Container.Resolve<IRouterFactory>(),

                            // reader and writer factories. Should we specify routes here or in config? Should routes be spcific to device, model or gateway?
                            new Dictionary<string, Func<IAgent, IAgentExecutable>>() {
                                {"reader",
                            (dev) => {
                                IAgentExecutable r = Container.ResolveKeyed<IAgentExecutable>("reader",
                                    new TypedParameter(typeof(IAgent), dev),
                                    new NamedParameter("name", "reader")
                                    );
                                logger.Debug(string.Format("Reader: ({0}:{1})", r.GetHashCode(), r.GetType().Name), () => {});
                                return r;
                                } },
                                { "writer",
                            (dev) => {
                                IAgentExecutable w = Container.ResolveKeyed<IAgentExecutable>("writer",
                                    new TypedParameter(typeof(IAgent), dev),
                                    new NamedParameter("name", "writer")
                                    );
                                logger.Debug(string.Format("Writer: ({0}:{1})", w.GetHashCode(), w.GetType().Name), () => {});
                                return w;
                                }
                            } }
                            );
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

       
    }
}
