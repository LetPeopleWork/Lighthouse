using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.OAuth;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.OAuth;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;
using Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Acceptance.AdoOAuthWriteConsent
{
    /// <summary>
    /// An Azure DevOps OAuth connection asks Microsoft only for the rights it needs: reading work items, or reading and
    /// writing them once write-back mappings are set up on it. What Microsoft granted is what decides whether a
    /// write-back is sent to Azure DevOps; a write-back without write consent is not sent and is logged once. A
    /// connection signed in before Lighthouse recorded rights, or signed in with a personal access token, keeps
    /// writing. Each scenario signs in through the OAuth service and writes back through the write-back service, with
    /// a database of its own, no web host, and only Microsoft's token endpoint answered by the test.
    /// </summary>
    [TestFixture]
    public class AdoOAuthWriteConsentTest
    {
        private const string AzureDevOpsWorkItemsReadOnly = "499b84ac-1321-427f-aa17-267ca6975798/vso.work";
        private const string AzureDevOpsWorkItemsReadAndWrite = "499b84ac-1321-427f-aa17-267ca6975798/vso.work_write";
        private const string StayOfflineSignedIn = "offline_access";

        private AzureDevOpsSignIn given = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => given = new AzureDevOpsSignIn();

        [TearDown]
        public void RemoveTheScenariosDatabase() => given.Dispose();

        /// <summary>Without write-back mappings the sign-in asks to read work items only.</summary>
        [Test]
        public async Task A_connection_without_write_back_asks_Microsoft_only_to_read_work_items()
        {
            given.AnAzureDevOpsOAuthConnectionWithoutWriteBack();

            var signIn = await given.WhenTheAdminStartsTheSignIn();

            AzureDevOpsSignIn.ThenTheSignInAsksFor(signIn, AzureDevOpsWorkItemsReadOnly, StayOfflineSignedIn);
        }

        /// <summary>With a write-back mapping the sign-in asks to read and write work items.</summary>
        [Test]
        public async Task A_connection_with_write_back_asks_Microsoft_to_read_and_write_work_items()
        {
            given.AnAzureDevOpsOAuthConnectionWithWriteBack();

            var signIn = await given.WhenTheAdminStartsTheSignIn();

            AzureDevOpsSignIn.ThenTheSignInAsksFor(signIn, AzureDevOpsWorkItemsReadAndWrite, StayOfflineSignedIn);
        }

        /// <summary>
        /// Microsoft granted only the right to read work items, so the connection has no write consent: once it gets a
        /// write-back mapping, its write-back is not sent and one warning is logged.
        /// </summary>
        [Test]
        public async Task Write_back_on_a_connection_signed_in_only_to_read_sends_nothing_and_logs_one_warning()
        {
            given.AnAzureDevOpsOAuthConnectionWithoutWriteBack();
            await given.TheAdminSignedInAndMicrosoftGranted($"{AzureDevOpsWorkItemsReadOnly} {StayOfflineSignedIn}");
            given.AWriteBackMappingWasAddedAfterwards();

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenNothingWasSentToAzureDevOps(result);
            given.ThenOneWarningNamesTheConnectionAndTellsTheAdminToSignInAgain();
        }

        /// <summary>
        /// Microsoft granted the right to read and write work items on the second sign-in, so the write-back that the
        /// read-only sign-in held back is now sent.
        /// </summary>
        [Test]
        public async Task After_the_admin_signs_in_again_with_write_consent_write_back_sends_the_change()
        {
            given.AnAzureDevOpsOAuthConnectionWithWriteBack();
            await given.TheAdminSignedInAndMicrosoftGranted($"{AzureDevOpsWorkItemsReadOnly} {StayOfflineSignedIn}");
            await given.TheAdminSignedInAndMicrosoftGranted($"{AzureDevOpsWorkItemsReadAndWrite} {StayOfflineSignedIn}");

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenTheChangeWasSentToAzureDevOpsOnce(result);
        }

        /// <summary>A granted scope entry that is exactly vso.work_write also counts as write consent.</summary>
        [Test]
        public async Task A_grant_naming_write_rights_without_the_resource_prefix_lets_write_back_send_the_change()
        {
            given.AnAzureDevOpsOAuthConnectionWithWriteBack();
            await given.TheAdminSignedInAndMicrosoftGranted("vso.work_write");

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenTheChangeWasSentToAzureDevOpsOnce(result);
        }

        /// <summary>
        /// A scope that only looks like write rights, an empty scope, and a token answer without any scope all
        /// count as read only, so a connection with a write-back mapping sends nothing.
        /// </summary>
        [TestCase("499b84ac-1321-427f-aa17-267ca6975798/vso.work_write_x", TestName = "A grant of a scope that only starts like write rights sends nothing")]
        [TestCase("", TestName = "A grant with an empty scope sends nothing")]
        [TestCase(null, TestName = "A grant without any scope sends nothing")]
        public async Task A_grant_that_does_not_name_write_rights_sends_nothing(string? grantedScope)
        {
            given.AnAzureDevOpsOAuthConnectionWithWriteBack();
            await given.TheAdminSignedInAndMicrosoftGranted(grantedScope);

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenNothingWasSentToAzureDevOps(result);
        }

        /// <summary>A read-only sign-in blocks nothing while the connection needs only to read.</summary>
        [Test]
        public async Task A_read_only_sign_in_does_not_block_a_write_while_the_connection_has_no_write_back_mapping()
        {
            given.AnAzureDevOpsOAuthConnectionWithoutWriteBack();
            await given.TheAdminSignedInAndMicrosoftGranted($"{AzureDevOpsWorkItemsReadOnly} {StayOfflineSignedIn}");

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenTheChangeWasSentToAzureDevOpsOnce(result);
        }

        /// <summary>A sign-in with no recorded rights counts as write-consented, so existing connections keep writing back.</summary>
        [Test]
        public async Task A_connection_signed_in_before_rights_were_recorded_keeps_writing_back()
        {
            given.AnAzureDevOpsOAuthConnectionWithWriteBack();
            given.ItWasSignedInBeforeLighthouseRecordedRights();

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenTheChangeWasSentToAzureDevOpsOnce(result);
        }

        /// <summary>A connection without an OAuth sign-in is not affected by write consent.</summary>
        [Test]
        public async Task A_connection_signed_in_with_a_personal_access_token_keeps_writing_back()
        {
            given.AnAzureDevOpsPersonalAccessTokenConnectionWithWriteBack();

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenTheChangeWasSentToAzureDevOpsOnce(result);
        }

        /// <summary>A token refresh never turns a read-only sign-in into write consent.</summary>
        [Test]
        public async Task A_token_refresh_does_not_give_a_read_only_sign_in_write_consent()
        {
            given.AnAzureDevOpsOAuthConnectionWithWriteBack();
            await given.TheAdminSignedInWithATokenAboutToExpireAndMicrosoftGranted($"{AzureDevOpsWorkItemsReadOnly} {StayOfflineSignedIn}");
            await given.TheTokenWasRefreshedAndMicrosoftAnswered($"{AzureDevOpsWorkItemsReadAndWrite} {StayOfflineSignedIn}");

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenNothingWasSentToAzureDevOps(result);
        }

        /// <summary>A token refresh keeps a sign-in with no recorded rights write-consented.</summary>
        [Test]
        public async Task A_token_refresh_keeps_a_connection_signed_in_before_rights_were_recorded_writing_back()
        {
            given.AnAzureDevOpsOAuthConnectionWithWriteBack();
            given.ItWasSignedInBeforeLighthouseRecordedRightsWithAnExpiredToken();
            await given.TheTokenWasRefreshedAndMicrosoftAnswered($"{AzureDevOpsWorkItemsReadOnly} {StayOfflineSignedIn}");

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenTheChangeWasSentToAzureDevOpsOnce(result);
        }

        /// <summary>
        /// One Azure DevOps connection named "Delivery Board", with one additional field "Custom.TargetDate" that a
        /// Feature "DB-1" holds the value 2026-11-01 in, on an instance whose clock the scenario holds still. The
        /// tracker side is a stand-in that records every write-back it is sent and accepts it; Microsoft's token
        /// endpoint answers with the scope the scenario says it granted.
        /// </summary>
        private sealed class AzureDevOpsSignIn : IDisposable
        {
            private const string ConnectionName = "Delivery Board";
            private const string TargetField = "Custom.TargetDate";
            private const string FeatureReference = "DB-1";
            private const string StoredValue = "2026-11-01";
            private const string ChangedValue = "2026-12-01";
            private const int AnHourInSeconds = 3600;
            private const int AMinuteInSeconds = 60;

            private readonly FakeTimeProvider clock = new(TestToday.FrozenNow);
            private readonly TokenEndpoint microsoft = new();
            private readonly RecordingLogger<WriteBackService> writeBackLog = new();
            private readonly List<IReadOnlyList<WriteBackFieldUpdate>> sentToAzureDevOps = [];
            private readonly Feature feature = new(new WorkItemBase { ReferenceId = FeatureReference });
            private readonly ServicePortHarness harness;
            private int connectionId;
            private int targetFieldId;

            public AzureDevOpsSignIn()
            {
                harness = new ServicePortHarness(RegisterWhatTheSignInAndTheWriteBackUse);
            }

            public void Dispose() => harness.Dispose();

            public void AnAzureDevOpsOAuthConnectionWithoutWriteBack() => AConnection(AuthenticationMethodKeys.AzureDevOpsOAuth, withWriteBack: false);

            public void AnAzureDevOpsOAuthConnectionWithWriteBack() => AConnection(AuthenticationMethodKeys.AzureDevOpsOAuth, withWriteBack: true);

            public void AnAzureDevOpsPersonalAccessTokenConnectionWithWriteBack() => AConnection(AuthenticationMethodKeys.AzureDevOpsPat, withWriteBack: true);

            public void AWriteBackMappingWasAddedAfterwards()
            {
                using var scope = harness.NewScope();
                var connections = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
                var connection = connections.GetById(connectionId)!;
                connection.WriteBackMappingDefinitions.Add(AMappingOntoTheTargetField());
                connections.Update(connection);
                connections.Save().GetAwaiter().GetResult();
            }

            public Task TheAdminSignedInAndMicrosoftGranted(string? grantedScope) => SignIn(grantedScope, AnHourInSeconds);

            public Task TheAdminSignedInWithATokenAboutToExpireAndMicrosoftGranted(string? grantedScope) => SignIn(grantedScope, AMinuteInSeconds);

            public void ItWasSignedInBeforeLighthouseRecordedRights() => AnEarlierSignIn(clock.GetUtcNow().AddHours(1));

            public void ItWasSignedInBeforeLighthouseRecordedRightsWithAnExpiredToken() => AnEarlierSignIn(clock.GetUtcNow().AddHours(-1));

            public async Task TheTokenWasRefreshedAndMicrosoftAnswered(string grantedScope)
            {
                microsoft.Grants(grantedScope, AnHourInSeconds);
                using var scope = harness.NewScope();
                await scope.ServiceProvider.GetRequiredService<IOAuthService>().EnsureFreshTokenAsync(connectionId, CancellationToken.None);
                Assert.That(microsoft.RefreshesAnswered, Is.EqualTo(1),
                    "WHAT: the token was not refreshed. WHY: the scenario is about what a refresh does to the rights a sign-in holds. " +
                    "HOW: the stored token must expire within the refresh window so that asking for a fresh token refreshes it.");
            }

            public async Task<Uri> WhenTheAdminStartsTheSignIn()
            {
                using var scope = harness.NewScope();
                return await scope.ServiceProvider.GetRequiredService<IOAuthService>().InitiateAsync(connectionId, CancellationToken.None);
            }

            public async Task<WriteBackResult> WhenAChangedValueIsWrittenBack()
            {
                using var scope = harness.NewScope();
                var connection = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!;
                var update = new WriteBackFieldUpdate { WorkItemId = FeatureReference, TargetFieldReference = TargetField, Value = ChangedValue };

                return await scope.ServiceProvider.GetRequiredService<IWriteBackService>().WriteFieldsToWorkItems(connection, [update]);
            }

            public static void ThenTheSignInAsksFor(Uri signIn, params string[] expectedScopes)
            {
                var askedFor = QueryHelpers.ParseQuery(signIn.Query).TryGetValue("scope", out var scope)
                    ? scope.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    : [];

                Assert.That(askedFor, Is.EquivalentTo(expectedScopes),
                    $"WHAT: the sign-in asked Microsoft for [{string.Join(", ", askedFor)}], expected [{string.Join(", ", expectedScopes)}]. " +
                    "WHY: a connection holds only the rights it needs: reading work items, and writing them only when write-back is set up on it. " +
                    "HOW: ask for the scopes the saved connection needs, read rights without write-back mappings and write rights with them.");
            }

            public void ThenNothingWasSentToAzureDevOps(WriteBackResult result)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(sentToAzureDevOps, Is.Empty,
                        $"WHAT: the write-back was sent to Azure DevOps {sentToAzureDevOps.Count} time(s). WHY: a connection whose sign-in holds no " +
                        "write consent never tries to write. HOW: before resolving the tracker, skip the write-back when the connection needs write " +
                        "rights and its sign-in was granted read rights only.");
                    Assert.That(result.ItemResults, Is.Empty,
                        "WHAT: the skipped write-back reported per-item results. WHY: nothing was written, so there is nothing to report as written or " +
                        "failed. HOW: return an empty result when the write-back is skipped.");
                    Assert.That(feature.AdditionalFieldValues[targetFieldId], Is.EqualTo(StoredValue),
                        "WHAT: the Feature's stored value changed although nothing was written. WHY: the stored copy follows only values the tracker " +
                        "accepted. HOW: persist nothing when the write-back is skipped.");
                }
            }

            public void ThenTheChangeWasSentToAzureDevOpsOnce(WriteBackResult result)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(sentToAzureDevOps, Has.Count.EqualTo(1),
                        $"WHAT: the write-back was sent to Azure DevOps {sentToAzureDevOps.Count} time(s), expected once. WHY: a connection with write " +
                        "consent, one signed in before rights were recorded, and one with a personal access token keep writing back. HOW: let the " +
                        "write-back through whenever the connection has no OAuth sign-in or its sign-in does not lack the rights the connection needs.");
                    Assert.That(sentToAzureDevOps.SelectMany(sent => sent).Select(update => (update.WorkItemId, update.Value)),
                        Is.EquivalentTo(new[] { (FeatureReference, ChangedValue) }),
                        "WHAT: the write-back did not carry the changed value of the Feature. WHY: write-back sends the value that changed. " +
                        "HOW: send the changed update unaltered.");
                    Assert.That(result.SuccessCount, Is.EqualTo(1),
                        "WHAT: the write-back did not report the accepted update. WHY: the admin sees what was written. HOW: return the tracker's result.");
                    Assert.That(writeBackLog.Warnings, Is.Empty,
                        $"WHAT: a write-back that was sent logged warnings: {string.Join(" | ", writeBackLog.Warnings)}. WHY: the warning is for a " +
                        "write-back that was skipped. HOW: warn only when the write-back is skipped for missing write consent.");
                }
            }

            public void ThenOneWarningNamesTheConnectionAndTellsTheAdminToSignInAgain()
            {
                var warnings = writeBackLog.Warnings;

                Assert.That(warnings, Has.Count.EqualTo(1),
                    $"WHAT: the skipped write-back logged {warnings.Count} warning(s): [{string.Join(" | ", warnings)}], expected exactly one. " +
                    "WHY: a support case must see why nothing was written, once per write-back run. HOW: log one Warning when the write-back is skipped.");
                const string Remedy = "WHY: the admin must know which connection skipped its write-back and what to do. HOW: name the connection's " +
                    "id and name and tell the admin to sign in again on the connection's settings to allow writing.";
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(warnings[0], Does.Contain(ConnectionName), $"WHAT: the warning '{warnings[0]}' does not name the connection '{ConnectionName}'. {Remedy}");
                    Assert.That(warnings[0], Does.Contain(connectionId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        $"WHAT: the warning '{warnings[0]}' does not name connection id {connectionId}. {Remedy}");
                    Assert.That(warnings[0], Does.Contain("sign in again").IgnoreCase, $"WHAT: the warning '{warnings[0]}' does not name the remedy. {Remedy}");
                }
            }

            private void AConnection(string authenticationMethodKey, bool withWriteBack)
            {
                using var scope = harness.NewScope();
                var connection = new WorkTrackingSystemConnection
                {
                    Name = ConnectionName,
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                    AuthenticationMethodKey = authenticationMethodKey,
                };
                connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = OAuthWorkTrackingOptionNames.ClientId, Value = "lighthouse-app", IsSecret = false });
                connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = OAuthWorkTrackingOptionNames.ClientSecret, Value = "lighthouse-app-secret", IsSecret = true });
                connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { DisplayName = "Target Date", Reference = TargetField });

                var connections = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
                connections.Add(connection);
                connections.Save().GetAwaiter().GetResult();

                connectionId = connection.Id;
                targetFieldId = connection.AdditionalFieldDefinitions.Single().Id;
                feature.AdditionalFieldValues[targetFieldId] = StoredValue;

                if (withWriteBack)
                {
                    AWriteBackMappingWasAddedAfterwards();
                }
            }

            private WriteBackMappingDefinition AMappingOntoTheTargetField() => new()
            {
                ValueSource = WriteBackValueSource.ForecastPercentile50,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                AdditionalFieldDefinitionId = targetFieldId,
                TargetValueType = WriteBackTargetValueType.Date,
            };

            private async Task SignIn(string? grantedScope, int tokenLifetimeSeconds)
            {
                var signIn = await WhenTheAdminStartsTheSignIn();
                var state = QueryHelpers.ParseQuery(signIn.Query)["state"].ToString();

                microsoft.Grants(grantedScope, tokenLifetimeSeconds);
                using var scope = harness.NewScope();
                await scope.ServiceProvider.GetRequiredService<IOAuthService>().CompleteAsync("code-from-microsoft", state, CancellationToken.None);
            }

            private void AnEarlierSignIn(DateTimeOffset expiresAt)
            {
                using var scope = harness.NewScope();
                var credentials = scope.ServiceProvider.GetRequiredService<IRepository<OAuthCredential>>();
                credentials.Add(new OAuthCredential
                {
                    WorkTrackingSystemConnectionId = connectionId,
                    AccessToken = "access-token-from-an-earlier-release",
                    RefreshToken = "refresh-token-from-an-earlier-release",
                    ExpiresAt = expiresAt,
                    Status = OAuthCredentialStatus.Valid,
                    UpdatedAt = clock.GetUtcNow().AddDays(-30),
                });
                credentials.Save().GetAwaiter().GetResult();
            }

            private void RegisterWhatTheSignInAndTheWriteBackUse(IServiceCollection services)
            {
                var serviceConfig = new Mock<IServiceConfig>();
                serviceConfig.SetupGet(config => config.BaseUrl).Returns("https://lighthouse.example");
                serviceConfig.SetupGet(config => config.OAuthStateSecret).Returns("a-state-secret-only-this-scenario-knows");

                var httpClients = new Mock<IHttpClientFactory>();
                httpClients.Setup(factory => factory.CreateClient(AdoOAuthProvider.HttpClientName))
                    .Returns(() => new HttpClient(microsoft, disposeHandler: false));

                services.AddSingleton<TimeProvider>(clock);
                services.AddSingleton(serviceConfig.Object);
                services.AddSingleton(httpClients.Object);
                services.AddHttpContextAccessor();
                services.AddSingleton<IOAuthStateTokenIssuer, OAuthStateTokenIssuer>();
                services.AddSingleton<IOAuthProvider, AdoOAuthProvider>();
                services.AddSingleton<IOAuthProviderRegistry, OAuthProviderRegistry>();
                services.AddScoped<IRepository<WorkTrackingSystemConnection>, WorkTrackingSystemConnectionRepository>();
                services.AddScoped<IRepository<OAuthCredential>, OAuthCredentialRepository>();
                services.AddScoped<IOAuthService, OAuthService>();

                services.AddSingleton<ILogger<WriteBackService>>(writeBackLog);
                services.AddSingleton(AzureDevOpsThatRecordsWhatItIsSent());
                services.AddSingleton(StoredWorkHolding(feature));
                services.AddSingleton(new Mock<IWorkItemRepository>().Object);
                services.AddScoped<IWriteBackService, WriteBackService>();
            }

            private IWorkTrackingConnectorFactory AzureDevOpsThatRecordsWhatItIsSent()
            {
                var connector = new Mock<IWorkTrackingConnector>();
                connector.As<IWriteBackTarget>()
                    .Setup(target => target.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                    .ReturnsAsync((WorkTrackingSystemConnection _, IReadOnlyList<WriteBackFieldUpdate> updates) =>
                    {
                        sentToAzureDevOps.Add(updates);
                        return new WriteBackResult
                        {
                            ItemResults = [.. updates.Select(update => new WriteBackItemResult
                            {
                                WorkItemId = update.WorkItemId,
                                TargetFieldReference = update.TargetFieldReference,
                                Success = true,
                                NotificationSuppression = NotificationSuppression.Suppressed,
                            })],
                        };
                    });

                var factory = new Mock<IWorkTrackingConnectorFactory>();
                factory.Setup(f => f.GetWorkTrackingConnector(WorkTrackingSystems.AzureDevOps)).Returns(connector.Object);
                return factory.Object;
            }

            private static IRepository<Feature> StoredWorkHolding(Feature storedFeature)
            {
                var features = new Mock<IRepository<Feature>>();
                features.Setup(repository => repository.GetAll()).Returns(() => new[] { storedFeature });
                features.Setup(repository => repository.Save()).Returns(Task.CompletedTask);
                return features.Object;
            }
        }

        /// <summary>
        /// Microsoft's token endpoint as the scenario needs it: it answers every code redemption and every refresh with
        /// fresh tokens and the scope the scenario says was granted, or with no scope at all when that is the case.
        /// </summary>
        private sealed class TokenEndpoint : HttpMessageHandler
        {
            private string? grantedScope;
            private int lifetimeSeconds;

            public int RefreshesAnswered { get; private set; }

            public void Grants(string? scope, int tokenLifetimeSeconds)
            {
                grantedScope = scope;
                lifetimeSeconds = tokenLifetimeSeconds;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var form = await request.Content!.ReadAsStringAsync(cancellationToken);
                if (form.Contains("grant_type=refresh_token", StringComparison.Ordinal))
                {
                    RefreshesAnswered++;
                }

                var answer = new Dictionary<string, object>
                {
                    ["access_token"] = $"access-token-{Guid.NewGuid():N}",
                    ["refresh_token"] = $"refresh-token-{Guid.NewGuid():N}",
                    ["expires_in"] = lifetimeSeconds,
                    ["token_type"] = "Bearer",
                };

                if (grantedScope is not null)
                {
                    answer["scope"] = grantedScope;
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(answer), Encoding.UTF8, "application/json"),
                };
            }
        }
    }
}
