using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices;
using DeviceReader.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using DeviceReader.Agents;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.EventHubs.Processor;
using Newtonsoft.Json;

namespace DeviceReader.Devices
{
    /// <summary>
    /// Device Manager aquires list of devices from registry and holds IDevice instances for them.    
    /// TODO: Listen for device registry changes
    /// </summary>
    public interface IDeviceManager
    {
        /// <summary>
        /// Device Manager Identity, should be unique per Device Manager instance in Iot Hub
        /// </summary>        
        string DeviceManagerId { get; }

        /// <summary>
        /// Gets device and represents it using given interface
        /// </summary>
        /// <typeparam name="T">Interface to cast device object as</typeparam>
        /// <param name="deviceId">Device Id</param>
        /// <returns></returns>
        T GetDevice<T>(string deviceId);
        /// <summary>
        /// Gets device list from registry, associated with this device manager identity.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<IDevice>> GetDeviceListAsync();
        //Task<Dictionary<string, IDevice>> GetDeviceListAsync();

        /// <summary>
        /// Gets SDK client for device. 
        /// </summary>
        /// <param name="deviceId">Id of device</param>
        /// <returns></returns>
        Task<DeviceClient> GetSdkClientAsync(string deviceId);

        Task StartAsync();
        Task StopAsync();
    }
    
    // TODO: need some validation for configurations, for ex missing values or smth. Perhaps existing library?
    // TODO: separate config to different class.

    public class IotHubConfig
    {
        public string ConnectionString { get; set; }
    }

    public class EventHubConfig
    {
        public string ConnectionString { get; set; }
        public string HubName { get; set; }
        public string ConsumerGroup { get; set; }
        public string AzureStorageContainer { get; set; }
        public string AzureStorageAccountName { get; set; }
        public string AzureStorageAccountKey { get; set; } 
    }

    public class DeviceManagerConfig
    {
        public string DeviceManagerId { get; set; }
        public int RegistryLimitRequests { get; set; }  = 1000;
        public IotHubConfig IotHub { get; set; }
        public EventHubConfig EventHub { get; set; }
    }

    


    public class DeviceManager : IDeviceManager, IDisposable
    {

        /// <summary>
        /// Class holds device info so we can use one ConcurrentDirectory instead of 3 for one object metadata.
        /// </summary>
        private class DeviceInfo
        {
            public Device Device { get; set; }
            public Microsoft.Azure.Devices.Device SdkDevice { get; set; }
            public long TagsVersion { get; set; } = 0;
        }


        RegistryManager registry;
        private ILogger _logger;
        private int registryCount = -1;
        private const int REGISTRY_LIMIT_REQUESTS = 1000;
        private string connectionString;
        private string hostName;
        private readonly string deviceManagerId;

        private Dictionary<string, string> connectionStringData;

        private const string TAG_DEVICEMANAGER_ID = "devicemanagerid";
        

        // device info
        private ConcurrentDictionary<string, DeviceInfo> _deviceinfo;

        private IAgentFactory _agentFactory;

        public string DeviceManagerId { get => deviceManagerId;  }

        // Device events processor
        private DeviceEventProcessorFactory _deviceEventProcessorFactory;
        private DeviceEventProcessor _deviceEventProcessor;
        private EventProcessorHost _eventProcessorHost;

        private readonly DeviceManagerConfig _configuration;

        //public DeviceManager(ILogger logger, IAgentFactory agentFactory, DeviceManagerConfig configuration, string connString, string deviceManagerId)
        public DeviceManager(ILogger logger,  IAgentFactory agentFactory, DeviceManagerConfig configuration)
        {
            this._logger = logger;
            _configuration = configuration;

            this.connectionString = _configuration.IotHub.ConnectionString;

            //this.connectionString = connString;
            this.connectionStringData = FromConnectionString(connectionString);

            this.hostName = connectionStringData.ContainsKey("HostName") ? connectionStringData["HostName"] : "";
            this._agentFactory = agentFactory;
           
            // deviceInfo
            _deviceinfo = new ConcurrentDictionary<string, DeviceInfo>();

            // device event processor factory.
            _deviceEventProcessorFactory = new DeviceEventProcessorFactory(_logger, this);

            // Device manager id
            //this.deviceManagerId = deviceManagerId;
            this.deviceManagerId = _configuration.DeviceManagerId;

        }

