using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DeviceReader.Devices;
using DeviceReader.WebService.Formatters;
using DeviceReader.WebService.Services;
using DeviceReader.WebService.Configuration;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using idunno.Authentication.Basic;
using DeviceReader.WebService.Middleware;

namespace DeviceReader.WebService
{
    public class Startup
    {

        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {            
            /*

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
                //.AddEnvironmentVariables();
            this.Configuration = builder.Build();
            */

            // Use passed on configuration
            this.Configuration = configuration;


        }

        public IConfiguration Configuration { get; }

        // Initialized in `ConfigureServices`
        public IContainer ApplicationContainer { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // https://stackoverflow.com/questions/51328992/asp-net-core-server-side-validation-failure-causes-microsoft-aspnetcore-mvc-seri
            // https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.1#automatic-http-400-responses
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
                options.SuppressInferBindingSourcesForParameters = true;
                options.SuppressConsumesConstraintForFormFileParameters = true;
            });

            services.AddLogging();

            // Password validation provider
            services.AddSingleton<IPasswordValidationProvider<string>, UserService>();

            // authentication schema lookup
            services.AddSingleton<IAuthenticationSchemeLookup<string>, UserService>();

            // IHttpContextAccessor is not registered by default
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IAuthenticationSchemeProvider, ConfigureCustomAuthenticationSchemeProvider>();


            // configure options for basic authentication
            services.AddSingleton<IConfigureOptions<BasicAuthenticationOptions>, ConfigureBasicAuthenticationOptions>();

            // add authentication schemes
            services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
           .AddBasic().AddAnonymous();
            services.ConfigureOptions<ConfigureBasicAuthenticationOptions>();

            // Authorization
            services.AddAuthorization();

            // Raw formatrequest body formatter. 
            services.AddMvc(o => o.InputFormatters.Insert(0, new RawRequestBodyFormatter()));
            // Prepare DI container
            this.ApplicationContainer = DependencyResolution.Setup(services, this.Configuration);

            // Create the IServiceProvider based on the container
            return new AutofacServiceProvider(this.ApplicationContainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {

            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));

            if (env.IsDevelopment())
            {
                loggerFactory.AddDebug();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // here should go device lookup middleware which, upon nonfind, should go to 404

            app.UseDevicesHelperMiddleware(options =>
            {
                options.DeviceIdKey = "id";
                options.Templates.Add("/api/devices/{id}/inbound");
            });


            app.UseAuthentication();                        

            app.UseMvc();

            // Start simulation agent thread
            appLifetime.ApplicationStarted.Register(this.StartDeviceManager);
            appLifetime.ApplicationStopping.Register(this.StopDeviceManager);

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(() => this.ApplicationContainer.Dispose());

        }

        private void StartDeviceManager()
        {
            
            IDeviceManager dm = this.ApplicationContainer.Resolve<IDeviceManager>();            
            dm.StartAsync().Wait();
        }

        private void StopDeviceManager()
        {
            IDeviceManager dm = this.ApplicationContainer.Resolve<IDeviceManager>();
            dm.StopAsync().Wait();
        }
    }
}
