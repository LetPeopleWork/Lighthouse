using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Moving every stored secret onto the key in force. Two rules carry the whole thing.
    //
    // Nothing is written that was not first read back as the credential it was. A value nobody can decrypt
    // is a value nobody can re-encrypt, and writing over it destroys the only copy - so it is left exactly
    // as it is and named in the report instead.
    //
    // A write names the value it observed. If a token refresh rewrote the row in between, the write finds
    // nothing to update and the row is left alone, which is right: the refresh already wrote it under the
    // key in force, so it arrived at the destination by another route. Writing anyway would put the token
    // the refresh replaced back over the one it obtained, and that is a credential nobody can recover
    // without going back to the work tracking system for a new one.
    //
    // What is left to do is written on the data itself - a stored secret names the key it is under - so an
    // interrupted pass needs no bookkeeping to resume from and a finished one finds nothing to do.
    public sealed class SecretCustodyService : ISecretCustodyService
    {
        internal const string AccessTokenField = "Access token";

        internal const string RefreshTokenField = "Refresh token";

        private readonly LighthouseAppContext context;

        private readonly ICryptoService cryptoService;

        private readonly IEncryptionKeyRingHolder keyRingHolder;

        private readonly IKeyRingMinter? minter;

        public SecretCustodyService(
            LighthouseAppContext context,
            ICryptoService cryptoService,
            IEncryptionKeyRingHolder keyRingHolder,
            IKeyRingMinter? minter = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(cryptoService);
            ArgumentNullException.ThrowIfNull(keyRingHolder);

            this.context = context;
            this.cryptoService = cryptoService;
            this.keyRingHolder = keyRingHolder;
            this.minter = minter;
        }

        public Task<SecretReadabilityReport> InspectAsync(CancellationToken cancellationToken = default)
        {
            return WalkAsync(moveThem: false, cancellationToken);
        }

        public Task<SecretReadabilityReport> ReEncryptAsync(CancellationToken cancellationToken = default)
        {
            return WalkAsync(moveThem: true, cancellationToken);
        }

        // Order is the safety. The key is made, written and read back before it becomes the key anything is
        // written under, so a failure anywhere before that leaves the ring that was in force and not one
        // stored secret touched. The key that was in force goes behind the new one rather than away, which
        // is what lets the pass be interrupted at any point and still leave every credential readable.
        public async Task<SecretReadabilityReport> RotateAsync(CancellationToken cancellationToken = default)
        {
            var inForce = keyRingHolder.Current;

            if (!inForce.CanMint || minter is null)
            {
                throw new MintingNotPermittedException(inForce.Custody);
            }

            var minted = minter.MintOnto(inForce.Without(LegacyDefaultEncryptionKey.Id));

            keyRingHolder.Replace(minted.WithLegacyDefault());

            var report = await WalkAsync(moveThem: true, cancellationToken);

            keyRingHolder.Replace(AnythingStillUnderThePublishedKey(report) ? minted.WithLegacyDefault() : minted);

            return report;
        }

        // Asked of what the pass actually found rather than of a count kept alongside it. A secret the pass
        // could not read still names the key it was written under, and if that is the key published with the
        // product then that key is still doing work and cannot be let go of.
        private static bool AnythingStillUnderThePublishedKey(SecretReadabilityReport report)
        {
            return report.Secrets.Any(secret =>
                secret.Outcome is not (SecretMoveOutcome.Moved or SecretMoveOutcome.MovedByAnotherWriter)
                && string.Equals(secret.KeyId, LegacyDefaultEncryptionKey.Id, StringComparison.Ordinal));
        }

        private async Task<SecretReadabilityReport> WalkAsync(bool moveThem, CancellationToken cancellationToken)
        {
            var activeKeyId = keyRingHolder.Current.ActiveKey.Id;
            var candidates = await CandidatesAsync(SecretEnvelope.Prefix + activeKeyId + ".", cancellationToken);

            var walked = new List<StoredSecretRecord>(candidates.Count);

            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                walked.Add(await WalkPastAsync(candidate, moveThem, cancellationToken));
            }

            return new SecretReadabilityReport(activeKeyId, walked);
        }

        private async Task<StoredSecretRecord> WalkPastAsync(
            StoredSecret candidate, bool moveThem, CancellationToken cancellationToken)
        {
            var secret = cryptoService.Read(candidate.StoredValue);

            var outcome = secret.State switch
            {
                SecretState.Unreadable => SecretMoveOutcome.CouldNotBeRead,
                SecretState.LegacyPlaintext => SecretMoveOutcome.NotEncrypted,
                _ when !moveThem => SecretMoveOutcome.Unmoved,
                _ => await MoveAsync(candidate, cryptoService.Encrypt(secret.PlainText!), cancellationToken),
            };

            return new StoredSecretRecord(
                candidate.ConnectionId,
                candidate.ConnectionName,
                candidate.Field,
                secret.KeyId,
                secret.State,
                outcome);
        }

        private async Task<SecretMoveOutcome> MoveAsync(
            StoredSecret candidate, string movedValue, CancellationToken cancellationToken)
        {
            try
            {
                var moved = candidate.Column switch
                {
                    SecretColumn.ConnectionOption => await context.Set<WorkTrackingSystemConnectionOption>()
                        .Where(option => option.Id == candidate.RowId && option.Value == candidate.StoredValue)
                        .ExecuteUpdateAsync(set => set.SetProperty(option => option.Value, movedValue), cancellationToken),

                    SecretColumn.AccessToken => await context.Set<OAuthCredential>()
                        .Where(credential => credential.Id == candidate.RowId && credential.AccessToken == candidate.StoredValue)
                        .ExecuteUpdateAsync(set => set.SetProperty(credential => credential.AccessToken, movedValue), cancellationToken),

                    _ => await context.Set<OAuthCredential>()
                        .Where(credential => credential.Id == candidate.RowId && credential.RefreshToken == candidate.StoredValue)
                        .ExecuteUpdateAsync(set => set.SetProperty(credential => credential.RefreshToken, movedValue), cancellationToken),
                };

                return moved == 1 ? SecretMoveOutcome.Moved : SecretMoveOutcome.MovedByAnotherWriter;
            }
            // A database that would not hand the row over is not a secret that cannot be read, and reporting
            // it as one would send an operator looking for a credential to reissue when nothing is wrong with
            // it. The row keeps naming the key it is under, so the next run picks it up.
            catch (DbException)
            {
                return SecretMoveOutcome.Unmoved;
            }
        }

        // The prefix is the whole of the question "is there anything left to do", and the database answers
        // it. Nothing is decrypted to find work, so a pass over an instance that is already finished costs
        // one query and no cryptography at all.
        private async Task<List<StoredSecret>> CandidatesAsync(string activeKeyPrefix, CancellationToken cancellationToken)
        {
            var options = await context.Set<WorkTrackingSystemConnectionOption>()
                .Where(option => option.IsSecret
                    && option.Value != null
                    && option.Value != string.Empty
                    && !option.Value.StartsWith(activeKeyPrefix))
                .Select(option => new StoredSecret(
                    option.WorkTrackingSystemConnectionId,
                    option.WorkTrackingSystemConnection.Name,
                    option.Key,
                    option.Value,
                    option.Id,
                    SecretColumn.ConnectionOption))
                .ToListAsync(cancellationToken);

            var credentials = await context.Set<OAuthCredential>()
                .Join(
                    context.WorkTrackingSystemConnections,
                    credential => credential.WorkTrackingSystemConnectionId,
                    connection => connection.Id,
                    (credential, connection) => new
                    {
                        credential.Id,
                        credential.AccessToken,
                        credential.RefreshToken,
                        ConnectionId = connection.Id,
                        connection.Name,
                    })
                .Where(row => !row.AccessToken.StartsWith(activeKeyPrefix)
                    || !row.RefreshToken.StartsWith(activeKeyPrefix))
                .ToListAsync(cancellationToken);

            foreach (var row in credentials)
            {
                if (!string.IsNullOrEmpty(row.AccessToken) && !row.AccessToken.StartsWith(activeKeyPrefix, StringComparison.Ordinal))
                {
                    options.Add(new StoredSecret(
                        row.ConnectionId, row.Name, AccessTokenField, row.AccessToken, row.Id, SecretColumn.AccessToken));
                }

                if (!string.IsNullOrEmpty(row.RefreshToken) && !row.RefreshToken.StartsWith(activeKeyPrefix, StringComparison.Ordinal))
                {
                    options.Add(new StoredSecret(
                        row.ConnectionId, row.Name, RefreshTokenField, row.RefreshToken, row.Id, SecretColumn.RefreshToken));
                }
            }

            return options;
        }

        private enum SecretColumn
        {
            ConnectionOption,
            AccessToken,
            RefreshToken,
        }

        private sealed record StoredSecret(
            int ConnectionId,
            string ConnectionName,
            string Field,
            string StoredValue,
            int RowId,
            SecretColumn Column);
    }
}
