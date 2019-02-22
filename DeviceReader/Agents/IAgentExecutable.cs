using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Models;


namespace DeviceReader.Agents
{
    

    public interface IAgentExecutableBase
    {
        
        /// <summary>
        /// Gets inbound measurements names and simple stats
        /// </summary>
        IEnumerable<MeasurementMetadataRecord> InboundMeasurements { get; }

        /// <summary>
        /// Gets outbound measurements names and simple stats
        /// </summary>
        IEnumerable<MeasurementMetadataRecord> OutboundMeasurements { get; }

        /// <summary>
        /// Name of executable. Used for message routing.
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// Device Agent Executable. Agent Executable can do different things, mostly reading (polling) from input and writing to output (IoT Hub).
    /// </summary>
    public interface IAgentExecutable: IAgentExecutableBase, IDisposable
    {
        /// <summary>
        /// Runs Agent Executable. 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RunAsync(CancellationToken ct);

        
        IAgent Agent { get; }
        /*
        /// <summary>
        /// Name of executable. Used for message routing.
        /// </summary>
        string Name { get; }
        */
       
    }

    public class DeviceAgentExecutableMetadata
    {
        /// <summary>
        /// Specifies name of output task to which send output.
        /// </summary>
        public string OutputName { get; set; }
    }
}
