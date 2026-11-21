using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Modules.Access.Application
{
    public interface IAuthModeResolver
    {
        RuntimeAuthStatus Resolve();
    }
}
