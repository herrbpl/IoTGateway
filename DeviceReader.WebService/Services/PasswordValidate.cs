using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeviceReader.WebService.Services
{
    /// <summary>
    /// Implements password checking used in Basic Authentication
    /// </summary>
    public class PasswordValidate: IPasswordValidation
    {

        private ILogger logger;
        private Dictionary<string, string> _users;
        private UserService _userService;

        public PasswordValidate(ILoggerFactory logger, Dictionary<string, string> authinfo)
        {
            this.logger = logger.CreateLogger(typeof(PasswordValidate).FullName);
            _users = authinfo;            
        }

        public bool Validate(string Username, string Password)
        {
            logger.LogInformation($"Validating '{Username}'");
            if (_users.ContainsKey(Username) && _users[Username] == Password) return true;
            return false;
        }

        public async Task<bool> ValidateAsync(string Username, string Password)
        {
            return Validate(Username, Password);
        }
    }
}
