using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices;
using DeviceReader.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using DeviceReader.Agents;

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
        Task<Dictionary<string, string>> GetDeviceListAsync();
        /// <summary>
        /// Gets SDK client for device. 
        /// </summary>
        /// <param name="deviceId">Id of device</param>
        /// <returns></returns>
        Task<DeviceClient> GetSdkClientAsync(string deviceId);
    }
    

    public class DeviceManager : IDeviceManager, IDisposable
    {

        RegistryManager registry;
        private ILogger _logger;
        private int registryCount = -1;
        private const int REGISTRY_LIMIT_REQUESTS = 1000;
        private string connectionString;
        private string hostName;
        private readonly string deviceManagerId;

        private Dictionary<string, string> connectionStringData;
        private ConcurrentDictionary<string, Device> _devices;
        private ConcurrentDictionary<string, Microsoft.Azure.Devices.Device> _sdkdevices;
        private IAgentFactory _agentFactory;

        public string DeviceManagerId { get => deviceManagerId;  }

        public DeviceManager(ILogger logger,  IAgentFactory agentFactory, string connString, string deviceManagerId)
        {
            this._logger = logger;            
            this.connectionString = connString;
            this.connectionStringData = FromConnectionString(connString);
            this.hostName = connectionStringData.ContainsKey("HostName") ? connectionStringData["HostName"] : "";
            this._agentFactory = agentFactory;
            // here we hold devices list. 
            this._devices = new ConcurrentDictionary<string, Device>();
            // List of SDK devices.
            this._sdkdevices = new ConcurrentDictionary<string, Microsoft.Azure.Devices.Device>();
            this.deviceManagerId = deviceManagerId;

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
            // return device from list.
            if (_devices.ContainsKey(deviceId)) return _devices[deviceId];

            // Create new device here instead of using IC. 
            Device device = new Device(deviceId, _logger, this, _agentFactory);
            
            _devices.GetOrAdd(deviceId, device);
           
            return device;
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

        private async Task<Microsoft.Azure.Devices.Device> GetSdkDeviceAsync(string deviceId)
        {
            // return device from list.
            if (_sdkdevices.ContainsKey(deviceId)) return _sdkdevices[deviceId];

            var sdkdevice = await this.GetRegistry().GetDeviceAsync(deviceId);

            if (sdkdevice == null)
            {
                throw new Exception("Device not found in registry!");
            }

            // add device to list
            _sdkdevices.GetOrAdd(deviceId, sdkdevice);

            return sdkdevice;
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
        public async Task<Dictionary<string, string>> GetDeviceListAsync()
        {                        
            Dictionary<string, string> result = new Dictionary<string, string>();
            var query = GetRegistry().CreateQuery($"SELECT * FROM devices WHERE tags.devicemanagerid='{DeviceManagerId}'", 100); // cannot really specify fields, it gives "result type is Raw" error
            while (query.HasMoreResults)
            {
                var page = await query.GetNextAsTwinAsync();
                foreach (var twin in page)
                {
                    result.Add(twin.DeviceId, twin.ToJson());
                }
            }
            return result;
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
