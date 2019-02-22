using DeviceReader.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeviceReader.WebService.Models.DeviceApiModels
{
    public class DeviceApiModelSimple
    {
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "AgentStatus")]
        public string AgentStatus { get; set; }

        [JsonProperty(PropertyName = "ConnectionStatus")]
        public string ConnectionStatus { get; set; }

        [JsonProperty(PropertyName = "AcceptsInboundMessages")]
        public bool AcceptsInboundMessages { get; set; }
        
        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "Device" },
            { "$uri", "/api/devices/" + this.Id },
            { "$inbound", "/api/devices/" + this.Id + "/inbound" },
            { "$executables", "/api/devices/" + this.Id + "/executables" },
            { "$reset", "/api/devices/" + this.Id + "/reset" }
        };

        public DeviceApiModelSimple()
        {
            this.Id = string.Empty;
            this.AgentStatus = string.Empty;
            this.ConnectionStatus = string.Empty;
            this.AcceptsInboundMessages = false;            
        }

        public static DeviceApiModelSimple FromServiceModel(IDevice device)
        {
            if (device == null) return null;
            return new DeviceApiModelSimple()
            {
                Id = device.Id,
                AgentStatus = device.AgentStatus.ToString(),
                ConnectionStatus = device.ConnectionStatus.ToString(),
                AcceptsInboundMessages = device.AcceptsInboundMessages                
            };
        }

    }
}
