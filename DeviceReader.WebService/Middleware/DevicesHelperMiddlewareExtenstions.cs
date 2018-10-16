using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using System;

namespace DeviceReader.WebService.Middleware
{
    
    
    public static class DevicesHelperMiddlewareExtenstions
    {
        public static IApplicationBuilder UseDevicesHelperMiddleware(this IApplicationBuilder app, DevicesHelperMiddlewareOptions options)
        {
            return app.UseMiddleware<DevicesHelperMiddleware>(Options.Create(options));
        }

        public static IApplicationBuilder UseDevicesHelperMiddleware(this IApplicationBuilder app, Action<DevicesHelperMiddlewareOptions> options)
        {
            var _options = new DevicesHelperMiddlewareOptions();
            if (options != null)
            {
                options.Invoke(_options);
            }

            return app.UseMiddleware<DevicesHelperMiddleware>(Options.Create(_options));
        }
    }
}
