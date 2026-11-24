using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.OAuth;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.OAuth;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;
using Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth;
using Lighthouse.Backend.Platform.Host;
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

namespace Lighthouse.Backend.Tests.Acceptance.JiraOAuthWriteConsent
{
    /// <summary>
    /// A Jira OAuth connection asks Atlassian only for the rights it needs: reading work items, or reading and
    /// writing them once write-back mappings are set up on it. What Atlassian granted is what decides whether a
    /// write-back is sent to Jira; a write-back without write consent is not sent, is logged once, and shows in the
    /// connection list as Rights Read needing a new sign-in. A Jira sign-in stored before rights were recorded counts
    /// as read only; a connection signed in with an API token is not affected. Each scenario signs in through the
    /// OAuth service and writes back through the write-back service, with a database of its own, no web host, and
    /// only Atlassian's token endpoint answered by the test.
    /// </summary>
    [TestFixture]
    public class JiraOAuthWriteConsentTest
    {
        private static readonly string[] ReadScopes =
        [
            "read:jira-work",
            "read:jira-user",
            "read:board-scope:jira-software",
            "read:sprint:jira-software",
            "read:issue:jira-software",
            "read:project:jira",
            "offline_access",
        ];

        private const string JiraWriteScope = "write:jira-work";
        private const string AtlassianReadOnlyGrant = "read:jira-work read:jira-user offline_access";
        private const string AtlassianReadAndWriteGrant = "read:jira-work read:jira-user write:jira-work offline_access";

        private JiraSignIn given = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => given = new JiraSignIn();

        [TearDown]
        public void RemoveTheScenariosDatabase() => given.Dispose();

        /// <summary>Without write-back mappings the sign-in asks to read only.</summary>
        [Test]
        public async Task A_connection_without_write_back_asks_Atlassian_only_to_read()
        {
            given.AJiraOAuthConnectionWithoutWriteBack();

            var signIn = await given.WhenTheAdminStartsTheSignIn();

            JiraSignIn.ThenTheSignInAsksFor(signIn, ReadScopes);
        }

        /// <summary>With a write-back mapping the sign-in also asks to write work items.</summary>
        [Test]
        public async Task A_connection_with_write_back_asks_Atlassian_to_read_and_write_work_items()
        {
            given.AJiraOAuthConnectionWithWriteBack();

            var signIn = await given.WhenTheAdminStartsTheSignIn();

            JiraSignIn.ThenTheSignInAsksFor(signIn, [.. ReadScopes, JiraWriteScope]);
        }

        /// <summary>
        /// Atlassian granted only read rights, so the connection has no write consent: once it gets a write-back
        /// mapping, its write-back is not sent, one warning is logged and the connection list asks for a new sign-in.
        /// </summary>
        [Test]
        public async Task Write_back_on_a_connection_signed_in_only_to_read_sends_nothing_and_logs_one_warning()
        {
            given.AJiraOAuthConnectionWithoutWriteBack();
            await given.TheAdminSignedInAndAtlassianGranted(AtlassianReadOnlyGrant);
            given.AWriteBackMappingWasAddedAfterwards();

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenNothingWasSentToJira(result);
            given.ThenOneWarningNamesTheConnectionAndTellsTheAdminToSignInAgain();
            given.ThenTheConnectionListShowsRights(WorkItemRights.Read, needsWriteConsent: true);
        }

        /// <summary>
        /// Atlassian granted write:jira-work on the second sign-in, so the write-back that the read-only sign-in held
        /// back is now sent and the connection list no longer asks for a new sign-in.
        /// </summary>
        [Test]
        public async Task After_the_admin_signs_in_again_with_write_consent_write_back_sends_the_change()
        {
            given.AJiraOAuthConnectionWithWriteBack();
            await given.TheAdminSignedInAndAtlassianGranted(AtlassianReadOnlyGrant);
            await given.TheAdminSignedInAndAtlassianGranted(AtlassianReadAndWriteGrant);

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenTheChangeWasSentToJiraOnce(result);
            given.ThenTheConnectionListShowsRights(WorkItemRights.ReadAndWrite, needsWriteConsent: false);
        }

