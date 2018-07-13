using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DeviceReader.Diagnostics;
using DeviceReader.Devices;
using DeviceReader.Protocols;


namespace DeviceReader
{
    class Program
    {

        static ILogger logger;
        static IDeviceAgentRunnerFactory runnerFactory;
        static  void Main(string[] args)
        {



            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;

            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

            IProtocolReaderFactory pf = new ProtocolReaderFactory(logger);

            runnerFactory = new DeviceAgentRunnerFactory(logger, pf);


            RunMultiple();

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        
        }
        static void RunOne()
        {
            IDevice dev1 = new Device("001", "Teeilmajaam");

            IDeviceAgent agent1 = new DeviceAgent(logger, dev1, runnerFactory);


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
                        IDeviceAgent agent = new DeviceAgent(logger, device, runnerFactory);
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
