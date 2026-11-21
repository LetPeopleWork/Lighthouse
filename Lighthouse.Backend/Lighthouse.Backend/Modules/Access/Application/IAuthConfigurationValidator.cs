using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Modules.Access.Domain;

namespace Lighthouse.Backend.Modules.Access.Application
{
    public interface IAuthConfigurationValidator
    {
        AuthConfigurationValidationResult Validate(
            AuthenticationConfiguration authenticationConfiguration,
            AuthorizationConfiguration authorizationConfiguration);
    }
}
