using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using DeviceReader.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Devices;
using Newtonsoft.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace DeviceReader.Agents
{
    public abstract class AgentExecutable: IAgentExecutable, IDisposable
    {
        protected readonly IDevice _device;
        protected readonly IAgent _agent;
        protected readonly ILogger _logger;        
        protected readonly IConfiguration _config; 
        private readonly string _name;        
        private int waitSeconds;
        protected readonly string KEY_AGENT_EXECUTABLE_ROOT;
        protected readonly string KEY_AGENT_EXECUTABLE_FREQUENCY;
        protected readonly TelemetryClient _tmclient = new TelemetryClient();

        public AgentExecutable(ILogger logger, IAgent agent, string name, IDevice device)
        {
            this._logger = logger;
            this._agent = agent ?? throw new ArgumentNullException("agent");
            this._name = name ?? throw new ArgumentNullException("name");
            this._config = agent.Configuration;


            // Names of configuration 
            this.KEY_AGENT_EXECUTABLE_ROOT = "executables:" + this.Name;
            this.KEY_AGENT_EXECUTABLE_FREQUENCY = KEY_AGENT_EXECUTABLE_ROOT + ":frequency";
            

            this.waitSeconds = _config.GetValue<int>(KEY_AGENT_EXECUTABLE_FREQUENCY, 5);
            this._device = device;
            
        }
        public string Name { get => _name; }

        public IAgent Agent { get => _agent; }

        public virtual async Task Runtime(CancellationToken ct)
        {
            return;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested == true)
            {
                _logger.Warn(string.Format("'{0}' executable stopped before it started", this.Name), () => { });
                ct.ThrowIfCancellationRequested();
            }
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    //_logger.Debug(string.Format("{0} stop requested.", this.Name), () => { });

                    break;
                }
                try
                {
                                        
                    // Execute runtime
                    if (_agent.Status == AgentStatus.Running) // to avoid cases where some executables have not yet started..
                    {
                        try
                        {
                            
                            await this.Runtime(ct);
                            
                        }
                        catch (Exception e)
                        {
                            _logger.Error(string.Format("'{0}': Error while running: {1}", this.Name, e), () => { });

                            try
                            {
                                _tmclient.TrackException(e, new Dictionary<string, string>()
                                {
                                    {  "DeviceId", _device.Id },
                                    {  "AgentExecutable", this.Name },
                                }, null);

                                var obj = new
                                {
                                    deviceid = Name,
                                    timestamp = DateTime.UtcNow,                                    
                                    data = e.Message
                                };
                                //observation.Data = olist;
                                //var js = JsonConvert.SerializeObject(observation);
                                var js = JsonConvert.SerializeObject(obj);
                                _logger.Debug($"Sending: {js}", () => { });
                                var data = Encoding.UTF8.GetBytes(js);
                                var props = new Dictionary<string, string>();
                                props.Add("MessageType", "Error");
                                await _device.SendOutboundAsync(data, "application/json", "utf-8", props);

                            } catch (Exception e2)
                            {
                                _logger.Error($"Device '{Name}': Unable to send error to upstream! {e}", () => { });

                            }
                        }
                    }

                    // wait
                    // TODO: Add possibility to signal that wait is to be cancelled and processing next cycle should begin immediately
                    await Task.Delay(waitSeconds, ct).ConfigureAwait(false);

                }
                catch (TaskCanceledException e) { }
                catch (OperationCanceledException e) { }
                catch (AggregateException e)
                {
                    if (e.InnerException is TaskCanceledException)
                    {

                    }
                    else
                    {
                        _logger.Error(string.Format("'{0}': Error while running: {1}", this.Name, e), () => { });
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(string.Format("'{0}': Error while running: {1}", this.Name, e), () => { });
                }

                
            }
            
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AgentExecutable() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.            
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
