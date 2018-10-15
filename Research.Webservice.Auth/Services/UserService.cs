using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Research.Webservice.Auth.Services
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

        public UserService(ILoggerFactory logger)
        {
            this._loggerFactory = logger;
            this.logger = logger.CreateLogger(typeof(UserService).FullName);
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
