using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using DeviceReader.Diagnostics;
using Newtonsoft.Json;

namespace DeviceReader.Router
{

    public class SimpleRouter : IRouter
    {
        private ILogger _logger;

        private string _name;
        private Func<string, IQueue<RouterMessage>> _queueFactory;
        private RouteTable _routeTable;
        private Dictionary<string, IQueue<RouterMessage>> _queues;
        private DropMessageEvent<RouterMessage> _dropMessageEvent;


        /// <summary>
        /// Creates router instance.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queueFactory">Factory function that creates instances of IQueue&lt;RouterMessage&gt; Parametrized factory is needed to specify queue persitance</param>        
        public SimpleRouter(ILogger logger, string name, RouteTable routeTable, Func<string, IQueue<RouterMessage>> queueFactory)
        {
            _logger = logger;
            _name = name;
            if (queueFactory == null) throw new ArgumentNullException("queueFactory");
            _queueFactory = queueFactory;
            _queues = new Dictionary<string, IQueue<RouterMessage>>();
            _routeTable = routeTable;
        }

        public string Name { get => _name; }

        public IEnumerable<IQueue<RouterMessage>> Queues { get => _queues.Values.AsEnumerable<IQueue<RouterMessage>>(); }

        DropMessageEvent<RouterMessage> IRouter.OnDropMessage { get => _dropMessageEvent; set => _dropMessageEvent = value; }

        public IQueue<RouterMessage> AddQueue(string name)
        {
            if (!_queues.ContainsKey(name)) _queues.Add(name, _queueFactory(_name+"_"+name));
            return GetQueue(name);
        }

        public void Clear()
        {
            foreach (var item in _queues)
            {
                item.Value.Flush();
                
            }
            _queues.Clear();
        }

        public void Dispose()
        {
            // Dispose any queues.
            foreach (var queue in _queues)
            {
                queue.Value.Dispose();
            }
            _queues.Clear();
            _queues = null;
            return;

        }

        public IQueue<RouterMessage> GetQueue(string name)
        {
            if (!_queues.ContainsKey(name)) return null;
            return _queues[name];
        }


        public IQueue<RouterMessage> RemoveQueue(string name)
        {
            if (!_queues.ContainsKey(name)) throw new ArgumentException("name");
            var q = _queues[name];
            _queues.Remove(name);
            return q;
        }

        public void Route(string source, RouterMessage message)
        {
            var routes = _routeTable.GetRoutes(source, message);

            if (routes.Count == 0) {
                _logger.Debug(string.Format("Router '{0}:{1}':No routes defined for source", _name, source), () => { });
                return;
            }
            foreach (var route in routes)
            {
                if (_queues.ContainsKey(route.Target)) {
                    _queues[route.Target].Enqueue(message);
                } else
                {
                    // should we throw? Or add default route?
                    _logger.Warn(string.Format("Router '{0}:{1}':Dropping message intended for {0}:{2}, target queue not existing.",_name, source, route.Target ),
                        () => { });
                    
                    if (_dropMessageEvent != null)
                    {
                        _dropMessageEvent.Invoke(message);
                    }
                }
            }

        }
       
    }

}
