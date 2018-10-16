using DeviceReader.Devices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeviceReader.WebService.Services
{
    class AuthenticationSource
    {
        public string AuthenticationScheme { get; set; }
        public Dictionary<string, string> Users { get; set; }
    }

    public class BasicAuthenticationConfiguration
    {
        
        public string UserName { get; set; }        
        public string Password { get; set; }
    }

    /// <summary>
    /// Proxy class isolating IDeviceManager from Authentication middleware.
    /// </summary>
    public class UserService : IPasswordValidationProvider<string>, IAuthenticationSchemeLookup<string>
    {

        public const string KEY_AUTHENTICATIONSCHEME_NAME = "authenticationscheme";
        public const string KEY_AUTHENTICATIONSCHEME_CONFIG = "authenticationscheme_config";

        private ILogger logger;
        private ILoggerFactory _loggerFactory;       

        private readonly IDeviceManager _deviceManager;

        public UserService(ILoggerFactory logger, IDeviceManager deviceManager)
        {
            this._loggerFactory = logger;
            this.logger = logger.CreateLogger(typeof(UserService).FullName);
            this._deviceManager = deviceManager;                                                
        }

        public string GetAuthenticationSchema(string lookup)
        {
            // get device
            if (_deviceManager.GetDeviceListAsync().Result.Any(x => x.Id == lookup))
            {
                var device = _deviceManager.GetDevice<IDevice>(lookup);
                var config = device.InboundChannel.ChannelConfig;

                if (config != null)
                {
                    return config.GetValue<string>(KEY_AUTHENTICATIONSCHEME_NAME, null);
                }
                

            }               
            return null;

        }
        public IPasswordValidation GetValidator(string selector)
        {

            var _users = new Dictionary<string, string>();
            // get device
            if (_deviceManager.GetDeviceListAsync().Result.Any(x => x.Id == selector))
            {
                var device = _deviceManager.GetDevice<IDevice>(selector);
                var config = device.InboundChannel.ChannelConfig;

                if (config != null && config.GetChildren().Any(cs => cs.Key == KEY_AUTHENTICATIONSCHEME_CONFIG))
                {
                    var section = config.GetSection(KEY_AUTHENTICATIONSCHEME_CONFIG);

                    

                    var data = section.Get<BasicAuthenticationConfiguration>();
                  
                    if (data != null && data.UserName != null)
                    {
                        _users.Add(data.UserName, data.Password);
                    }
                    
                }


            }
            return new PasswordValidate(_loggerFactory, _users);
        }
        
    }
}
