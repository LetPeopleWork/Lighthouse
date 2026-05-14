namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public class OAuthRefreshFailedException : Exception
    {
        public OAuthRefreshFailedException()
        {
        }

        public OAuthRefreshFailedException(string message)
            : base(message)
        {
        }

        public OAuthRefreshFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
