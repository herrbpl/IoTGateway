using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Research.Webservice.Auth.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Research.Webservice.Auth.Configuration
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


            // for now, we just extract id from path. 
            var routematcher = new RouteMatcher();
            var rv = routematcher.Match("/api/values/{id}", request.Path);
            var id = "";
            if (rv.ContainsKey("id"))
            {
                id = rv["id"].ToString();
                Console.WriteLine($"GOT id:{rv["id"].ToString()} from path '{request.Path}'");
            }

            var schema = _authenticationSchemeLookup.GetAuthenticationSchema(id);

            
            // For API requests, use authentication tokens.
            if (schema != null)
            {                                
                return await GetSchemeAsync(schema);
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

