using DeviceReader.Diagnostics;
using DeviceReader.Models;
using DeviceReader.Parsers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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
                {KEY_AGENT_FORMAT_CONFIG, "notexistingfile.json"}                
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();

            Assert.Throws<FileNotFoundException>(() => {
                IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, KEY_AGENT_FORMAT_CONFIG, config);
            });            
        }

    }
}
