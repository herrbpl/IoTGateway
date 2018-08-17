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

namespace DeviceReader.Devices
{
    class DeviceAgentWriter : AgentExecutable
    {
        StreamWriter writer;
        IDevice _writer;
        // Don't overthink it. Just add IDevice to constructor. 
        public DeviceAgentWriter(ILogger logger, IAgent agent, string name, IDevice writer):base(logger,agent, name) {
            // create output channels iotHub, etc etc..       
            this.writer = new StreamWriter(_agent.Name + ".out", true);
            this.writer.AutoFlush = true;
            _writer = writer;

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger.Debug($"Disposing DeviceAgentWriter!", () => { });
                if (this.writer != null)
                {
                    this.writer.Flush();
                    this.writer.Close();
                    this.writer.Dispose();
                    this.writer = null;
                }

                base.Dispose(disposing);
            }

        }

        public override async Task Runtime(CancellationToken ct)
        {
            // No upstream connectivity..
            // if (!_writer.Connected) return;


            var queue = this._agent.Router.GetQueue(this.Name);
            if (queue != null)
            {
                //this.writer = new StreamWriter(_agent.Name + ".out", true);
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
                        var js = JsonConvert.SerializeObject(observation);
                        var output = (string)observation.Data[0].Value + ":" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                        _logger.Debug(string.Format("Writing observation to upstream:\r\n{0}", output), () => { });
                        // save data.
                        //Encoding.UTF8.GetBytes(output);
                        await writer.WriteLineAsync(output);
                        //await writer.WriteLineAsync(js);
                        //await writer.FlushAsync();
                        var data = Encoding.UTF8.GetBytes(js);
                        await _writer.SendOutboundAsync(data, "application/json", "utf-8", null);
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
