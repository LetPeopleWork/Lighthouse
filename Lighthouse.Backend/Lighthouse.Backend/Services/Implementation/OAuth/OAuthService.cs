using System.Collections.Concurrent;
using System.Diagnostics;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;

namespace Lighthouse.Backend.Services.Implementation.OAuth
{
#pragma warning disable S107 // OAuth flow legitimately needs 9 collaborators plus refresh options (ADR-010 single-flight refresh window/timeout). Splitting them into an aggregate just to dodge the threshold would add indirection without a domain rationale.
    public class OAuthService : IOAuthService
#pragma warning restore S107
    {
        private const string CallbackPath = "/api/oauth/callback";
        private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(5);

        private readonly IOAuthProviderRegistry providerRegistry;
        private readonly IRepository<WorkTrackingSystemConnection> connectionRepository;
        private readonly IRepository<OAuthCredential> credentialRepository;
        private readonly ICryptoService cryptoService;
        private readonly IOAuthStateTokenIssuer stateTokenIssuer;
        private readonly IServiceConfig serviceConfig;
        private readonly TimeProvider timeProvider;
        private readonly ILogger<OAuthService> logger;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly OAuthRefreshOptions refreshOptions;
        private readonly ConcurrentDictionary<int, SemaphoreSlim> refreshLocks = new();

        public OAuthService(
            IOAuthProviderRegistry providerRegistry,
            IRepository<WorkTrackingSystemConnection> connectionRepository,
            IRepository<OAuthCredential> credentialRepository,
            ICryptoService cryptoService,
            IOAuthStateTokenIssuer stateTokenIssuer,
            IServiceConfig serviceConfig,
            TimeProvider timeProvider,
            ILogger<OAuthService> logger,
            IHttpContextAccessor httpContextAccessor)
            : this(
                providerRegistry,
                connectionRepository,
                credentialRepository,
                cryptoService,
                stateTokenIssuer,
                serviceConfig,
                timeProvider,
                logger,
                httpContextAccessor,
                OAuthRefreshOptions.Default)
        {
        }

        public OAuthService(
            IOAuthProviderRegistry providerRegistry,
            IRepository<WorkTrackingSystemConnection> connectionRepository,
            IRepository<OAuthCredential> credentialRepository,
            ICryptoService cryptoService,
            IOAuthStateTokenIssuer stateTokenIssuer,
            IServiceConfig serviceConfig,
            TimeProvider timeProvider,
            ILogger<OAuthService> logger,
            IHttpContextAccessor httpContextAccessor,
            OAuthRefreshOptions refreshOptions)
        {
            this.providerRegistry = providerRegistry;
            this.connectionRepository = connectionRepository;
            this.credentialRepository = credentialRepository;
            this.cryptoService = cryptoService;
            this.stateTokenIssuer = stateTokenIssuer;
            this.serviceConfig = serviceConfig;
            this.timeProvider = timeProvider;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
            this.refreshOptions = refreshOptions;
        }

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

            UpsertValidCredential(connection.Id, tokens);
            await credentialRepository.Save();

            logger.LogInformation(
                "oauth.flow.completed {ConnectionId} {ProviderKey} {DurationMs} {Scopes}",
                connection.Id,
                connection.AuthenticationMethodKey,
                durationMs,
                provider.DefaultScopes);

            return new OAuthCompleteResult(connection.Id, OAuthCredentialStatus.Valid, null);
        }

        private void UpsertValidCredential(int connectionId, OAuthTokens tokens)
        {
            var existing = credentialRepository.GetByPredicate(c => c.WorkTrackingSystemConnectionId == connectionId);
            var now = timeProvider.GetUtcNow();

            if (existing is not null)
            {
                existing.AccessToken = tokens.AccessToken;
                existing.RefreshToken = tokens.RefreshToken;
                existing.ExpiresAt = tokens.ExpiresAt;
                existing.Status = OAuthCredentialStatus.Valid;
                existing.UpdatedAt = now;
                credentialRepository.Update(existing);
                return;
            }

            credentialRepository.Add(new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connectionId,
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt,
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = now,
            });
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

        public async Task<string> EnsureFreshTokenAsync(int connectionId, CancellationToken cancellationToken)
        {
            var credential = LoadValidCredentialOrThrow(connectionId);

            if (IsTokenFresh(credential))
            {
                return cryptoService.Decrypt(credential.AccessToken);
            }

            return await RefreshUnderLockAsync(connectionId, credential.Id, cancellationToken);
        }

        private OAuthCredential LoadValidCredentialOrThrow(int connectionId)
        {
            var credential = credentialRepository.GetByPredicate(c => c.WorkTrackingSystemConnectionId == connectionId);
            if (credential is null)
            {
                throw new InvalidOperationException(
                    $"No OAuth credential found for connection {connectionId}. Complete the OAuth connect flow before invoking it.");
            }

            if (credential.Status != OAuthCredentialStatus.Valid)
            {
                throw new OAuthCredentialNotValidException(
                    $"OAuth credential for connection {connectionId} is in status {credential.Status}. Reconnect required.");
            }

            return credential;
        }

