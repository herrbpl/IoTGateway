using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Models;
using DeviceReader.Router;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Agents
{
    public delegate void AgentStatusChangeEvent<T>(T status, object context);
    public enum AgentStatus {
        Running = 0,
        Stopped = 1,
        Stopping = 2,
        Starting = 3,
        Error = 255
    };
    /// <summary>
    /// IAgent manages agent executables which do real work and pass messages between each other.
    /// TODO: Cleanup stopping measurement
    /// TODO: Add statistical counters    
    /// </summary>
    public interface IAgent: IDisposable
    {
        bool IsRunning { get; }


        AgentStatus Status { get;  }
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

        /// <summary>
        /// Inbound messaging functionality
        /// </summary>
        IChannel<string, Observation> Inbound { get; }

        /// <summary>
        /// Indicates whether agent accepts inbound messages (ie, has inbound configured)
        /// </summary>
        //bool AcceptsInboundMessages { get; }


        // send message to agent input queue
        //Task SendMessage(string message);

        /// <summary>
        /// Send message of type T.
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="message">message for sending</param>
        /// <returns></returns>
        //Task SendMessage<T>(T message);

        /// <summary>
        /// Sets agent status handler callback.
        /// </summary>
        /// <param name="onstatuschange"></param>
        void SetAgentStatusHandler(AgentStatusChangeEvent<AgentStatus> onstatuschange);
        
        long StoppingTime { get; }
        DateTime StopStartTime { get; }
        DateTime StopStopTime { get; }
    }
}
