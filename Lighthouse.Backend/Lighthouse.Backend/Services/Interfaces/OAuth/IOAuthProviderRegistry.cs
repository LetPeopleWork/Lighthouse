namespace Lighthouse.Backend.Services.Interfaces.OAuth
{
    public interface IOAuthProviderRegistry
    {
        IOAuthProvider GetByKey(string authenticationMethodKey);
    }
}
