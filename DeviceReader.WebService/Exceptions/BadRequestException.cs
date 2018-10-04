using System;
using System.Runtime.Serialization;

namespace DeviceReader.WebService.Exeptions
{
    [Serializable]
    internal class BadReqestException : Exception
    {
        public BadReqestException()
        {
        }

        public BadReqestException(string message) : base(message)
        {
        }

        public BadReqestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BadReqestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}