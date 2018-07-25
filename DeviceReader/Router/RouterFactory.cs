using System;
using System.Collections.Generic;
using System.Text;
using DeviceReader.Diagnostics;

namespace DeviceReader.Router
{
    public interface IRouterFactory
    {
        IRouter Create(string name);
    }
    
    public class DefaultRouterFactory : IRouterFactory
    {
        private ILogger _logger;
        
        private Func<string, IRouter> _routerFactory;

        public DefaultRouterFactory(ILogger logger, Func<string, IRouter> routerFactory)
        {
            _logger = logger;
            _routerFactory = routerFactory ?? throw new ArgumentNullException("routerFactory");
        }
        public IRouter Create(string name)
        {
            _logger.Debug(string.Format("Creating router: {0}", name), () => { });
            return _routerFactory(name);
        }
    }
}
