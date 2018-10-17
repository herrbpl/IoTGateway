using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Agents;
using DeviceReader.Models;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Devices
{

    public class DeviceAgentWriterFilter
    {
        /// <summary>
        /// Which data tags to include
        /// </summary>
        public List<string> Include { get; set; } = new List<string>() { "*" };

        /// <summary>
        /// which data tags to exclude
        /// </summary>
        public List<string> Exclude { get; set; } = new List<string>();

        /// <summary>
        /// which properties to include
        /// </summary>
        public List<string> Properties { get; set; } = new List<string>() { "*" };
    }

    public class DeviceAgentWriter : AgentExecutable
    {
        
        protected readonly IDevice _writer;

        protected readonly string KEY_AGENT_EXECUTABLE_FILTER;

        protected  DeviceAgentWriterFilter _filter;

        // Don't overthink it. Just add IDevice to constructor. 
        public DeviceAgentWriter(ILogger logger, IAgent agent, string name, IDevice writer):base(logger,agent, name) {
            
            _writer = writer;
            KEY_AGENT_EXECUTABLE_FILTER = KEY_AGENT_EXECUTABLE_ROOT + ":filter";
            // Try to get filter.

            //https://stackoverflow.com/questions/39169701/how-to-extract-a-list-from-appsettings-json-in-net-core
            //_filter = new DeviceAgentWriterFilter();
            //_filter.
            
            _config.Bind(KEY_AGENT_EXECUTABLE_FILTER, _filter);

            if (_filter == null) _filter = new DeviceAgentWriterFilter();

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {            
                
            }
            base.Dispose(disposing);

        }

        public override async Task Runtime(CancellationToken ct)
        {            

            var queue = this._agent.Router.GetQueue(this.Name);
            if (queue != null)
            {                
                // process queue
                while (!queue.IsEmpty)
                {
                    if (ct.IsCancellationRequested) break;
                    
                    // try to process

                    var o = queue.Peek();

                    // Expect message to contain observations..
                    if (o.Type == typeof(Observation))
                    {
                        var observation = (Observation)o.Message;


                        // Here, do filtration. And message transformation. To save data, send only filtered data tags

                        // First we exclude all specified. Then Include all in include. So exclude has higher priority.

                        foreach (var record in observation.Data)
                        {
                            
                        }


                        var js = JsonConvert.SerializeObject(observation);
                        
                        var data = Encoding.UTF8.GetBytes(js);
                        await _writer.SendOutboundAsync(data, "application/json", "utf-8", null);
                    } else
                    {
                        _logger.Warn($"Received message with type '{o.Type.Name}', don't know how to handle, dropping message", () => { });
                    }

                    queue.Dequeue();
                }
                /*
                writer.Close();
                writer.Dispose();
                writer = null;
                */
            }
            
        }   
        
    }
}