        /// <summary>
        /// A scope that only looks like write rights, another write scope, an empty scope, and a token answer without
        /// any scope all count as read only, so a connection with a write-back mapping sends nothing.
        /// </summary>
        [TestCase("read:jira-work write:jira-work-x offline_access", TestName = "A grant of a scope that only starts like write rights sends nothing")]
        [TestCase("read:jira-work write:comment:jira offline_access", TestName = "A grant of another write scope sends nothing")]
        [TestCase("", TestName = "A grant with an empty scope sends nothing")]
        [TestCase(null, TestName = "A grant without any scope sends nothing")]
        public async Task A_grant_that_does_not_name_write_rights_sends_nothing(string? grantedScope)
        {
            given.AJiraOAuthConnectionWithWriteBack();
            await given.TheAdminSignedInAndAtlassianGranted(grantedScope);

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenNothingWasSentToJira(result);
        }

        /// <summary>A read-only sign-in blocks nothing while the connection needs only to read.</summary>
        [Test]
        public async Task A_read_only_sign_in_does_not_block_a_write_while_the_connection_has_no_write_back_mapping()
        {
            given.AJiraOAuthConnectionWithoutWriteBack();
            await given.TheAdminSignedInAndAtlassianGranted(AtlassianReadOnlyGrant);

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenTheChangeWasSentToJiraOnce(result);
            given.ThenTheConnectionListShowsRights(WorkItemRights.Read, needsWriteConsent: false);
        }

        /// <summary>A Jira sign-in with no recorded rights never asked to write, so it counts as read only.</summary>
        [Test]
        public void A_connection_signed_in_before_rights_were_recorded_counts_as_read_only()
        {
            given.AJiraOAuthConnectionWithWriteBack();
            given.ItWasSignedInBeforeLighthouseRecordedRights();

            var result = given.WhenAChangedValueIsWrittenBack().GetAwaiter().GetResult();

            given.ThenNothingWasSentToJira(result);
            given.ThenOneWarningNamesTheConnectionAndTellsTheAdminToSignInAgain();
            given.ThenTheConnectionListShowsRights(WorkItemRights.Read, needsWriteConsent: true);
        }

        /// <summary>A connection without an OAuth sign-in is not affected by write consent and shows no rights.</summary>
        [Test]
        public async Task A_connection_signed_in_with_an_API_token_keeps_writing_back()
        {
            given.AJiraApiTokenConnectionWithWriteBack();

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenTheChangeWasSentToJiraOnce(result);
            given.ThenTheConnectionListShowsNoRights();
        }

        /// <summary>A token refresh never turns a read-only sign-in into write consent.</summary>
        [Test]
        public async Task A_token_refresh_does_not_give_a_read_only_sign_in_write_consent()
        {
            given.AJiraOAuthConnectionWithWriteBack();
            await given.TheAdminSignedInWithATokenAboutToExpireAndAtlassianGranted(AtlassianReadOnlyGrant);
            await given.TheTokenWasRefreshedAndAtlassianAnswered(AtlassianReadAndWriteGrant);

            var result = await given.WhenAChangedValueIsWrittenBack();

            given.ThenNothingWasSentToJira(result);
        }

        /// <summary>
        /// One Jira connection named "Delivery Board", with one additional field "Custom.TargetDate" that a
        /// Feature "DB-1" holds the value 2026-11-01 in, on an instance whose clock the scenario holds still. The
        /// tracker side is a stand-in that records every write-back it is sent and accepts it; Atlassian's token
        /// endpoint answers with the scope the scenario says it granted.
        /// </summary>
        private sealed class JiraSignIn : IDisposable
        {
            private const string ConnectionName = "Delivery Board";
            private const string TargetField = "Custom.TargetDate";
            private const string FeatureReference = "DB-1";
            private const string StoredValue = "2026-11-01";
            private const string ChangedValue = "2026-12-01";
            private const int AnHourInSeconds = 3600;
            private const int AMinuteInSeconds = 60;

