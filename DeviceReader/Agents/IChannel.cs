using DeviceReader.Parsers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceReader.Agents
{
    /// <summary>
    /// Encapsulates messaging
    /// </summary>
    /// <typeparam name="TInput">Format parser input type</typeparam>
    /// <typeparam name="TOutput">Format parser output type</typeparam>
    public interface IChannel<TInput, TOutput>
    {
        /// <summary>
        /// Indicates whether agent accepts inbound messages (ie, has inbound configured)
        /// </summary>
        bool AcceptsMessages { get; }

        /// <summary>
        /// Format parser for inbound messaging
        /// </summary>
        IFormatParser<TInput, TOutput> FormatParser { get; }

        /// <summary>
        /// Configuration of channel
        /// </summary>
        IConfiguration ChannelConfig { get;  }

        /// <summary>
        /// Sends inbound message of type TInput
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendAsync(TInput message);

        /// <summary>
        /// Send message of type T.
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="message">message for sending</param>
        /// <returns></returns>
        Task SendAsync<T>(T message);

        /// <summary>
        /// Send message of type T.
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="message">message for sending</param>
        /// <returns></returns>
        Task SendAsync(TOutput message);
    }
}
