using System.Diagnostics;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.OAuth
{
#pragma warning disable S107 // OAuth flow legitimately needs 8 collaborators: provider registry, two repositories, crypto, state-token issuer, service config, time provider, and logger. Splitting them into an aggregate just to dodge the threshold would add indirection without a domain rationale.
    public class OAuthService(
        IOAuthProviderRegistry providerRegistry,
        IRepository<WorkTrackingSystemConnection> connectionRepository,
        IRepository<OAuthCredential> credentialRepository,
        ICryptoService cryptoService,
        IOAuthStateTokenIssuer stateTokenIssuer,
        IServiceConfig serviceConfig,
        TimeProvider timeProvider,
        ILogger<OAuthService> logger) : IOAuthService
#pragma warning restore S107
    {
        private const string CallbackPath = "/api/oauth/callback";

        public Task<Uri> InitiateAsync(int connectionId, CancellationToken cancellationToken)
        {
            var connection = LoadConnectionOrThrow(connectionId);
            var provider = providerRegistry.GetByKey(connection.AuthenticationMethodKey);
            var stateToken = stateTokenIssuer.Issue(connectionId, connection.AuthenticationMethodKey);
            var flowContext = BuildFlowContext(connection, provider, stateToken);

            var authorizationUrl = provider.BuildAuthorizationUrl(flowContext);

            logger.LogInformation(
                "oauth.flow.initiated {ConnectionId} {ProviderKey}",
                connectionId,
                connection.AuthenticationMethodKey);

            return Task.FromResult(authorizationUrl);
        }

        public async Task<OAuthCompleteResult> CompleteAsync(string code, string state, CancellationToken cancellationToken)
        {
            var claims = stateTokenIssuer.Verify(state);
            var connection = LoadConnectionOrThrow(claims.ConnectionId);
            var provider = providerRegistry.GetByKey(connection.AuthenticationMethodKey);
            var flowContext = BuildFlowContext(connection, provider, state);

            var start = Stopwatch.GetTimestamp();
            var tokens = await provider.ExchangeCodeAsync(code, flowContext, cancellationToken);
            var durationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            var credential = new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt,
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = timeProvider.GetUtcNow(),
            };
            credentialRepository.Add(credential);
            await credentialRepository.Save();

            logger.LogInformation(
                "oauth.flow.completed {ConnectionId} {ProviderKey} {DurationMs} {Scopes}",
                connection.Id,
                connection.AuthenticationMethodKey,
                durationMs,
                provider.DefaultScopes);

            return new OAuthCompleteResult(connection.Id, OAuthCredentialStatus.Valid, null);
        }

        public async Task DisconnectAsync(int connectionId, CancellationToken cancellationToken)
        {
            var credential = credentialRepository.GetByPredicate(c => c.WorkTrackingSystemConnectionId == connectionId);
            if (credential is null)
            {
                logger.LogDebug(
                    "oauth.credential.status_changed {ConnectionId} {FromStatus} {ToStatus}",
                    connectionId,
                    "None",
                    OAuthCredentialStatus.Disconnected);
                return;
            }

            var fromStatus = credential.Status;
            credential.Status = OAuthCredentialStatus.Disconnected;
            credential.AccessToken = string.Empty;
            credential.RefreshToken = string.Empty;
            credential.UpdatedAt = timeProvider.GetUtcNow();
            credentialRepository.Update(credential);
            await credentialRepository.Save();

            logger.LogInformation(
                "oauth.credential.status_changed {CredentialId} {ConnectionId} {FromStatus} {ToStatus}",
                credential.Id,
                connectionId,
                fromStatus,
                OAuthCredentialStatus.Disconnected);
        }

        public Task<string> EnsureFreshTokenAsync(int connectionId, CancellationToken cancellationToken)
        {
            var credential = credentialRepository.GetByPredicate(c => c.WorkTrackingSystemConnectionId == connectionId);
            if (credential is null)
            {
                throw new InvalidOperationException(
                    $"No OAuth credential found for connection {connectionId}. Complete the OAuth connect flow before invoking it.");
            }

            if (credential.Status != OAuthCredentialStatus.Valid)
            {
                throw new InvalidOperationException(
                    $"OAuth credential for connection {connectionId} is in status {credential.Status}. Reconnect required.");
            }

            var decryptedAccessToken = cryptoService.Decrypt(credential.AccessToken);
            return Task.FromResult(decryptedAccessToken);
        }

        private WorkTrackingSystemConnection LoadConnectionOrThrow(int connectionId)
        {
            var connection = connectionRepository.GetById(connectionId);
            if (connection is null)
            {
                throw new ArgumentException(
                    $"WorkTrackingSystemConnection with Id {connectionId} was not found.",
                    nameof(connectionId));
            }
            return connection;
        }

        private OAuthFlowContext BuildFlowContext(
            WorkTrackingSystemConnection connection,
            IOAuthProvider provider,
            string stateToken)
        {
            if (string.IsNullOrWhiteSpace(serviceConfig.BaseUrl))
            {
                throw new InvalidOperationException(
                    "Lighthouse:BaseUrl is not configured. OAuth callback URL cannot be derived. " +
                    "Set Lighthouse:BaseUrl in server configuration before initiating an OAuth flow.");
            }

            var encryptedClientId = connection.GetWorkTrackingSystemConnectionOptionByKey(OAuthWorkTrackingOptionNames.ClientId);
            var encryptedClientSecret = connection.GetWorkTrackingSystemConnectionOptionByKey(OAuthWorkTrackingOptionNames.ClientSecret);
            var clientId = cryptoService.Decrypt(encryptedClientId);
            var clientSecret = cryptoService.Decrypt(encryptedClientSecret);
            var redirectUri = new Uri(new Uri(serviceConfig.BaseUrl), CallbackPath);

            return new OAuthFlowContext(
                connection.Id,
                connection.AuthenticationMethodKey,
                clientId,
                clientSecret,
                redirectUri,
                stateToken,
                provider.DefaultScopes);
        }
    }
}
