namespace DeviceReader.Parsers
{
    using DeviceReader.Data;
    using DeviceReader.Diagnostics;
    using DeviceReader.Models;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
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
        public string TagNameTemplate { get; set; } = "{code}.{statname}.{statperiod}.{source}";
    }

    /// <summary>
    /// Class for vaisala xml parameter type conversion
    /// </summary>
    public class ParameterTypeMapRecord
    {        
        [JsonProperty("DATATYPE")]
        public string DataType { get; set; }

        [JsonProperty("AS_STRING", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool AsString { get; set; } = false;
    }

    public class VaisalaXMLFormatParser: AbstractFormatParser<VaisalaXMLFormatParserOptions, string, Observation>
    {
        internal const string DEFAULT_PARAMETER_TYPEMAP_FILE = "VaisalaXML-Parameter-Datatype-map.json";
        XmlSchemaSet schemas = new XmlSchemaSet();

        protected Dictionary<string, ParameterTypeMapRecord> _conversionTable;

        public VaisalaXMLFormatParser(ILogger logger, string optionspath, IConfiguration configroot):
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
                jsonString = StringResources.Resources[DEFAULT_PARAMETER_TYPEMAP_FILE];
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

                            if (StringResources.Exists(item))
                            {
                                var xmlString = StringResources.Resources[item];
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



            dynamic convertedValue = ObservationData.GetAsTyped(value, _conversionTable[parametername].DataType, _conversionTable[parametername].AsString);

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
