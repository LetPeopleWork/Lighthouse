namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public class OAuthStateTokenExpiredException : Exception
    {
        public OAuthStateTokenExpiredException()
        {
        }

        public OAuthStateTokenExpiredException(string message)
            : base(message)
        {
        }

        public OAuthStateTokenExpiredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
