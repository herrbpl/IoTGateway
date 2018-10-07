using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeviceReader.Devices;
using Newtonsoft.Json;

namespace DeviceReader.WebService.Models.DeviceApiModels
{
    public class DeviceApiListModel
    {
        [JsonProperty(PropertyName = "Items")]
        public List<DeviceApiModelSimple> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "DeviceList" },
            { "$uri", "/devices" }
        };

        public DeviceApiListModel()
        {
            this.Items = new List<DeviceApiModelSimple>();
        }

        public static DeviceApiListModel FromServiceModel(IEnumerable<IDevice> devices)
        {
            if (devices == null) return null;
            return new DeviceApiListModel()
            {
                Items = devices.Select(DeviceApiModelSimple.FromServiceModel).Where(x => x != null).ToList()
            };
        }
    }
}
