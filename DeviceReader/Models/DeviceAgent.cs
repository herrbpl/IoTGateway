using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;

namespace DeviceReader.Models
{


    public interface IDeviceAgent
    {
        bool IsRunning { get; }
        Task RunAsync();
        void Stop();
    }
    class DeviceAgent : IDeviceAgent
    {
        public bool IsRunning { get { return (_executingTask == null? false:_executingTask.IsCompleted); } }

        public CancellationTokenSource CancellationTokenSource { get => this._cts;  } 


        private CancellationTokenSource _cts;
        private ILogger _logger;
        private Device _device;
        private Task _executingTask;

        private Func<DeviceAgent, Task> _runDelegate;

        public DeviceAgent(ILogger logger, Device device, Func<DeviceAgent, Task> runDelegate)
        {
            this._cts = new CancellationTokenSource();
            this._logger = logger;
             
            this._device = device;
            if (runDelegate != null)
            {
                this._runDelegate = runDelegate;
            } else
            {
                this._runDelegate = DefaultRunner;
            }            
        }

        

        public DeviceAgent(ILogger logger, Device device)        
            : this(logger, device, null)
        {

        }
                
        protected async Task DefaultRunner(DeviceAgent agent)
        {
           
            while (!agent.CancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    // Check config change.. or should we? Perhaps.. Stop and recreate?
                    // source can change. Target can change. 
                    // execute protocol reader
                    // execute parser/formatter
                    // execute filter
                    // post to output/iot Hub
                    _logger.Info("Runner thread loop", () => { });
                    await Task.Delay(1000, agent.CancellationTokenSource.Token);
                } catch (OperationCanceledException ex)
                {
                    _logger.Info("Cancellation requested!", () => { });
                }
            }
        }

        public async Task RunAsync()
        {  
            
            if (this.IsRunning) { return;  }

            _logger.Info(string.Format("Starting device {0}", this._device.Id), () => { });

            
            Task t = new Task( () =>
            {

                while (!this.CancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        // Check config change.. or should we? Perhaps.. Stop and recreate?
                        // source can change. Target can change. 
                        // execute protocol reader
                        // execute parser/formatter
                        // execute filter
                        // post to output/iot Hub
                        _logger.Info("Runner thread loop", () => { });
                         Task.Delay(1000, this.CancellationTokenSource.Token).Wait();
                    }
                    catch (OperationCanceledException ex)
                    {
                        _logger.Info("Cancellation requested!", () => { });
                    }
                }

            });

            try
            {
                this._executingTask = t;
                t.Start();
                await this._executingTask; // this._runDelegate(this);                    
            }
            catch (AggregateException ex)
            {
                _logger.Info(string.Format("Error: {0}", ex.InnerException), () => { });
            }
            //await this._runDelegate(this); // this._runDelegate(this);                    

        }

        public void Stop()
        {            
                _logger.Info(string.Format("Stopping device {0}", this._device.Id), () => { });
                this._cts.Cancel();            
            
        }
    }
}
