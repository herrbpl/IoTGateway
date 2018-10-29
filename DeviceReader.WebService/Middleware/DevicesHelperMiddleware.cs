using DeviceReader.Devices;
using DeviceReader.WebService.Configuration;
using DeviceReader.WebService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeviceReader.WebService.Middleware
{
    /*
    public static class DevicesHelperMiddlewareConstants
    {
        public const string DeviceId = "DeviceManager-DeviceId";
        public const string AuthenticationSchema = "DeviceManager-AuthenticationSchema";
    }
    */
    // https://www.blinkingcaret.com/2017/09/13/create-your-own-asp-net-core-middleware/
    public class DevicesHelperMiddlewareOptions
    {
        /// <summary>
        /// Placeholder that indicates device id in template string
        /// </summary>
        public string DeviceIdKey { get; set; } = "id";

        /// <summary>
        /// List of template string to scan against.
        /// </summary>
        public List<string> Templates { get; set; } = new List<string>();
    }

    /// <summary>
    /// Extracts Device ID from path and checks from device Manager, if device exists. If not, return 404. If exists, device id is stored into HttpContext and proceed with next middleware.
    /// </summary>
    public class DevicesHelperMiddleware
    {

        public const string DEVICEID = "DeviceManager-DeviceId";
        public const string AUTHENTICATIONSCHEMA = "DeviceManager-AuthenticationSchema";


        private readonly RequestDelegate _next;
        private readonly DevicesHelperMiddlewareOptions _options;
        public DevicesHelperMiddleware(RequestDelegate next, IOptions<DevicesHelperMiddlewareOptions> options)
        {
            _next = next;
            _options = options.Value;
        }
       
        public async Task Invoke(HttpContext httpContext, ILoggerFactory loggerFactory, IDeviceManager deviceManager, IAuthenticationSchemeLookup<string> authenticationSchemeLookup)
        {
            var logger = loggerFactory.CreateLogger(typeof(DevicesHelperMiddleware).FullName);

            var routematcher = new RouteMatcher();

            var id = "";

            // use first match
            foreach (var item in _options.Templates)
            {
                var rv = routematcher.Match(item, httpContext.Request.Path);
                
                if (rv.ContainsKey(_options.DeviceIdKey))
                {
                    id = rv[_options.DeviceIdKey].ToString();
                    break;
                }
            }

            // for now, just log extracted id.
            logger.LogInformation($"Extracted id '{id}' from path '{httpContext.Request.Path}'");

            
            // if id is extracted, check if such resource exists.
            // TODO: add custom option for checking and remove hard dependency on IDeviceManager - make middleware to a generic one?
            if (id != "")
            {
                if (!deviceManager.GetDeviceListAsync().Result.Any(x => x.Id == id))
                {
                    logger.LogWarning($"Device with Id '{id} not found'");
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync("{\"error\": \"Device not found\"}");
                    
                    return;
                }

                // all calls to such middleware require agent to be running.. 
                var device = deviceManager.GetDevice<IDevice>(id);

                if (!device.AcceptsInboundMessages)
                {
                    logger.LogWarning($"Device with Id '{id} has no agent running'");
                    httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync("{\"error\": \"Device does not accept inbound messaging.\"}");

                    
                    return;
                }


                // device found, add info into http context.
                httpContext.Items.Add(DevicesHelperMiddleware.DEVICEID, id);
                var schema = authenticationSchemeLookup.GetAuthenticationSchema(id);
                httpContext.Items.Add(DevicesHelperMiddleware.AUTHENTICATIONSCHEMA, schema);


            }

            await _next(httpContext);
        }

    }
}
