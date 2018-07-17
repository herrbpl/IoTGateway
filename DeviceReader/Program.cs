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
using Autofac;
using Autofac.Core;
using System.Linq;

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
            lg.LogLevel = LogLevel.Debug;

            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

            // logger
            builder.RegisterInstance(lg).As<ILoggingConfig>().SingleInstance().ExternallyOwned();
            builder.RegisterInstance(logger).As<ILogger>().SingleInstance().ExternallyOwned();
            

            // register protocols
            builder.RegisterProtocolReaders();
            // register parsers
            builder.RegisterFormatParsers();


            // Register Runner
            //builder.RegisterType<DeviceAgentReader>().As<IDeviceAgentExecutable>();

            builder.RegisterType<DeviceAgentReader>().Keyed<IDeviceAgentExecutable>("reader");
            builder.RegisterType<DeviceAgentWriter>().Keyed<IDeviceAgentExecutable>("writer");

            Container = builder.Build();
           

            RunMultiple();
            //RunOne();
            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        
        }
        
        static void RunMultiple()
        {
            IDictionary<string, IDeviceAgent> agents = new Dictionary<string, IDeviceAgent>();
            CancellationTokenSource stopall = new CancellationTokenSource();
            char ch;
            Console.WriteLine("Press C to quit, S to stop agent, X to start agent");
            while (true)
            {
                Console.WriteLine("Total Agents: {0}", agents.Count);

                foreach (var agent in agents)
                {
                    Console.WriteLine("Agent {0}: running: {1}", agent.Value.Device.Id, agent.Value.IsRunning);
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
                        
                        //IDeviceAgent agent = new DeviceAgent(logger, device, runnerFactory);
                        IDeviceAgent agent = new DeviceAgent(Container.Resolve<ILogger>(), device, 
                            new List<Func<IDeviceAgent, IDeviceAgentExecutable>>() {
                            (dev) => {
                                IDeviceAgentExecutable r = Container.ResolveKeyed<IDeviceAgentExecutable>("reader", new TypedParameter(typeof(IDeviceAgent), dev));
                                logger.Debug(string.Format("Reader: ({0}:{1})", r.GetHashCode(), r.GetType().Name), () => {});
                                return r;
                                },
                            (dev) => {
                                IDeviceAgentExecutable w = Container.ResolveKeyed<IDeviceAgentExecutable>("writer", new TypedParameter(typeof(IDeviceAgent), dev));
                                logger.Debug(string.Format("Writer: ({0}:{1})", w.GetHashCode(), w.GetType().Name), () => {});
                                return w;
                                }
                            });
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
