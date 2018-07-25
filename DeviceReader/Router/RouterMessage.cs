using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Router
{

    /// <summary>
    /// Message to be routed.
    /// </summary>
    public class RouterMessage
    {
        public Type Type;
        public object Message;
    }
}
