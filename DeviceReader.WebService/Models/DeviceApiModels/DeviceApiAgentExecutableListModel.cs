using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeviceReader.Devices;
using Newtonsoft.Json;

namespace DeviceReader.WebService.Models.DeviceApiModels
{
    public class DeviceApiAgentExecutableListModel
    {
        private string deviceId;

        [JsonProperty(PropertyName = "executables")]
        public List<DeviceApiAgentExecutableModel> Executables { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "AgentExecutableList" },
            { "$uri", $"/api/devices/{deviceId}/executables" }
        };

        public DeviceApiAgentExecutableListModel()
        {
            //this._device = device;
            this.Executables = new List<DeviceApiAgentExecutableModel>();
        }

        public static DeviceApiAgentExecutableListModel FromServiceModel(IDevice device)
        {
            if (device == null) return null;

            var model = new DeviceApiAgentExecutableListModel()
            {
                deviceId = device.Id,
                Executables = device.AgentExecutables.Select((x) =>
                {
                    return DeviceApiAgentExecutableModel.FromServiceModel(device, x.Name);
                }).ToList()
            };
            return model;

        }
    }
}
