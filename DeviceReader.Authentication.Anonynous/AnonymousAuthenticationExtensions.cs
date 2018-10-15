// Used sample from idunno.Authentication.Basic by Barry Dorrans

using System;
using Microsoft.AspNetCore.Authentication;

using DeviceReader.Authentication.Anonymous;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to add Anonymous authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class AnonymousAuthenticationAppBuilderExtensions
    {
        public static AuthenticationBuilder AddAnonymous(this AuthenticationBuilder builder)
            => builder.AddAnonymous(AnonymousAuthenticationDefaults.AuthenticationScheme);

        public static AuthenticationBuilder AddAnonymous(this AuthenticationBuilder builder, string authenticationScheme)
            => builder.AddAnonymous(authenticationScheme, configureOptions: null);

        public static AuthenticationBuilder AddAnonymous(this AuthenticationBuilder builder, Action<AuthenticationSchemeOptions> configureOptions)
            => builder.AddAnonymous(AnonymousAuthenticationDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddAnonymous(
            this AuthenticationBuilder builder,
            string authenticationScheme,
            Action<AuthenticationSchemeOptions> configureOptions)
        {
            return builder.AddScheme<AuthenticationSchemeOptions, AnonymousAuthenticationHandler>(authenticationScheme, configureOptions);
        }
    }
}


