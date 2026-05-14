using System.Diagnostics;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public class OAuthService(
        IOAuthProviderRegistry providerRegistry,
        IRepository<WorkTrackingSystemConnection> connectionRepository,
        IRepository<OAuthCredential> credentialRepository,
        ICryptoService cryptoService,
        IOAuthStateTokenIssuer stateTokenIssuer,
        IServiceConfig serviceConfig,
        TimeProvider timeProvider,
        ILogger<OAuthService> logger) : IOAuthService
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
            OAuthStateClaims claims;
            try
            {
                claims = stateTokenIssuer.Verify(state);
            }
            catch (OAuthStateTokenInvalidException ex)
            {
                logger.LogWarning(
                    ex,
                    "oauth.callback.invalid_state {Reason}",
                    ex.Message);
                throw;
            }

            var connection = LoadConnectionOrThrow(claims.ConnectionId);
            var provider = providerRegistry.GetByKey(connection.AuthenticationMethodKey);
            var flowContext = BuildFlowContext(connection, provider, state);

            var start = Stopwatch.GetTimestamp();
            OAuthTokens tokens;
            try
            {
                tokens = await provider.ExchangeCodeAsync(code, flowContext, cancellationToken);
            }
            catch (Exception ex)
            {
                var failureDurationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                logger.LogWarning(
                    ex,
                    "oauth.flow.failed {ConnectionId} {ProviderKey} {DurationMs}",
                    connection.Id,
                    connection.AuthenticationMethodKey,
                    failureDurationMs);
                throw;
            }
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
            throw new NotImplementedException("OAuthService.EnsureFreshTokenAsync ships in step 02-01");
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
