using System;
using System.Runtime.Serialization;

namespace DeviceReader.Protocols.UMB
{
    [Serializable]
    public class FrameValidationException : Exception
    {
        public FrameValidationException()
        {
        }

        public FrameValidationException(string message) : base(message)
        {
        }

        public FrameValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FrameValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}