namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth
{
    public class OAuthCredentialNotValidException : Exception
    {
        public OAuthCredentialNotValidException()
        {
        }

        public OAuthCredentialNotValidException(string message)
            : base(message)
        {
        }

        public OAuthCredentialNotValidException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