            private readonly FakeTimeProvider clock = new(TestToday.FrozenNow);
            private readonly TokenEndpoint atlassian = new();
            private readonly RecordingLogger<WriteBackService> writeBackLog = new();
            private readonly List<IReadOnlyList<WriteBackFieldUpdate>> sentToJira = [];
            private readonly Feature feature = new(new WorkItemBase { ReferenceId = FeatureReference });
            private readonly ServicePortHarness harness;
            private int connectionId;
            private int targetFieldId;

            public JiraSignIn()
            {
                harness = new ServicePortHarness(RegisterWhatTheSignInAndTheWriteBackUse);
            }

            public void Dispose() => harness.Dispose();

            public void AJiraOAuthConnectionWithoutWriteBack() => AConnection(AuthenticationMethodKeys.JiraOAuth, withWriteBack: false);

            public void AJiraOAuthConnectionWithWriteBack() => AConnection(AuthenticationMethodKeys.JiraOAuth, withWriteBack: true);

            public void AJiraApiTokenConnectionWithWriteBack() => AConnection(AuthenticationMethodKeys.JiraCloud, withWriteBack: true);

            public void AWriteBackMappingWasAddedAfterwards()
            {
                using var scope = harness.NewScope();
                var connections = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
                var connection = connections.GetById(connectionId)!;
                connection.WriteBackMappingDefinitions.Add(AMappingOntoTheTargetField());
                connections.Update(connection);
                connections.Save().GetAwaiter().GetResult();
            }

            public Task TheAdminSignedInAndAtlassianGranted(string? grantedScope) => SignIn(grantedScope, AnHourInSeconds);

            public Task TheAdminSignedInWithATokenAboutToExpireAndAtlassianGranted(string? grantedScope) => SignIn(grantedScope, AMinuteInSeconds);

            public void ItWasSignedInBeforeLighthouseRecordedRights() => AnEarlierSignIn(clock.GetUtcNow().AddHours(1));

            public void ItWasSignedInBeforeLighthouseRecordedRightsWithAnExpiredToken() => AnEarlierSignIn(clock.GetUtcNow().AddHours(-1));

            public async Task TheTokenWasRefreshedAndAtlassianAnswered(string grantedScope)
            {
                atlassian.Grants(grantedScope, AnHourInSeconds);
                using var scope = harness.NewScope();
                await scope.ServiceProvider.GetRequiredService<IOAuthService>().EnsureFreshTokenAsync(connectionId, CancellationToken.None);
                Assert.That(atlassian.RefreshesAnswered, Is.EqualTo(1),
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
                    $"WHAT: the sign-in asked Atlassian for [{string.Join(", ", askedFor)}], expected [{string.Join(", ", expectedScopes)}]. " +
                    "WHY: a connection holds only the rights it needs: reading work items, and writing them only when write-back is set up on it. " +
                    "HOW: ask for the scopes the saved connection needs, read rights without write-back mappings and write rights with them.");
            }

