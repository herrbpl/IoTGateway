using System;
using System.Runtime.Serialization;

namespace DeviceReader.WebService.Exeptions
{
    [Serializable]
    internal class InvalidDeviceConfigurationProviderType : Exception
    {
        public InvalidDeviceConfigurationProviderType()
        {
        }

        public InvalidDeviceConfigurationProviderType(string message) : base(message)
        {
        }

        public InvalidDeviceConfigurationProviderType(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidDeviceConfigurationProviderType(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}