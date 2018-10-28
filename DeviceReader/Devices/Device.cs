using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Agents;
using DeviceReader.Diagnostics;
using DeviceReader.Models;
using DeviceReader.Parsers;
using DeviceReader.Configuration;
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
    /// TODO: Remove dependency on Microsoft.Azure.Devices.Client in Interfaces
    /// TODO: Refactor inter-device message passing in a way that inbound and outbound channels use same structure and methods.
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
        /// Indicates whether device accepts inbound messages
        /// </summary>
        bool AcceptsInboundMessages { get; }

        string AgentConfig { get; }

        /// <summary>
        /// Inbound channel for device
        /// </summary>
        IChannel<string, Observation> InboundChannel { get; }

        /// <summary>
        /// Initialized device connections, retrieves config and starts agent if needed.
        /// </summary>
        /// <returns></returns>
        //Task Initialize();

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
    /// TODO: add method for device reload in case of external configuration change. Problem. There is no trigger for Azure table entity change. 
    /// Focus on it later. Perhaps use some other config back end then, with triggers. Or create a watchdog.
    /// </summary>
    public class Device: IDevice, IDisposable
    {        

        public string Id { get; private set; }        
        public AgentStatus AgentStatus { get => (_agent == null ? 
                (agenterror? AgentStatus.Error: AgentStatus.Stopped)                
                : _agent.Status); }

        public ConnectionStatus ConnectionStatus { get => _connectionStatus; }

        public bool AcceptsInboundMessages { get => (_agent != null ? _agent.Inbound.AcceptsMessages : false); }

        public string AgentConfig { get => agentConfig; }
        
        public IChannel<string, Observation> InboundChannel { get => _agent?.Inbound; }


        private readonly DeviceManager _deviceManager;
        private readonly ILogger _logger;
        private DeviceClient _deviceClient;

        private ConnectionStatus _connectionStatus;
        private ConnectionStatusChangeReason _connectionStatusChangeReason;
        private IAgent _agent;
        private IAgentFactory _agentFactory;

        private bool agenterror = false;

        private string agentConfig = "";
        private Twin _twin;

        // Device configuration provider        
        private readonly IDeviceConfigurationProviderFactory _deviceConfigurationProviderFactory;

        // on deserialization, constructor is not being run. 
        public Device(string id, ILogger logger, 
            DeviceManager deviceManager, 
            IAgentFactory agentFactory,             
            IDeviceConfigurationProviderFactory deviceConfigurationProviderFactory
            )
        {
            Id = id;
            _logger = logger;
            _deviceManager = deviceManager;
            _connectionStatus = ConnectionStatus.Disconnected;
            _connectionStatusChangeReason = ConnectionStatusChangeReason.Connection_Ok;
            _agentFactory = agentFactory;
            _twin = null;            
            _deviceConfigurationProviderFactory = deviceConfigurationProviderFactory;
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
                
                // Handle connection status change
                _deviceClient.SetConnectionStatusChangesHandler((s, s2) => {
                    _logger.Info($"Device {Id} status changed from [{_connectionStatus.ToString()}] to [{s.ToString()}] (reason: {s2.ToString()})", () => { });
                    _connectionStatus = s;
                    _connectionStatusChangeReason = s2;
                });

                // register Update properties 
                await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(this.OnDeviceDesiredPropertyUpdate, null);
                /*
                await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(async (desiredProperties, objectContext) => {

                    if (desiredProperties.Version > _twin.Properties.Desired.Version)
                    {                        
                        _twin.Properties.Desired = desiredProperties; // should merge here instead.
                        _logger.Debug($"Device {Id} twin changes:\n{desiredProperties.ToJson(Formatting.Indented)}", () => { });
                                               
                    
                        JObject localconfigTwin = new JObject();
                        string localconfig = "";
                        try
                        {
                            // get config from provider.
                            localconfig = await _deviceConfigurationProvider.GetConfigurationAsync(Id, desiredProperties);
                            localconfigTwin = JObject.Parse(localconfig);
                            agentConfig = localconfigTwin.ToString();
                        } catch (Exception e)
                        {
                            _logger.Error($"Error while retrieving configuration for device '{Id}': {e}", () => { });
                            await setAgentStatus("Error", $"Error while retrieving configuration for device '{Id}': {e.Message}");
                            localconfig = "{}";
                            agentConfig = "{}";
                        }

                        

                        // change agent config - ditch old agent and create new. 
                        if (_agent != null)
                        {
                            await _agent.StopAsync(CancellationToken.None);
                            _agent.Dispose();
                            _agent = null;
                        }
                        if (localconfigTwin.ContainsKey("enabled") && localconfigTwin.GetValue("enabled").Value<string>() == "true")
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

                }, null);
                */



                await _deviceClient.OpenAsync();
                await setAgentStatus("Stopped", "");

                // this call gets only device portion of twin, so no tags, only desired and reported.
                _twin = await _deviceClient.GetTwinAsync();

                // Start agent if configured
                await ReconfigureAgent();

                /*
                // twin.Properties.Desired.
                _logger.Debug($"Device {Id} twin:\n{_twin.ToJson(Formatting.Indented)}", () => { });

                JObject configTwin = new JObject();
                string aconfig = "";
                try
                {
                    aconfig = await _deviceConfigurationProvider.GetConfigurationAsync(Id, _twin.Properties.Desired);
                    configTwin = JObject.Parse(aconfig);
                    agentConfig = configTwin.ToString();
                } catch (Exception e)
                {
                    _logger.Error($"Error while retrieving configuration for device '{Id}': {e}", () => { });
                    await setAgentStatus("Error", $"Error while retrieving configuration for device '{Id}': {e.Message}");
                    aconfig = "{}";
                    agentConfig = "{}";
                }

               

                if (configTwin.ContainsKey("enabled") && configTwin.GetValue("enabled").Value<string>() == "true")
                {
                    if (_agent == null)
                    {
                        _agent = await createAgent(agentConfig);

                    }
                    if (_agent != null)
                    {
                        _agent.SetAgentStatusHandler(OnAgentStatusChange);
                        await _agent.StartAsync(CancellationToken.None);
                    }
                }
                */
               
            }

        }
        
        /// <summary>
        /// Executes when property updates are received from IoT Hub
        /// </summary>
        /// <param name="desiredProperties">Desired properties got from IoT Hub</param>
        /// <param name="userContext"></param>
        /// <returns></returns>
        private async Task OnDeviceDesiredPropertyUpdate(TwinCollection desiredProperties, object userContext)
        {
            // Merge twin updates and reconfigure agent. Apparently, when patch is received, it is not merged anywhere in deviceclient..
            _logger.Debug($"Desired properties patch received: {desiredProperties.ToJson()}", () => { });
            _logger.Debug($"Existing desired properties: {_twin.Properties.Desired.ToJson()}", () => { });

            // should we get new twin or try to merge ourselves? 


            if (desiredProperties.Version > _twin.Properties.Desired.Version)
            {
                // update twin.
                _twin = await _deviceClient.GetTwinAsync();

                await ReconfigureAgent();
            }
        }

        /// <summary>
        /// Regonfigures agent. If reconfiguration fails, for example, because of invalid config, report it but do not throw.
        /// </summary>
        /// <returns></returns>
        private async Task ReconfigureAgent()
        {

            // get new configuration
            // if config exists in twin, use this
            // if config provider is specified, try to load from provider and override config in twin
            // if still no config, use empty config

            string newconfig = "";
            bool startagent = false;
            
            // Fetching config
            if (_twin.Properties.Desired.Contains("config"))
            {                
                newconfig = _twin.Properties.Desired["config"].ToString();
            }

            if (_twin.Properties.Desired.Contains("configprovider"))
            {
                string configprovider = _twin.Properties.Desired["configprovider"].ToString();
                string configprovideroptions = null;
                if (_twin.Properties.Desired.Contains("configprovideroptions"))
                {
                    configprovideroptions = _twin.Properties.Desired["configprovideroptions"].ToString();
                }
                try
                {
                    var provider = _deviceConfigurationProviderFactory.Get(configprovider, configprovideroptions);
                    newconfig = await provider.GetConfigurationAsync<string, string>(Id);
                } catch (Exception e)
                {
                    _logger.Error($"Cannot use configuration provider '{configprovider}': {e}", () => { });
                }                
            }

            // if no config retrieved then use empty config
            if (newconfig == null || newconfig == "") newconfig = "{}";

            // parsing what we got. 
            try
            {
                JObject jConfig = JObject.Parse(newconfig);
                newconfig = jConfig.ToString();
                startagent = (jConfig.ContainsKey("enabled") && jConfig.GetValue("enabled").Value<string>() == "true");

            } catch (Exception e)
            {
                _logger.Error($"Unable to parse config for device {Id}: {e}", () => { });
                await setAgentStatus("Error", $"Error while retrieving configuration for device '{Id}': {e.Message}");
                newconfig = "{}";
            }

            agentConfig = newconfig;
            

            // If agent is running, kill it and restart
            if (_agent != null)
            {
                await _agent.StopAsync(CancellationToken.None);
                _agent.Dispose();
                _agent = null;
            }

            // Restart agent if enabled.
            if (startagent)
            {
                _agent = await createAgent(agentConfig);                
                if (_agent != null)
                {
                    {
                        _agent.SetAgentStatusHandler(OnAgentStatusChange);
                        await _agent.StartAsync(CancellationToken.None);
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
                agentstatus["statusmessage"] = statusmessage; // there is some kind of format expectancy to this. Perhaps it is not escaped correctly..
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
                agenterror = false;
                _agent = _agentFactory.CreateAgent(agentConfig);
            }
            catch (Exception e)
            {
                // error while creating agent, most likely invalid configuration
                _logger.Error($"Device {Id}: error while creating agent: {e}", () => { });
                agenterror = true;
                await setAgentStatus("error", e.Message);
                
            }
            return _agent;
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
            //GC.Collect();
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
                        _deviceClient = null;
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
