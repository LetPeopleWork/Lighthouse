namespace Lighthouse.Backend.Services.Implementation.OAuth
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