            public void ThenNothingWasSentToJira(WriteBackResult result)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(sentToJira, Is.Empty,
                        $"WHAT: the write-back was sent to Jira {sentToJira.Count} time(s). WHY: a connection whose sign-in holds no " +
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

            public void ThenTheChangeWasSentToJiraOnce(WriteBackResult result)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(sentToJira, Has.Count.EqualTo(1),
                        $"WHAT: the write-back was sent to Jira {sentToJira.Count} time(s), expected once. WHY: a connection with write " +
                        "consent, one signed in before rights were recorded, and one with a personal access token keep writing back. HOW: let the " +
                        "write-back through whenever the connection has no OAuth sign-in or its sign-in does not lack the rights the connection needs.");
                    Assert.That(sentToJira.SelectMany(sent => sent).Select(update => (update.WorkItemId, update.Value)),
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

            public void ThenTheConnectionListShowsRights(WorkItemRights expectedRights, bool needsWriteConsent)
            {
                var listed = TheConnectionAsListed();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(listed.GrantedRights, Is.EqualTo(expectedRights),
                        $"WHAT: the connection list shows rights {listed.GrantedRights?.ToString() ?? "none"}, expected {expectedRights}. WHY: the admin " +
                        "sees what the Jira sign-in may do. HOW: show the rights recorded for a Jira OAuth sign-in, and Read for one stored before rights were recorded.");
                    Assert.That(listed.NeedsWriteConsent, Is.EqualTo(needsWriteConsent),
                        $"WHAT: the connection list says needsWriteConsent {listed.NeedsWriteConsent}, expected {needsWriteConsent}. WHY: the Sign in again " +
                        "warning shows exactly when write-back is set up and the sign-in may only read. HOW: derive it from the rights the connection needs.");
                }
            }

            public void ThenTheConnectionListShowsNoRights()
            {
                var listed = TheConnectionAsListed();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(listed.GrantedRights, Is.Null,
                        "WHAT: a connection without an OAuth sign-in lists rights. WHY: only an OAuth sign-in has rights to record. HOW: send no rights for it.");
                    Assert.That(listed.NeedsWriteConsent, Is.False,
                        "WHAT: a connection without an OAuth sign-in asks for a new sign-in. WHY: there is nothing to sign in again. HOW: never flag it.");
                }
            }

            private WorkTrackingSystemConnectionDto TheConnectionAsListed()
            {
                using var scope = harness.NewScope();
                var connection = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!;
                var credential = scope.ServiceProvider.GetRequiredService<IRepository<OAuthCredential>>()
                    .GetByPredicate(c => c.WorkTrackingSystemConnectionId == connectionId);
                return new WorkTrackingSystemConnectionDto(connection, credential);
            }

            private void AConnection(string authenticationMethodKey, bool withWriteBack)
            {
                using var scope = harness.NewScope();
                var connection = new WorkTrackingSystemConnection
                {
                    Name = ConnectionName,
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
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

                atlassian.Grants(grantedScope, tokenLifetimeSeconds);
                using var scope = harness.NewScope();
                await scope.ServiceProvider.GetRequiredService<IOAuthService>().CompleteAsync("code-from-atlassian", state, CancellationToken.None);
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
                httpClients.Setup(factory => factory.CreateClient(JiraOAuthProvider.HttpClientName))
                    .Returns(() => new HttpClient(atlassian, disposeHandler: false));

                services.AddSingleton<TimeProvider>(clock);
                services.AddSingleton(serviceConfig.Object);
                services.AddSingleton(httpClients.Object);
                services.AddHttpContextAccessor();
                services.AddSingleton<IOAuthStateTokenIssuer, OAuthStateTokenIssuer>();
                services.AddSingleton<IOAuthProvider, JiraOAuthProvider>();
                services.AddSingleton<IOAuthProviderRegistry, OAuthProviderRegistry>();
                services.AddScoped<IRepository<WorkTrackingSystemConnection>, WorkTrackingSystemConnectionRepository>();
                services.AddScoped<IRepository<OAuthCredential>, OAuthCredentialRepository>();
                services.AddScoped<IOAuthService, OAuthService>();

                services.AddSingleton<ILogger<WriteBackService>>(writeBackLog);
                services.AddSingleton(JiraThatRecordsWhatItIsSent());
                services.AddSingleton(StoredWorkHolding(feature));
                services.AddSingleton(new Mock<IWorkItemRepository>().Object);
                services.AddScoped<IWriteBackService, WriteBackService>();
            }

            private IWorkTrackingConnectorFactory JiraThatRecordsWhatItIsSent()
            {
                var connector = new Mock<IWorkTrackingConnector>();
                connector.As<IWriteBackTarget>()
                    .Setup(target => target.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                    .ReturnsAsync((WorkTrackingSystemConnection _, IReadOnlyList<WriteBackFieldUpdate> updates) =>
                    {
                        sentToJira.Add(updates);
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
                factory.Setup(f => f.GetWorkTrackingConnector(WorkTrackingSystems.Jira)).Returns(connector.Object);
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
        /// Atlassian's token endpoint as the scenario needs it: it answers every code redemption and every refresh with
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
