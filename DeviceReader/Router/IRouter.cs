using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Router
{
    /// <summary>
    /// Execute when dropping message.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    public delegate void DropMessageEvent<T>(T message);

    /// <summary>
    /// Router for messaging between tasks. It basically handles queue mechanism needed for message passing between different executable.
    /// Currently not providing removing routes.
    /// TODO: Add removing of routes. NB! Route removing does not necessarlily mean that destination queue can be removed as there might be still messages in queue 
    /// and something could be reading that queue. 
    /// TODO: investigate possibility of queue pooling. Perhaps one queue pool for entire gateway?
    /// <see cref="https://insidethecpu.com/2015/07/31/microservices-in-c-part-2-consistent-message-delivery/"/>
    /// </summary>
    /// <typeparam name="T">Message type for routing</typeparam>
    public interface IRouter : IDisposable
    {
        /// <summary>
        /// Name of router
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets input queue for executable.        
        /// </summary>
        /// <param name="name">Name of queue</param>
        /// <returns></returns>
        IQueue<RouterMessage> GetQueue(string name);

        /// <summary>
        /// Adds queue if it does not exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IQueue<RouterMessage> AddQueue(string name);

        /// <summary>
        /// Removes queue with given and returns it. Throws if queue is not found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IQueue<RouterMessage> RemoveQueue(string name);

        IEnumerable<IQueue<RouterMessage>> Queues { get; }

        void Clear();

        /// <summary>
        /// Routes message to target queue.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
        void Route(string source, RouterMessage message);

        DropMessageEvent<RouterMessage> OnDropMessage { get; set; }

    }
}
