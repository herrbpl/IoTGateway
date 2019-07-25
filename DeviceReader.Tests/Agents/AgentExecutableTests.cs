using DeviceReader.Agents;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using DeviceReader.Extensions;
using DeviceReader.Models;
using DeviceReader.Router;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DeviceReader.Tests.Agents
{

    public class MockAgent : IAgent
    {
        IConfiguration _configuration;
        string _id;
        bool _isRunning = false;
        AgentStatus _agentStatus = AgentStatus.Stopped;

        public MockAgent(string id, IConfiguration configuration)
        {
            _id = id;
            _configuration = configuration;
        }

        public bool IsRunning { get => _isRunning; }

        public AgentStatus Status { get => _agentStatus; }
        public string Name { get { return _configuration.GetValue<string>("name", null); } }

        public IRouter Router => throw new NotImplementedException();

        public IConfiguration Configuration { get => _configuration;  }

        public Task ExecutingTask => throw new NotImplementedException();

        public IChannel<string, Observation> Inbound => throw new NotImplementedException();

        public IEnumerable<IAgentExecutableBase> AgentExecutables => throw new NotImplementedException();

        public long StoppingTime => throw new NotImplementedException();

        public DateTime StopStartTime => throw new NotImplementedException();

        public DateTime StopStopTime => throw new NotImplementedException();

        public void Dispose()
        {
            return;
        }

        public void SetAgentStatusHandler(AgentStatusChangeEvent<AgentStatus> onstatuschange)
        {
            throw new NotImplementedException();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this._agentStatus = AgentStatus.Starting;
            await Task.Delay(100);
            this._isRunning = true;
            this._agentStatus = AgentStatus.Running;

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this._agentStatus = AgentStatus.Stopping;
            await Task.Delay(100);
            this._isRunning = false;
            this._agentStatus = AgentStatus.Stopped;
        }
    }


    public class MockDevice : IDevice
    {
        ILogger _logger;
        string _id;
        public MockDevice(ILogger logger, string id)
        {
            _id = id;
            _logger = logger;
        }

        public string Id { get => _id; }

        public IReadOnlyDictionary<string, object> Metadata => throw new NotImplementedException();

        public AgentStatus AgentStatus => throw new NotImplementedException();

        public ConnectionStatus ConnectionStatus => throw new NotImplementedException();

        public bool AcceptsInboundMessages => throw new NotImplementedException();

        public string AgentConfig => throw new NotImplementedException();

        public IChannel<string, Observation> InboundChannel => throw new NotImplementedException();

        public IEnumerable<IAgentExecutableBase> AgentExecutables => throw new NotImplementedException();

        public Task<TItem> GetCacheValueAsync<TItem>(string key)
        {
            throw new NotImplementedException();
        }

        public async Task SendOutboundAsync(byte[] data, string contenttype, string contentencoding, Dictionary<string, string> properties)
        {
            _logger.Info($"SendOutboundAsync: {UTF8Encoding.UTF8.GetString(data)}", () => { });
        }

        public Task SetCacheValueAsync<TItem>(string key, TItem value, TimeSpan expiration)
        {
            throw new NotImplementedException();
        }

        public Task StartAsync()
        {
            throw new NotImplementedException();
        }

        public Task StopAsync()
        {
            throw new NotImplementedException();
        }
    }

    public class MockAgentExecutable : AgentExecutable
    {
        public MockAgentExecutable(ILogger logger, IAgent agent, string name, IDevice device) :
             base(logger, agent, name, device)
        {
        }
    }

    public class AgentExecutableTests
    {
        private readonly ITestOutputHelper output;
        ILogger logger;

        private string AgentConfigBaseTemplate = $@"
{{
    'name': 'device',
    'executables': {{ 
        'reader': {{            
            'format':'me14',
            'protocol':'me14',
            'protocol_config': '#CONFIG#',
            'frequency': #FREQUENCY#,
            'type': 'reader'
        }},
        'writer': {{            
            'frequency': 1000,
            'type': 'zeroagent'
        }}
    }},
    'routes': {{
        'reader': {{ 
            'writer': {{
                'target': 'writer',
                'evaluator': ''
            }}
        }}                         
    }}
}}
";

        public AgentExecutableTests(ITestOutputHelper output)
        {
            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;

            Logger.DefaultLoggerFactory.AddProvider(new XUnitLoggerProvider(output));
            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);
        }

        private IConfiguration getConfig(string config, Dictionary<string, string> replacements)
        {
            if (replacements != null)
            {
                foreach (var item in replacements)
                {
                    config = config.Replace(item.Key, item.Value);
                }
            }

            logger.Info(config, () => { });
            IConfigurationBuilder cb = new ConfigurationBuilder();
            cb.AddJsonString(config);
            return cb.Build();
        }

        [Fact]
        public void Should_Fail_With_InvalidFrequencyInput()
        {
            Assert.Throws<Newtonsoft.Json.JsonReaderException>(() =>
            {
                var config4 = getConfig(AgentConfigBaseTemplate, new Dictionary<string, string>() { { "#FREQUENCY#", "* * * * *" } });
            });
        }

        [Fact]
        public void Should_Choose_Scheduler()
        {
            List<Tuple<string, string, Exception>> cases = new List<Tuple<string, string, Exception>>()
            {
                new Tuple<string, string, Exception>("#FREQUENCY#", "1000", null),
                new Tuple<string, string, Exception>("#FREQUENCY#", "'* * * * *'", null),
                new Tuple<string, string, Exception>("#FREQUENCY#", "'InvalidCronString'", null)
            };

            IDevice device = new MockDevice(logger, "device");
            

            foreach (var item in cases)
            {
                var config = getConfig(AgentConfigBaseTemplate, new Dictionary<string, string>() { { item.Item1, item.Item2 } });
                IAgent agent = new MockAgent("device", config);

                IAgentExecutable agentExecutable = new MockAgentExecutable(logger, agent, "reader", device);
                
            }

           

            

            

        }

    }
}