        // where are device secrets stored? Shall we use sas or token based auth?
        // currently, use AMPQ as it can be used for connection pooling..
        // where are devices info stored? Cannot really use registry if it returns only 1000 items?

        protected async Task<DeviceClient> GetDeviceClientAsync(string deviceId)
        {
            string deviceToken = this.getDeviceToken(deviceId);
            var auth = new DeviceAuthenticationWithToken(deviceId, deviceToken);                
            return null;
        }

        // token that device will use to interact with iot hub
        internal string getDeviceToken(string deviceId)
        {
            string key = connectionStringData.ContainsKey("SharedAccessKey") ? connectionStringData["SharedAccessKey"] : "";
            var sasToken = new SharedAccessSignatureBuilder()
                {
                    Key = key,
                    Target = $"{this.hostName}/devices/{deviceId}",
                    TimeToLive = TimeSpan.FromMinutes(20)                    
                }
            .ToSignature();
            return sasToken;
        }

        internal IDevice GetDevice(string deviceId)
        {

            if (!_deviceinfo.ContainsKey(deviceId))
            {
                throw new ArgumentException();
            }

            DeviceInfo di;
            if (!_deviceinfo.TryGetValue(deviceId, out di))
            {
                _logger.Error($"Unable to retrieve device {deviceId} info", () => { });
                throw new Exception($"Unable to retrieve device {deviceId} info");
            }

            return di.Device;

            
        }

        public T GetDevice<T>(string deviceId)
        {            
            return (T)GetDevice(deviceId);            
        }

        public async Task<DeviceClient> GetSdkClientAsync(string deviceId)
        {
            var sdkdevice = await GetSdkDeviceAsync(deviceId);
            
            // currently using device connection string as auth source. Later can implement auth by device token. Only downside is that tokens need to be refreshed.
            var primarykey = sdkdevice.Authentication.SymmetricKey.PrimaryKey;
            string deviceConnectionString = $"Hostname={hostName};DeviceId={deviceId};SharedAccessKey={primarykey}";
            
            // Device client.
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, new ITransportSettings[]
                        {
                            new AmqpTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only)
                            {
                                AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                                {
                                    Pooling = true,
                                    MaxPoolSize = (uint)ushort.MaxValue // add to config.
                                }
                            }
                        });

