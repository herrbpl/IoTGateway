namespace DeviceReader.Parsers
{
    using DeviceReader.Diagnostics;
    using DeviceReader.Models;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;

    public class VaisalaXMLFormatParserOptions
    {
        public string SchemaPath { get; set; } = "";
        public List<string> SchemaFiles { get; set; } = new List<string> { "vaisala_v3_common.xsd", "vaisala_v3_observation.xsd" };
        public string ParameterTypeMapFile { get; set; } = "";
        public string TagNameTemplate { get; set; } = "{code}.{statname}.{statperiod}";
    }

    /// <summary>
    /// Class for vaisala xml parameter type conversion
    /// </summary>
    public class ParameterTypeMapRecord
    {        
        [JsonProperty("DATATYPE")]
        public string DataType { get; set; }
    }

    public class VaisalaXMLFormatParser: AbstractFormatParser<VaisalaXMLFormatParserOptions, string, Observation>
    {

        XmlSchemaSet schemas = new XmlSchemaSet();

        protected Dictionary<string, ParameterTypeMapRecord> _conversionTable;

        public VaisalaXMLFormatParser(ILogger logger, string optionspath, IConfigurationRoot configroot):
            base(logger, optionspath, configroot)
        {
            // load conversion table
            _conversionTable = new Dictionary<string, ParameterTypeMapRecord>();

            string conversionfilepath = (_options.SchemaPath != "" ? _options.SchemaPath + Path.DirectorySeparatorChar + _options.ParameterTypeMapFile : _options.ParameterTypeMapFile);


            string jsonString = "";

            _logger.Debug($"Schema path '{_options.SchemaPath}'", () => { });

            // if empty path, use built in resource
            if (conversionfilepath == "")
            {
                var byteArray = Properties.Resources.VaisalaXML_Parameter_Datatype_map;
                jsonString = System.Text.Encoding.UTF8.GetString(byteArray);
            }
            else
            {
                // if file not found, fail
                if (!File.Exists(conversionfilepath)) throw new FileNotFoundException(conversionfilepath);

                jsonString = File.ReadAllText(conversionfilepath);
            }

            // try to convert to structure
            try
            {
                _conversionTable = JsonConvert.DeserializeObject<Dictionary<string, ParameterTypeMapRecord>> (jsonString);
                _logger.Debug("Conversion table loaded!", () => { });
            }
            catch (Exception e)
            {
                _logger.Error($"{e}", () => { });
                throw e;
            }


            // load files and build schemaset

            if (_options.SchemaFiles != null )
            {
                foreach (var item in _options.SchemaFiles)
                {

                    string filepath = (_options.SchemaPath != "" ? _options.SchemaPath + Path.DirectorySeparatorChar + item : item);
                    if (!File.Exists(filepath))
                    {
                        if (_options.SchemaPath != "")
                        {
                            _logger.Error($"Schema not found: '{filepath}'", () => { });
                            throw new FileNotFoundException(filepath);
                        }
                        else
                        {
                            // use built in resource
                            // for now, not dynamically checking if resource exist, use built in names.
                            if (item.Equals("vaisala_v3_common.xsd"))
                            {
                                
                                var byteArray = Properties.Resources.vaisala_v3_common;
                                var xmlString = System.Text.Encoding.UTF8.GetString(byteArray);
                                var schema = XmlSchema.Read(new StringReader(xmlString), XmlValidationCallback);
                                schemas.Add(schema);

                            } 

                            // use built in resource
                            // for now, not dynamically checking if resource exist, use built in names.
                            else if (item.Equals("vaisala_v3_observation.xsd"))
                            {
                                var byteArray = Properties.Resources.vaisala_v3_observation;
                                var xmlString = System.Text.Encoding.UTF8.GetString(byteArray);
                                var schema = XmlSchema.Read(new StringReader(xmlString), XmlValidationCallback);
                                schemas.Add(schema);
                            }

                            else
                            {
                                _logger.Warn($"Unknown resource name '{item}'", () => { });
                            }

                        }
                    }
                    else
                    {

                        // read file
                        XmlTextReader schema_reader = new XmlTextReader(filepath);

                        // Schema 
                        XmlSchema schema = XmlSchema.Read(schema_reader, XmlValidationCallback);

                        // add to schema set
                        schemas.Add(schema);
                    }
                }
            }
        }


        public override Task<List<Observation>> ParseAsync(string input, CancellationToken cancellationToken)
        {

            

            if (input == null || input.Length == 0) return Task.FromResult(new List<Observation>());

            XDocument xmlDoc = XDocument.Parse(input);
            xmlDoc.Validate(schemas, XmlValidationCallback);

            string namespace_observation = "http://xml.vaisala.com/schema/internal/jx/observation/v3";
            XNamespace nso = XNamespace.Get(namespace_observation);

            string namespace_common = "http://xml.vaisala.com/schema/internal/jx/common/v3";
            XNamespace nsg = XNamespace.Get(namespace_common);

            //string stationid = xmlDoc.Root.Elements(nso + "observation").Elements(nso + "source").Elements(nsg + "id").FirstOrDefault().Value;


            var stationdata = (from observationRoot in xmlDoc.Root.Elements(nso + "observation")
                               let geodataroot = observationRoot.Elements(nso + "source").Elements(nsg + "geoPositionPoint").FirstOrDefault()
                               let stationid = observationRoot.Elements(nso + "source").Elements(nsg + "id").FirstOrDefault().Value
                               select new Observation
                               {
                                   DeviceId = observationRoot.Elements(nso + "source").Elements(nsg + "id").FirstOrDefault().Value
                                   ,
                                   GeoPositionPoint = new ObservationLocation
                                   {
                                       X = Double.Parse(geodataroot.Attributes("x").FirstOrDefault().Value.ToString(), CultureInfo.InvariantCulture),
                                       Y = Double.Parse(geodataroot.Attributes("y").FirstOrDefault().Value.ToString(), CultureInfo.InvariantCulture),
                                       Z = Double.Parse(geodataroot.Attributes("z").FirstOrDefault().Value.ToString(), CultureInfo.InvariantCulture),
                                       Srs = geodataroot.Attributes("srs").FirstOrDefault().Value.ToString()
                                   }
                                   //null
                                   /*
                                   observationRoot.Elements(nso + "source").Elements(nsg + "geoPositionPoint").FirstOrDefault().Attributes().ToDictionary(
                                       (x) => { return x.Name.LocalName; }, (d) => { return d.Value; })
                                   */
                                   // first non-null 
                                   , Timestamp = (from observations in xmlDoc.Root.Elements(nso + "observation").Elements(nso + "observation")
                                                  where observations.Element(nso + "dataValues") != null
                                                  from datavalues in observations.Elements(nso + "dataValues")
                                                  from datavalue in datavalues.Elements(nso + "dataValue")
                                                  select (DateTime)DateTime.Parse(datavalues.Attributes("timestamp").FirstOrDefault().Value).ToUniversalTime()

                                                  ).FirstOrDefault()


                                  ,
                                  Data = (from observations in xmlDoc.Root.Elements(nso + "observation").Elements(nso + "observation")
                                         where observations.Element(nso + "dataValues") != null
                                         from datavalues in observations.Elements(nso + "dataValues")
                                         from datavalue in datavalues.Elements(nso + "dataValue")
                                          select new ObservationData
                                          {
                                              Timestamp = (DateTime)DateTime.Parse(datavalues.Attributes("timestamp").FirstOrDefault().Value).ToUniversalTime()
                                              ,
                                              TagName = datavalue.Attributes("parameterName").FirstOrDefault().Value + "." +
                                                stationid.Replace(".", "_") + "." +
                                                (string)observations.Elements(nso + "source").Elements(nsg + "id").FirstOrDefault().Value + "." +
                                                datavalue.Attributes("code").FirstOrDefault().Value + "." +
                                                datavalue.Attributes("statisticName").FirstOrDefault().Value + "." +
                                                datavalue.Attributes("statisticPeriod").FirstOrDefault().Value

                                              ,
                                              Source = observations.Elements(nso + "source").Elements(nsg + "id").FirstOrDefault().Value
                                              ,
                                              Measure = datavalue.Attributes("parameterName").FirstOrDefault().Value
                                              ,
                                              Code = datavalue.Attributes("code").FirstOrDefault().Value
                                              ,
                                              Height = Double.Parse(datavalue.Attributes("heightMetres").FirstOrDefault().Value, CultureInfo.InvariantCulture)
                                              ,
                                              Unit = datavalue.Attributes("unitName").FirstOrDefault().Value
                                              ,
                                              QualityLevel = Int32.Parse(datavalue.Attributes("qualityLevel").FirstOrDefault().Value)
                                              ,
                                              QualityValue = Int32.Parse(datavalue.Attributes("qualityValue").FirstOrDefault().Value)
                                              ,
                                              StatName = datavalue.Attributes("statisticName").FirstOrDefault().Value
                                              ,
                                              StatPeriod = datavalue.Attributes("statisticPeriod").FirstOrDefault().Value
                                              ,
                                              Value = this.ConvertToDatatype(datavalue.Value, datavalue.Attributes("parameterName").FirstOrDefault().Value)

                                          }).ToList()
                              }).ToList();
            // update tagname

            foreach (var o1 in stationdata)
            {
                foreach (var o2 in o1.Data)
                {
                    o2.TagName = ObservationData.GetTagName(_options.TagNameTemplate, o2);
                }
            }
            
            return Task.FromResult(stationdata);
        }

        private dynamic ConvertToDatatype(string value, string parametername)
        {
            if (!_conversionTable.ContainsKey(parametername)) return (string)value;



            dynamic convertedValue = ObservationData.GetAsTyped(value, _conversionTable[parametername].DataType);

            /*
            // data value type conversion
            if (_conversionTable[parametername].DataType == "double")
            {
                double res;
                if (Double.TryParse(value.Replace(".", ","), out res))
                {
                    convertedValue = res;
                }
                else
                {
                    _logger.Warn($"Unable to convert value '{value}' to double", () => { });
                    return null;
                }
            }
            else if (_conversionTable[parametername].DataType == "integer")
            {
                int res;
                if (Int32.TryParse(value, out res))
                {
                    convertedValue = res;
                }
                else
                {
                    _logger.Warn($"Unable to convert value '{value}' to int32", () => { });
                    return null;
                }
            }
            else if (_conversionTable[parametername].DataType == "boolean")
            {
                bool hasres = false;

                var bvalue = value.ToLowerInvariant();

                var knownTrue = new HashSet<string> { "true", "t", "yes", "y", "1", "-1" };
                var knownFalse = new HashSet<string> { "false", "f", "no", "n", "0" };

                if (knownTrue.Contains(bvalue)) { convertedValue = true; hasres = true; }
                if (knownFalse.Contains(bvalue)) { convertedValue = false; hasres = true; }


                if (!hasres)
                {
                    _logger.Warn($"Unable to convert value '{value}' to boolean", () => { });
                    return null;
                }
            }
            else // string
            {
                convertedValue = (string)value;
            }
            */
            return convertedValue;
        }

        private void XmlValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
                _logger.Warn($"{args.Message}", () => { });
            else if (args.Severity == XmlSeverityType.Error)
            {                
                _logger.Error($"{args.Message}", () => { });
                throw new InvalidDataException(args.Message);
            }
        }
    }    
}
