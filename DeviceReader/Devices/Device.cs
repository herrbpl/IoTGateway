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
using Microsoft.Extensions.Configuration;
using DeviceReader.Extensions;

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

        public bool AcceptsInboundMessages { get => (_agent != null && _agent.Inbound != null ? _agent.Inbound.AcceptsMessages : false); }

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

        private readonly IDeviceConfigurationLoader _deviceConfigurationLoader;

        // on deserialization, constructor is not being run. 
        public Device(string id, ILogger logger, 
            DeviceManager deviceManager, 
            IAgentFactory agentFactory,             
            IDeviceConfigurationProviderFactory deviceConfigurationProviderFactory,
            IDeviceConfigurationLoader deviceConfigurationLoader
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
            _deviceConfigurationLoader = deviceConfigurationLoader;
        }

        private void OnAgentStatusChange(AgentStatus status, object context)
        {
            setAgentStatus(status.ToString(), "");
        }

        private Task restartTask = null;
        private CancellationTokenSource restartTaskCTS = null;

        /// <summary>
        /// Wait upon x milliseconds before restarting agent. 
        /// </summary>
        private void RestartDeviceOnConnectionDisabledEvent()
        {
            if (restartTask != null && restartTask.Status == TaskStatus.Running)
            {
                _logger.Debug($"Device {Id}: Restart already scheduled", () => { });
                return;
            }

            _logger.Debug($"Device {Id}: Scheduling restart.", () => { });
            restartTaskCTS = new CancellationTokenSource();

            restartTask = Task.Run(() => 
                {
                    // delay
                    try
                    {
                        Task.Delay(1000, restartTaskCTS.Token);

                        if (!restartTaskCTS.IsCancellationRequested)
                        {
                            StopAsync().Wait();
                            StartAsync().Wait();
                        }
                    } catch (Exception e)
                    {
                        _logger.Error($"Device {Id}: error while restarting device", () => { });
                    }
         
                }, restartTaskCTS.Token);
        

        }

        private void CancelRestartDeviceOnConnectionDisabledEvent()
        {
            if (restartTaskCTS == null || restartTaskCTS.IsCancellationRequested)
            {
                _logger.Debug($"Device {Id}: Cancellation token null or already triggered", () => { });
                restartTaskCTS = null;
                return;
            }
            _logger.Debug($"Device {Id}: Restart cancelled", () => { });
            restartTaskCTS.Cancel();
        }


        // Initialize device if not initialized.
        public async Task Initialize()
        {
            if (_deviceClient == null)
            {
                _deviceClient = await _deviceManager.GetSdkClientAsync(Id);

                // Handle connection status change
                // NB! THere is issue in Azure IoT SDK which disables connection - https://github.com/Azure/azure-iot-sdk-csharp/issues/211
                // Current recommended workaround is to dispose and create client.
                _deviceClient.SetConnectionStatusChangesHandler((s, s2) => {
                    _logger.Info($"Device {Id} status changed from [{_connectionStatus.ToString()}] to [{s.ToString()}] (reason: {s2.ToString()})", () => { });

                    if (_connectionStatus == ConnectionStatus.Disabled && s != ConnectionStatus.Disabled)
                    {
                        CancelRestartDeviceOnConnectionDisabledEvent();
                    }
                    else if (_connectionStatus != ConnectionStatus.Disabled && s == ConnectionStatus.Disabled)
                    {
                        RestartDeviceOnConnectionDisabledEvent();
                    }
                    _connectionStatus = s;
                    _connectionStatusChangeReason = s2;
                });

                // register Update properties 
                await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(this.OnDeviceDesiredPropertyUpdate, null);

                // handle device methods
                await _deviceClient.SetMethodDefaultHandlerAsync(DeviceMethodHandler, null);
                
                await _deviceClient.OpenAsync();
                await setAgentStatus("Stopped", "");

                // this call gets only device portion of twin, so no tags, only desired and reported.
                _twin = await _deviceClient.GetTwinAsync();

                // Start agent if configured
                await ReconfigureAgent();

                
               
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
        private async Task<MethodResponse> ReconfigureAgent()
        {

            // get new configuration
            // if config exists in twin, use this
            // if config provider is specified, try to load from provider and override config in twin
            // if still no config, use empty config

            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            string newconfig = "";
            bool startagent = false;

            // config is applied as follows
            // all "configsources" - array of configsourcename = "configprovider:configkey" 
            // if exists, "config" will be used as direct source and applied as latest
            Dictionary<string, string> configs = new Dictionary<string, string>();

            // default config. 
            configs.Add("$DefaultConfig", $@"
{{
    'name': '{Id}',
    'executables': {{ }},
    'routes': {{ }},
    'enabled': 'false'
}}
");

            // Placeholders replacement. Currently, only one but can expanded on later
            Dictionary<string, string> replacements = new Dictionary<string, string>();
            replacements.Add("${DEVICEID}", this.Id);


            // Fetching config
            if (_twin.Properties.Desired.Contains("configsource"))
            {
                var cs = _twin.Properties.Desired["configsource"];
                try
                {
                    Dictionary<string, string> configs_ = await _deviceConfigurationLoader.LoadConfigurationAsync(cs, replacements);
                    foreach (var item in configs_)
                    {
                        configs[item.Key] = item.Value;
                    }
                } catch (Exception e)
                {
                    _logger.Error($"Device {Id}: Unable to load configurationsource: {e}", () => { });
                    errors.Add($"Unable to load configurationsource: {e.Message}");
                }
            }
            
            // Fetching config
            if (_twin.Properties.Desired.Contains("config"))
            {
                var c = _twin.Properties.Desired["config"];                
                newconfig = _twin.Properties.Desired["config"].ToString();
                configs.Add("$ConfigFromTwin", newconfig);
            }
           

            // try to create config.
            IConfigurationBuilder cb = new ConfigurationBuilder();
            foreach (var item in configs)
            {
                try
                {
                    cb.AddJsonString(item.Value);
                }
                catch (Exception e)
                {
                    _logger.Error($"Device {Id}: Invalid configuration source '{item.Key}': {e}", () => { });
                    errors.Add($"Invalid configuration source '{item.Key}': {e.Message}");                    
                }
            }

            var agentConfig_ = cb.Build();

            // To be safe, start agent only when all configuration sources are successfully loaded and agent is enabled. 
            startagent = agentConfig_.GetValue<Boolean>("enabled", false) && (errors.Count == 0);

            // If errors in configuation loading, set error status
            if (errors.Count > 0)
            {
                await setAgentStatus("Error", $"Unable to load all configuration sources: {String.Join("\r\n", errors.ToArray())}");
            }

            

            // dump agentconfig into variable so it is visible in web service later on.
            StringBuilder sb = new StringBuilder();

            foreach (var item in agentConfig_.AsEnumerable())
            {
                sb.AppendLine(item.Key + "=" + item.Value);
            }

            

            agentConfig = sb.ToString();
            _logger.Debug($"Device {Id}: Reconfiguring agent with config:\r\n{agentConfig}", () => { });

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
                try
                {
                    agenterror = false;
                    _agent = _agentFactory.CreateAgent(agentConfig_);
                    
                    if (_agent != null)
                    {
                        {
                            _agent.SetAgentStatusHandler(OnAgentStatusChange);
                            await _agent.StartAsync(CancellationToken.None);
                        }
                    }
                } catch (Exception e)
                {
                    _logger.Error($"Unable to start agent: {e}", () => { });
                    errors.Add($"Unable to start agent: {e.Message}");
                    agenterror = true;
                }
            }

            int status = 200;
            JObject jResponse = new JObject();


            if (warnings.Count > 0)
            {
                status = 200;
                jResponse["warnings"] = JToken.FromObject(warnings);
                
            }

            if (errors.Count > 0)
            {
                status = 400;
                jResponse["errors"] = JToken.FromObject(errors);
                
            }

            jResponse.Add("status", status);

            

            return new MethodResponse(Encoding.UTF8.GetBytes(jResponse.ToString()),(int) status);

        }

        /// <summary>
        /// Device method handler.        
        /// </summary>
        /// <param name="methodHandler"></param>
        /// <param name="userContext"></param>
        /// <returns></returns>
        private async Task<MethodResponse> DeviceMethodHandler(MethodRequest methodRequest, object userContext)
        {
            _logger.Info($"Command request: {methodRequest.Name} ({methodRequest.DataAsJson})", () => { });
            if (methodRequest.Name.Equals("reconfigure"))
            {

                var response = await ReconfigureAgent();
                _logger.Info($"Command response: {response.Status}:{response.ResultAsJson}", () => { });
                return response;
            }

            if (methodRequest.Name.Equals("restart"))
            {
                RestartDeviceOnConnectionDisabledEvent();                
                return new MethodResponse(200);
            }

            _logger.Warn($"Command {methodRequest.Name} not found!", () => { });
            return new MethodResponse(404);

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
            try
            {
                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            } catch (Exception e)
            {
                _logger.Warn($"Unable to set status: {e}", () => { });
            }
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
                    _logger.Debug($"Device {Id}: Unregistering device status callback.", () => { });
                    _deviceClient.SetConnectionStatusChangesHandler((s, s2) => { });
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
