using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeviceReader.Router
{
    /// <summary>
    /// This class specifies route table. Can be improved, as this is not main interest now.
    /// TODO: Add string expression evaluation and building compiled expressions from evaluation
    ///         Also evaluate route execution based on evaluator expression
    /// TODO: Add priority to routes.
    /// </summary>    
    public class RouteTable
    {
        // need to store multiple sources -> one target
        // need to store source -> multiple targets (but each target can be only once for once source)

        // source , targets. Perhaps allow multiple routes between same source and target, if distinguished by condition?

        private Dictionary<string, Dictionary<string, Route>> _routes;
        public Dictionary<string, Dictionary<string, Route>> Routes { get => _routes; }

        public RouteTable()
        {
            _routes = new Dictionary<string, Dictionary<string, Route>>();
        }

        public void AddRoute(string source, string target, string evaluator)
        {
            if (!_routes.ContainsKey(source)) _routes.Add(source, new Dictionary<string, Route>());
            var targets = _routes[source];
            if (!targets.ContainsKey(target)) targets.Add(target, new Route { Target = target, Evaluator = evaluator });
        }
       
        // gets list of routes from source. 
        public List<Route> GetRoutes(string source, RouterMessage data)
        {
            if (_routes.ContainsKey(source))
            {
                var d = _routes[source].Values.ToList();
                return d;
            }
            return new List<Route>();
        }

    }
}
