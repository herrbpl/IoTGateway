using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DeviceReader.Diagnostics;
using DeviceReader.Devices;
using DeviceReader.Protocols;
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

        private static IContainer Container { get; set; }

        static  void Main(string[] args)
        {

            var builder = new ContainerBuilder();

            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;

            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

            // logger
            builder.RegisterInstance(lg).As<ILoggingConfig>().SingleInstance().ExternallyOwned();
            builder.RegisterInstance(logger).As<ILogger>().SingleInstance().ExternallyOwned();


            // Agent Runner Factory
            //builder.RegisterType<DeviceAgentRunnerFactory>().As<IDeviceAgentRunnerFactory>().SingleInstance();                               

            // protocol reader - as it is using httpclient, should every request being made by new client or is there a pool? 

            
            // AUtoregister all implemented interfaces? Something better later than using simple text.
            builder.RegisterType<DummyProtocolReader>().As<IProtocolReader>().WithMetadata<ProtocolReaderMetadata>(
                m => m.For(am => am.ProtocolName, "dummy")
                );
            builder.RegisterType<HttpProtocolReader>().As<IProtocolReader>().WithMetadata<ProtocolReaderMetadata>(
                m => m.For(am => am.ProtocolName, "http")
                );

            // protocol reader factory. We should resolve correct protocol based on string. Trouble is that factory should know nothing about AutoFac and thus Resolve function. 

            GetProtocolReaderDelegate gprd = (requestedProtocolReader) => {
                IComponentContext context = Container.Resolve<IComponentContext>();
                logger.Debug("GetProtocolReaderFactory.GetProtocolReaderDelegate called!", () => { });
                IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>> _protocols = context.Resolve<IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>>>();
                IProtocolReader protocolReader = _protocols.FirstOrDefault(pr => pr.Metadata.ProtocolName.Equals(requestedProtocolReader.Device.Config.ProtocolReader))?.Value;
                if (protocolReader == null) throw new ArgumentException(string.Format("ProtocolReader {0} is not supported.", requestedProtocolReader), "requestedProtocolReader");
                return protocolReader;
            };

            builder.RegisterType<ProtocolReaderFactory>().As<IProtocolReaderFactory>().SingleInstance().WithParameter(
                new TypedParameter(typeof(GetProtocolReaderDelegate), gprd)
                );


            // now, runner. How can I get runner inside the agent? // (ILogger logger, IDeviceAgent deviceagent, IProtocolReader protocolReader)
            builder.RegisterType<DefaultDeviceRunner>().As<IDeviceAgentRunner>();
            


            // Get Agent runner from factory.
            //builder.
            //IProtocolReaderFactory pf = new ProtocolReaderFactory(logger);
            

            //runnerFactory = new DeviceAgentRunnerFactory(logger, pf);
            //builder.Populate(services);
            Container = builder.Build();

            /*
            IDevice dev1 = new Device("test1", "test1");
            IDeviceAgent ag1 = new DeviceAgent(logger, dev1, (dev)  => {
                return Container.Resolve<IDeviceAgentRunner>(new TypedParameter(typeof(IDeviceAgent), dev));
            });
            */
           

            //RunMultiple();
            RunOne();
            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        
        }
        static void RunOne()
        {
            /*
            IDevice dev1 = new Device("001", "Teeilmajaam");

            IDeviceAgent agent1 = new DeviceAgent(logger, dev1, runnerFactory);
            */


            IDevice dev1 = new Device("test1", "test1");

            // create new agent. Runner configuration depends on agent config.
            IDeviceAgent agent1 = new DeviceAgent(logger, dev1, (dev) => {
                IDeviceAgentRunner r = Container.Resolve<IDeviceAgentRunner>(new TypedParameter(typeof(IDeviceAgent), dev));
                logger.Debug(string.Format("Runner Hash {0}", r.GetHashCode()), () => { });
                return r;
            });

            char ch;
            Console.WriteLine("Press C to quit, S to stop agent, X to start agent");
            while (true)
            {
                Console.WriteLine("Agent status: {0}", agent1.IsRunning);
                ch = Console.ReadKey().KeyChar;
                if (ch == 'c' || ch == 'C')
                {
                    if (agent1.IsRunning) { agent1.Stop(); }
                    logger.Info("Exiting", () => { });
                    break;
                }
                else if (ch == 's' || ch == 'S')
                {
                    Console.WriteLine("Stop!");
                    if (agent1.IsRunning) { agent1.Stop(); }
                }
                else if (ch == 'x' || ch == 'X')
                {
                    Console.WriteLine("Start!");
                    if (!agent1.IsRunning) { agent1.Start(); }
                }
                else
                {
                    Console.WriteLine("Press C to quit, S to stop agent, X to start agent");
                }

            }
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
                        IDevice device = new Device(sch, "Device " + sch);
                        var str = JsonConvert.SerializeObject(device, Formatting.Indented);
                        logger.Info(str, () => { });
                        //IDeviceAgent agent = new DeviceAgent(logger, device, runnerFactory);
                        IDeviceAgent agent = new DeviceAgent(Container.Resolve<ILogger>(), device, (dev) => {
                            IDeviceAgentRunner r = Container.Resolve<IDeviceAgentRunner>(new TypedParameter(typeof(IDeviceAgent), dev));
                            logger.Debug(string.Format("Runner Hash {0}", r.GetHashCode()), () => { });
                            return r;
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
