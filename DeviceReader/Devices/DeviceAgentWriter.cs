using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Agents;
using DeviceReader.Models;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Globalization;

namespace DeviceReader.Devices
{
    class DeviceAgentWriter : AgentExecutable
    {
        StreamWriter writer;
        
        public DeviceAgentWriter(ILogger logger, IAgent agent, string name):base(logger,agent, name) {
            // create output channels iotHub, etc etc..       
            this.writer = new StreamWriter(_agent.Name + ".out");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.writer != null)
                {
                    this.writer.Dispose();
                    this.writer = null;
                }

                base.Dispose(disposing);
            }

        }

        public override async Task Runtime(CancellationToken ct)
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

                    // Expect message to contain observations..
                    if (o.Type == typeof(Observation))
                    {
                        var observation = (Observation)o.Message;
                        //var js = JsonConvert.SerializeObject(observation, Formatting.Indented);
                        var output = (string)observation.Data[0].Value + ":" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                        //_logger.Info(string.Format("Writing observation to upstream:\r\n{0}", output), () => { });
                        // save data.
                        
                        await writer.WriteLineAsync(output);
                        await writer.FlushAsync();

                    }                                        

                    queue.Dequeue();
                }
            }
        }    
    }
}
