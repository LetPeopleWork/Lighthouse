namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public class OAuthProviderResponseException : Exception
    {
        public OAuthProviderResponseException(string providerKey, int httpStatus, string idpErrorCode, string idpErrorDescription)
            : base(FormatMessage(providerKey, httpStatus, idpErrorCode, idpErrorDescription))
        {
            ProviderKey = providerKey;
            HttpStatus = httpStatus;
            IdpErrorCode = idpErrorCode;
            IdpErrorDescription = idpErrorDescription;
        }

        public string ProviderKey { get; }

        public int HttpStatus { get; }

        public string IdpErrorCode { get; }

        public string IdpErrorDescription { get; }

        private static string FormatMessage(string providerKey, int httpStatus, string idpErrorCode, string idpErrorDescription)
        {
            return $"OAuth provider '{providerKey}' returned HTTP {httpStatus}: {idpErrorCode} - {idpErrorDescription}";
        }
    }
}
