using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Research.Webservice.Auth.Services
{
    public interface IPasswordValidation
    {
        Task<bool> ValidateAsync(string Username, string Password);
        bool Validate(string Username, string Password);
    }
}
