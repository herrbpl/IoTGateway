using idunno.Authentication.Basic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Research.Webservice.Auth.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Research.Webservice.Auth.Configuration
{
    // see https://andrewlock.net/access-services-inside-options-and-startup-using-configureoptions/
    public class ConfigureBasicAuthenticationOptions : IConfigureOptions<BasicAuthenticationOptions>, IPostConfigureOptions<BasicAuthenticationOptions>
    {
        private readonly IPasswordValidation _passwordValidation;

        public ConfigureBasicAuthenticationOptions(IPasswordValidation passwordValidation)
        {
            _passwordValidation = passwordValidation;
            Console.WriteLine("Initializing Options class");
            
        }
        public void Configure(BasicAuthenticationOptions options)
        {
            options.Realm = "idunno";
            options.Events = new BasicAuthenticationEvents
            {
                // https://andrewlock.net/access-services-inside-options-and-startup-using-configureoptions/
                // http://thedatafarm.com/dotnet/twitter-education-re-aspnet-core-scope/
                OnValidateCredentials = context =>
                {
                    var validator = _passwordValidation;
                    Console.WriteLine("Method call from ConfOptions ibjection");
                    if (validator.Validate(context.Username, context.Password))
                    {
                        var claims = new[]
                        {
                                    new Claim(ClaimTypes.NameIdentifier, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                                    new Claim(ClaimTypes.Name, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer)
                                };

                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                        context.Success();
                    }

                    return Task.CompletedTask;
                }
            };
        }

        public void PostConfigure(string name, BasicAuthenticationOptions options)
        {

            options.Realm = "idunno";
            options.Events = new BasicAuthenticationEvents
            {
                // https://andrewlock.net/access-services-inside-options-and-startup-using-configureoptions/
                // http://thedatafarm.com/dotnet/twitter-education-re-aspnet-core-scope/
                OnValidateCredentials = context =>
                {
                    var validator = _passwordValidation;
                    Console.WriteLine("Method call from ConfOptions ibjection");
                    if (validator.Validate(context.Username, context.Password))
                    {
                        var claims = new[]
                        {
                                    new Claim(ClaimTypes.NameIdentifier, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                                    new Claim(ClaimTypes.Name, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer)
                                };

                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                        context.Success();
                    }
                    
                    return Task.CompletedTask;
                }
            };
        }
    }
}

