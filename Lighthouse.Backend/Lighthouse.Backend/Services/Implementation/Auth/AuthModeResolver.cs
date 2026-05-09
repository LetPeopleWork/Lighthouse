using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class AuthModeResolver : IAuthModeResolver
    {
        private readonly IOptions<AuthenticationConfiguration> authenticationConfiguration;
        private readonly IOptions<AuthorizationConfiguration> authorizationConfiguration;
        private readonly IAuthConfigurationValidator validator;
        private readonly ILicenseService licenseService;
        private readonly IPlatformService platformService;
        private readonly ILogger<AuthModeResolver> logger;

        public AuthModeResolver(
            IOptions<AuthenticationConfiguration> authenticationConfiguration,
            IOptions<AuthorizationConfiguration> authorizationConfiguration,
            IAuthConfigurationValidator validator,
            ILicenseService licenseService,
            IPlatformService platformService,
            ILogger<AuthModeResolver> logger)
        {
            this.authenticationConfiguration = authenticationConfiguration;
            this.authorizationConfiguration = authorizationConfiguration;
            this.validator = validator;
            this.licenseService = licenseService;
            this.platformService = platformService;
            this.logger = logger;
        }

        public RuntimeAuthStatus Resolve()
        {
            var authConfig = authenticationConfiguration.Value;
            var authzConfig = authorizationConfiguration.Value;

            var validationResult = validator.Validate(authConfig, authzConfig);
            if (!validationResult.IsValid)
            {
                logger.LogError(
                    "Authentication/authorization configuration is invalid: {Reason}. The instance will enter misconfigured mode",
                    validationResult.ErrorReason);

                return new RuntimeAuthStatus
                {
                    Mode = AuthMode.Misconfigured,
                    MisconfigurationMessage = validationResult.ErrorReason,
                };
            }

            if (!authConfig.Enabled)
            {
                logger.LogDebug("Authentication is disabled by configuration");
                return new RuntimeAuthStatus { Mode = AuthMode.Disabled };
            }

            if (platformService.IsStandalone)
            {
                logger.LogWarning("Authentication is enabled in configuration but the current runtime is a standalone/Tauri variant. Auth is not supported for standalone variants and will be disabled");
                return new RuntimeAuthStatus { Mode = AuthMode.Disabled };
            }

            if (!licenseService.CanUsePremiumFeatures())
            {
                logger.LogWarning("Authentication is enabled and configured correctly, but Premium licensing is not valid. The instance will enter blocked mode");
                return new RuntimeAuthStatus { Mode = AuthMode.Blocked };
            }

            return new RuntimeAuthStatus { Mode = AuthMode.Enabled };
        }
    }
}
