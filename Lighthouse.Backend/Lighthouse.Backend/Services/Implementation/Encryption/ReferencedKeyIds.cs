using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Read off the front of the stored values rather than by decrypting them. The id of the key that
    // wrote a secret is written there in plain text - that is what makes an envelope readable by the
    // right key and refused by every other one - so asking any other way would cost a decrypt of every
    // credential to learn something already visible. It would also make drawing a table depend on every
    // stored secret being readable, and the administrator most likely to be looking at this screen is
    // the one whose instance cannot read them.
    //
    // A value written before the envelope format existed carries no id and is not counted: nothing can
    // say which key wrote it, and inventing one would name a key that may never have existed.
    public sealed class ReferencedKeyIds : IReferencedKeyIds
    {
        // The id sits between the version token and the nonce, and a key id is at most 32 characters, so
        // this much of a stored value always holds the whole id and never holds any ciphertext. One
        // character less would silently rename the longest key an operator can define.
        //
        // The prefix is taken in the database, not in memory, and that is what makes it safe on a value
        // that only looks like an envelope - a truncated column, a half-finished restore, somebody
        // editing by hand. SQL substring returns what is there when asked for more than a value has,
        // where the same call in memory would throw; and a remnant with no second separator in it names
        // no key, so nothing downstream mistakes it for one.
        private const int AsFarAsAKeyIdCanReach = 37;

        private readonly LighthouseAppContext context;

        public ReferencedKeyIds(LighthouseAppContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            this.context = context;
        }

        public async Task<IReadOnlyCollection<string>> ReadAsync(CancellationToken cancellationToken = default)
        {
            var options = await context.Set<WorkTrackingSystemConnectionOption>()
                .Where(option => option.IsSecret && option.Value.StartsWith(SecretEnvelope.Prefix))
                .Select(option => option.Value.Substring(0, AsFarAsAKeyIdCanReach))
                .ToListAsync(cancellationToken);

            var accessTokens = await context.Set<OAuthCredential>()
                .Where(credential => credential.AccessToken.StartsWith(SecretEnvelope.Prefix))
                .Select(credential => credential.AccessToken.Substring(0, AsFarAsAKeyIdCanReach))
                .ToListAsync(cancellationToken);

            var refreshTokens = await context.Set<OAuthCredential>()
                .Where(credential => credential.RefreshToken.StartsWith(SecretEnvelope.Prefix))
                .Select(credential => credential.RefreshToken.Substring(0, AsFarAsAKeyIdCanReach))
                .ToListAsync(cancellationToken);

            return [.. options
                .Concat(accessTokens)
                .Concat(refreshTokens)
                .Select(NameOnTheFrontOf)
                .OfType<string>()
                .Distinct(StringComparer.Ordinal)];
        }

        private static string? NameOnTheFrontOf(string storedValue)
        {
            var id = storedValue.AsSpan(SecretEnvelope.Prefix.Length);
            var end = id.IndexOf('.');

            return end <= 0 ? null : id[..end].ToString();
        }
    }
}
