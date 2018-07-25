using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Agents;
using DeviceReader.Models;
using Newtonsoft.Json;

namespace DeviceReader.Devices
{
    class DeviceAgentWriter : AgentExecutable
    {

        public DeviceAgentWriter(ILogger logger, IAgent agent, string name):base(logger,agent, name) {
            // create output channels iotHub, etc etc..            
        }
        public override async Task Runtime(CancellationToken ct)
        {
            var queue = this._agent.Router.GetQueue(this.Name);
            if (queue != null)
            {                
                _logger.Info(string.Format("Device {0}: queue length: {1} ", _agent, queue.Count), () => { });
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
                        var js = JsonConvert.SerializeObject(observation, Formatting.Indented);
                        _logger.Info(string.Format("Writing observation to upstream:\r\n{0}", js), () => { });
                    }

                    _logger.Info(string.Format("Device {0}: reading data from queue", _agent.Name), () => { });
                    // dequeue

                    queue.Dequeue();
                }
            }
        }    
    }
}
