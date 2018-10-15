using DeviceReader.Devices;
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

    public class UserService : IPasswordValidationProvider<string>, IAuthenticationSchemeLookup<string>
    {
        private ILogger logger;
        private ILoggerFactory _loggerFactory;
        private Dictionary<string, AuthenticationSource> _authinfo;

        private readonly IDeviceManager _deviceManager;

        public UserService(ILoggerFactory logger, IDeviceManager deviceManager)
        {
            this._loggerFactory = logger;
            this.logger = logger.CreateLogger(typeof(UserService).FullName);
            this._deviceManager = deviceManager;

            var x = new Dictionary<string, string>() { { "abc", "cba" } };

            _authinfo = new Dictionary<string, AuthenticationSource>()
            {
                { "0", new AuthenticationSource () {
                    AuthenticationScheme = "Basic", 
                    Users = new Dictionary<string, string>() { { "abc", "cba" } }
                } },
                { "1", new AuthenticationSource () {
                    AuthenticationScheme = "Anonymous",
                    Users = new Dictionary<string, string>()
                } }

            };
                        
        }

        public string GetAuthenticationSchema(string lookup)
        {
            if (_authinfo.ContainsKey(lookup))
            {
                return _authinfo[lookup].AuthenticationScheme;
            }

            return null;

        }
        public IPasswordValidation GetValidator(string selector)
        {
            if (_authinfo.ContainsKey(selector))
            {
                return new PasswordValidate(_loggerFactory, _authinfo[selector].Users);
            }
            return new PasswordValidate(_loggerFactory, new Dictionary<string, string>());
        }
        
    }
}
