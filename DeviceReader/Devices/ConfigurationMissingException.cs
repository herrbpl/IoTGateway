using System;
using System.Runtime.Serialization;

namespace DeviceReader.Devices
{
    [Serializable]
    internal class ConfigurationMissingException : Exception
    {
        public ConfigurationMissingException()
        {
        }

        public ConfigurationMissingException(string message) : base(message)
        {
        }

        public ConfigurationMissingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ConfigurationMissingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}