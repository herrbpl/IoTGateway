using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeviceReader.Authentication.Anonymous
{
    /// <summary>
    /// Log of this implementation has been copied from idunno.Authentication.Basic by Barry Dorrans
    /// </summary>
    internal class AnonymousAuthenticationHandler: AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public AnonymousAuthenticationHandler(
           IOptionsMonitor<AuthenticationSchemeOptions> options,
           ILoggerFactory logger,
           UrlEncoder encoder,
           ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }
        /// <summary>
        /// Since we allow all, return success while emylating identity
        /// </summary>
        /// <returns></returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {

            var claims = new[]
                        {
                                    new Claim(ClaimTypes.NameIdentifier, "Anonymous", ClaimValueTypes.String, AnonymousAuthenticationDefaults.AuthenticationScheme),
                                    new Claim(ClaimTypes.Name, "Anonymous", ClaimValueTypes.String, AnonymousAuthenticationDefaults.AuthenticationScheme)
                                };

            var identity = new ClaimsIdentity(claims, AnonymousAuthenticationDefaults.AuthenticationScheme);
            
            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
