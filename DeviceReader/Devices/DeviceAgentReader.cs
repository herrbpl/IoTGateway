using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Protocols;
using DeviceReader.Parsers;
using DeviceReader.Models;
using DeviceReader.Agents;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Devices
{

    /// <summary>
    /// TODO: Add output storage, save run timestamp, calculate parameters for fetch (TimeBegin, TimeEnd). // should we get history data too or just current/latest data point..?
    /// I think initially we only poll current data. No need to get historical (unless it is explicitly demanded)    
    /// XXX: Should device client run even when there is no agent running?
    /// </summary>
    public class DeviceAgentReader : AgentExecutable
    {
        private IProtocolReaderFactory _protocolReaderFactory;
        private IFormatParserFactory<string, Observation> _formatParserFactory;
        private string format;
        private readonly string KEY_AGENT_EXECUTABLE_FORMAT;

        private string protocol;
        private readonly string KEY_AGENT_EXECUTABLE_PROTOCOL;

        private readonly string KEY_AGENT_EXECUTABLE_PROTOCOL_CONFIG;

        private int counter = 0;
        private IProtocolReader _protocolReader;
        IFormatParser<string, Observation> _parser;

        //public DeviceAgentReader(ILogger logger, IAgent agent, IProtocolReaderFactory protocolReaderFactory, IFormatParserFactory<string, List<T>> formatParserFactory, string name)
        public DeviceAgentReader(ILogger logger, IAgent agent, string name, IProtocolReaderFactory protocolReaderFactory, IFormatParserFactory<string, Observation> formatParserFactory) :
             base(logger, agent, name)
        {
            this._protocolReaderFactory = protocolReaderFactory;
            this._formatParserFactory = formatParserFactory;
            this.KEY_AGENT_EXECUTABLE_FORMAT = this.KEY_AGENT_EXECUTABLE_ROOT + ":format";
            this.KEY_AGENT_EXECUTABLE_PROTOCOL = this.KEY_AGENT_EXECUTABLE_ROOT + ":protocol";
            this.KEY_AGENT_EXECUTABLE_PROTOCOL_CONFIG = this.KEY_AGENT_EXECUTABLE_ROOT + ":protocol_config";

            format = this._config.GetValue<string>(this.KEY_AGENT_EXECUTABLE_FORMAT, null) ?? throw new ConfigurationMissingException(KEY_AGENT_EXECUTABLE_FORMAT);
            protocol = this._config.GetValue<string>(this.KEY_AGENT_EXECUTABLE_PROTOCOL, null) ?? throw new ConfigurationMissingException(KEY_AGENT_EXECUTABLE_PROTOCOL);
            
            //_protocolReader = _protocolReaderFactory.GetProtocolReader(protocol, this._config.GetSection(this.KEY_AGENT_EXECUTABLE_ROOT));
            _protocolReader = _protocolReaderFactory.GetProtocolReader(protocol, KEY_AGENT_EXECUTABLE_PROTOCOL_CONFIG,  this._config);
            _parser = _formatParserFactory.GetFormatParser(format);
        }



        public override async Task Runtime(CancellationToken ct)
        {
            // perform input poll
            await PollInput(ct);

            // There might be messages sent by some process (like web service input)
            await ProcessQueue(ct);
        }

        private async Task PollInput(CancellationToken ct)
        {
            _logger.Debug($"Reading count: {counter++} ", () => { });
            // fetch result
            var result = await _protocolReader.ReadAsync(ct);
           /* string result = @"2018-10-07  03:02,01,M14,amtij
01   7.1;02   100;03   7.0;05   0.5;06     9;14 13.66;15     1;16     0;
21  -0.5;26   0.7;27    41;30   7.3;31   8.5;32   0.1;33   1.4;34   115;
35   0.0;36    22;38  -0.1;39 255.7;40   0.0;41   0.0;42  0.00;43   0.0;
44   0.0;
=
2F21";
*/          
            // parse input
            var observations = await _parser.ParseAsync(result, ct);

            
            // send messages for routing..
            foreach (var observation in observations)
            {
                this.Agent.Router.Route(this.Name, new Router.RouterMessage
                {
                    Type = typeof(Observation),
                    Message = observation
                });
            }            
            observations = null;
            
        }

        private async Task ProcessQueue(CancellationToken ct)
        {
            var queue = this._agent.Router.GetQueue(this.Name);
            if (queue != null)
            {
                //_logger.Debug(string.Format("Device {0}: queue length: {1} ", _agent, queue.Count), () => { });
                // process queue
                while (!queue.IsEmpty)
                {
                    if (ct.IsCancellationRequested) break;
                    // try to process

                    var o = queue.Peek();

                    // In case of empty message, just drop it and move on.
                    if (o == null || o.Message == null)
                    {
                        _logger.Warn($"Empty message received, dropping", () => { });
                        queue.Dequeue();
                        continue;
                    }

                    if (o.Type == typeof(Observation)) // just pass it on..
                    {
                        var observation = (Observation)(o.Message);
                        if (observation.DeviceId == this.Name)
                        {

                            this.Agent.Router.Route(this.Name, new Router.RouterMessage
                            {
                                Type = typeof(Observation),
                                Message = observation
                            });
                        } else
                        {
                            _logger.Warn(
                                $"Inbound message DeviceId '{observation.DeviceId}' does not match destination Name '{Name}' dropping message", () => { });
                        }
                        observation = null;
                    } 

                    else if (o.Type == typeof(List<Observation>)) // just pass it on..
                    {
                        var observations = (List<Observation>)(o.Message);
                        foreach (var observation in observations)
                        {
                            if (observation != null && observation.DeviceId == this.Name)
                            {
                                this.Agent.Router.Route(this.Name, new Router.RouterMessage
                                {
                                    Type = typeof(Observation),
                                    Message = observation
                                });
                            }
                            else
                            {
                                _logger.Warn(
                                    $"Inbound message DeviceId '{observation.DeviceId}' does not match destination Name '{Name}' dropping message", () => { });
                            }
                        }
                        observations = null;
                    }

                    // 
                    else if (o.Type == typeof(string))
                    {
                        try
                        {
                            string data = (string)o.Message;
                            var observations = await this._parser.ParseAsync(data, ct);

                            // send messages for routing..
                            foreach (var observation in observations)
                            {
                                this.Agent.Router.Route(this.Name, new Router.RouterMessage
                                {
                                    Type = typeof(Observation),
                                    Message = observation
                                });
                            }

                        }
                        catch (Exception e)
                        {
                            _logger.Error(string.Format("'{0}:{1}':Error while processing input from queue, dropping message", _agent.Name, this.Name), () => { });
                        }
                    }
                    else
                    {
                        _logger.Warn($"Received message with type '{o.Type.Name}', don't know how to handle, dropping message", () => { });
                    }
                    // dequeue
                    queue.Dequeue();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._protocolReader != null)
                {
                    this._protocolReader.Dispose();
                    this._protocolReader = null;
                }

                if (this._parser != null)
                {
                    this._parser.Dispose();
                    this._parser = null;
                }

                base.Dispose(disposing);
            }

        }
    }

}
