using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Router;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Agents
{
    public interface IAgent
    {
        bool IsRunning { get; }
        /// <summary>
        /// Name of agent. Expected to be unique for (pool of) agents as queue persistance mechanism is dependent of that
        /// </summary>
        
        string Name { get; }
        /// <summary>
        /// Agent IRouter, routes messages between agent executables.
        /// </summary>
        IRouter Router { get; }

        /// <summary>
        /// Agent configuration. 
        /// </summary>
        IConfigurationRoot Configuration { get; }

        // Required temporarily for waiting on all device agents. I think it should go to device manager where there would be await StopAll
        Task ExecutingTask { get; }
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
        
    }
}
