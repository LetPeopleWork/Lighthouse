namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth
{
    public class OAuthRefreshTimeoutException : Exception
    {
        public OAuthRefreshTimeoutException()
        {
        }

        public OAuthRefreshTimeoutException(string message)
            : base(message)
        {
        }

        public OAuthRefreshTimeoutException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
