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


        private IConfiguration GetME14CombinedConfig()
        {
            var dict = new Dictionary<string, string>
            {
                {KEY_AGENT_FORMAT_CONFIG+":Mode", ME14_Mode.Combined.ToString()}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();
            return config;
        }

        private IConfiguration GetME14DSCConfig()
        {
            var dict = new Dictionary<string, string>
            {
                {KEY_AGENT_FORMAT_CONFIG+":Mode", ME14_Mode.DCS_DST.ToString()}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();
            return config;
        }

        [Fact]
        public void MECombinedLoading_TestInvalidId()
        {
                        
            IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, KEY_AGENT_FORMAT_CONFIG, GetME14CombinedConfig());

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                string input = "2018-10-01  10:00,05,ME14,Test\r\n01    10;36    01;14     1;30    20;72    20;\r\n";
                var response = dummyparser.ParseAsync(input, CancellationToken.None).Result;
            });

            
        }

        [Fact]
        public void MECombinedLoading_Test01()
        {
            
            IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, KEY_AGENT_FORMAT_CONFIG, GetME14CombinedConfig());


            // 01 - DRS511_1 and DRS511_2 in message
            // 02 - DRS511_3 and DRS511_4 in message
            // 03 - DSC211_1/DST111_1 in message
            // 04 - DSC211_2/DST111_2 in message
            string input1 = "2018-10-01  10:00,01,ME14,Test\r\n01    10;36    01;14     1;30    20;72    20;\r\n";
            var result1 = dummyparser.ParseAsync(input1, CancellationToken.None).Result;
            var jsonstring = JsonConvert.SerializeObject(result1, Formatting.Indented);
            logger.Debug($"{jsonstring}", () => { });
            Assert.True(result1[0].DeviceId == "Test");
            Assert.True(result1[0].Data[0].TagName == "TA.MEAN.PT1M.HMP155_1");
            Assert.True(result1[0].Data[1].TagName == "SST.VALUE.PT1M.DRS511_1");
            Assert.True(result1[0].Data[2].TagName == "BATTERYV.VALUE.PT1S.PMU701_1");
            Assert.True(result1[0].Data[3].TagName == "TSURF.VALUE.PT1M.DRS511_1");            

        }

        [Fact]
        public void MECombinedLoading_Test02()
        {

            IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, KEY_AGENT_FORMAT_CONFIG, GetME14CombinedConfig());


            // 01 - DRS511_1 and DRS511_2 in message
            // 02 - DRS511_3 and DRS511_4 in message
            // 03 - DSC211_1/DST111_1 in message
            // 04 - DSC211_2/DST111_2 in message            
            var input1 = "2018-10-01  10:00,02,ME14,Test\r\n01    10;36    01;14     1;30    20;72    20;\r\n";
            var result1 = dummyparser.ParseAsync(input1, CancellationToken.None).Result;
            var jsonstring = JsonConvert.SerializeObject(result1, Formatting.Indented);
            logger.Debug($"{jsonstring}", () => { });
            Assert.True(result1[0].DeviceId == "Test");

            Assert.Equal("TA.MEAN.PT1M.HMP155_1", result1[0].Data[0].TagName);
            Assert.Equal("SST.VALUE.PT1M.DRS511_3", result1[0].Data[1].TagName);
            Assert.Equal("BATTERYV.VALUE.PT1S.PMU701_1", result1[0].Data[2].TagName);
            Assert.Equal("TSURF.VALUE.PT1M.DRS511_3", result1[0].Data[3].TagName);

        }

        [Fact]
        public void MECombinedLoading_Test03()
        {

            IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, KEY_AGENT_FORMAT_CONFIG, GetME14CombinedConfig());


            // 01 - DRS511_1 and DRS511_2 in message
            // 02 - DRS511_3 and DRS511_4 in message
            // 03 - DSC211_1/DST111_1 in message
            // 04 - DSC211_2/DST111_2 in message            
            var input1 = "2018-10-01  10:00,03,ME14,Test\r\n01    10;36    01;14     1;30    20;72    20;\r\n";
            var result1 = dummyparser.ParseAsync(input1, CancellationToken.None).Result;
            var jsonstring = JsonConvert.SerializeObject(result1, Formatting.Indented);
            logger.Debug($"{jsonstring}", () => { });
            Assert.True(result1[0].DeviceId == "Test");
            Assert.Equal("TA.MEAN.PT1M.HMP155_1", result1[0].Data[0].TagName);
            Assert.Equal("SST.VALUE.PT30S.DSC211_1", result1[0].Data[1].TagName);
            Assert.Equal("BATTERYV.VALUE.PT1S.PMU701_1", result1[0].Data[2].TagName);
            Assert.Equal("WLT.VALUE.PT30S.DSC211_1", result1[0].Data[3].TagName );

        }

        [Fact]
        public void MECombinedLoading_Test04()
        {

            IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, KEY_AGENT_FORMAT_CONFIG, GetME14CombinedConfig());


            // 01 - DRS511_1 and DRS511_2 in message
            // 02 - DRS511_3 and DRS511_4 in message
            // 03 - DSC211_1/DST111_1 in message
            // 04 - DSC211_2/DST111_2 in message            
            var input1 = "2018-10-01  10:00,04,ME14,Test\r\n01    10;36    01;14     1;30    20;72    20;\r\n";
            var result1 = dummyparser.ParseAsync(input1, CancellationToken.None).Result;
            var jsonstring = JsonConvert.SerializeObject(result1, Formatting.Indented);
            logger.Debug($"{jsonstring}", () => { });
            Assert.True(result1[0].DeviceId == "Test");
            Assert.Equal("TA.MEAN.PT1M.HMP155_1", result1[0].Data[0].TagName);
            Assert.Equal("SST.VALUE.PT30S.DSC211_2", result1[0].Data[1].TagName);
            Assert.Equal("BATTERYV.VALUE.PT1S.PMU701_1", result1[0].Data[2].TagName);
            Assert.Equal("WLT.VALUE.PT30S.DSC211_2", result1[0].Data[3].TagName);


        }

        [Fact]
        public void MEDSCLoading_Test()
        {

            IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, KEY_AGENT_FORMAT_CONFIG, GetME14DSCConfig());


            // 01 - DRS511_1 and DRS511_2 in message
            // 02 - DRS511_3 and DRS511_4 in message
            // 03 - DSC211_1/DST111_1 in message
            // 04 - DSC211_2/DST111_2 in message            
            var input1 = "2018-10-01  10:00,03,ME14,Test\r\n01    10;36    01;14     1;30    20;72    20;\r\n";
            var result1 = dummyparser.ParseAsync(input1, CancellationToken.None).Result;
            var jsonstring = JsonConvert.SerializeObject(result1, Formatting.Indented);
            logger.Debug($"{jsonstring}", () => { });
            Assert.True(result1[0].DeviceId == "Test");
            Assert.Equal("TA.MEAN.PT1M.HMP155_1", result1[0].Data[0].TagName);
            Assert.Equal("SST.VALUE.PT30S.DSC211_1", result1[0].Data[1].TagName);
            Assert.Equal("BATTERYV.VALUE.PT1S.PMU701_1", result1[0].Data[2].TagName);
            Assert.Equal("WLT.VALUE.PT30S.DSC211_1", result1[0].Data[3].TagName);

        }

    }
}
