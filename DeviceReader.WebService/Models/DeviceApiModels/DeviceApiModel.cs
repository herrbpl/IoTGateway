using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DeviceReader.Devices;
using Newtonsoft.Json;

namespace DeviceReader.WebService.Models.DeviceApiModels
{
    public class DeviceApiModel: DeviceApiModelSimple
    {
        /*
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "AgentStatus")]
        public string AgentStatus { get; set; }

        [JsonProperty(PropertyName = "ConnectionStatus")]
        public string ConnectionStatus { get; set; }

        [JsonProperty(PropertyName = "AcceptsInboundMessages")]
        public bool AcceptsInboundMessages { get; set; }
        */

        
        /// https://stackoverflow.com/questions/31199510/serialize-object-to-json-that-already-contains-one-json-property
         [JsonProperty(PropertyName = "Config")]
        //[JsonConverter(typeof(SpecialJsonConverter))]
        public string Config { get; set; }
        
        /*
        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Device" },
            { "$uri", "/devices/" + this.Id }
        };
        */
        

        public DeviceApiModel()
        {
            this.Id = string.Empty;
            this.AgentStatus = string.Empty;
            this.ConnectionStatus = string.Empty;
            this.AcceptsInboundMessages = false;
            this.Config = string.Empty;
        }

        public new static DeviceApiModel FromServiceModel(IDevice device)
        {
            if (device == null) return null;
            return new DeviceApiModel() {
                Id = device.Id,
                AgentStatus = device.AgentStatus.ToString(),
                ConnectionStatus = device.ConnectionStatus.ToString(),
                AcceptsInboundMessages = device.AcceptsInboundMessages,
                // Temporarily disable this property as it would require configuration conversion to JSON from c# internal format. No time now
                // Config = device.AgentConfig
                Config = "{}"
            };
        }

    }
    /// https://stackoverflow.com/questions/31199510/serialize-object-to-json-that-already-contains-one-json-property
    /// 
    public sealed class SpecialJsonConverter : JsonConverter
    {
        public override bool CanRead => false;
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var reader = new JsonTextReader(new StringReader(value.ToString()));
            writer.WriteToken(reader);
        }
    }
}
