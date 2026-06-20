using Lighthouse.Backend.Services.Implementation.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Lighthouse.Backend.Tests.TestHelpers.JwtBearerAuth
{
    public sealed class JwtBearerTestConfiguration(
        RsaSecurityKey signingKey,
        string issuer,
        string audience) : IPostConfigureOptions<JwtBearerOptions>
    {
        public void PostConfigure(string? name, JwtBearerOptions options)
        {
            if (name != SmartAuthSchemeSelector.JwtBearerScheme)
            {
                return;
            }

            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                NameClaimType = "name",
            };

            var configuration = new OpenIdConnectConfiguration { Issuer = issuer };
            configuration.SigningKeys.Add(signingKey);
            options.Configuration = configuration;
            options.ConfigurationManager =
                new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
        }
    }
}
