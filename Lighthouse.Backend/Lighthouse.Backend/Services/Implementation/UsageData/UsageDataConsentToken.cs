using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Services.Implementation.UsageData
{
    internal static class UsageDataConsentToken
    {
        /// <summary>
        /// Turns the token a browser presents into the value its consent is stored under. The part
        /// that records a decision and the part that checks one have to arrive at the same value
        /// from the same token, and nothing would say so if they stopped: the record would simply
        /// not be found, every batch would be dropped for want of a consent that is sitting right
        /// there, and the only sign of it is usage data quietly ceasing to exist. One place to
        /// compute it is what makes that impossible rather than merely unlikely.
        ///
        /// The token is 256 bits of randomness, so a fast digest is the right tool and a password
        /// KDF is not: there is no low-entropy secret here to slow an attacker down over.
        /// </summary>
        internal static string HashOf(string token)
        {
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}
