namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public class OAuthStateTokenInvalidException : Exception
    {
        public OAuthStateTokenInvalidException()
        {
        }

        public OAuthStateTokenInvalidException(string message)
            : base(message)
        {
        }

        public OAuthStateTokenInvalidException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
