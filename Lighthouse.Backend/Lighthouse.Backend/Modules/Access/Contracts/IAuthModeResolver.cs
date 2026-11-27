using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Modules.Access.Contracts
{
    public interface IAuthModeResolver
    {
        RuntimeAuthStatus Resolve();
    }
}
