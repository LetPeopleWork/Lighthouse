using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class AuthModeResolver : IAuthModeResolver
    {
        private readonly IOptions<AuthenticationConfiguration> configuration;
        private readonly IAuthConfigurationValidator validator;
        private readonly ILicenseService licenseService;
        private readonly IPlatformService platformService;
        private readonly ILogger<AuthModeResolver> logger;

        public AuthModeResolver(
            IOptions<AuthenticationConfiguration> configuration,
            IAuthConfigurationValidator validator,
            ILicenseService licenseService,
            IPlatformService platformService,
            ILogger<AuthModeResolver> logger)
        {
            this.configuration = configuration;
            this.validator = validator;
            this.licenseService = licenseService;
            this.platformService = platformService;
            this.logger = logger;
        }

        public RuntimeAuthStatus Resolve()
        {
            var config = configuration.Value;

            if (!config.Enabled)
            {
                logger.LogDebug("Authentication is disabled by configuration");
                return new RuntimeAuthStatus { Mode = AuthMode.Disabled };
            }

            if (platformService.IsStandalone)
            {
                logger.LogWarning("Authentication is enabled in configuration but the current runtime is a standalone/Tauri variant. Auth is not supported for standalone variants and will be disabled");
                return new RuntimeAuthStatus { Mode = AuthMode.Disabled };
            }

            var validationResult = validator.Validate(config);
            if (!validationResult.IsValid)
            {
                logger.LogError("Authentication configuration is invalid: {Reason}. The instance will enter misconfigured mode and login will not be available", validationResult.ErrorReason);
                return new RuntimeAuthStatus
                {
                    Mode = AuthMode.Misconfigured,
                    MisconfigurationMessage = validationResult.ErrorReason,
                };
            }

            if (!licenseService.CanUsePremiumFeatures())
            {
                logger.LogWarning("Authentication is enabled and configured correctly, but Premium licensing is not valid. The instance will enter blocked mode");
                return new RuntimeAuthStatus { Mode = AuthMode.Blocked };
            }

            logger.LogDebug("Authentication is enabled and configured correctly with valid Premium licensing");
            return new RuntimeAuthStatus { Mode = AuthMode.Enabled };
        }
    }
}
