using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Research.Webservice.Auth.Services
{
    public interface IPasswordValidationProvider<T>
    {
        IPasswordValidation GetValidator(T selector);
    }
}
