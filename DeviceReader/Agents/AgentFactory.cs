using DeviceReader.Diagnostics;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Agents
{
    public interface IAgentFactory
    {
        /// <summary>
        /// Creates IAgent from config. Config is generally JSON strong, for example from device twin desired parameters.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        IAgent CreateAgent(string config);
    }


    public class AgentFactory : IAgentFactory
    {        
        private ILogger _logger;
        private Func<string, IAgent> _getAgentDelegate;

        public AgentFactory(ILogger logger, Func<string, IAgent> getAgentDelegate)
        {
            _logger = logger;
            _getAgentDelegate = getAgentDelegate ?? throw new ArgumentNullException("getAgentDelegate"); ;
        }

        public IAgent CreateAgent(string config)
        {
            return _getAgentDelegate(config);
        }
    }
}
