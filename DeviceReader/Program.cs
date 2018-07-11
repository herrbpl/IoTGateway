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

            Dictionary<Device, DeviceAgent> devices = new Dictionary<Device, DeviceAgent>();

            devices.Add(dev, da);

            CancellationTokenSource _cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("Ctrl-C pressed");
                // cancel the cancellation to allow the program to shutdown cleanly
                
                try
                {
                    foreach (KeyValuePair<Device, DeviceAgent> dd in devices)
                    {
                        dd.Value.Stop();
                    }
                    if (_cts != null && !_cts.Token.IsCancellationRequested)
                    {
                        _cts.Cancel();
                        eventArgs.Cancel = true;
                    } else
                    {
                        eventArgs.Cancel = false;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(string.Format("Error: {0}", ex), () => { });
                }
                
            };

            //da.RunAsync().Wait();
            da.RunAsync();

            Device dx;
            DeviceAgent dax;

            for (int i = 0; i<5;i++)
            {
                dx = new Device(i.ToString(), "Device" + i.ToString(), null);
                dax = new DeviceAgent(logger, dx);
                devices.Add(dx, dax);
                dax.RunAsync();
            }

            da.RunAsync();
            // just a bloody loop
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    Task.Delay(1000, _cts.Token).Wait();
                }
            } catch (OperationCanceledException ex)
            {
                logger.Info("Cancelled", () => { });
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is OperationCanceledException)
                {
                    logger.Info("Cancelled", () => { });
                } else
                {
                    throw ex;
                }

            }

            // reset cancellation source

            _cts.Dispose();
            _cts = new CancellationTokenSource();
   
            logger.Info(string.Format("Device Agent status: {0}", da.IsRunning), () => { });

            // now try to run again
            Thread.Sleep(1000);
            da.RunAsync();
            // just a bloody loop
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    
                    Task.Delay(1000, _cts.Token).Wait();
                }
            }
            catch (OperationCanceledException ex)
            {
                logger.Info("Cancelled", () => { });
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is OperationCanceledException)
                {
                    logger.Info("Cancelled", () => { });
                }
                else
                {
                    throw ex;
                }

            }

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
