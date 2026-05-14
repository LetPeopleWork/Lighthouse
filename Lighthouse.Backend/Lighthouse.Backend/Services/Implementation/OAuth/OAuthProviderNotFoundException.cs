namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public class OAuthProviderNotFoundException : Exception
    {
        public OAuthProviderNotFoundException()
        {
        }

        public OAuthProviderNotFoundException(string message)
            : base(message)
        {
        }

        public OAuthProviderNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
