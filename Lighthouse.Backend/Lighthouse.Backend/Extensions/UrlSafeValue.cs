using System.Security.Cryptography;

namespace Lighthouse.Backend.Extensions
{
    public static class UrlSafeValue
    {
        /// <summary>
        /// A cryptographically random value in base64url - safe in a header, a URL and a JSON body
        /// without further escaping.
        /// </summary>
        /// <remarks>
        /// Three older call sites spell this out inline (the API key, the embed session token and
        /// the OAuth state token). They are deliberately left alone here: they are security paths
        /// with their own tests, and folding them in belongs in a change that is about them rather
        /// than riding along with an unrelated feature.
        /// </remarks>
        public static string Generate(int byteLength)
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
