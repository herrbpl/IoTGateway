using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using DeviceReader.WebService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DeviceReader.WebService.Configuration
{
    // from https://blog.markvincze.com/matching-route-templates-manually-in-asp-net-core/ 
    // TODO: move to utils library
    public class RouteMatcher
    {
        public RouteValueDictionary Match(string routeTemplate, string requestPath)
        {
            var template = TemplateParser.Parse(routeTemplate);

            var matcher = new TemplateMatcher(template, GetDefaults(template));

            RouteValueDictionary values = new RouteValueDictionary();

            matcher.TryMatch(new Microsoft.AspNetCore.Http.PathString(requestPath), values);            
            
            return values;
        }

        // This method extracts the default argument values from the template.
        private RouteValueDictionary GetDefaults(RouteTemplate parsedTemplate)
        {
            var result = new RouteValueDictionary();

            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    result.Add(parameter.Name, parameter.DefaultValue);
                }
            }

            return result;
        }
    }


    // see https://andrewlock.net/access-services-inside-options-and-startup-using-configureoptions/
    // https://gist.github.com/sam9291/dba558f417a04b1775b51b20eb0f96ab
    public class ConfigureBasicAuthenticationOptions : IConfigureOptions<BasicAuthenticationOptions>, IPostConfigureOptions<BasicAuthenticationOptions>
    {
        private readonly IPasswordValidationProvider<string> _passwordValidationProvider;

        private readonly IActionDescriptorCollectionProvider _provider;


        public ConfigureBasicAuthenticationOptions(IPasswordValidationProvider<string> passwordValidationProvider, IActionDescriptorCollectionProvider provider)
        {
            _passwordValidationProvider = passwordValidationProvider;
            _provider = provider;            
            
        }
        public void Configure(BasicAuthenticationOptions options)
        {
            options.Realm = "idunno";
            options.Events = new BasicAuthenticationEvents
            {
                // https://andrewlock.net/access-services-inside-options-and-startup-using-configureoptions/
                // http://thedatafarm.com/dotnet/twitter-education-re-aspnet-core-scope/

                // Route parsing in ASPNet Core
                // https://gist.github.com/wcharczuk/2284226
                // https://blog.markvincze.com/matching-route-templates-manually-in-asp-net-core/

                OnValidateCredentials = context =>
                {
                    /*
                    Console.WriteLine("Method call from ConfOptions ibjection");
                    // how to get id part from path ?

                    // for now, we just extract id from path. 
                    var routematcher = new RouteMatcher();
                    var rv = routematcher.Match("/api/values/{id}", context.Request.Path);

                    if (rv.ContainsKey("id"))
                    {

                        Console.WriteLine($"GOT id:{rv["id"].ToString()} from path '{context.Request.Path}'");
                    }


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
                    */
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
                                        
                    // for now, we just extract id from path. 
                    var routematcher = new RouteMatcher();
                    var rv = routematcher.Match("/api/values/{id}", context.Request.Path);
                    var id = "";
                    if (rv.ContainsKey("id"))
                    {
                        id = rv["id"].ToString();                        
                    }

                    var validator = _passwordValidationProvider.GetValidator(id);

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

