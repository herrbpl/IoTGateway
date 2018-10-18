using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ObservationLocation
    {
        [JsonProperty("x")]
        public double X { get; set; }
        [JsonProperty("y")]
        public double Y { get; set; }
        [JsonProperty("z")]
        public double Z { get; set; }
        [JsonProperty("srs")]
        public string Srs { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ObservationData
    {        
        public DateTime Timestamp { get; set; }

        [JsonProperty("tagname")]
        public string TagName { get; set; }
        [JsonProperty("value")]
        public dynamic Value { get; set; }
        [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
        public string Source { get; set; }
        [JsonProperty("measure", NullValueHandling = NullValueHandling.Ignore)]
        public string Measure { get; set; }
        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public string Code { get; set; }
        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
        public double Height { get; set; }
        [JsonProperty("unit", NullValueHandling = NullValueHandling.Ignore)]
        public string Unit { get; set; }
        [JsonProperty("qualitylevel", NullValueHandling = NullValueHandling.Ignore)]
        public double QualityLevel { get; set; }
        [JsonProperty("qualityvalue", NullValueHandling = NullValueHandling.Ignore)]
        public double QualityValue { get; set; }
        [JsonProperty("statname", NullValueHandling = NullValueHandling.Ignore)]
        public string StatName { get; set; }
        [JsonProperty("statperiod", NullValueHandling = NullValueHandling.Ignore)]
        public string StatPeriod { get; set; }   
        
        /// <summary>
        /// Puts together tag name from observation elements.
        /// TODO: on first use, generate and complile code to execute it without extra looping. Mainly for speed.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="observation"></param>
        /// <returns></returns>
        public static string GetTagName(string template, ObservationData observation)
        {
            var result = template;
            var resolver = new DefaultContractResolver();
            
            
            foreach(var property in observation.GetType().GetProperties())
            {
                string part = "";

                foreach(var a in property.GetCustomAttributesData())
                {                    
                    if (a.AttributeType.Name == "JsonPropertyAttribute")
                    {
                        if (a.ConstructorArguments.Count > 0)
                        {
                            part = a.ConstructorArguments[0].Value.ToString();
                        }
                    }
                }
                if (part == "") part = property.Name;
                //JsonPropertyAttribute.GetCustomAttributes()
                //var part = resolver.GetResolvedPropertyName(property.Name);
                
                var value = property.GetValue(observation)?.ToString();
                if (part != null)
                {
                    result = result.Replace("{" + part + "}", value);
                }
            }
            return result;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Observation
    {
        [JsonProperty("deviceid")]
        public string DeviceId { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty("location")]
        public ObservationLocation GeoPositionPoint { get; set; }
        [JsonProperty("data")]
        public List<ObservationData> Data { get; set; }
    }
}