            return deviceClient;
        }

        /// <summary>
        /// Required to get device authentication information. 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        private async Task<Microsoft.Azure.Devices.Device> GetSdkDeviceAsync(string deviceId)
        {
            if (!_deviceinfo.ContainsKey(deviceId))
            {
                throw new ArgumentException();
            }

            DeviceInfo di;
            if (!_deviceinfo.TryGetValue(deviceId, out di))
            {
                _logger.Error($"Unable to retrieve device {deviceId} info", () => { });
                throw new Exception($"Unable to retrieve device {deviceId} info");
            }

            if (di.SdkDevice != null)
            {                
                return di.SdkDevice;
            } else
            {
                di.SdkDevice = await this.GetRegistry().GetDeviceAsync(deviceId);
                if (di.SdkDevice == null)
                {
                    throw new Exception("Device not found in registry!");
                }
                // Update only SdkDevice part
                _deviceinfo.AddOrUpdate(deviceId, di, (k, v) => { v.SdkDevice = di.SdkDevice; return v; });
            }
            return di.SdkDevice;
            
        }


        // from MS IOT monitoring solution
        // Temporary workaround, see https://github.com/Azure/device-simulation-dotnet/issues/136
        private RegistryManager GetRegistry()
        {
            if (this.registryCount > REGISTRY_LIMIT_REQUESTS)
            {                
                this.registry.CloseAsync();

                try
                {
                    this.registry.Dispose();
                }
                catch (Exception e)
                {
                    // Errors might occur here due to pending requests, they can be ignored
                    _logger.Debug("Ignoring registry manager Dispose() error", () => new { e });
                }

                this.registryCount = -1;
            }

            if (this.registryCount == -1)
            {
                string connString = this.connectionString;
                this.registry = RegistryManager.CreateFromConnectionString(connString);
                this.registry.OpenAsync();
            }

            this.registryCount++;

            return this.registry;
        }

        private Dictionary<string, string> FromConnectionString(string connectionstring)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var item in connectionstring.Split(";"))
            {
                
                var v = item.Split("=",2);
                string key = v[0];
                string value = null;
                if (v.Length > 1) value = v[1];
                result.Add(key, value);
            }
            return result;
        }

        // Get all devices managed by this gateway. Lates, should be lazily iterable if large number of objects
        //public async Task<Dictionary<string, IDevice>> GetDeviceListAsync()
        public async Task<IEnumerable<IDevice>> GetDeviceListAsync()
        {

            /*
            
            var query = GetRegistry().CreateQuery($"SELECT * FROM devices WHERE tags.{TAG_DEVICEMANAGER_ID}='{DeviceManagerId}'", 100); // cannot really specify fields, it gives "result type is Raw" error
            while (query.HasMoreResults)
            {
                var page = await query.GetNextAsTwinAsync();
                foreach (var twin in page)
                {                    
                    result.Add(twin.DeviceId, twin.ToJson());                    
                }
            }

            return result;
            */
            /*var res = (from devicedata in _deviceinfo
                       select new KeyValuePair<string, IDevice>(devicedata.Key, (IDevice)devicedata.Value.Device));
            */

            var res = (from devicedata in _deviceinfo
                       select (IDevice)devicedata.Value.Device);

            //var result = res.ToDictionary(v => v.Key, v => v.Value);
            return res;

        }

        protected async Task SyncDeviceRegistry()
        {
            _logger.Debug($"Device Sync started", () => { });
            // list of existing devices
            var existingdevices = _deviceinfo.Keys.ToList();
            
            var query = GetRegistry().CreateQuery($"SELECT * FROM devices WHERE tags.{TAG_DEVICEMANAGER_ID}='{DeviceManagerId}'", 100); // cannot really specify fields, it gives "result type is Raw" error
            while (query.HasMoreResults)
            {
                var page = await query.GetNextAsTwinAsync();
                foreach (var twin in page)
                {

                    await RegisterDevice(twin.DeviceId);
                    SetDeviceTagVersion(twin.DeviceId, twin.Version.Value);
                    
                    if (existingdevices.Contains(twin.DeviceId)) existingdevices.Remove(twin.DeviceId);
                    
                }
            }

            // now unregister all devices not found in registry.            
            foreach (var item in existingdevices) 
            {
                await UnRegisterDevice(item);
            }
            _logger.Debug($"Device Sync completed", () => { });
        }

        /// <summary>
        /// Process events from event Processor Host
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        public async Task OnDeviceEvent(EventData eventData )
        {            
            string hubName = (string)eventData.Properties["hubName"];

            if (hubName+ ".azure-devices.net" != hostName)
            {
                _logger.Warn($"Got message from non-connected IoT Hub: {hubName}, dropping message", () => { });
                return;
            }

            string deviceId = (string)eventData.Properties["deviceId"];
            string messageSource = (string)eventData.SystemProperties["iothub-message-source"];
            string operationType = (string)eventData.Properties["opType"];
            JToken jPayload = JToken.Parse(Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count));

            switch (messageSource)
            {
                case "deviceLifecycleEvents":
                    await ProcessDeviceLifeCycleEvent(eventData, operationType, jPayload, hubName, deviceId);
                    //syncCommand = this.GetDeviceLifecycleSyncCommand(eventData, operationType, jPayload, hubName, deviceId);
                    break;
                case "twinChangeEvents":
                    await ProcessDeviceTwinChangeEvent(eventData, operationType, jPayload, hubName, deviceId);
                    //syncCommand = this.GetTwinChangeSyncCommand(eventData, operationType, jPayload, hubName, deviceId);
                    break;
                default:
                    _logger.Warn($"Message source '{messageSource}' not supported", () => { });
                    break;
            }

        }

        /// <summary>
        /// Process twin changes. This is required only, when tags change. Other changes are handled by device itself
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="operationType"></param>
        /// <param name="jPayload"></param>
        /// <param name="hubName"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        private async Task ProcessDeviceTwinChangeEvent(EventData eventData, string operationType, JToken jPayload, string hubName, string deviceId)
        {
            switch (operationType)
            {
                case "updateTwin": // this is a patch
                case "replaceTwin": // this contains full twin state
                    // if newer than current twin, update twin data
                    //_logger.Debug($"{jPayload.ToString(Newtonsoft.Json.Formatting.Indented)}", () => { });

                    var settings = new JsonSerializerSettings
                    {
                        DateParseHandling = DateParseHandling.DateTimeOffset
                    };

                    var oldtagsversion = GetDeviceTagVersion(deviceId);
                    //var oldtwin = _devicetwins.ContainsKey(deviceId) ? _devicetwins[deviceId] : new Twin();

                    var newtwin = JsonConvert.DeserializeObject<Twin>(jPayload.ToString(), settings);

                    if (newtwin.Version.HasValue && newtwin.Version.Value > oldtagsversion)
                    {                                                

                        if (newtwin.Tags != null && newtwin.Tags.Contains(TAG_DEVICEMANAGER_ID))
                        {
                            _logger.Debug($"NewTwin is {newtwin.ToJson(Newtonsoft.Json.Formatting.Indented)}", () => { });
                            if (newtwin.Tags[TAG_DEVICEMANAGER_ID] != DeviceManagerId )
                            {
                                await UnRegisterDevice(deviceId);
                            } else
                            {
                                await RegisterDevice(deviceId);
                                SetDeviceTagVersion(deviceId, newtwin.Version.Value);
                            }
                        }                                                                     

                        
                    }


                    break;
                                   
                default:
                    _logger.Warn($"Operation type '{operationType}' not supported", () => { });
                    break;
            }
        }

        /// <summary>
        /// Process device lifecycle changes. 
        /// Some code from https://github.com/Azure-Samples/iot-hub-notifications-sync-graphdb/blob/master/src/SyncGraphDbApp/TwinChangesEventProcessor.cs
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="operationType"></param>
        /// <param name="jPayload"></param>
        /// <param name="hubName"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        private async Task ProcessDeviceLifeCycleEvent(EventData eventData, string operationType, JToken jPayload, string hubName, string deviceId)
        {

            var settings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.DateTimeOffset
            };

            var oldtagsversion = GetDeviceTagVersion(deviceId);
            Twin newtwin;
            

            switch (operationType)
            {
                case "createDeviceIdentity": // this is a patch

                    // if newer than current twin, update twin data
                    //_logger.Debug($"{jPayload.ToString(Newtonsoft.Json.Formatting.Indented)}", () => { });                                                                              
                    newtwin = JsonConvert.DeserializeObject<Twin>(jPayload.ToString(), settings);
                    if (newtwin.Version.HasValue && newtwin.Version.Value > oldtagsversion)
                    {

                        if (newtwin.Tags != null && newtwin.Tags.Contains(TAG_DEVICEMANAGER_ID))
                        {                            
                            if (newtwin.Tags[TAG_DEVICEMANAGER_ID] == DeviceManagerId)
                            {
                                await RegisterDevice(deviceId);
                                SetDeviceTagVersion(deviceId, newtwin.Version.Value);
                            }                            
                        }
                        
                    }

                    break;
                case "deleteDeviceIdentity": // this contains full twin state
                    newtwin = JsonConvert.DeserializeObject<Twin>(jPayload.ToString(), settings);
                    if (newtwin.Version.HasValue && newtwin.Version.Value > oldtagsversion)
                    {
                        await UnRegisterDevice(deviceId);                        
                    }
                                        
                    break;
                default:
                    _logger.Warn($"Operation type '{operationType}' not supported", () => { });
                    break;
            }
        }


        private long GetDeviceTagVersion(string deviceId)
        {
            if (!_deviceinfo.ContainsKey(deviceId))
            {
                return 0;
            }
           
            DeviceInfo di;
            if (!_deviceinfo.TryGetValue(deviceId, out di))
            {
                _logger.Error($"Unable to retrieve device {deviceId} info", () => { });
                throw new Exception($"Unable to retrieve device {deviceId} info");
            }
            return di.TagsVersion;

        }

        private void SetDeviceTagVersion(string deviceId, long tagsVersion )
        {
            if (!_deviceinfo.ContainsKey(deviceId))
            {
                throw new ArgumentException();
            }

            DeviceInfo di;
            if (!_deviceinfo.TryGetValue(deviceId, out di))
            {
                _logger.Error($"Unable to retrieve device {deviceId} info", () => { });
                throw new Exception($"Unable to retrieve device {deviceId} info");
            }

            di.TagsVersion = tagsVersion;
            
            // Update tags version
            _deviceinfo.AddOrUpdate(deviceId, di, (k, v) => { v.TagsVersion = di.TagsVersion; return v; });

        }

        /// <summary>
        /// Registers device with device registry. May be called multiple times, should be idempotent.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task RegisterDevice(string deviceId)
        {
            DeviceInfo di = null;
            Device d = null;

            // check if device exists
            if (!_deviceinfo.ContainsKey(deviceId)) {
                // create record 
                _logger.Debug($"Registering device {deviceId}.", () => { });

                // new instance of device
                d = new Device(deviceId, _logger, this, _agentFactory);

                di = new DeviceInfo() 
                {
                    Device = d
                    , SdkDevice = null
                    , TagsVersion = 0
                };

                if (!_deviceinfo.TryAdd(deviceId, di))
                {
                    _logger.Warn($"Unable to register device {deviceId}", () => { });

                }
                else
                {
                    _logger.Debug($"Registering device {deviceId} completed.", () => { });
                }
            } else
            {
                if (!_deviceinfo.TryGetValue(deviceId, out di))
                {
                    _logger.Warn($"Unable to access concurrent device information {deviceId}", () => { });
                }                
            }


            // get or create device
            
            var device = di.Device;

            // initialize/start device
            if (device.ConnectionStatus != ConnectionStatus.Connected)
            {
                await device.StartAsync();
            }
            
        }

        /// <summary>
        /// Called when device is removed or assigned to another device manager. 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task UnRegisterDevice(string deviceId)
        {
                        
            if (!_deviceinfo.ContainsKey(deviceId))
            {
                return;
            }

            DeviceInfo di;
            if (!_deviceinfo.TryRemove(deviceId, out di))
            {
                _logger.Warn($"Unable to access concurrent device information {deviceId}", () => { });
                return;
            }

            _logger.Debug($"Unregistering device {deviceId}.", () => { });

            if (di.Device != null)
            {
                await di.Device.StopAsync();
                di.Device.Dispose();
                di.Device = null;
            }

           
            _logger.Debug($"Unregistering device {deviceId} completed.", () => { });
        }

        public async Task StartAsync()
        {
            // initiat sync, load devices from registry and registers them
            // create eventprocessorhost 
            // start loop which periodically does full sync. 

            await SyncDeviceRegistry();

            // probably running already.
            if (_eventProcessorHost != null)
            {
                return;
            }

            string storageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", _configuration.EventHub.AzureStorageAccountName, _configuration.EventHub.AzureStorageAccountKey);

            // Event Processor Host
            _eventProcessorHost = new EventProcessorHost(
                _configuration.EventHub.HubName,
                _configuration.EventHub.ConsumerGroup,
                _configuration.EventHub.ConnectionString,
                storageConnectionString,
                _configuration.EventHub.AzureStorageContainer);

            var _eventProcessorOptions = new EventProcessorOptions();

            // start processing events.
            await _eventProcessorHost.RegisterEventProcessorFactoryAsync(_deviceEventProcessorFactory, _eventProcessorOptions);
            
        }

        public async Task StopAsync()
        {
            // stop event processor.             
            if (_eventProcessorHost != null)
            {
                await _eventProcessorHost.UnregisterEventProcessorAsync();
            }
            
            _eventProcessorHost = null;
            // Unregister all devices.
            var existingdevices = _deviceinfo.Keys.ToList();
            foreach (var item in existingdevices)
            {
                await UnRegisterDevice(item);
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

                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DeviceManager() {
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
