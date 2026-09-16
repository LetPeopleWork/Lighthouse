using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.ConnectionHealth;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.ConnectionHealth;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.ConnectionHealth
{
#pragma warning disable S107 // Answering "how is this connection" needs all three stored facts - the connection, its verdict and its OAuth grant - plus the connector that asks the tracker, the reader that refuses an undecryptable secret before anything leaves the machine, and the clock the observed-at moment comes from. No subset of those is ever decided together, so a parameter object here would name nothing and only move the count.
    public sealed class ConnectionHealthService(
        IRepository<WorkTrackingSystemConnection> connectionRepository,
        ConnectionHealthVerdictRepository verdictRepository,
        IRepository<OAuthCredential> credentialRepository,
        IWorkTrackingConnectorFactory connectorFactory,
        ICryptoService cryptoService,
        ConnectionHealthCadence cadence,
        ILighthouseClock clock,
        ILogger<ConnectionHealthService> logger)
        : IConnectionHealthService
#pragma warning restore S107
    {
        /// <summary>
        /// The one code that means "the credential was refused". Every other code a connector can answer
        /// with - including the ones about permissions or a bad URL - reads as unreachable, because
        /// naming the credential wrongly costs an administrator an afternoon reissuing one that worked.
        /// </summary>
        private const string RejectedCredentialCode = "authentication_failed";

        private const string UnreadableSecretCode = "secret_cannot_be_read";

        public Task<IReadOnlyList<ConnectionHealthDto>> GetHealthAsync()
        {
            var verdicts = verdictRepository.GetAll().ToDictionary(verdict => verdict.WorkTrackingSystemConnectionId);

            // One row per connection, because the schema says so: OAuthCredentials carries a unique index
            // on the connection id. Grouping defensively here would turn a corrupt table into a plausible
            // answer instead of a failure somebody looks at.
            var credentials = credentialRepository.GetAll().ToDictionary(credential => credential.WorkTrackingSystemConnectionId);

            IReadOnlyList<ConnectionHealthDto> health =
            [
                .. connectionRepository.GetAll().Select(connection => Describe(
                    connection,
                    verdicts.GetValueOrDefault(connection.Id),
                    credentials.GetValueOrDefault(connection.Id))),
            ];

            return Task.FromResult(health);
        }

        public async Task<ConnectionHealthDto?> TestConnectionAsync(int connectionId)
        {
            var connection = connectionRepository.GetById(connectionId);
            if (connection == null)
            {
                return null;
            }

            await RecordAsync(connection, await ClassifyAsync(connection));

            return Describe(connection, VerdictFor(connectionId), CredentialFor(connectionId));
        }

        public async Task RecordRefreshSucceededAsync(WorkTrackingSystemConnection connection)
        {
            ArgumentNullException.ThrowIfNull(connection);

            await RecordAsync(connection, ConnectionValidationResult.Success());
        }

        public async Task RecordRefreshFailedAsync(WorkTrackingSystemConnection connection)
        {
            ArgumentNullException.ThrowIfNull(connection);

            await RecordAsync(connection, await ClassifyAsync(connection));
        }

        public async Task RefreshStaleVerdictsAsync(CancellationToken cancellationToken)
        {
            var now = clock.Now.UtcDateTime;
            var cutoff = now - cadence.HowFreshAnAnswerMustBe;

            // One at a time. Every connection asked at once is a burst of outbound calls the moment a
            // process starts, which is the picture the decision against a probe loop had in mind; asked in
            // turn it is a handful of requests spread over a few seconds that nobody is waiting on.
            foreach (var connection in connectionRepository.GetAll().ToList())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (await verdictRepository.TryClaimForProbeAsync(connection.Id, cutoff, now, cancellationToken))
                {
                    await AskHowItIs(connection);
                }
            }
        }

        /// <summary>
        /// One connection that answers by raising must not cost every other connection its health. The
        /// connection keeps the verdict the claim left it - the moment advanced, so it is not asked again
        /// until it goes stale again, which is what stops a broken connector being retried on every tick.
        /// </summary>
        private async Task AskHowItIs(WorkTrackingSystemConnection connection)
        {
            try
            {
                await RecordAsync(connection, await ClassifyAsync(connection));
            }
#pragma warning disable CA1031 // one connection's connector must not decide whether the others get asked
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogWarning(exception, "Checking the health of '{ConnectionName}' failed", connection.Name);
            }
        }


        /// <summary>
        /// Why this connection is not working, asked of the connection itself. The refresh that failed
        /// arrives here carrying nothing but an exception, so the only thing that can tell a refused
        /// credential from a tracker that was down is the classifier the connectors already ship.
        /// </summary>
        private async Task<ConnectionValidationResult> ClassifyAsync(WorkTrackingSystemConnection connection)
        {
            // A secret this instance can no longer decrypt must never be handed to the tracker. The tracker
            // would refuse it exactly as it refuses an expired one, and an administrator reading that goes
            // off to reissue a credential that was intact. The answer is here, before anything leaves the
            // machine, and it is the same question the connection screen asks.
            var lostField = ConnectionSecrets.FieldsThatCannotBeRead(connection, cryptoService).FirstOrDefault();
            if (lostField != null)
            {
                return ConnectionValidationResult.Failure(
                    UnreadableSecretCode,
                    $"The stored {lostField} on '{connection.Name}' cannot be read with the current encryption key. "
                    + "Enter it again to store it under the key this instance uses now.",
                    fieldName: lostField);
            }

            return await connectorFactory
                .GetWorkTrackingConnector(connection.WorkTrackingSystem)
                .ValidateConnection(connection);
        }

        /// <summary>
        /// The verdict replaces whatever was recorded before rather than joining it. The question the
        /// popover asks is "how is this connection now", so a second row would only ever be the answer
        /// to a question nobody asked.
        /// </summary>
        private async Task RecordAsync(WorkTrackingSystemConnection connection, ConnectionValidationResult result)
        {
            var verdict = VerdictFor(connection.Id);

            if (verdict == null)
            {
                verdict = new ConnectionHealthVerdict { WorkTrackingSystemConnectionId = connection.Id };
                verdictRepository.Add(verdict);
            }

            // An existing verdict was just read through the same context, so writing to it is enough to
            // have it saved - calling Update as well reads as though it were doing something.
            verdict.State = StateFor(result);
            verdict.Code = result.Code;
            verdict.Message = result.Message;
            verdict.ObservedAt = clock.Now.UtcDateTime;

            await verdictRepository.Save();
        }

        private static ConnectionHealthState StateFor(ConnectionValidationResult result)
        {
            if (result.IsValid)
            {
                return ConnectionHealthState.Healthy;
            }

            return result.Code is RejectedCredentialCode or UnreadableSecretCode
                ? ConnectionHealthState.AuthenticationFailed
                : ConnectionHealthState.Unreachable;
        }

        private ConnectionHealthVerdict? VerdictFor(int connectionId)
            => verdictRepository.GetByPredicate(verdict => verdict.WorkTrackingSystemConnectionId == connectionId);

        private OAuthCredential? CredentialFor(int connectionId)
            => credentialRepository.GetByPredicate(credential => credential.WorkTrackingSystemConnectionId == connectionId);

        /// <summary>
        /// A broken OAuth grant is folded in rather than replacing the verdict, so the wording an
        /// administrator already knows survives the icon that carried it. A recorded rejection still wins:
        /// a live 401 is the tracker speaking later than a credential row that still believes it is valid.
        /// </summary>
        private static ConnectionHealthDto Describe(
            WorkTrackingSystemConnection connection,
            ConnectionHealthVerdict? verdict,
            OAuthCredential? credential)
        {
            if (verdict?.State != ConnectionHealthState.AuthenticationFailed
                && credential is { Status: not OAuthCredentialStatus.Valid })
            {
                return new ConnectionHealthDto(
                    connection.Id,
                    connection.Name,
                    connection.WorkTrackingSystem.ToString(),
                    nameof(ConnectionHealthState.AuthenticationFailed),
                    $"The authorisation for '{connection.Name}' is no longer valid. Reconnect it to restore access.",
                    credential.UpdatedAt.UtcDateTime);
            }

            return new ConnectionHealthDto(
                connection.Id,
                connection.Name,
                connection.WorkTrackingSystem.ToString(),
                (verdict?.State ?? ConnectionHealthState.Unknown).ToString(),
                verdict?.Message,
                verdict?.ObservedAt);
        }
    }
}
