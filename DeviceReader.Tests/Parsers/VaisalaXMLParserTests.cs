namespace DeviceReader.Tests.Parsers
{
    using DeviceReader.Data;
    using DeviceReader.Diagnostics;
    using DeviceReader.Models;
    using DeviceReader.Parsers;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Resources;
    using System.Text;
    using System.Xml.Schema;
    using Xunit;
    using Xunit.Abstractions;

    public class VaisalaXMLParserTests_BasicTests
    {

        public const string KEY_AGENT_EXECUTABLE_ROOT = "executables:reader";
        public const string KEY_AGENT_FORMAT_CONFIG = KEY_AGENT_EXECUTABLE_ROOT + ":format_config";


        LoggingConfig lg;
        ILogger logger;

        private static global::System.Resources.ResourceManager resourceMan;
        private readonly ITestOutputHelper output;


        public VaisalaXMLParserTests_BasicTests(ITestOutputHelper output)
        {
            lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;

            Logger.DefaultLoggerFactory.AddProvider(new XUnitLoggerProvider(output));
            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

        }

        [Fact]
        public void FailIfXmlSchemaResourcesAreNotLoaded()
        {
            
            object obj = ResourceManager.GetObject("vaisala_v3_common");
            var byteArray = ((byte[])(obj));
            var xmlString = System.Text.UTF8Encoding.UTF8.GetString(byteArray);

            XmlSchema schema = XmlSchema.Read(new StringReader(xmlString), (a, b) => { });
            Assert.NotNull(schema);

            schema = null;

            obj = ResourceManager.GetObject("vaisala_v3_observation");
            byteArray = ((byte[])(obj));
            xmlString = System.Text.UTF8Encoding.UTF8.GetString(byteArray);

            schema = XmlSchema.Read(new StringReader(xmlString), (a, b) => { });
            Assert.NotNull(schema);
        }

        [Fact]
        public void FailIfEmptyPathDoesNotLoadResources()
        {
            IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.VaisalaXMLFormatParser(logger, null, null);
            Assert.NotNull(dummyparser);            
        }

        [Fact]
        public void FailIfFilesNotExist()
        {

            var dict = new Dictionary<string, string>
            {
                {KEY_AGENT_FORMAT_CONFIG+":SchemaPath", "."},
                {KEY_AGENT_FORMAT_CONFIG+":SchemaFiles:0", "vaisala_v3_common.xsd"},
                {KEY_AGENT_FORMAT_CONFIG+":SchemaFiles:1", "notexistingfile.json"}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();
            logger.Debug("Starting test", () => { });

            Assert.Throws<System.IO.FileNotFoundException>(() =>
           {
               IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.VaisalaXMLFormatParser(logger, KEY_AGENT_FORMAT_CONFIG, config);
           });
            
        }

        [Fact]
        public void AsStringParameter_Test_FailIfNotReturningAsString()
        {
            var jsonString = StringResources.Resources[VaisalaXMLFormatParser.DEFAULT_PARAMETER_TYPEMAP_FILE];

            jsonString = @"
{ ""SURFACE_STATE"": {
    ""DATATYPE"": ""string"",
    ""AS_STRING"": ""true""
  }
}
";
            var _conversionTable = JsonConvert.DeserializeObject<Dictionary<string, ParameterTypeMapRecord>>(jsonString);

            string value = "28.0";
            string parametername = "SURFACE_STATE";

            dynamic convertedValue = ObservationData.GetAsTyped(value, _conversionTable[parametername].DataType);
            
            Assert.IsType<String>(convertedValue);
            Assert.Equal("28.0", convertedValue);


            jsonString = @"
{ ""SURFACE_STATE"": {
    ""DATATYPE"": ""double_to_integer"",
    ""AS_STRING"": ""true""
  }
}
";
             _conversionTable = JsonConvert.DeserializeObject<Dictionary<string, ParameterTypeMapRecord>>(jsonString);

            
            convertedValue = ObservationData.GetAsTyped(value, _conversionTable[parametername].DataType, true);

            Assert.IsType<String>(convertedValue);
            Assert.Equal("28", convertedValue);



        }

        internal static global::System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DeviceReader.Properties.Resources", typeof(DeviceReader.Parsers.VaisalaXMLFormatParser).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

    }
}
