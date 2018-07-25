using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Router
{
    /// @see https://gist.github.com/UizzUW/945c5740c93ecbf505f789143734d22f 

    public class Route
    {
        public string Target { get; set; }

        //public Expression<Func<IDeviceAgent, string, T, bool>> Evaluator { get; set; } // trouble with this is that it cannot easily be serialized.
        /// Study it later. Currently, not main importance. 
        /// @see https://gist.github.com/UizzUW/945c5740c93ecbf505f789143734d22f 
        /// https://stackoverflow.com/questions/4793981/converting-expressiont-bool-to-string
        /// http://geekswithblogs.net/mrsteve/archive/2016/02/29/friendly-readable-expression-trees-debug-visualizer.aspx
        /// https://www.strathweb.com/2018/01/easy-way-to-create-a-c-lambda-expression-from-a-string-with-roslyn/
        /// 
        public string Evaluator { get; set; }
    }
}
