using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // The settings page asks this on every load, so the rows are narrowed in the database first: anything
    // already carrying an envelope that names the key in force is nothing to move, and on an instance that
    // has moved everything that is every row, so nothing is decrypted at all. What is left to read is
    // bounded by the number of credentials the operator is about to be offered a button for.
    //
    // Reading is the only way to finish the answer. A value that predates the envelope names no key, and a
    // value naming a key this instance no longer holds cannot be moved however much it would like to be -
    // both look identical to any predicate over the stored text, and only a decrypt attempt separates them.
    // A never-encrypted value is deliberately not counted: the pass leaves those where they are and names
    // them for re-entry, so offering a move on their account would promise something it does not do.
    public sealed class ReadableSecretsNotOnTheActiveKey : IReadableSecretsNotOnTheActiveKey
    {
        private readonly LighthouseAppContext context;

        private readonly ICryptoService cryptoService;

        private readonly IEncryptionKeyRingHolder keyRingHolder;

        public ReadableSecretsNotOnTheActiveKey(
            LighthouseAppContext context, ICryptoService cryptoService, IEncryptionKeyRingHolder keyRingHolder)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(cryptoService);
            ArgumentNullException.ThrowIfNull(keyRingHolder);

            this.context = context;
            this.cryptoService = cryptoService;
            this.keyRingHolder = keyRingHolder;
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            var writtenUnderTheKeyInForce = SecretEnvelope.Prefix + keyRingHolder.Current.ActiveKey.Id + ".";

            var options = await context.Set<WorkTrackingSystemConnectionOption>()
                .Where(option => option.IsSecret
                    && !string.IsNullOrEmpty(option.Value)
                    && !option.Value.StartsWith(writtenUnderTheKeyInForce))
                .Select(option => option.Value)
                .ToListAsync(cancellationToken);

            var accessTokens = await context.Set<OAuthCredential>()
                .Where(credential => !string.IsNullOrEmpty(credential.AccessToken)
                    && !credential.AccessToken.StartsWith(writtenUnderTheKeyInForce))
                .Select(credential => credential.AccessToken)
                .ToListAsync(cancellationToken);

            var refreshTokens = await context.Set<OAuthCredential>()
                .Where(credential => !string.IsNullOrEmpty(credential.RefreshToken)
                    && !credential.RefreshToken.StartsWith(writtenUnderTheKeyInForce))
                .Select(credential => credential.RefreshToken)
                .ToListAsync(cancellationToken);

            return options
                .Concat(accessTokens)
                .Concat(refreshTokens)
                .Count(CouldBeMovedOntoTheKeyInForce);
        }

        private bool CouldBeMovedOntoTheKeyInForce(string storedValue)
        {
            var state = cryptoService.Read(storedValue).State;

            return state is SecretState.Envelope or SecretState.LegacyCbc;
        }
    }
}