        private bool IsTokenFresh(OAuthCredential credential)
        {
            return credential.ExpiresAt - timeProvider.GetUtcNow() > RefreshWindow;
        }

        private async Task<string> RefreshUnderLockAsync(int connectionId, int credentialId, CancellationToken cancellationToken)
        {
            var semaphore = refreshLocks.GetOrAdd(credentialId, _ => new SemaphoreSlim(1, 1));
            var acquired = await semaphore.WaitAsync(refreshOptions.SemaphoreTimeout, cancellationToken);
            if (!acquired)
            {
                throw new OAuthRefreshTimeoutException(
                    $"Timed out waiting to refresh OAuth credential {credentialId} for connection {connectionId}.");
            }

            try
            {
                var credential = LoadValidCredentialOrThrow(connectionId);
                if (IsTokenFresh(credential))
                {
                    return cryptoService.Decrypt(credential.AccessToken);
                }

                return await PerformRefreshAsync(connectionId, credential, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<string> PerformRefreshAsync(int connectionId, OAuthCredential credential, CancellationToken cancellationToken)
        {
            var connection = LoadConnectionOrThrow(connectionId);
            var provider = providerRegistry.GetByKey(connection.AuthenticationMethodKey);

            var encryptedClientId = connection.GetWorkTrackingSystemConnectionOptionByKey(OAuthWorkTrackingOptionNames.ClientId);
            var encryptedClientSecret = connection.GetWorkTrackingSystemConnectionOptionByKey(OAuthWorkTrackingOptionNames.ClientSecret);
            var clientId = cryptoService.Decrypt(encryptedClientId);
            var clientSecret = cryptoService.Decrypt(encryptedClientSecret);
            var refreshToken = cryptoService.Decrypt(credential.RefreshToken);

            var refreshContext = new OAuthRefreshContext(refreshToken, clientId, clientSecret);
            OAuthTokens refreshed;
            try
            {
                refreshed = await provider.RefreshTokenAsync(refreshContext, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Cancellation must propagate without marking the credential failed.
                await MarkRefreshFailedAndThrowAsync(connectionId, credential, connection.AuthenticationMethodKey, ex);
                throw; // unreachable — MarkRefreshFailedAndThrowAsync always throws
            }

            credential.AccessToken = cryptoService.Encrypt(refreshed.AccessToken);
            credential.RefreshToken = cryptoService.Encrypt(refreshed.RefreshToken);
            credential.ExpiresAt = refreshed.ExpiresAt;
            credential.UpdatedAt = timeProvider.GetUtcNow();
            credentialRepository.Update(credential);
            await credentialRepository.Save();

            logger.LogInformation(
                "oauth.token.refreshed {CredentialId} {ConnectionId} {ProviderKey}",
                credential.Id,
                connectionId,
                connection.AuthenticationMethodKey);

            return refreshed.AccessToken;
        }

        private async Task MarkRefreshFailedAndThrowAsync(
            int connectionId,
            OAuthCredential credential,
            string providerKey,
            Exception providerException)
        {
            var fromStatus = credential.Status;
            credential.Status = OAuthCredentialStatus.RefreshFailed;
            credential.UpdatedAt = timeProvider.GetUtcNow();
            credentialRepository.Update(credential);
            await credentialRepository.Save();

            logger.LogWarning(
                providerException,
                "oauth.token.refresh_failed {CredentialId} {ConnectionId} {ProviderKey} {ErrorType}",
                credential.Id,
                connectionId,
                providerKey,
                providerException.GetType().Name);

            logger.LogInformation(
                "oauth.credential.status_changed {CredentialId} {ConnectionId} {FromStatus} {ToStatus}",
                credential.Id,
                connectionId,
                fromStatus,
                OAuthCredentialStatus.RefreshFailed);

            throw new OAuthRefreshFailedException(
                $"OAuth token refresh failed for connection {connectionId}. Reconnect required.",
                providerException);
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
            var encryptedClientId = connection.GetWorkTrackingSystemConnectionOptionByKey(OAuthWorkTrackingOptionNames.ClientId);
            var encryptedClientSecret = connection.GetWorkTrackingSystemConnectionOptionByKey(OAuthWorkTrackingOptionNames.ClientSecret);
            var clientId = cryptoService.Decrypt(encryptedClientId);
            var clientSecret = cryptoService.Decrypt(encryptedClientSecret);
            var redirectUri = new Uri(new Uri(ResolveBaseUrl()), CallbackPath);

            return new OAuthFlowContext(
                connection.Id,
                connection.AuthenticationMethodKey,
                clientId,
                clientSecret,
                redirectUri,
                stateToken,
                provider.DefaultScopes);
        }

        private string ResolveBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(serviceConfig.BaseUrl))
            {
                return serviceConfig.BaseUrl;
            }

            var request = httpContextAccessor.HttpContext?.Request;
            if (request is null || !request.Host.HasValue)
            {
                throw new InvalidOperationException(
                    "Lighthouse:BaseUrl is not configured and no HTTP request context is available. " +
                    "Set Lighthouse:BaseUrl in server configuration so OAuth callback URLs can be derived.");
            }

            return $"{request.Scheme}://{request.Host.Value}";
        }
    }
}
