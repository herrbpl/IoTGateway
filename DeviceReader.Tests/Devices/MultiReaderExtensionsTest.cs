using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using DeviceReader.Models;
using DeviceReader.Extensions;
using DeviceReader.Parsers;
using DeviceReader.Protocols;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Autofac;

namespace DeviceReader.Tests.Devices
{

    public class MultiReaderExtensionsTest
    {
        public const string KEY_AGENT_EXECUTABLE_MULTIREADER = "executables:reader:multireader";

        private readonly ITestOutputHelper output;
        LoggingConfig lg;
        ILogger logger;

        public MultiReaderExtensionsTest(ITestOutputHelper output)
        {
            lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;
            this.output = output;
            Logger.DefaultLoggerFactory.AddProvider(new XUnitLoggerProvider(output));
            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);
        }

        [Fact]
        public void MultiReader_TestMergedMultiRead()
        {
            var multireader = new MultiReader<Observation>();

            for (int i = 0; i < 5; i++)
            {
                var reader = new MultiReaderRow<Observation>()
                {
                    FormatParser = new MockParser(),
                    ProtocolReader = new MockProtocolReader($"Test{i}")
                };

                multireader.AddReader<string>($"Test{i}", reader);
            }
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var result = multireader.ReadAsyncMerged<Observation>("Test0", CancellationToken.None).Result;
            stopwatch.Stop();
            logger.Debug($"Call time took {stopwatch.ElapsedMilliseconds} ms", () => { });
            var c = result.Count;
            Assert.Equal(1, c);
            foreach (var item in result)
            {
                logger.Debug($"{ JsonConvert.SerializeObject(item, Formatting.Indented) }", () => { });
            }

        }

        [Fact]
        public void MultiReader_TestLoadFromConfig()
        {         

            var jsonstring = @"
{
	'executables': {
		'reader': {
			'multireader': {
				's1': {    
                    'canfail': 'true',
					'format': 'me14',
					'protocol': 'me14',                   
					'protocol_config': {
						'Port':  5000,
						'HostName': '127.0.0.1',
						'Timeout': 10
					}
				},
				's2': {                
                    'canfail': 'true',
					'format': 'me14',
                    'format_config': {
                                  'Mode': 'DSC_DST'
                                },
					'protocol': 'me14',                   
					'protocol_config': {
						'Port':  2320,
						'HostName': '37.157.77.184',
						'Timeout': 30,
                        'Simple': 'true',
                        'MessageId': '01'
				}
               }
			}
		}
	}
}
";
/*            
            jsonstring = @"
{
	'executables': {
		'reader': {
			'multireader': {
				's1': {                
					'format': 'me14',
                    'format_config': {
                                  'Mode': 'DSC_DST'
                                },
					'protocol': 'me14',                   
					'protocol_config': {
						'Port':  2320,
						'HostName': '37.157.77.184',
						'Timeout': 30,
                        'Simple': 'true',
                        'MessageId': '01'
                    }
				}
			}
		}
	}
}
";
*/
            var config = new ConfigurationBuilder()
                //.AddInMemoryCollection(dict)
                .AddJsonString(jsonstring)
                .Build();

            var mr = new MultiReader<Observation>();



            var builder = new ContainerBuilder();

            // logger
            builder.RegisterInstance(lg).As<ILoggingConfig>().SingleInstance().ExternallyOwned();

            //builder.RegisterInstance(logger).As<ILogger>().SingleInstance().ExternallyOwned();

            builder.RegisterType<Logger>().As<ILogger>().WithParameter(
                new NamedParameter("processId", Process.GetCurrentProcess().Id.ToString())
                ).WithParameter(
                new NamedParameter("config", lg)
                );
            //.SingleInstance().ExternallyOwned();

            // register protocol readers
            builder.RegisterProtocolReaders();

            builder.RegisterFormatParsers();


            // create container
            IContainer Container = builder.Build();

            // Protocol reader factory
            IProtocolReaderFactory prf = Container.Resolve<IProtocolReaderFactory>();
            IFormatParserFactory<string,Observation> formatParserFactory = Container.Resolve<IFormatParserFactory<string, Observation>>();
            
            



            mr.AddFromConfig<Observation>(formatParserFactory, prf, KEY_AGENT_EXECUTABLE_MULTIREADER, config);

            //var res0 = mr.ReadAsync(CancellationToken.None).Result;

            var result = mr.ReadAsyncMerged<Observation>("s2", CancellationToken.None).Result;

            MultiReaderRow<Observation>[] x = mr;

            // I think this can be done more elegantly.
            foreach (var item in x)
            {
                if (item.FormatParser != null) item.FormatParser.Dispose();
                if (item.ProtocolReader != null) item.ProtocolReader.Dispose();
            }

            output.WriteLine($"{JsonConvert.SerializeObject(result, Formatting.Indented)}");


        }

    }

}
