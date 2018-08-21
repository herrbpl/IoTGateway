using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
    /// <summary>
    /// IDevice is device representative. It allows send data to input of device (when using push instead of poll)    
    /// It also provides method(s) to send data to upstream. Should this be restricted to internals?
    /// TODO: Add way to retrieve/check credentials for inbound messaging (basic auth, perhaps oAuth).
    /// </summary>
    public interface IDevice
    {
        /// <summary>
        /// DeviceId
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Agent status. Can say if agent is running or not before attempting to send inbound message
        /// </summary>
        AgentStatus AgentStatus { get; }

        /// <summary>
        /// Device Client connection status, says if device connection to upstream is established.
        /// </summary>
        Microsoft.Azure.Devices.Client.ConnectionStatus ConnectionStatus { get; }

        /// <summary>
        /// Initialized device connections, retrieves config and starts agent if needed.
        /// </summary>
        /// <returns></returns>
        Task Initialize();

        /// <summary>
        /// Initializes and Starts device client, retrieves config and starts agent if enabled
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// Stops agent and device client.
        /// </summary>
        /// <returns></returns>
        Task StopAsync();

        /// <summary>
        /// Send inbound message to device, for example when sending over https to device.
        /// TODO: return value suitable for https return codes (code, message)        
        /// </summary>
        /// <param name="data">Data as byte array</param>
        /// <returns>Returns 200 if OK, 400 if not OK, 500 if device not initialized/agent not running</returns>
        Task SendInboundAsync(byte[] data);

        /// <summary>
        /// Sends outbound message to upstream, for example to IoT Hub.
        /// </summary>
        /// <param name="data">Data as byte array. Strings are expected to in UTF8</param>
        /// <param name="contenttype">Content type, for example text/json</param>
        /// <param name="properties">Properties to include with message</param>
        /// <returns></returns>
        Task SendOutboundAsync(byte[] data, string contenttype, string contentencoding, Dictionary<string, string> properties);
    }
    

    /// <summary>
    /// Device. Each device has its own IoT Hub client. 
    /// </summary>
    public class Device: IDevice, IDisposable, IWriter
    {        
        public string Id { get; private set; }

        public bool Connected { get => _connectionStatus == ConnectionStatus.Connected; }

        public AgentStatus AgentStatus { get => (_agent == null ? AgentStatus.Stopped : _agent.Status); }

        public ConnectionStatus ConnectionStatus { get => _connectionStatus; }

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
        }

        private void OnAgentStatusChange(AgentStatus status, object context)
        {
            setAgentStatus(status.ToString(), "");
        }

        // Initialize device if not initialized.
        public async Task Initialize()
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
                        twin.Properties.Desired = desiredProperties;
                        _logger.Debug($"Device {Id} twin changes:\n{desiredProperties.ToJson(Formatting.Indented)}", () => { });
                        // change agent config - ditch old agent and create new. 
                        if (desiredProperties.Contains("config"))
                        {

                            JObject configTwin = desiredProperties["config"];
                            agentConfig = configTwin.ToString();

                            if (_agent != null)
                            {
                                await _agent.StopAsync(CancellationToken.None);                                
                                _agent.Dispose();
                                _agent = null;
                            }
                            if (configTwin.ContainsKey("enabled") && configTwin.GetValue("enabled").Value<string>() == "true")
                            {
                                _agent = await createAgent(agentConfig);
                                _agent.SetAgentStatusHandler(OnAgentStatusChange);
                                if (_agent != null)
                                {
                                    {
                                        await _agent.StartAsync(CancellationToken.None);                                       
                                    }
                                }
                            }
                            
                                
                        }

                    }

                }, null);

                await _deviceClient.OpenAsync();
                await setAgentStatus("Stopped", "");
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
                            _agent.SetAgentStatusHandler(OnAgentStatusChange);
                        }
                        if (_agent != null)
                        {                            
                            await _agent.StartAsync(CancellationToken.None);                           
                        }
                    }
                }
            }

        }
                       
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

        // should we have device upstream queue as well for sending data out? So we could call send whenever and actual dispatching occurs whenever connection comes online?
        // pros: device has direct send method available, prevents losing messages and does not depend for that on agent executables implementation
        // cons: duplicates agent writer framework. Thus memory footprint increases.
        // TODO: Add device its own queue and agent which posts messages whenever IoT Hub is connected.
        public async Task SendAsync(string data, Dictionary<string, string> properties)
        {
            await Initialize();

            var message = new Message(Encoding.ASCII.GetBytes(data));
            
            if (properties != null)
            {
                foreach (var item in properties)
                {
                    message.Properties.Add(item.Key, item.Value);
                }
            }
            await _deviceClient.SendEventAsync(message);
        }

        // should we add input queue here as well ? So even when agent is not running, we will be accepting input and process it when agent is started? 
        // initially not, as when device is disabled, it should not receive and process input..
        public async Task SendInboundAsync(byte[] data)
        {
            // no agent or agent not running
            if (_agent == null || _agent.Status != AgentStatus.Running)
            {
                throw new AgentNotRunningException();
            }

            string s = System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
            await _agent.SendMessage(s);
            return;
        }

        public async Task SendOutboundAsync(byte[] data, string contenttype, string contentencoding, Dictionary<string, string> properties)
        {
            var message = new Message(data);
            if (contenttype != null) message.ContentType = contenttype;
            if (contentencoding != null )  message.ContentEncoding = contentencoding;
            if (properties != null)
            {
                foreach (var item in properties)
                {
                    message.Properties.Add(item.Key, item.Value);
                }
            }
            await _deviceClient.SendEventAsync(message);            
        }

        public async Task StartAsync()
        {
            await Initialize(); 

        }

        public async Task StopAsync()
        {
            if (_agent != null)
            {
                await _agent.StopAsync(CancellationToken.None);
                _agent.Dispose();
                _agent = null;
            }
            if (_deviceClient != null)
            {
                try
                {
                    await _deviceClient.CloseAsync();
                }
                catch (Exception e)
                {
                    _logger.Warn($"Exception while closing device client: {e}", () =>
                    {
                    });
                }
                _deviceClient.Dispose();
                _deviceClient = null;
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

                    if (_agent != null)
                    {
                        _agent.Dispose();
                        _agent = null;
                    }

                    if (_deviceClient != null)
                    {
                        _deviceClient.Dispose();
                    }
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

    [Serializable]
    internal class AgentNotRunningException : Exception
    {
        public AgentNotRunningException()
        {
        }

        public AgentNotRunningException(string message) : base(message)
        {
        }

        public AgentNotRunningException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AgentNotRunningException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
