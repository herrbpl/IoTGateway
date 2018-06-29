using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Models;
using Newtonsoft.Json;
using DeviceReader.Diagnostics;
namespace DeviceReader
{
    class Program
    {
        static  void Main(string[] args)
        {



            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Info;

            Logger logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);
            
            Device dev = new Device("Deviceee", "Device1", null);
            
            logger.Info("DeviceReader started", () => { });
            
            var x = JsonConvert.SerializeObject(dev);
            Console.WriteLine(x);
            
            string xx = "{\"Id\":\"Deviceee\",\"Name\":\"DeviceName\",\"Source\":null}";

            var dev2 = JsonConvert.DeserializeObject<Device>(xx);
            
            Console.WriteLine(JsonConvert.SerializeObject(dev2));

            DeviceAgent da = new DeviceAgent(logger, dev);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                // cancel the cancellation to allow the program to shutdown cleanly
                da.Stop();

                Console.WriteLine("Ctrl-C pressed");
            };

            da.RunAsync().Wait();
            



            /*
            CancellationTokenSource _cts = new CancellationTokenSource();

            DeviceReader.Models.Device mydevice = new Models.Device();
            IDeviceAgent mydeviceagent = new DeviceAgent(mydevice, _cts.Token);


         
            

            Console.WriteLine("Hello World!");
            try
            {
                List<Task> tl = new List<Task>();

                IDevice d = new Device(_cts);
                tl.Add(d.RunAsync(_cts.Token));

                IDevice d2 = new Device(_cts);
                tl.Add(d2.RunAsync(_cts.Token));

                Task.WaitAll(tl.ToArray());

            } catch (AggregateException e)
            {
                if (e.InnerException is TaskCanceledException)
                {
                    Console.WriteLine("Exiting");

                } else
                {
                    throw e.InnerException;
                }

            } 
            */
            Console.ReadLine();
        
        }
    }
}
