using DeviceReader.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DeviceReader.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ObservationLocation: ICloneable
    {
        [JsonProperty("x")]
        public double X { get; set; }
        [JsonProperty("y")]
        public double Y { get; set; }
        [JsonProperty("z")]
        public double Z { get; set; }
        [JsonProperty("srs")]
        public string Srs { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ObservationData: ICloneable
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

        /// <summary>
        /// Converting given string to specific type
        /// TODO: make sure it works for all regions, esp. double stuff
        /// TODO: add other common data types, like date etc.
        /// TODO: This should live someplace else. It is not a good fit for this function
        /// </summary>
        /// <param name="datavalue">value to be converted</param>
        /// <param name="dataType">data type to convert to, currently double, integer, boolean, string are accepted</param>
        /// <param name="toStringAfter">Recasts results as string after conversion</param>
        /// <param name="throwiffail"></param>
        /// <returns></returns>
        public static dynamic GetAsTyped(string datavalue, string dataType, bool toStringAfter = false, bool throwiffail = false)
        {
            dynamic convertedValue = null;

            if (dataType == "double")
            {
                // https://devio.wordpress.com/2009/10/15/parsing-culture-invariant-floating-point-numbers/
                double f;
                
                if (double.TryParse(datavalue, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out f))
                {
                    convertedValue = f;
                }
                else if (double.TryParse(datavalue, out f))
                {
                    convertedValue = f;
                }
                else
                {
                    if (throwiffail) throw new ArgumentException($"Unable to convert value '{datavalue}' to double");
                }                
            }
            else if (dataType == "integer")
            {
                int res;
                if (Int32.TryParse(datavalue, out res))
                {
                    convertedValue = res;
                }
                else
                {
                    if (throwiffail) throw new ArgumentException($"Unable to convert value '{datavalue}' to int32");
                                        
                }
            }
            // this is a hack. Correct way should be building conversion functions with Roslyn or smth so arbitary expression could be used
            // see https://www.strathweb.com/2018/01/easy-way-to-create-a-c-lambda-expression-from-a-string-with-roslyn/
            else if (dataType == "double_to_integer")
            {                

                // https://devio.wordpress.com/2009/10/15/parsing-culture-invariant-floating-point-numbers/
                double f;

                if (double.TryParse(datavalue, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out f))
                {
                    convertedValue = (int)f;
                }
                else if (double.TryParse(datavalue, out f))
                {
                    convertedValue = (int)f;
                }                
                else
                {
                    if (throwiffail) throw new ArgumentException($"Unable to convert value '{datavalue}' to int32");

                }
            }
            // another hack. Just have no time to properly engineer and refactor this at moment.
            else if (dataType == "string_to_nwsnumber")
            {
                Dictionary<string, string> _map = new Dictionary<string, string>()
                {
                    { "C","0" },
                    { "P","1" },
                    { "L","2" },
                    { "R","3" },
                    { "S","4" },
                    { "IP","5" },
                    { "H","7" },
                    { "ZL","11" },
                    { "ZR","12" }                    
                };
                // remove + and - if exists
                datavalue = datavalue.Replace("-", "").Replace("+", "").Trim();
                if (!_map.ContainsKey(datavalue))
                {
                    if (throwiffail) throw new ArgumentException($"Unable to convert value '{datavalue}' to NWS code");

                }
                else
                {
                    convertedValue = _map[datavalue];
                }
            }
            else if (dataType == "boolean")
            {
                bool hasres = false;

                var value = datavalue.ToLowerInvariant();

                var knownTrue = new HashSet<string> { "true", "t", "yes", "y", "1", "-1" };
                var knownFalse = new HashSet<string> { "false", "f", "no", "n", "0" };

                if (knownTrue.Contains(value)) { convertedValue = true; hasres = true; }
                if (knownFalse.Contains(value)) { convertedValue = false; hasres = true; }


                if (!hasres)
                {
                    if (throwiffail) throw new ArgumentException($"Unable to convert value '{datavalue}' to boolean");                    
                }
            }
            else // string
            {
                convertedValue = (string)datavalue;
            }

            if (toStringAfter)
            {
                convertedValue = $"{convertedValue}";
            }

            return convertedValue;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Observation: IMergeable<Observation>, ICloneable
    {
        [JsonProperty("deviceid")]
        public string DeviceId { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("devicemodel")]
        public string DeviceModel { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty("location")]
        public ObservationLocation GeoPositionPoint { get; set; } = new ObservationLocation();

        /// <summary>
        /// Data in this observation. 
        /// TODO: Consider converting to dictionary - benefit is having single tagname represented once.
        /// </summary>
        [JsonProperty("data")]
        public List<ObservationData> Data { get; set; } = new List<ObservationData>();

        public object Clone()
        {
            var result = (Observation)this.MemberwiseClone();
            result.GeoPositionPoint = (ObservationLocation)this.GeoPositionPoint?.Clone();
            result.Data = new List<ObservationData>();
            foreach (var item in Data)
            {
                result.Data.Add((ObservationData)item.Clone());
            }
            return result;
        }

        /// <summary>
        /// Merge two observation records. Existing obbject is not changed
        /// </summary>
        /// <param name="mergeWith"></param>
        /// <param name="mergeOptions"></param>
        /// <returns>Merged observation class instance</returns>
        public Observation Merge(Observation mergeWith, MergeOptions mergeOptions = null)
        {
            if (mergeOptions == null) mergeOptions = MergeOptions.DefaultMergeOptions;

            if (mergeWith == null || this.Equals(mergeWith))
            {
                return this;
            }
            


            Observation result = null;
            if (mergeOptions.MergeConflicAction == MergeConflicAction.KeepFirst || 
                mergeOptions.MergeConflicAction == MergeConflicAction.Throw)
            {
                result = (Observation)this.Clone();
                //result = this;
            } else
            {
                result = (Observation)mergeWith.Clone();
                //result = mergeWith;
            }
            
            if (!string.IsNullOrEmpty(mergeOptions.PrefixFirst)) {
                // all items in 
                foreach (var item in result.Data)
                {                    
                    item.TagName = mergeOptions.PrefixFirst + "." + item.TagName;                    
                }
            }

            var prefixSecond = "";
            if (!string.IsNullOrEmpty(mergeOptions.PrefixSecond))
            {
                prefixSecond = mergeOptions.PrefixSecond + ".";
            }
            
            
            foreach (var item in mergeWith.Data)
            {
                var cp = (ObservationData)item.Clone();                
                cp.TagName = prefixSecond + cp.TagName;

                if (result.Data.Where(od => od.TagName == cp.TagName).Any())
                {
                    var first = result.Data.Where(od => od.TagName == cp.TagName).FirstOrDefault();

                    if (mergeOptions.MergeConflicAction == MergeConflicAction.Throw)
                    {
                        throw new MergeConflictException($"{cp.TagName}");
                    } else if (mergeOptions.MergeConflicAction == MergeConflicAction.KeepSecond)
                    {
                        result.Data.Remove(first);
                        result.Data.Add(cp);
                    } 
                } else
                {
                    result.Data.Add(cp);
                }

            }
            
            return result;
        }
    }

}
