using System;
using System.Runtime.Serialization;

namespace DeviceReader.Protocols.UMB
{
    [Serializable]
    public class FrameVersionUnsupportedException : Exception
    {
        public FrameVersionUnsupportedException()
        {
        }

        public FrameVersionUnsupportedException(string message) : base(message)
        {
        }

        public FrameVersionUnsupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FrameVersionUnsupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}