using System;
using System.Runtime.Serialization;

namespace DeviceReader.Protocols.UMB
{
    [Serializable]
    public class FrameIncompleteException : Exception
    {
        public FrameIncompleteException()
        {
        }

        public FrameIncompleteException(string message) : base(message)
        {
        }

        public FrameIncompleteException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FrameIncompleteException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}