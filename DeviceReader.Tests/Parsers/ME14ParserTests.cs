using DeviceReader.Diagnostics;
using DeviceReader.Models;
using DeviceReader.Parsers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace DeviceReader.Tests.Parsers
{
    public class ME14ParserTests_BasicTests
    {

        public const string KEY_AGENT_EXECUTABLE_ROOT = "executables:reader";
        public const string KEY_AGENT_FORMAT_CONFIG = KEY_AGENT_EXECUTABLE_ROOT + ":format_config";


        LoggingConfig lg;
        ILogger logger;

        private readonly ITestOutputHelper output;


        public ME14ParserTests_BasicTests(ITestOutputHelper output)
        {
            lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;

            Logger.DefaultLoggerFactory.AddProvider(new XUnitLoggerProvider(output));
            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);

        }

        [Fact]
        public void FailIfNoSchemaFound()
        {
            var dict = new Dictionary<string, string>
            {
                {KEY_AGENT_FORMAT_CONFIG+":SchemaPath", "notexistingfile.json"}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();

            Assert.Throws<FileNotFoundException>(() =>
            {
                IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, KEY_AGENT_FORMAT_CONFIG, config);
            });
        }

        [Fact]
        public void TestForInvalidSchemaFile()
        {
            var dict = new Dictionary<string, string>
            {
                {KEY_AGENT_FORMAT_CONFIG+":SchemaPath", "Parsers"+Path.DirectorySeparatorChar+"me14-observations-with-errors.json"}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();

            Assert.Throws<Newtonsoft.Json.JsonReaderException>(() =>
            {
                IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, KEY_AGENT_FORMAT_CONFIG, config);
            });
        }


        [Fact]
        public void FailIfInputIsInvalid()
        {
            IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, null, null);

            // Too short message
            Assert.Throws<ArgumentException>(() =>
            {
                string input = "This is invalid message";
                var response = dummyparser.ParseAsync(input, CancellationToken.None).Result;
            });


        }

        [Fact]
        public void FailIfHeaderIsInvalid()
        {
            IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, null, null);
            // header missing or errorneous
            Assert.Throws<ArgumentException>(() =>
            {
                string input = "This is invalid message\r\nline2\r\nline3\r\nline4";
                var response = dummyparser.ParseAsync(input, CancellationToken.None).Result;
            });

            // header missing or errorneous, wrong count of fields
            Assert.Throws<ArgumentException>(() =>
            {
                string input = "a,b,c,d,e\r\nline2\r\nline3\r\nline4";
                var response = dummyparser.ParseAsync(input, CancellationToken.None).Result;
            });

            // Datetime format invalid
            Assert.Throws<ArgumentException>(() =>
            {
                string input = "2018-10-01 11:24s,b,c,d\r\nline2\r\nline3\r\nline4";
                var response = dummyparser.ParseAsync(input, CancellationToken.None).Result;                
            });

            
        }

        [Fact]
        public void FailIfDataInvalid()
        {
            IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, null, null);


            // Device id
            string input1 = "2018-10-01  10:00,01,ME14,Test\r\n01    10;02    20;03      1;99    20;\r\n=\r\n1234\r\n";
            var result1 = dummyparser.ParseAsync(input1, CancellationToken.None).Result;
            var jsonstring = JsonConvert.SerializeObject(result1, Formatting.Indented);
            logger.Debug($"{jsonstring}", () => { });
            Assert.True(result1[0].DeviceId == "Test");

            // only two should exist
            Assert.True(result1[0].Data.Count == 2);





        }

    }
}
