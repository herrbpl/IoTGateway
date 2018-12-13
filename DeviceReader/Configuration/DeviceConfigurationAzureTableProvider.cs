using DeviceReader.Diagnostics;
using DeviceReader.Extensions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceReader.Configuration
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
    public class DeviceConfigurationAzureTableProvider : DeviceConfigurationProviderBase<DeviceConfigurationAzureTableProviderOptions>
    {
        
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
            DeviceConfigurationAzureTableProviderOptions options ):base(options)
        {
            _logger = logger;            
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


        /// <summary>
        /// Gets configuration from table indicated by input.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public override async Task<TOut> GetConfigurationAsync<TIn, TOut>(TIn input)
        {
            _logger.Debug($"Retrieving configuration for '{input.ToString()}'", () => { });
            await Initialize();
            string deviceId = input.ToString();

            // check if exist. If not, create new configuration
            TableOperation retrieveOperation = TableOperation.Retrieve<DeviceConfigEntity>(DeviceConfigEntity.PARTITIONKEY, deviceId);

            TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);

            TOut result = default(TOut);
            string configstr = "";


            if (retrievedResult.Result == null)
            {
                throw new KeyNotFoundException($"Unable to find configuration for key '{input.ToString()}'");
                /*
                 * Removed automatic configuration entry generation to avoid polluting configuration database with errorneus entries.
                configstr = DefaultConfig(deviceId);
                await UpsertConfig(deviceId, configstr);                                
                */
            }
            else
            {
                configstr = ((DeviceConfigEntity)retrievedResult.Result).AgentConfig;
                
            }

            if (typeof(TOut) == typeof(string))
            {
                result = (TOut)(object)configstr;
            }
            else if (typeof(TOut) == typeof(IConfiguration))
            {
                ConfigurationBuilder cb = new ConfigurationBuilder();
                cb.AddJsonString(configstr);
                var cfg = cb.Build();
                result = (TOut)(object)cfg;
            }
            else { throw new InvalidCastException(); }
            return result;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _table = null;
                    _tableClient = null;                    
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
    }
}
