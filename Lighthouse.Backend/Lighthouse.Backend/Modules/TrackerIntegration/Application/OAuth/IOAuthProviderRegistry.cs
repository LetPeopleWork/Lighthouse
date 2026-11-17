namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth
{
    public interface IOAuthProviderRegistry
    {
        IOAuthProvider GetByKey(string authenticationMethodKey);
    }
}
