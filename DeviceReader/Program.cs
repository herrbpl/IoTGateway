using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Models;
using Newtonsoft.Json;
using DeviceReader.Diagnostics;
using DeviceReader.Devices;
using DeviceReader.Models;


namespace DeviceReader
{
    class Program
    {
        

        static  void Main(string[] args)
        {



            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;

            ILogger logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

            IDevice dev1 = new Device("001", "Teeilmajaam", null);
            IDeviceAgentRunner runner1 = new DefaultDeviceRunner(logger, dev1);
            IDeviceAgent agent1 = new DeviceAgent(logger, dev1, runner1);


            char ch;
            Console.WriteLine("Press C to quit, S to stop agent, X to start agent");
            while (true)
            {
                Console.WriteLine("Agent status: {0}", agent1.IsRunning);
                ch = Console.ReadKey().KeyChar;
                if (ch == 'c' || ch == 'C')
                {
                    if (agent1.IsRunning) { agent1.Stop();  }
                    logger.Info("Exiting", () => { });
                    break;
                }
                else if (ch == 's' || ch == 'S') {
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

            Console.ReadLine();
        
        }
    }
}
