using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Extensions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace DeviceReader.Configuration
{
    class DeviceConfigurationLoader : IDeviceConfigurationLoader
    {
        private readonly ILogger _logger;
        private readonly IDeviceConfigurationProviderFactory _deviceConfigurationProviderFactory;
        public DeviceConfigurationLoader(ILogger logger, IDeviceConfigurationProviderFactory deviceConfigurationProviderFactory)
        {
            _logger = logger;
            _deviceConfigurationProviderFactory = deviceConfigurationProviderFactory;
        }

        public async Task<Dictionary<string,string>> LoadConfigurationAsync(JToken configurationSource, Dictionary<string, string> replacements)
        {
            Dictionary<string, string> configs = new Dictionary<string, string>();

            _logger.Debug($"Configurationsource type: {configurationSource.Type}", () => { });
            
            switch (configurationSource.Type)
            {

                case JTokenType.Object:
                    _logger.Debug($"'configsource has type of an object.", () => { });
                    foreach (var property in configurationSource.Value<JObject>().Properties())
                    {
                        string configname = property.Name;
                        _logger.Debug($"Property {configname}value has type of {property.Value.Type}", () => { });
                        if (property.Value.Type != JTokenType.String)
                        {
                            _logger.Warn($"Expected string value in format of providerkey,configkey", () => { });
                            continue;
                        }                        
                        string source = property.Value.ToString();
                        configs.Add(configname, source);
                    }

                    break;
                case JTokenType.Array:
                    _logger.Debug($"'configsource has type of an array.", () => { });
                    // work array
                    var ja = configurationSource.Value<JArray>();
                    for (int i = 0; i < ja.Count; i++)
                    {
                        string configname = i.ToString();
                        if (ja[i].Type != JTokenType.String)
                        {
                            _logger.Warn($"Expected string value in format of providerkey,configkey", () => { });
                            continue;
                        }
                        string source = ja[i].ToString();
                        configs.Add(configname, source);
                    }
                    break;
                case JTokenType.String:
                    // check for string format
                    configs.Add("configsource", configurationSource.ToString());
                    break;
                default:
                    _logger.Warn($"Expected JSON should contain object, array or string. Ignoring", () => { });
                    break;
            }

            Dictionary<string, string> result = new Dictionary<string, string>();

            IConfigurationBuilder cb = new ConfigurationBuilder();

            // now load and process 
            foreach (var item in configs)
            {

                var pairs = item.Value.Split(',');
                var configprovider = pairs[0].Trim();
                string configkey = "${DEVICEID}";
                if (pairs.Length > 1) { configkey = pairs[1].Trim();  }

                // replace placeholders
                foreach (var replacement in replacements)
                {
                    configkey = configkey.Replace(replacement.Key, replacement.Value);
                }
                
                try
                {
                    using (var provider = _deviceConfigurationProviderFactory.Get(configprovider, null))
                    {
                        var newconfig = await provider.GetConfigurationAsync<string, string>(configkey);
                        _logger.Debug($"Retrieved from '{configprovider}':'{configkey}':\r\n:{newconfig}", () => { });
                        result.Add(item.Key, newconfig);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error($"Error while loading configuration source '{item.Key}': {e}", () => { });
                    throw e;
                }
            }

            return result;

            //throw new NotImplementedException("Code not finished!");
            

        }
    }
}
