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

        private readonly IKeyRingMinter minter;

        private readonly OneSecretPassAtATime oneAtATime;

        public SecretCustodyService(
            LighthouseAppContext context,
            ICryptoService cryptoService,
            IEncryptionKeyRingHolder keyRingHolder,
            IKeyRingMinter minter,
            OneSecretPassAtATime oneAtATime)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(cryptoService);
            ArgumentNullException.ThrowIfNull(keyRingHolder);
            ArgumentNullException.ThrowIfNull(minter);
            ArgumentNullException.ThrowIfNull(oneAtATime);

            this.context = context;
            this.cryptoService = cryptoService;
            this.keyRingHolder = keyRingHolder;
            this.minter = minter;
            this.oneAtATime = oneAtATime;
        }

        public Task<SecretReadabilityReport> InspectAsync(CancellationToken cancellationToken = default)
        {
            return WalkAsync(moveThem: false, cancellationToken);
        }

        public Task<SecretReadabilityReport> ReEncryptAsync(CancellationToken cancellationToken = default)
        {
            return oneAtATime.RunAsync(stopping => WalkAsync(moveThem: true, stopping), cancellationToken);
        }

        // Order is the safety. The key is made, written and read back before it becomes the key anything is
        // written under, so a failure anywhere before that leaves the ring that was in force and not one
        // stored secret touched. The key that was in force goes behind the new one rather than away, which
        // is what lets the pass be interrupted at any point and still leave every credential readable.
        public Task<SecretReadabilityReport> RotateAsync(CancellationToken cancellationToken = default)
        {
            return oneAtATime.RunAsync(MintThenMoveEverythingAsync, cancellationToken);
        }

        // The ring only ever grows here. Every key that could read something a moment ago can still read it
        // afterwards, which is what makes an interruption survivable - and it is also why nothing is taken
        // off the ring at the end. A request that loaded a credential before the rotation started is still
        // holding it, and narrowing the ring under that request would turn a credential it is about to use
        // into one it cannot read.
        private async Task<SecretReadabilityReport> MintThenMoveEverythingAsync(CancellationToken cancellationToken)
        {
            var minted = minter.MintOnto(keyRingHolder.Current);

            keyRingHolder.Replace(minted.WithLegacyDefault());

            return await WalkAsync(moveThem: true, cancellationToken);
        }

        private async Task<SecretReadabilityReport> WalkAsync(bool moveThem, CancellationToken cancellationToken)
        {
            // Taken once and then used for all three things a pass decides - what is left to do, what each
            // row is written under, and what the report is labelled with. An operator replacing a mounted
            // keys file while this runs is a pair of actions the product invites, and asking again per row
            // would let the three answers come apart without anything noticing.
            var activeKey = keyRingHolder.Current.ActiveKey;

            // A pass that writes asks the database what is left to do, and everything already on the key in
            // force is not it. A pass that only looks is answering a different question - what is stored -
            // and filtering the answered-already rows out of it would leave a freshly rotated instance
            // reporting nothing at all.
            var candidates = await CandidatesAsync(
                moveThem ? SecretEnvelope.Prefix + activeKey.Id + "." : null, cancellationToken);

            var walked = new List<StoredSecretRecord>(candidates.Count);

            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                walked.Add(await WalkPastAsync(candidate, activeKey, moveThem, cancellationToken));
            }

            return new SecretReadabilityReport(activeKey.Id, walked);
        }

        private async Task<StoredSecretRecord> WalkPastAsync(
            Candidate candidate, EncryptionKey activeKey, bool moveThem, CancellationToken cancellationToken)
        {
            var secret = cryptoService.Read(candidate.StoredValue);

            var outcome = secret.State switch
            {
                SecretState.Unreadable => SecretMoveOutcome.CouldNotBeRead,
                SecretState.LegacyPlaintext => SecretMoveOutcome.NotEncrypted,
                _ when !moveThem => SecretMoveOutcome.Unmoved,
                _ => await MoveAsync(candidate, cryptoService.Encrypt(secret.PlainText!, activeKey), cancellationToken),
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
            Candidate candidate, string movedValue, CancellationToken cancellationToken)
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

                    SecretColumn.RefreshToken => await context.Set<OAuthCredential>()
                        .Where(credential => credential.Id == candidate.RowId && credential.RefreshToken == candidate.StoredValue)
                        .ExecuteUpdateAsync(set => set.SetProperty(credential => credential.RefreshToken, movedValue), cancellationToken),

                    // A fourth place to store a credential would arrive here silently and be written into the
                    // refresh token if this fell through to a default.
                    _ => throw new NotSupportedException($"There is no way to write the stored secret held in {candidate.Column}."),
                };

                return moved == 1 ? SecretMoveOutcome.Moved : SecretMoveOutcome.MovedByAnotherWriter;
            }
            // A database that would not take the write is not a secret that cannot be read, and reporting it
            // as one would send an operator looking for a credential to reissue when nothing is wrong with
            // it. It is not silence either: the row is named in the report as one this pass could not write,
            // so "nothing happened and I was not told" is not a state an operator can end up in. The row
            // keeps naming the key it is under, so running the pass again picks it up.
            catch (DbException)
            {
                return SecretMoveOutcome.CouldNotBeWritten;
            }
        }

        // With a prefix, this is the whole of the question "is there anything left to do", and the database
        // answers it. Nothing is decrypted to find work, so a pass over an instance that is already finished
        // costs one query and no cryptography at all. Without one, it is every stored secret there is, which
        // is two queries whatever the instance holds.
        private async Task<List<Candidate>> CandidatesAsync(string? activeKeyPrefix, CancellationToken cancellationToken)
        {
            var options = context.Set<WorkTrackingSystemConnectionOption>()
                .Where(option => option.IsSecret && !string.IsNullOrEmpty(option.Value));

            var credentialRows = context.Set<OAuthCredential>()
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
                    });

            if (activeKeyPrefix is not null)
            {
                options = options.Where(option => !option.Value.StartsWith(activeKeyPrefix));

                credentialRows = credentialRows.Where(row => !row.AccessToken.StartsWith(activeKeyPrefix)
                    || !row.RefreshToken.StartsWith(activeKeyPrefix));
            }

            var candidates = await options
                .Select(option => new Candidate(
                    option.WorkTrackingSystemConnectionId,
                    option.WorkTrackingSystemConnection.Name,
                    option.Key,
                    option.Value,
                    option.Id,
                    SecretColumn.ConnectionOption))
                .ToListAsync(cancellationToken);

            var credentials = await credentialRows.ToListAsync(cancellationToken);

            // A credential row is fetched when either of its two tokens qualifies, so each token is asked
            // about again here. The question is the same one the database answered.
            foreach (var row in credentials)
            {
                if (Qualifies(row.AccessToken, activeKeyPrefix))
                {
                    candidates.Add(new Candidate(
                        row.ConnectionId, row.Name, AccessTokenField, row.AccessToken, row.Id, SecretColumn.AccessToken));
                }

                if (Qualifies(row.RefreshToken, activeKeyPrefix))
                {
                    candidates.Add(new Candidate(
                        row.ConnectionId, row.Name, RefreshTokenField, row.RefreshToken, row.Id, SecretColumn.RefreshToken));
                }
            }

            return candidates;
        }

        private static bool Qualifies(string storedValue, string? activeKeyPrefix)
        {
            return !string.IsNullOrEmpty(storedValue)
                && (activeKeyPrefix is null || !storedValue.StartsWith(activeKeyPrefix, StringComparison.Ordinal));
        }

        private enum SecretColumn
        {
            ConnectionOption,
            AccessToken,
            RefreshToken,
        }

        // What the pass carries between finding a secret and deciding what happened to it. Deliberately not
        // the record the report is made of: this one holds the stored value, and that one must never be able
        // to, because the report travels to a browser and into a log.
        private sealed record Candidate(
            int ConnectionId,
            string ConnectionName,
            string Field,
            string StoredValue,
            int RowId,
            SecretColumn Column);
    }
}
