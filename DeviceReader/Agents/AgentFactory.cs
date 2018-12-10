using DeviceReader.Diagnostics;
using DeviceReader.Extensions;
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
        IAgent CreateAgent(string[] config);
        IAgent CreateAgent(IConfiguration config);
    }


    public class AgentFactory : IAgentFactory
    {        
        private ILogger _logger;
        //private Func<string, IAgent> _getAgentDelegate;
        private Func<IConfiguration, IAgent> _getAgentDelegate;

        //public AgentFactory(ILogger logger, Func<string, IAgent> getAgentDelegate)
        public AgentFactory(ILogger logger, Func<IConfiguration, IAgent> getAgentDelegate)
        {
            _logger = logger;
            _getAgentDelegate = getAgentDelegate ?? throw new ArgumentNullException("getAgentDelegate"); ;
        }

        public IAgent CreateAgent(string config)
        {
            return CreateAgent(new string[] { config });
        }

        public IAgent CreateAgent(string[] config)
        {
            // create configuration from json string..
            IConfigurationBuilder cb = new ConfigurationBuilder();
            foreach (var item in config)
            {
                try
                {
                    cb.AddJsonString(item);
                } catch (Exception e)
                {

                    throw new ArgumentException($"Unable to parse json configuration: {e.Message}");
                }
            }
            
            var cbc = cb.Build();
            return _getAgentDelegate(cbc);
        }

        public IAgent CreateAgent(IConfiguration config)
        {
            return _getAgentDelegate(config);
        }
    }
}
