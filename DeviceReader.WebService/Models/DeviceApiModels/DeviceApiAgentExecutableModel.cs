using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeviceReader.Devices;
using DeviceReader.Models;
using Newtonsoft.Json;

namespace DeviceReader.WebService.Models.DeviceApiModels
{
    public class DeviceApiAgentExecutableModel
    {
        [JsonIgnore]
        public string DeviceId { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string AgentExecutableName { get; set; }                

        [JsonProperty(PropertyName = "InboundMeasurements")]
        public List<MeasurementMetadataRecord> InboundMeasurements { get; set; }

        [JsonProperty(PropertyName = "OutboundMeasurements")]
        public List<MeasurementMetadataRecord> OutboundMeasurements { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "AgentExecutable" },
            { "$uri", $"/api/devices/{DeviceId}/executables/{AgentExecutableName}" }
        };

        private DeviceApiAgentExecutableModel()
        {
            InboundMeasurements = new List<MeasurementMetadataRecord>();
            OutboundMeasurements = new List<MeasurementMetadataRecord>();
        }

        public static DeviceApiAgentExecutableModel FromServiceModel(IDevice device, string AgentExecutableName)
        {
            if (device == null) return null;

            var agentexecutableModel = new DeviceApiAgentExecutableModel();
            agentexecutableModel.DeviceId = device.Id;
            agentexecutableModel.AgentExecutableName = AgentExecutableName;

            var x = device.AgentExecutables.Where(t => t.Name == AgentExecutableName).FirstOrDefault();
            if (x != null)
            {
                agentexecutableModel.InboundMeasurements = x.InboundMeasurements.ToList();
                agentexecutableModel.OutboundMeasurements = x.OutboundMeasurements.ToList();
            }

            return agentexecutableModel;
        }
    }
}
