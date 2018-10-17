using DeviceReader.Diagnostics;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceReader.Devices
{
    // specify options for device configuration
    public class DeviceConfigurationAzureTableProviderOptions
    {
        public string ConnectionString { get; set; }
        public string ConfigTableName { get; set; } = "DeviceConfig";
    }

    /// <summary>
    /// Device configuration entity
    /// </summary>
    public class DeviceConfigEntity : TableEntity
    {

        static public readonly string PARTITIONKEY = "devices";

        public DeviceConfigEntity(string deviceId)
        {
            this.PartitionKey = PARTITIONKEY;
            this.RowKey = deviceId;
        }

        public DeviceConfigEntity() { }

        public string AgentConfig { get; set; }        
        
    }

    /// <summary>
    /// Get configuration for device agent.  
    /// TODO - consider moving to separate library
    /// </summary>
    public class DeviceConfigurationAzureTableProvider : IDeviceConfigurationProvider<TwinCollection>
    {
        private readonly DeviceConfigurationAzureTableProviderOptions _options;
        private readonly ILogger _logger;
        private CloudTableClient _tableClient;
        private CloudTable _table;

        /// <summary>
        /// Create new configuration provider
        /// TODO: consider to moving to IOptions
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public DeviceConfigurationAzureTableProvider(ILogger logger, 
            DeviceConfigurationAzureTableProviderOptions options )
        {
            _logger = logger;
            _options = options;
        }

        // initializes connectivity
        private async Task Initialize()
        {
            if (_tableClient == null)
            {

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_options.ConnectionString);

                // create client
                _tableClient = storageAccount.CreateCloudTableClient();

                // Create the CloudTable object that represents the table.
                _table = _tableClient.GetTableReference(_options.ConfigTableName);

                // Create the table if it doesn't exist.
                await _table.CreateIfNotExistsAsync();

            }
        }

        /// <summary>
        /// Get configuration for device
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<string> GetConfigurationAsync(string deviceId, TwinCollection input)
        {
            
            if (input.Contains("config"))
            {
                _logger.Debug($"Using config in twin for '{deviceId}'", () => { });
                // should we update one in Table Storage
                
                return input["config"].ToString();
            }
            return await GetConfigurationAsync(deviceId);
        }


        /// <summary>
        /// Get configuration for device
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task<string> GetConfigurationAsync(string deviceId)
        {
            
            _logger.Debug($"Retrieving configuration for '{deviceId}'", () => { });
            await Initialize();

            // check if exist. If not, create new configuration
            TableOperation retrieveOperation = TableOperation.Retrieve<DeviceConfigEntity>(DeviceConfigEntity.PARTITIONKEY, deviceId);

            TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);

            if (retrievedResult.Result == null)
            {
                var result = DefaultConfig(deviceId);
                await UpsertConfig(deviceId, result);
                return result;
                // insert into 
            } else
            {
                var result = ((DeviceConfigEntity)retrievedResult.Result).AgentConfig;
                return result;
            }            
        }

        private async Task UpsertConfig(string deviceId, string config)
        {
            await Initialize();
            var entity = new DeviceConfigEntity(deviceId);
            entity.AgentConfig = config;
            entity.Timestamp = DateTime.UtcNow;            
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);
            await _table.ExecuteAsync(insertOrReplaceOperation);
        }

        /// <summary>
        /// Default config if non was found
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        private string DefaultConfig(string deviceId)
        {
            string jsontemplate = $@"
{{
    'name': '{deviceId}',
    'executables': {{ }},
    'routes': {{ }},
    'enabled': 'false'
}}
";
            return jsontemplate;
        }

    }
}
