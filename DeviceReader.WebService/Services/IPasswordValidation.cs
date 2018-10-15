using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeviceReader.WebService.Services
{
    public interface IPasswordValidation
    {
        Task<bool> ValidateAsync(string Username, string Password);
        bool Validate(string Username, string Password);
    }
}
