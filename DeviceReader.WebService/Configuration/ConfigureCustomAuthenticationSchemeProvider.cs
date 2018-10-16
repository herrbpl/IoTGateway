using DeviceReader.WebService.Middleware;
using DeviceReader.WebService.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeviceReader.WebService.Configuration
{
    /// <summary>
    /// https://stackoverflow.com/questions/46464469/how-to-configureservices-authentication-based-on-routes-in-asp-net-core-2-0 
    /// https://github.com/aspnet/Security/issues/1479#issuecomment-360928524
    /// </summary>
    public class ConfigureCustomAuthenticationSchemeProvider : AuthenticationSchemeProvider
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAuthenticationSchemeLookup<string> _authenticationSchemeLookup;


        public ConfigureCustomAuthenticationSchemeProvider(
            IHttpContextAccessor httpContextAccessor,
            IOptions<AuthenticationOptions> options,
            IAuthenticationSchemeLookup<string> authenticationSchemeLookup
            )
            : base(options)
        {
            this.httpContextAccessor = httpContextAccessor;
            this._authenticationSchemeLookup = authenticationSchemeLookup;            
        }

        private async Task<AuthenticationScheme> GetRequestSchemeAsync()
        {
            var request = httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                throw new ArgumentNullException("The HTTP request cannot be retrieved.");
            }


            var items = httpContextAccessor.HttpContext?.Items;
            if (items == null) { return null; }

            // use context info from middleware
            if (items.ContainsKey(DevicesHelperMiddleware.DEVICEID) && items.ContainsKey(DevicesHelperMiddleware.AUTHENTICATIONSCHEMA) 
                && items[DevicesHelperMiddleware.AUTHENTICATIONSCHEMA] != null)
            {
                return await GetSchemeAsync(items[DevicesHelperMiddleware.AUTHENTICATIONSCHEMA].ToString());
            }
          
            // For the other requests, return null to let the base methods
            // decide what's the best scheme based on the default schemes
            // configured in the global authentication options.
            return null;
        }

        public override async Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync() =>
            await GetRequestSchemeAsync() ??
            await base.GetDefaultAuthenticateSchemeAsync();

        public override async Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync() =>
            await GetRequestSchemeAsync() ??
            await base.GetDefaultChallengeSchemeAsync();

        public override async Task<AuthenticationScheme> GetDefaultForbidSchemeAsync() =>
            await GetRequestSchemeAsync() ??
            await base.GetDefaultForbidSchemeAsync();

        public override async Task<AuthenticationScheme> GetDefaultSignInSchemeAsync() =>
            await GetRequestSchemeAsync() ??
            await base.GetDefaultSignInSchemeAsync();

        public override async Task<AuthenticationScheme> GetDefaultSignOutSchemeAsync() =>
            await GetRequestSchemeAsync() ??
            await base.GetDefaultSignOutSchemeAsync();
    }
}

