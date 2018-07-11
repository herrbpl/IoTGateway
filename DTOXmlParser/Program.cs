using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using Newtonsoft;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;

namespace DTOXmlParser
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // common
                XmlTextReader reader_common = new XmlTextReader("vaisala_v3_common.xsd");
                XmlSchema schema_common = XmlSchema.Read(reader_common, ValidationCallback);
                //schema_common.Write(Console.Out);

                // observation
                XmlTextReader reader_observation = new XmlTextReader("vaisala_v3_observation.xsd");
                XmlSchema schema_observation = XmlSchema.Read(reader_observation, ValidationCallback);
               // schema_observation.Write(Console.Out);

                // create schema set
                XmlSchemaSet schemas = new XmlSchemaSet();
                schemas.Add(schema_common);
                schemas.Add(schema_observation);


                // read data itself
                XDocument xmlDoc = XDocument.Load("amtij.xml");
                                
                // validate xml
                Console.WriteLine("Validating source");
                bool errors = false;
                xmlDoc.Validate(schemas, (o, e) =>
                {
                    Console.WriteLine("{0}", e.Message);
                    errors = true;
                });
                Console.WriteLine("xmlDoc {0}", errors ? "did not validate" : "validated");

                // turn to json
                // structure?
                /*
                 body :{ 
                    StationId: ddd,
                    Location: { x:23.3, y:54.3, z:52.0, srs:"EPSG:4326" }
                    Observations: [
                        {
                           TimeStamp: "2018-07-04T12:50:03.001Z"
                           Source: WMT700_1,
                           Code: "WD"
                           Path: "WMT700_1.WIND_DIRECTION.MEAN.PT10M.DEGREES"
                           H:8.0
                           qv:"8500"
                           ql:"0"
                           value: 359.0
                           unit: "DEGREES"
                           metric: "WIND_DIRECTION"
                        } , ...
                    ]
                 }                  
                  
                 */

                string namespace_observation = "http://xml.vaisala.com/schema/internal/jx/observation/v3";
                XNamespace nso = XNamespace.Get(namespace_observation);

                string namespace_common = "http://xml.vaisala.com/schema/internal/jx/common/v3";
                XNamespace nsg = XNamespace.Get(namespace_common);

                string stationid = xmlDoc.Root.Elements(nso + "observation").Elements(nso + "source").Elements(nsg + "id").FirstOrDefault().Value;

                var stationdata = from observationRoot in xmlDoc.Root.Elements(nso + "observation")
                                  select new
                                  {
                                      deviceId = observationRoot.Elements(nso + "source").Elements(nsg + "id").FirstOrDefault().Value
                                      , geoPositionPoint = observationRoot.Elements(nso + "source").Elements(nsg + "geoPositionPoint").FirstOrDefault().Attributes().ToDictionary(
                                          (x) => { return x.Name.LocalName; }, (d) => { return d.Value;  })
                                      , data = from observations in xmlDoc.Root.Elements(nso + "observation").Elements(nso + "observation")
                                               where observations.Element(nso + "dataValues") != null
                                               from datavalues in observations.Elements(nso + "dataValues")
                                               from datavalue in datavalues.Elements(nso + "dataValue")
                                               select new
                                               {
                                                   timestamp = (DateTime)DateTime.Parse(datavalues.Attributes("timestamp").FirstOrDefault().Value).ToUniversalTime()
                                                   ,
                                                   tagname = datavalue.Attributes("parameterName").FirstOrDefault().Value+"."+
                                                     stationid.Replace(".", "_")+"."+
                                                     (string)observations.Elements(nso + "source").Elements(nsg + "id").FirstOrDefault().Value + "." +
                                                     datavalue.Attributes("code").FirstOrDefault().Value + "." +
                                                     datavalue.Attributes("statisticName").FirstOrDefault().Value + "." +
                                                     datavalue.Attributes("statisticPeriod").FirstOrDefault().Value

                                                   ,
                                                   source = (string)observations.Elements(nso + "source").Elements(nsg + "id").FirstOrDefault().Value
                                                   ,
                                                   metric = (string)datavalue.Attributes("parameterName").FirstOrDefault().Value
                                                   ,
                                                   code = (string)datavalue.Attributes("code").FirstOrDefault().Value
                                                   ,
                                                   height = (double)Double.Parse(datavalue.Attributes("heightMetres").FirstOrDefault().Value, CultureInfo.InvariantCulture)
                                                   ,
                                                   unit = (string)datavalue.Attributes("unitName").FirstOrDefault().Value
                                                   ,
                                                   ql = (int)Int32.Parse(datavalue.Attributes("qualityLevel").FirstOrDefault().Value)
                                                   ,
                                                   qv = (int)Int32.Parse(datavalue.Attributes("qualityValue").FirstOrDefault().Value)
                                                   ,
                                                   statname = (string)datavalue.Attributes("statisticName").FirstOrDefault().Value
                                                   ,
                                                   statperiod = (string)datavalue.Attributes("statisticPeriod").FirstOrDefault().Value
                                                   , value = (string)datavalue.Value
               
                                               }
                                  };
               

                
                JObject jo = JObject.FromObject(new { body = stationdata });
                
                
                Console.WriteLine(jo);


            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }



            Console.ReadLine();
        }

        static void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
                Console.Write("WARNING: ");
            else if (args.Severity == XmlSeverityType.Error)
                Console.Write("ERROR: ");

            Console.WriteLine(args.Message);
        }
    }
}
