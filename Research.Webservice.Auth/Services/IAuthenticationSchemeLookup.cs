using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Research.Webservice.Auth.Services
{
    /// <summary>
    /// Gets authentication Schema for input
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAuthenticationSchemeLookup<T>
    {
        /// <summary>
        /// Get authentication schema by lookup
        /// </summary>
        /// <param name="lookup">lookup value to get authentication schema for</param>
        /// <returns>Authentication schema</returns>
        string GetAuthenticationSchema(T lookup);
    }
}
