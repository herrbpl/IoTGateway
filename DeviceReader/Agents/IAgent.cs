using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Router;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Agents
{
    /// <summary>
    /// IAgent manages agent executables which do real work and pass messages between each other.
    /// TODO: Cleanup stopping measurement
    /// TODO: Add statistical counters
    /// TODO: Add more granular state information besides IsRunning, for example stopped, starting, running, stopping, error
    /// TODO: Add events on granular state change. 
    /// </summary>
    public interface IAgent: IDisposable
    {
        bool IsRunning { get; }

        /// <summary>
        /// Name of agent. Expected to be unique for (pool of) agents as queue persistance mechanism is dependent of that
        /// Perhaps it is best to give give agent an GUID and make it unique globally.
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
        
        long StoppingTime { get; }
        DateTime StopStartTime { get; }
        DateTime StopStopTime { get; }
    }
}
