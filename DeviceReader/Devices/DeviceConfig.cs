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
        string ProtocolReader { get; }
        string FormatParser { get; }
        string Host { get; }
        int Port { get; }
        // how to protect username and password in config? // Azure Key Vault?
        // used to connect source when pull, used as identification when source pushes (basic auth)
        string UserName { get; }
        string Password { get; }
        int PollFrequency { get; }
        string IotHubConnectionString { get; }
    }

    public class DeviceConfig : IDeviceConfig
    {
        public string DeviceId { get; set; }

        public SourceDirection Direction { get; set;  }

        public string ProtocolReader { get; set; }

        public string FormatParser { get; set;  }

        public string Host { get; set;  }

        public int Port { get; set;  }

        public string UserName { get; set;  }

        public string Password { get; set;  }

        public int PollFrequency { get; set;  }

        public string IotHubConnectionString { get; set; }
    }
}
