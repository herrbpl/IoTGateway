using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Agents;
using DeviceReader.Diagnostics;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeviceReader.Devices
{

    public interface IDevice
    {
        string Id { get; }
        Task SendData(string data);
    }
    

    /// <summary>
    /// Device. Each device has its own IoT Hub client. 
    /// </summary>
    public class Device: IDevice, IDisposable, IWriter
    {        
        public string Id { get; private set; }

        public bool Connected { get => _connectionStatus == ConnectionStatus.Connected; }

        private readonly IDeviceManager _deviceManager;
        private readonly ILogger _logger;
        private DeviceClient _deviceClient;

        private ConnectionStatus _connectionStatus;
        private ConnectionStatusChangeReason _connectionStatusChangeReason;
        private IAgent _agent;
        private IAgentFactory _agentFactory;
        private string agentConfig = "";
        private Twin twin;

        // on deserialization, constructor is not being run. 
        public Device(string id, ILogger logger, IDeviceManager deviceManager, IAgentFactory agentFactory)
        {
            Id = id;
            _logger = logger;
            _deviceManager = deviceManager;
            _connectionStatus = ConnectionStatus.Disconnected;
            _connectionStatusChangeReason = ConnectionStatusChangeReason.Connection_Ok;
            _agentFactory = agentFactory;
            twin = null;
            //_deviceClient = _deviceManager.GetSdkClientAsync(id);
            //_deviceClient.OpenAsync();
        }

        // Initialize device if not initialized.
        private async Task Initialize()
        {
            if (_deviceClient == null)
            {
                _deviceClient = await _deviceManager.GetSdkClientAsync(Id);
                _deviceClient.SetConnectionStatusChangesHandler((s, s2) => {
                    _logger.Info($"Device {Id} status changed from [{_connectionStatus.ToString()}] to [{s.ToString()}] (reason: {s2.ToString()})", () => { });
                    _connectionStatus = s;
                    _connectionStatusChangeReason = s2;
                });
                // register Update properties 
                await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(async (desiredProperties, objectContext) => {

                    if (desiredProperties.Version > twin.Properties.Desired.Version)
                    {

                        _logger.Debug($"Device {Id} twin changes:\n{desiredProperties.ToJson(Formatting.Indented)}", () => { });
                        // change agent config - ditch old agent and create new. 
                        if (desiredProperties.Contains("config"))
                        {

                            JObject configTwin = desiredProperties["config"];
                            agentConfig = configTwin.ToString();

                            if (_agent != null)
                            {
                                await _agent.StopAsync(CancellationToken.None);
                                await setAgentStatus("stopped", "");
                                _agent.Dispose();
                                _agent = null;
                            }
                            if (configTwin.ContainsKey("enabled") && configTwin.GetValue("enabled").Value<string>() == "true")
                            {
                                _agent = await createAgent(agentConfig);
                                if (_agent != null)
                                {
                                    {
                                        await _agent.StartAsync(CancellationToken.None);
                                        if (_agent.IsRunning)
                                        {
                                            await setAgentStatus("running", null);
                                        }
                                    }
                                }
                            }
                            
                                
                        }

                    }

                }, null);

                await _deviceClient.OpenAsync();
                await setAgentStatus("stopped", "");
                twin = await _deviceClient.GetTwinAsync();

                // twin.Properties.Desired.
                _logger.Debug($"Device {Id} twin:\n{twin.ToJson(Formatting.Indented)}", () => { });
                if (twin.Properties.Desired.Contains("config"))
                {
                    
                    JObject configTwin = twin.Properties.Desired["config"];
                    agentConfig = configTwin.ToString();

                    if (configTwin.ContainsKey("enabled") && configTwin.GetValue("enabled").Value<string>() == "true")
                    {
                        if (_agent == null)
                        {
                            _agent = await createAgent(agentConfig);
                        }
                        if (_agent != null)
                        {                            
                            await _agent.StartAsync(CancellationToken.None);
                            
                            if (_agent.IsRunning)
                            {
                                await setAgentStatus("running", null);
                            }                          
                        }
                    }
                }
            }

        }
        // create agent. 
        // start agent if required.
        

       
        private async Task setAgentStatus(string status, string statusmessage)
        {
            _logger.Info($"Device {Id}: agent status set to {status}", () => { });
            TwinCollection reportedProperties, agentstatus;
            reportedProperties = new TwinCollection();
            agentstatus = new TwinCollection();
            agentstatus["state"] = status;
            if (statusmessage != null)
            {
                agentstatus["statusmessage"] = statusmessage;
            } else
            {
                agentstatus["statusmessage"] = "";
            }
            reportedProperties["agentstatus"] = agentstatus;
            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        private async Task<IAgent> createAgent(string agentConfig)
        {
            IAgent _agent = null;
            try
            {
                // create new agent
                _agent = _agentFactory.CreateAgent(agentConfig);
            }
            catch (Exception e)
            {
                // error while creating agent, most likely invalid configuration
                _logger.Error($"Device {Id}: error while creating agent: {e}", () => { });
                await setAgentStatus("error", e.ToString());
                
            }
            return _agent;
        }

        public Task SendAsync(byte[] message, Dictionary<string, string> properties)
        {
            throw new NotImplementedException();
        }

        public async Task SendData(string data)
        {
            await Initialize();
            var telemetryDataPoint = new
            {
                eventtime = DateTime.UtcNow,
                deviceid = Id,
                message = data

            };
            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));
            
            await _deviceClient.SendEventAsync(message);
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
                    if (_agent != null)
                    {
                        _agent.StopAsync(CancellationToken.None).Wait();
                        setAgentStatus("stopped", "Device disposed").Wait();
                    }
                    _deviceClient.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Device() {
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
