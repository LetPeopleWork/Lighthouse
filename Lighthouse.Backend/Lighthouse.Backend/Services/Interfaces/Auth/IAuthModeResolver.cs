using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Services.Interfaces.Auth
{
    public interface IAuthModeResolver
    {
        RuntimeAuthStatus Resolve();
    }
}
