using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Research.Webservice.Auth.Configuration;
using Research.Webservice.Auth.Services;

namespace Research.Webservice.Auth
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // To inject options during configuration phase, see here:
            // https://andrewlock.net/access-services-inside-options-and-startup-using-configureoptions/ 
            // http://benjamincollins.com/blog/using-dependency-injection-while-configuring-services/
            // https://joonasw.net/view/creating-auth-scheme-in-aspnet-core-2

            services.AddLogging();
            
            // user provider
            services.AddSingleton<IPasswordValidation, UserService>();
          
            services.AddSingleton<IConfigureOptions<BasicAuthenticationOptions>, ConfigureBasicAuthenticationOptions>();
            //services.ConfigureOptions<ConfigureBasicAuthenticationOptions>();
            //services.ConfigureOptions<BasicAuthenticationOptions>();
            services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
           .AddBasic();
            services.ConfigureOptions<ConfigureBasicAuthenticationOptions>();

            /*
            .AddBasic(options =>
            {
                options.Realm = "idunno";
                options.Events = new BasicAuthenticationEvents
                {
                    // https://andrewlock.net/access-services-inside-options-and-startup-using-configureoptions/
                    // http://thedatafarm.com/dotnet/twitter-education-re-aspnet-core-scope/
                    // https://joonasw.net/view/creating-auth-scheme-in-aspnet-core-2
                    OnValidateCredentials = context =>
                    {

                        if (context.Username == context.Password)
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
            });
            */


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
