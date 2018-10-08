using DeviceReader.Agents;
using DeviceReader.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Devices
{
    class DeviceAgentZero: AgentExecutable
    {
        public DeviceAgentZero(ILogger logger, IAgent agent, string name) : base(logger, agent, name)
        {                     

        }
        
        public override async Task Runtime(CancellationToken ct)
        {
            var queue = this._agent.Router.GetQueue(this.Name);
            if (queue != null)
            {                
                while (!queue.IsEmpty)
                {
                    if (ct.IsCancellationRequested) break;

                    // try to process

                    var o = queue.Peek();

                    _logger.Debug($"processing queue:{o.Type.Name}", () => { });                    

                    queue.Dequeue();
                }
                
            }

        }
    }
}
