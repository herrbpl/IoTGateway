using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Devices
{
    public enum SourceProtocol
    {
        MES14 = 1,
        MES16 = 2,
        HTTPS = 3
    }

    public enum SourceDirection
    {
        PULL = 1,
        PUSH = 2
    }

    public interface IDeviceClientConfig
    {
        string IotHubConnectionString { get; }
    }

    public interface IDeviceConfig
    {
        string DeviceId { get; }
        SourceDirection Direction { get; }
        SourceProtocol Protocol { get; }
        string Host { get; }
        int Port { get; }
        // how to protect username and password in config? // Azure Key Vault?
        // used to connect source when pull, used as identification when source pushes (basic auth)
        string UserName { get; }
        string Password { get; }
        int PollFrequency { get; }
        string IotHubConnectionString { get; }
    }

    class DeviceConfig : IDeviceConfig
    {
        public string DeviceId { get; }

        public SourceDirection Direction { get; }

        public SourceProtocol Protocol { get; }

        public string Host { get; }

        public int Port { get; }

        public string UserName { get; }

        public string Password { get; }

        public int PollFrequency { get; }

        public string IotHubConnectionString { get; }
    }
}
