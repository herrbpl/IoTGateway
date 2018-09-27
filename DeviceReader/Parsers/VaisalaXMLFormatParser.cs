namespace DeviceReader.Parsers
{
    using DeviceReader.Diagnostics;
    using DeviceReader.Models;
    using Microsoft.Extensions.Configuration;
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
    }

    public class VaisalaXMLFormatParser: AbstractFormatParser<VaisalaXMLFormatParserOptions, string, Observation>
    {

        XmlSchemaSet schemas = new XmlSchemaSet();

        public VaisalaXMLFormatParser(ILogger logger, string optionspath, IConfigurationRoot configroot):
            base(logger, optionspath, configroot)
        {
            // load files and build schemaset
            
            if (_options != null && _options.SchemaFiles == null)
            {
                foreach (var item in _options.SchemaFiles)
                {

                    string filepath = (_options.SchemaPath != "" ? _options.SchemaPath + Path.DirectorySeparatorChar + item : item);
                    if (!File.Exists(filepath))
                    {
                        _logger.Error($"Schema not found: '{filepath}'", () => { });
                        throw new FileNotFoundException(filepath);
                    }

                    // read file
                    XmlTextReader schema_reader = new XmlTextReader(filepath);

                    // Schema 
                    XmlSchema schema = XmlSchema.Read(schema_reader, XmlValidationCallback);

                    // add to schema set
                    schemas.Add(schema);
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
                              let  geodataroot = observationRoot.Elements(nso + "source").Elements(nsg + "geoPositionPoint").FirstOrDefault()
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
                                             Measure= datavalue.Attributes("parameterName").FirstOrDefault().Value
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
                                             Value = datavalue.Value

                                         }).ToList()
                              }).ToList();

            return Task.FromResult(stationdata);
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
