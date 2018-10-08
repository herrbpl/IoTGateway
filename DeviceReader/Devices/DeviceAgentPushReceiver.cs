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
    /// This class is meant to process inbound messages.
    /// Q: Which formats should we process? Or should we try all ? Is this resposibility of caller or this agent?
    /// A: For now, lets assume, that caller has responsibility for processing input and we only deal with correctly formatted input.
    /// This means that caller has to have information available about formats. This is just an injection point.
    /// </summary>
    public class DeviceAgentPushReceiver : AgentExecutable
    {
        private IDevice device;
        //public DeviceAgentReader(ILogger logger, IAgent agent, IProtocolReaderFactory protocolReaderFactory, IFormatParserFactory<string, List<T>> formatParserFactory, string name)
        public DeviceAgentPushReceiver(ILogger logger, IAgent agent, string name, IDevice device) :
             base(logger, agent, name)
        {
            this.device = device;
        }

        public override async Task Runtime(CancellationToken ct)
        {
            
            // There might be messages sent by some process (like web service input)
            await ProcessQueue(ct);
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

                    try
                    {

                        if (o.Type == typeof(Observation)) // just pass it on..
                        {
                            var observation = (Observation)(o.Message);
                            if (observation.DeviceId == this.device.Id)
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
                                    $"Inbound message DeviceId '{observation.DeviceId}' does not match destination Name '{this.device.Id}' dropping message", () => { });
                            }
                        }

                        else if (o.Type == typeof(List<Observation>)) // just pass it on..
                        {
                            var observations = (List<Observation>)(o.Message);
                            foreach (var observation in observations)
                            {
                                if (observation != null && observation.DeviceId == this.device.Id)
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
                                        $"Inbound message DeviceId '{observation.DeviceId}' does not match destination Name '{this.device.Id}' dropping message", () => { });
                                }
                            }
                        }
                        else
                        {
                            _logger.Warn($"Received message with type '{o.Type.Name}', don't know how to handle, dropping message", () => { });
                        }
                    } catch (Exception e)
                    {
                        _logger.Error($"Error while processing message {e}", () => { });
                    }
                    // dequeue
                    queue.Dequeue();
                }
            }
        }
        
    }

}
