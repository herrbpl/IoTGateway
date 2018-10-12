using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Research.Webservice.Auth.Services
{
    public class UserService : IPasswordValidation
    {
        private ILogger logger;
        private Dictionary<string, string> _users;
        public UserService(ILoggerFactory logger)
        {
            this.logger = logger.CreateLogger(typeof(UserService).FullName);
            _users = new Dictionary<string, string>()
            {
                { "root", "toor" },
                { "user", "user" }
            };
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
