using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeviceReader.WebService.Services
{
    public interface IPasswordValidationProvider<T>
    {
        IPasswordValidation GetValidator(T selector);
    }
}
