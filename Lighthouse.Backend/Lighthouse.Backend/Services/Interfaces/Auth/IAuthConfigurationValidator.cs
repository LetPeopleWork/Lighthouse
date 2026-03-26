using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Services.Interfaces.Auth
{
    public interface IAuthConfigurationValidator
    {
        AuthConfigurationValidationResult Validate(AuthenticationConfiguration configuration);
    }
}
