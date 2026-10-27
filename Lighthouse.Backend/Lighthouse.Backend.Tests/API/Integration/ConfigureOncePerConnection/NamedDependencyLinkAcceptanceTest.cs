using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using Moq.Protected;
using Serilog;
using Serilog.Extensions.Logging;
using AdoWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// Acceptance harness for a Portfolio whose Dependency Field names a link: a Jira link phrase, or an
    /// Azure DevOps relation type with its end. A scenario describes what the tracker holds - its Features,
    /// the links between them, the link types and fields it defines - names a Dependency Field, and then
    /// refreshes the Portfolio through the running application.
    ///
    /// Only the transport to the tracker is faked. Jira is the production connector over a recorded HTTP
    /// handler; Azure DevOps is the production connector over a recorded client, one per organisation, and
    /// the same connector instance serves every organisation, as the connector factory hands one cached
    /// instance to every connection of a tracker. The work item service, the updater, the refresh log and
    /// the database are production. Everything a scenario does or reads afterwards goes through the routes
    /// the screens use: Validate through the validate route, a Portfolio's settings through its settings
    /// route, what a Feature waits on and where it hangs through the Feature routes, and what a refresh
    /// had to say through Refresh History and the log, the way an administrator reads them. Only the
    /// Portfolios a scenario starts from are stored directly, before the application is first asked.
    ///
    /// Every request either tracker answered is recorded, so "naming a link costs nothing extra" is a count.
    /// </summary>
    public abstract class NamedDependencyLinkAcceptanceTest
    {
        protected const string PaymentsPlatform = "Payments Platform";

        protected const string RetailRollout = "Retail Rollout";

        protected const string NorthernStores = "Northern Stores";

        protected const string ContosoOrganisation = "https://dev.azure.com/contoso";

        protected const string FabrikamOrganisation = "https://dev.azure.com/fabrikam";

        // Jira's stock link types, as an instance ships them.
        protected static readonly JiraLinkType Blocks = new("Blocks", "is blocked by", "blocks");

        protected static readonly JiraLinkType Dependency = new("Dependency", "is depended on by", "depends on");

        protected static readonly JiraLinkType Relates = new("Relates", "relates to", "relates to");

        // Azure DevOps relation types, as the organisation lists them.
        protected const string Predecessor = "System.LinkTypes.Dependency-Reverse";

        protected const string Successor = "System.LinkTypes.Dependency-Forward";

        protected const string Related = "System.LinkTypes.Related";

        protected const string ContosoDependsOn = "Contoso.DependsOn";

        protected const string ContosoDependsOnForward = "Contoso.DependsOn-Forward";

        protected const string ContosoDependsOnReverse = "Contoso.DependsOn-Reverse";

        protected const string Hierarchy = "System.LinkTypes.Hierarchy-Reverse";

        private const string RefreshLogRoute = "/api/latest/systeminfo/refreshlog";

        private const string ValidateRoute = "/api/latest/worktrackingsystemconnections/validate";

        private const string FeaturesByReferenceRoute = "/api/latest/features/references?featureReferences=";

        private const string PortfolioRoute = "/api/latest/portfolios/";

        private const int TheDependencyFieldId = 4801;

        private const int TheParentOverrideFieldId = 4802;

        private const int AFieldNoSettingNamesId = 14801;

        private const string FeatureType = "Epic";

        private const string AdoFeatureType = "Feature";

        private const string KeyClause = "key = \"";

        private const string TheAdoPortfoliosQuery = "[System.TeamProject] = 'Retail'";

        private static readonly DateTime WhenEverythingWasCreated = new(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);

        private readonly Dictionary<string, AFeatureOnJira> jiraFeatures = new(StringComparer.Ordinal);

        private readonly List<JiraLinkType> jiraLinkTypes = [];

        private readonly List<string> jiraFieldNames = [];

        private readonly List<string> requestsJiraAnswered = [];

        private bool jiraRefusesToListItsLinkTypes;

        private bool jiraAnswersItsLinkTypeListUnreadably;

        private readonly Dictionary<string, AnAzureDevOpsOrganisation> organisations = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Portfolio> adoPortfolios = new(StringComparer.Ordinal);

        private readonly Dictionary<string, Team> adoTeams = new(StringComparer.Ordinal);

        private CapturedLogMessages whatWasLogged = new();

        private SerilogLoggerFactory theLog = null!;

        private Portfolio theJiraPortfolio = null!;

        private List<string> warningsOfTheLastRefresh = [];

        private int nextConnectionId = 9600;

        private TestWebApplicationFactory<Program>? theHost;

        private WebApplicationFactory<Program>? theRunningLighthouse;

        private AzureDevOpsOverEveryOrganisation? theAdoConnector;

        /// <summary>One finding a refresh recorded, as Refresh History hands it to the screen.</summary>
        protected sealed record Finding(string Kind, string Setting, string Named, IReadOnlyList<string> Seen);

        /// <summary>
        /// What Refresh History shows for one Portfolio: the findings of its latest full run, with that run's
        /// time, and what kind of run the newest row was. <c>Findings</c> is null when the answer carries no
        /// findings at all, which is what a Refresh History without the feature answers.
        /// </summary>
        /// <summary>The Portfolio settings other than the Dependency Field that read an Additional Field.</summary>
        public enum ASettingThatReadsAField
        {
            ParentOverrideField,
            SizeEstimateField,
            FeatureOwnerField,
            EstimationField,
        }

        /// <summary>What the validate route answered: its status, and whether and why it accepted the connection.</summary>
        protected sealed record ValidateAnswer(HttpStatusCode Status, bool IsValid, string Message);

        protected sealed record WhatRefreshHistoryShows(IReadOnlyList<Finding>? Findings, string? FindingsAsOf, string NewestRunMode, string NewestRunAt, bool NewestRunSucceeded = true)
        {
            /// <summary>The first finding, or null when there is none or no findings were sent at all.</summary>
            public Finding? TheFirstFinding => Findings is { Count: > 0 } ? Findings[0] : null;
        }

        [SetUp]
        public void ForgetWhatTheLastScenarioDescribed()
        {
            jiraFeatures.Clear();
            jiraLinkTypes.Clear();
            jiraLinkTypes.AddRange([Blocks, Dependency, Relates]);
            jiraFieldNames.Clear();
            requestsJiraAnswered.Clear();
            jiraRefusesToListItsLinkTypes = false;
            jiraAnswersItsLinkTypeListUnreadably = false;
            organisations.Clear();
            adoPortfolios.Clear();
            adoTeams.Clear();
            warningsOfTheLastRefresh = [];
            nextConnectionId = 9600;
            theAdoConnector = null;

            whatWasLogged = new CapturedLogMessages();
            theLog = new SerilogLoggerFactory(
                new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(whatWasLogged).CreateLogger(),
                dispose: true);

            theJiraPortfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();
            theJiraPortfolio.Name = PaymentsPlatform;
        }

        [TearDown]
        public void StopTheLighthouseThisScenarioStarted()
        {
            if (theRunningLighthouse is not null)
            {
                using (var scope = theRunningLighthouse.Services.CreateScope())
                {
                    scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
                }

                theRunningLighthouse.Dispose();
                theRunningLighthouse = null;
            }

            theHost?.Dispose();
            theHost = null;
            theLog.Dispose();
        }

        // ----- What the Jira instance holds -------------------------------------------------------------

        protected void JiraHoldsTheFeatures(params string[] keys)
        {
            foreach (var key in keys)
            {
                jiraFeatures[key] = new AFeatureOnJira(key);
            }
        }

        /// <summary>
        /// "<paramref name="from"/> <paramref name="phrase"/> <paramref name="to"/>", as an issue's links read in
        /// Jira. A link is written once and served from both ends, so the other issue carries the other phrase.
        /// </summary>
        protected void OnJira(string from, string phrase, string to)
        {
            var type = jiraLinkTypes.Find(candidate => candidate.Outward == phrase)
                ?? jiraLinkTypes.Find(candidate => candidate.Inward == phrase);

            Assert.That(type, Is.Not.Null, $"No link type on this instance reads '{phrase}' from either end.");

            if (type!.Outward == phrase)
            {
                TheJiraFeature(from).Links.Add(type.LinkWhoseOutwardIssueIs(to));
                TheJiraFeature(to).Links.Add(type.LinkWhoseInwardIssueIs(from));
            }
            else
            {
                TheJiraFeature(from).Links.Add(type.LinkWhoseInwardIssueIs(to));
                TheJiraFeature(to).Links.Add(type.LinkWhoseOutwardIssueIs(from));
            }
        }

        /// <summary>An administrator renamed the inward end of the stock Blocks link, which Jira allows.</summary>
        protected void JiraRenamedTheBlocksInwardPhraseTo(string phrase)
        {
            jiraLinkTypes.Remove(Blocks);
            jiraLinkTypes.Add(Blocks with { Inward = phrase });
        }

        protected void JiraDefinesTheField(string name) => jiraFieldNames.Add(name);

        /// <summary>The instance answers its issue link type list with 503 Service Unavailable.</summary>
        protected void JiraRefusesToListItsLinkTypes() => jiraRefusesToListItsLinkTypes = true;

        /// <summary>The instance answers its issue link type list with 200 OK and a page no JSON reader can parse.</summary>
        protected void JiraAnswersItsLinkTypeListUnreadably() => jiraAnswersItsLinkTypeListUnreadably = true;

        protected void OnJiraTheFieldOf(string key, string fieldName, string value)
        {
            var index = jiraFieldNames.IndexOf(fieldName);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), $"The scenario fills '{fieldName}' on {key} before saying the instance defines it.");

            TheJiraFeature(key).FieldValues[JiraWireFormat.TheCustomFieldIdAt(index)] = value;
        }

        // ----- What the administrator configured --------------------------------------------------------

        /// <summary>
        /// The Portfolio's Dependency Field names an Additional Field whose Reference is
        /// <paramref name="reference"/>, the way an administrator registers a link, a relation type or a field.
        /// </summary>
        protected void PaymentsPlatformNamesAsItsDependencyField(string reference)
            => NameTheDependencyField(theJiraPortfolio, reference);

        /// <summary>
        /// The connection carries an Additional Field whose Reference is <paramref name="reference"/>, which
        /// the Portfolio does not name as its Dependency Field yet.
        /// </summary>
        protected void PaymentsPlatformsConnectionRegisters(string reference)
            => RegisterTheDependencyField(theJiraPortfolio, reference);

        /// <summary>The admin picks the Additional Field the connection registered as the Dependency Field.</summary>
        protected Task PaymentsPlatformsAdminNamesTheRegisteredFieldAsItsDependencyField()
        {
            var registered = theJiraPortfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions
                .Single(definition => !definition.IsPredefined).Id;

            return TheAdminSavesTheSettingsOf(theJiraPortfolio, settings =>
            {
                settings["dependencyFieldSource"] = "Own";
                settings["dependencyOverrideAdditionalFieldDefinitionId"] = registered;
            });
        }

        // ----- What somebody does -----------------------------------------------------------------------

        protected Task PaymentsPlatformRefreshes() => ThePortfolioRefreshes(theJiraPortfolio);

        /// <summary>
        /// A Portfolio in steady use, so its next refresh only downloads what moved. The first refresh on a
        /// new connection also registers the fields the tracker predefines, which changes what the next one
        /// asks for, so it takes two before the cheap path is in use.
        /// </summary>
        protected async Task PaymentsPlatformIsInSteadyUse()
        {
            await PaymentsPlatformRefreshes();
            await PaymentsPlatformRefreshes();
        }

        /// <summary>
        /// Refreshes until one refresh downloads only what moved, and answers with when the last refresh that
        /// downloaded everything ran and with what Refresh History shows after the cheap one. A new connection's
        /// first refreshes download everything, because the first one also registers the fields the tracker
        /// predefines, so how many it takes is the application's business and not the scenario's.
        /// </summary>
        protected async Task<(string LastFullDownloadAt, WhatRefreshHistoryShows AfterACheapRefresh)> PaymentsPlatformRefreshesUntilARefreshDownloadsOnlyWhatMoved()
        {
            var lastFullDownloadAt = string.Empty;

            for (var refresh = 0; refresh < 4; refresh++)
            {
                await PaymentsPlatformRefreshes();
                var shown = await WhatRefreshHistoryShowsForPaymentsPlatform();

                if (shown.NewestRunMode == "Delta" && lastFullDownloadAt.Length > 0)
                {
                    return (lastFullDownloadAt, shown);
                }

                lastFullDownloadAt = shown.NewestRunAt;
            }

            Assert.Fail("Four refreshes of an unchanged Portfolio in a row all downloaded everything, so no cheap refresh could be observed.");
            return (lastFullDownloadAt, null!);
        }

        /// <summary>Refreshes once more than Refresh History keeps rows for, so its oldest rows are pruned.</summary>
        protected async Task PaymentsPlatformRefreshesMoreTimesThanRefreshHistoryKeeps()
        {
            int kept;
            using (var scope = TheRunningLighthouse().CreateScope())
            {
                kept = scope.ServiceProvider.GetRequiredService<IAppSettingService>().GetRefreshLogRetentionRuns();
            }

            for (var refresh = 0; refresh <= kept; refresh++)
            {
                await PaymentsPlatformRefreshes();
            }
        }

        /// <summary>
        /// Something the next refresh asks the tracker for has changed, so that refresh downloads everything
        /// again; the administrator lengthened how far back closed Features are kept.
        /// </summary>
        protected Task SomethingMakesPaymentsPlatformsNextRefreshDownloadEverything()
            => TheAdminSavesTheSettingsOf(theJiraPortfolio, LengthenHowLongClosedFeaturesAreKept);

        /// <summary>The same change on an Azure DevOps Portfolio, whose next refresh then downloads everything again.</summary>
        protected Task SomethingMakesTheNextRefreshDownloadEverything(string adoPortfolio)
            => TheAdminSavesTheSettingsOf(adoPortfolios[adoPortfolio], LengthenHowLongClosedFeaturesAreKept);

        /// <summary>The admin picks None as the Dependency Field again; the link stays registered on the connection.</summary>
        protected Task PaymentsPlatformStopsNamingADependencyField()
            => TheAdminSavesTheSettingsOf(theJiraPortfolio, settings => settings["dependencyOverrideAdditionalFieldDefinitionId"] = null);

        // ----- What anybody can see afterwards ----------------------------------------------------------

        /// <summary>What Features → Dependencies lists the Feature as waiting on, in key order.</summary>
        protected async Task<IReadOnlyList<string>> WhatTheFeatureWaitsOn(string key)
            => [.. (await TheDependenciesListedFor(key)).Select(entry => entry.ReferenceId).Order(StringComparer.Ordinal)];

        protected async Task<IReadOnlyList<DependencySource>> WhereTheFeaturesDependenciesWereReadFrom(string key)
            => [.. (await TheDependenciesListedFor(key)).Select(entry => entry.Source).Distinct()];

        protected Task<WhatRefreshHistoryShows> WhatRefreshHistoryShowsForPaymentsPlatform()
            => WhatRefreshHistoryShowsFor(theJiraPortfolio.Id);

        protected Task<WhatRefreshHistoryShows> WhatRefreshHistoryShowsFor(string adoPortfolio)
            => WhatRefreshHistoryShowsFor(adoPortfolios[adoPortfolio].Id);

        protected Task<WhatRefreshHistoryShows> WhatRefreshHistoryShowsForTheTeam(string adoTeam)
            => WhatRefreshHistoryShowsFor(adoTeams[adoTeam].Id, RefreshType.Team);

        /// <summary>Every Warning the most recent refresh wrote, from the connector and the service alike, worded as the console writes it.</summary>
        protected IReadOnlyList<string> WhatTheLastRefreshWarned() => warningsOfTheLastRefresh;

        protected int RequestsJiraAnsweredDuringTheLastRefresh { get; private set; }

        // ----- Azure DevOps -----------------------------------------------------------------------------

        protected AnAzureDevOpsOrganisation TheOrganisationAt(string url)
        {
            if (!organisations.TryGetValue(url, out var organisation))
            {
                organisation = new AnAzureDevOpsOrganisation();
                organisations[url] = organisation;
            }

            return organisation;
        }

        /// <summary>
        /// A Portfolio on its own Azure DevOps connection, pointed at the organisation at <paramref name="url"/>.
        /// Its query answers with every work item that organisation holds.
        /// </summary>
        protected void AnAzureDevOpsPortfolio(string name, string url)
        {
            TheOrganisationAt(url);

            var connection = AnAzureDevOpsConnectionTo(url);
            var portfolio = new Portfolio
            {
                Id = connection.Id,
                Name = name,
                DataRetrievalValue = TheAdoPortfoliosQuery,
                WorkTrackingSystemConnectionId = connection.Id,
                WorkTrackingSystemConnection = connection,
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add(AdoFeatureType);
            portfolio.ToDoStates.Clear();
            portfolio.ToDoStates.Add("New");
            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add("Active");
            portfolio.DoneStates.Clear();
            portfolio.DoneStates.Add("Closed");

            adoPortfolios[name] = portfolio;
        }

        protected void NamesAsItsDependencyField(string adoPortfolio, string reference)
            => NameTheDependencyField(adoPortfolios[adoPortfolio], reference);

        protected void NamesAsItsParentOverrideField(string adoPortfolio, string reference)
            => NamesIn(adoPortfolio, ASettingThatReadsAField.ParentOverrideField, reference);

        /// <summary>The Portfolio names <paramref name="reference"/> in one of its settings that read a field.</summary>
        protected void NamesIn(string adoPortfolio, ASettingThatReadsAField setting, string reference)
        {
            var portfolio = adoPortfolios[adoPortfolio];
            var fieldId = TheParentOverrideFieldId + (10 * (int)setting) + portfolio.Id;

            portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = fieldId,
                DisplayName = reference,
                Reference = reference,
            });

            switch (setting)
            {
                case ASettingThatReadsAField.ParentOverrideField:
                    portfolio.OwnParentOverride(fieldId);
                    break;
                case ASettingThatReadsAField.SizeEstimateField:
                    portfolio.OwnSizeEstimateField(fieldId);
                    break;
                case ASettingThatReadsAField.FeatureOwnerField:
                    portfolio.OwnFeatureOwnerField(fieldId);
                    break;
                case ASettingThatReadsAField.EstimationField:
                    portfolio.OwningEstimationField(fieldId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(setting), setting, "Not a setting that reads a field.");
            }
        }

        protected Task RefreshesNow(string adoPortfolio) => ThePortfolioRefreshes(adoPortfolios[adoPortfolio]);

        /// <summary>
        /// A Team on its own Azure DevOps connection, pointed at the organisation at <paramref name="url"/>.
        /// Its query answers with every work item that organisation holds, all of them in progress.
        /// </summary>
        protected void AnAzureDevOpsTeam(string name, string url)
        {
            TheOrganisationAt(url);

            var connection = AnAzureDevOpsConnectionTo(url);
            var team = new Team
            {
                Id = connection.Id,
                Name = name,
                DataRetrievalValue = TheAdoPortfoliosQuery,
                WorkTrackingSystemConnectionId = connection.Id,
                WorkTrackingSystemConnection = connection,
                WorkItemTypes = [AdoFeatureType],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Closed"],
            };

            adoTeams[name] = team;
        }

        /// <summary>The Team names <paramref name="reference"/> as its Parent Override Field.</summary>
        protected void TheTeamNamesAsItsParentOverrideField(string adoTeam, string reference)
        {
            var team = adoTeams[adoTeam];
            var fieldId = TheParentOverrideFieldId + team.Id;

            team.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = fieldId,
                DisplayName = reference,
                Reference = reference,
            });

            team.OwnParentOverride(fieldId);
        }

        /// <summary>The Team's connection carries an Additional Field for <paramref name="reference"/> that no setting of the Team names.</summary>
        protected void TheConnectionOfTheTeamAlsoRegisters(string adoTeam, string reference)
        {
            var connection = adoTeams[adoTeam].WorkTrackingSystemConnection;

            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = AFieldNoSettingNamesId + connection.Id + connection.AdditionalFieldDefinitions.Count,
                DisplayName = reference,
                Reference = reference,
            });
        }

        protected Task TheTeamRefreshes(string adoTeam)
            => TheRefreshOf(services => services.GetRequiredService<ITeamUpdater>().TriggerUpdate(adoTeams[adoTeam].Id));

        /// <summary>
        /// Refreshes the Team until one refresh downloads only what moved, and answers with when the last
        /// refresh that downloaded everything ran and with what Refresh History shows after the cheap one.
        /// </summary>
        protected async Task<(string LastFullDownloadAt, WhatRefreshHistoryShows AfterACheapRefresh)> TheTeamRefreshesUntilARefreshDownloadsOnlyWhatMoved(string adoTeam)
        {
            var lastFullDownloadAt = string.Empty;

            for (var refresh = 0; refresh < 4; refresh++)
            {
                await TheTeamRefreshes(adoTeam);
                var shown = await WhatRefreshHistoryShowsForTheTeam(adoTeam);

                if (shown.NewestRunMode == "Delta" && lastFullDownloadAt.Length > 0)
                {
                    return (lastFullDownloadAt, shown);
                }

                lastFullDownloadAt = shown.NewestRunAt;
            }

            Assert.Fail("Four refreshes of an unchanged Team in a row all downloaded everything, so no cheap refresh could be observed.");
            return (lastFullDownloadAt, null!);
        }

        /// <summary>The parent the Team's work in progress names for an Azure DevOps work item.</summary>
        protected async Task<string> TheParentTheTeamShowsFor(string adoTeam, int workItemId)
        {
            TheRunningLighthouse();
            using var client = theRunningLighthouse!.CreateClient().AsSystemAdmin();
            var today = theRunningLighthouse.Services.GetRequiredService<ILighthouseClock>().Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            using var response = await client.GetAsync(new Uri($"/api/latest/teams/{adoTeams[adoTeam].Id}/metrics/wip?asOfDate={today}", UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var item = JsonNode.Parse(body)!.AsArray()
                .OfType<JsonObject>()
                .FirstOrDefault(workItem => workItem["referenceId"]?.GetValue<string>() == $"{workItemId}");

            return item is null ? $"<{workItemId} is not in the Team's work in progress>" : item["parentWorkItemReference"]?.GetValue<string>() ?? string.Empty;
        }

        /// <summary>The stored Feature for an Azure DevOps work item, keyed by its id as Azure DevOps names it.</summary>
        protected Task<IReadOnlyList<string>> WhatTheWorkItemWaitsOn(int workItemId) => WhatTheFeatureWaitsOn($"{workItemId}");

        /// <summary>The parent the Feature's page names for an Azure DevOps work item.</summary>
        protected async Task<string> TheParentShownFor(int workItemId)
        {
            var feature = await TheFeatureAsListed($"{workItemId}");

            return feature is null ? $"<{workItemId} was never stored>" : feature["parentWorkItemReference"]?.GetValue<string>() ?? string.Empty;
        }

        /// <summary>
        /// An Azure DevOps connection as the administrator would press Validate on it, with the Additional
        /// Fields given. Validate is posted to the validate route the screen calls.
        /// </summary>
        protected async Task<ValidateAnswer> TheAdministratorValidatesAConnectionTo(string url, params string[] references)
        {
            TheOrganisationAt(url);
            var connection = AnAzureDevOpsConnectionTo(url);

            foreach (var reference in references)
            {
                connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { DisplayName = reference, Reference = reference });
            }

            var unsaved = new WorkTrackingSystemConnectionDto(connection) { Id = 0 };

            TheRunningLighthouse();
            using var client = theRunningLighthouse!.CreateClient().AsSystemAdmin();
            using var response = await client.PostAsJsonAsync(new Uri(ValidateRoute, UriKind.Relative), unsaved);
            var body = await response.Content.ReadAsStringAsync();
            var answer = JsonNode.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body) as JsonObject ?? [];

            return new ValidateAnswer(
                response.StatusCode,
                answer["isValid"]?.GetValue<bool>() ?? false,
                answer["message"]?.GetValue<string>() ?? body);
        }

        // ----- The running application -----------------------------------------------------------------

        private static void NameTheDependencyField(Portfolio portfolio, string reference)
            => portfolio.OwnDependencyField(RegisterTheDependencyField(portfolio, reference));

        private static int RegisterTheDependencyField(Portfolio portfolio, string reference)
        {
            var id = TheDependencyFieldId + portfolio.Id;

            portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = id,
                DisplayName = reference,
                Reference = reference,
            });

            return id;
        }

        private WorkTrackingSystemConnection AnAzureDevOpsConnectionTo(string url)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = nextConnectionId++,
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                Name = $"Azure DevOps at {url}",
                AuthenticationMethodKey = AuthenticationMethodKeys.AzureDevOpsPat,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = url, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = "encrypted-token", IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "10", IsSecret = false },
            ]);

            return connection;
        }

        private AzureDevOpsOverEveryOrganisation TheAzureDevOpsConnector()
            => theAdoConnector ??= new AzureDevOpsOverEveryOrganisation(organisations, theLog.CreateLogger<AzureDevOpsWorkTrackingConnector>());

        private AFeatureOnJira TheJiraFeature(string key)
        {
            if (!jiraFeatures.TryGetValue(key, out var feature))
            {
                Assert.Fail($"The scenario describes {key}'s links before saying Jira holds {key} at all.");
            }

            return feature!;
        }

        /// <summary>What Features → Dependencies lists for one Feature, read through the route that screen reads.</summary>
        private async Task<List<(string ReferenceId, DependencySource Source)>> TheDependenciesListedFor(string key)
        {
            var feature = await TheFeatureAsListed(key);

            Assert.That(feature, Is.Not.Null, $"{key} was never stored, so nothing can be said about what it waits on.");

            var dependsOn = feature!["dependsOn"];
            Assert.That(dependsOn, Is.InstanceOf<JsonArray>(), $"The Feature answer for {key} carries no list of what it waits on.");

            return [.. dependsOn!.AsArray().Select(entry => (
                entry!["referenceId"]!.GetValue<string>(),
                Enum.Parse<DependencySource>(entry["source"]!.ToString(), ignoreCase: true)))];
        }

        private async Task<JsonObject?> TheFeatureAsListed(string reference)
        {
            TheRunningLighthouse();
            using var client = theRunningLighthouse!.CreateClient().AsSystemAdmin();
            using var response = await client.GetAsync(new Uri($"{FeaturesByReferenceRoute}{Uri.EscapeDataString(reference)}", UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            return JsonNode.Parse(body)!.AsArray()
                .OfType<JsonObject>()
                .FirstOrDefault(feature => feature["referenceId"]?.GetValue<string>() == reference);
        }

        private static void LengthenHowLongClosedFeaturesAreKept(JsonObject settings)
            => settings["doneItemsCutoffDays"] = settings["doneItemsCutoffDays"]!.GetValue<int>() + 30;

        /// <summary>
        /// The Portfolio's admin opens its settings page, changes one thing and saves, through the routes the
        /// page reads and saves with.
        /// </summary>
        private async Task TheAdminSavesTheSettingsOf(Portfolio portfolio, Action<JsonObject> change)
        {
            TheRunningLighthouse();
            using var client = theRunningLighthouse!.CreateClient().AsSystemAdmin();
            var route = $"{PortfolioRoute}{portfolio.Id}";

            using var read = await client.GetAsync(new Uri($"{route}/settings", UriKind.Relative));
            var readBody = await read.Content.ReadAsStringAsync();
            Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.OK), readBody);

            var settings = JsonNode.Parse(readBody)!.AsObject();
            change(settings);

            using var content = new StringContent(settings.ToJsonString(), Encoding.UTF8, "application/json");
            using var saved = await client.PutAsync(new Uri(route, UriKind.Relative), content);
            Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// The newest row Refresh History holds for this Portfolio or Team, read through the endpoint the
        /// screen reads. A Portfolio refresh also asks for a forecast, which writes a row of its own, so the
        /// row is found by what it refreshed and not by being the newest one there.
        /// </summary>
        private async Task<WhatRefreshHistoryShows> WhatRefreshHistoryShowsFor(int entityId, RefreshType type = RefreshType.Portfolio)
        {
            using var client = theRunningLighthouse!.CreateClient().AsSystemAdmin();
            using var response = await client.GetAsync(new Uri(RefreshLogRoute, UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            using var rows = JsonDocument.Parse(body);
            var newest = rows.RootElement.ValueKind == JsonValueKind.Array
                ? rows.RootElement.EnumerateArray()
                    .Where(row => IsARowFor(row, type, entityId))
                    .OrderByDescending(row => row.GetProperty("id").GetInt32())
                    .FirstOrDefault()
                : default;

            Assert.That(newest.ValueKind, Is.EqualTo(JsonValueKind.Object),
                $"Refresh History holds no row for {type} {entityId}. It answered {(int)response.StatusCode}: {body}");

            var findings = newest.TryGetProperty("latestFullRunFindings", out var listed) && listed.ValueKind == JsonValueKind.Array
                ? listed.EnumerateArray().Select(AsFinding).ToList()
                : null;

            var asOf = newest.TryGetProperty("latestFullRunAt", out var at) && at.ValueKind == JsonValueKind.String ? at.GetString() : null;

            return new WhatRefreshHistoryShows(
                findings, asOf, newest.GetProperty("mode").ToString(), newest.GetProperty("executedAt").ToString(), newest.GetProperty("success").GetBoolean());
        }

        private static Finding AsFinding(JsonElement finding)
            => new(
                finding.GetProperty("kind").ToString(),
                finding.GetProperty("setting").ToString(),
                finding.GetProperty("named").GetString() ?? string.Empty,
                [.. finding.GetProperty("seen").EnumerateArray().Select(seen => seen.GetString() ?? string.Empty)]);

        private static bool IsARowFor(JsonElement row, RefreshType type, int entityId)
        {
            var recordedType = row.GetProperty("type");
            var isOfThatType = recordedType.ValueKind == JsonValueKind.String
                ? recordedType.GetString() == $"{type}"
                : recordedType.GetInt32() == (int)type;

            return isOfThatType && row.GetProperty("entityId").GetInt32() == entityId;
        }

        /// <summary>
        /// Triggers one refresh and waits for the update queue to go idle, keeping what that refresh warned
        /// about and how many requests it sent to Jira.
        /// </summary>
        private Task ThePortfolioRefreshes(Portfolio portfolio)
            => TheRefreshOf(services => services.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolio.Id));

        /// <summary>
        /// The host's clock stands still, so two refreshes in one scenario would otherwise carry the same
        /// moment and could not be told apart in the refresh history.
        /// </summary>
        protected void AMinutePasses()
            => ((FakeTimeProvider)TheRunningLighthouse().GetRequiredService<TimeProvider>()).Advance(TimeSpan.FromMinutes(1));

        private async Task TheRefreshOf(Action<IServiceProvider> trigger)
        {
            var services = TheRunningLighthouse();
            var statusStore = services.GetRequiredService<IUpdateStatusStore>();

            var warningsBefore = whatWasLogged.WarningsAsWritten.Count;
            var requestsBefore = requestsJiraAnswered.Count;

            trigger(services);

            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (statusStore.HasActiveWork())
            {
                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail("The update queue never went idle, so the refresh this scenario describes never finished.");
                }

                await Task.Delay(20);
            }

            warningsOfTheLastRefresh = [.. whatWasLogged.WarningsAsWritten.Skip(warningsBefore)];
            RequestsJiraAnsweredDuringTheLastRefresh = requestsJiraAnswered.Count - requestsBefore;
        }

        /// <summary>
        /// The whole application over its own database, with the real Jira and Azure DevOps connectors handed
        /// to it in place of the ones the factory would build. The forecast is faked because Monte Carlo is not
        /// deterministic and nothing here is about a forecast; the licence is granted because a refresh nobody
        /// is licensed for reads no Additional Fields at all.
        /// </summary>
        private IServiceProvider TheRunningLighthouse()
        {
            if (theRunningLighthouse is not null)
            {
                return theRunningLighthouse.Services;
            }

            theHost = new TestWebApplicationFactory<Program>();

            var jira = JiraConnectorTestSetup.AConnectorOver(AJiraAnsweringForThatInstance(), theLog.CreateLogger<JiraWorkTrackingConnector>());
            var ado = TheAzureDevOpsConnector();

            var connectorFactory = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactory
                .Setup(factory => factory.GetWorkTrackingConnector(WorkTrackingSystems.Jira))
                .Returns(jira);
            connectorFactory
                .Setup(factory => factory.GetWorkTrackingConnector(WorkTrackingSystems.AzureDevOps))
                .Returns(ado);

            var licensed = new Mock<ILicenseService>();
            licensed.Setup(service => service.CanUsePremiumFeatures()).Returns(true);

            var log = theLog;

            theRunningLighthouse = theHost.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWorkTrackingConnectorFactory>();
                services.AddScoped(_ => connectorFactory.Object);

                services.RemoveAll<ILicenseService>();
                services.AddScoped(_ => licensed.Object);

                services.RemoveAll<IForecastService>();
                services.AddScoped(_ => ForecastServiceDoubles.AnsweringComputed().Object);

                services.RemoveAll<ILoggerFactory>();
                services.AddSingleton<ILoggerFactory>(log);
            }));

            using var setupScope = theRunningLighthouse.Services.CreateScope();
            var database = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            database.Database.EnsureDeleted();
            database.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }

            var portfolios = setupScope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolios.Add(theJiraPortfolio);

            foreach (var portfolio in adoPortfolios.Values)
            {
                portfolios.Add(portfolio);
            }

            var teams = setupScope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            foreach (var team in adoTeams.Values)
            {
                teams.Add(team);
            }

            portfolios.Save().GetAwaiter().GetResult();

            return theRunningLighthouse.Services;
        }

        private HttpMessageHandler AJiraAnsweringForThatInstance()
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) => Task.FromResult(AnAnswerTo(request)));

            return mock.Object;
        }

        private HttpResponseMessage AnAnswerTo(HttpRequestMessage request)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            lock (requestsJiraAnswered)
            {
                requestsJiraAnswered.Add(path);
            }

            if (jiraRefusesToListItsLinkTypes && path.EndsWith(JiraWireFormat.IssueLinkTypeEndpoint, StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("{\"errorMessages\":[\"Service Unavailable\"]}", Encoding.UTF8, "application/json"),
                };
            }

            if (jiraAnswersItsLinkTypeListUnreadably && path.EndsWith(JiraWireFormat.IssueLinkTypeEndpoint, StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html><body>Down for maintenance</body></html>", Encoding.UTF8, "text/html"),
                };
            }

            var body = path switch
            {
                _ when path.EndsWith(JiraWireFormat.ServerInfoEndpoint, StringComparison.Ordinal) => JiraWireFormat.ACloudDeployment,
                _ when path.EndsWith(JiraWireFormat.CredentialCheckEndpoint, StringComparison.Ordinal) => "{\"accountId\":\"someone\"}",
                _ when path.EndsWith(JiraWireFormat.FieldListEndpoint, StringComparison.Ordinal) => JiraWireFormat.TheFieldListDefining(jiraFieldNames),
                _ when path.EndsWith(JiraWireFormat.IssueLinkTypeEndpoint, StringComparison.Ordinal)
                    => JiraWireFormat.TheLinkTypeListDefining(jiraLinkTypes.Select((type, index) => JiraWireFormat.ALinkTypeDefinition(index, type))),
                _ when path.EndsWith("/changelog", StringComparison.Ordinal) => JiraWireFormat.AChangelogPageHolding([]),
                _ when path.Contains(JiraWireFormat.SearchEndpoint, StringComparison.Ordinal)
                    => JiraWireFormat.OnePageOf(TheFeaturesAskedForBy(TheQueryIn(request)).Select(feature => feature.OnTheWire())),
                _ => "{}",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private static string TheQueryIn(HttpRequestMessage request)
        {
            var query = request.RequestUri?.Query ?? string.Empty;
            var jql = query.TrimStart('?').Split('&').FirstOrDefault(part => part.StartsWith("jql=", StringComparison.Ordinal)) ?? string.Empty;

            return Uri.UnescapeDataString(jql.Length > 4 ? jql[4..] : JiraConnectorTestSetup.BodyOf(request));
        }

        private List<AFeatureOnJira> TheFeaturesAskedForBy(string jql)
        {
            if (!jql.Contains(KeyClause, StringComparison.Ordinal))
            {
                return [.. jiraFeatures.Values];
            }

            var keys = jql.Split(KeyClause, StringSplitOptions.None)
                .Skip(1)
                .Select(part => part[..part.IndexOf('"', StringComparison.Ordinal)])
                .ToHashSet(StringComparer.Ordinal);

            return [.. jiraFeatures.Values.Where(feature => keys.Contains(feature.Key))];
        }

        /// <summary>One Feature as Jira serves it each time it is asked.</summary>
        private sealed class AFeatureOnJira(string key)
        {
            public string Key { get; } = key;

            public List<string> Links { get; } = [];

            public Dictionary<string, string> FieldValues { get; } = [];

            public string OnTheWire()
            {
                var furtherFields = string.Concat(FieldValues.Select(field => ", \"" + field.Key + "\": \"" + field.Value + "\""));

                return JiraWireFormat.AnIssueWithItsHistory(
                    Key,
                    FeatureType,
                    JiraWireFormat.AnInstant(WhenEverythingWasCreated),
                    JiraWireFormat.AnInstant(WhenEverythingWasCreated),
                    furtherFields,
                    Links,
                    []);
            }
        }

        /// <summary>
        /// An Azure DevOps organisation as its client answers: its work items with their relations and field
        /// values, the fields it defines and the relation types it lists. Every request is recorded, and the
        /// relation-type listing can be told to come back empty or be refused.
        /// </summary>
        protected sealed class AnAzureDevOpsOrganisation
        {
            private readonly Dictionary<int, AWorkItemThere> workItems = [];

            public AnAzureDevOpsOrganisation()
            {
                var client = new Mock<WorkItemTrackingHttpClient>(new Uri("https://dev.azure.com/anywhere"), new VssCredentials());

                client
                    .Setup(c => c.QueryByWiqlAsync(It.IsAny<Wiql>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        Requests.Add("query");
                        return Task.FromResult(new WorkItemQueryResult { WorkItems = [.. workItems.Keys.Select(id => new WorkItemReference { Id = id })] });
                    });

                client
                    .Setup(c => c.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<DateTime?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<WorkItemErrorPolicy?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .Returns((IEnumerable<int> ids, IEnumerable<string> fields, DateTime? _, WorkItemExpand? expand, WorkItemErrorPolicy? _, object _, CancellationToken _) =>
                    {
                        var askedFor = ids.ToList();
                        Requests.Add($"{WhatTheReadIsFor(fields, expand)}:{askedFor.Count}");

                        return Task.FromResult<List<AdoWorkItem>>([.. askedFor
                            .Where(workItems.ContainsKey)
                            .Select(id => expand == WorkItemExpand.Relations ? workItems[id].Relations() : workItems[id].Payload())]);
                    });

                client
                    .Setup(c => c.GetWorkItemFieldsAsync(It.IsAny<GetFieldsExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        Requests.Add("fields");
                        return Task.FromResult(FieldsItDefines.ConvertAll(name => new WorkItemField2 { Name = name, ReferenceName = name }));
                    });

                client
                    .Setup(c => c.GetRelationTypesAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        Requests.Add("relation types");

                        if (RefusesToListItsRelationTypes)
                        {
                            throw new VssServiceException("TF401027: You need the Git 'GenericRead' permission to perform this action.");
                        }

                        if (TheListingFailsWith is not null)
                        {
                            throw TheListingFailsWith;
                        }

                        return Task.FromResult(AnswersTheListingWithoutAList ? null! : RelationTypesItLists.ConvertAll(AsListed));
                    });

                client
                    .Setup(c => c.GetRevisionsAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        Requests.Add("revisions");
                        return Task.FromResult(new List<AdoWorkItem>());
                    });

                Client = client.Object;
            }

            public WorkItemTrackingHttpClient Client { get; }

            public List<string> Requests { get; } = [];

            public List<string> FieldsItDefines { get; } = [];

            /// <summary>
            /// The relation types the organisation lists, by reference name. A directional type is listed end by
            /// end; a non-directional one, such as Related, under its one name. It starts as everything an Azure
            /// DevOps Services organisation answered when asked, attachments and hyperlinks included.
            /// </summary>
            public List<string> RelationTypesItLists { get; } = [.. RelationTypesAnOrganisationAnswered.Select(type => type.ReferenceName)];

            public bool RefusesToListItsRelationTypes { get; set; }

            /// <summary>What the request for the relation types fails with instead of answering, when set.</summary>
            public Exception? TheListingFailsWith { get; set; }

            /// <summary>The request for the relation types is answered, but with no list in the answer at all.</summary>
            public bool AnswersTheListingWithoutAList { get; set; }

            public int RelationTypeListings => Requests.Count(request => request == "relation types");

            // The identity sweep reads the change stamp alone, which is not a download of the work items.
            private static string WhatTheReadIsFor(IEnumerable<string>? fields, WorkItemExpand? expand)
            {
                if (expand == WorkItemExpand.Relations)
                {
                    return "relations";
                }

                return fields is not null && fields.SequenceEqual(TheChangeStampAlone) ? "stamps" : "payload";
            }

            private static readonly string[] TheChangeStampAlone = [AzureDevOpsFieldNames.ChangedDate];

            public void Holds(params int[] ids)
            {
                foreach (var id in ids)
                {
                    workItems[id] = new AWorkItemThere(id);
                }
            }

            /// <summary><paramref name="from"/> carries a relation of type <paramref name="rel"/> pointing at <paramref name="to"/>.</summary>
            public void Relates(int from, string rel, int to) => workItems[from].RelationsItCarries.Add((rel, to));

            public void Fills(int id, string field, string value) => workItems[id].FieldValues[field] = value;

            /// <summary>
            /// What an Azure DevOps Services organisation answered for its relation types, recorded from a real
            /// one. Read from the source tree, so adding to the recording needs no change to the project file.
            /// </summary>
            private static readonly List<WorkItemRelationType> RelationTypesAnOrganisationAnswered = TheRecordedRelationTypes();

            /// <summary>
            /// Azure DevOps Services cannot define a link type of its own, but an on-premises Azure DevOps Server
            /// can, and lists each end of it beside the built-in ones. Contoso's pair is such a custom type.
            /// </summary>
            private static readonly List<WorkItemRelationType> CustomRelationTypesAServerDefines =
            [
                ACustomRelationTypeEnd(ContosoDependsOnForward, "Depends On", ContosoDependsOnReverse, isForward: true),
                ACustomRelationTypeEnd(ContosoDependsOnReverse, "Depended On By", ContosoDependsOnForward, isForward: false),
                ACustomRelationTypeEnd("Fabrikam.Needs-Forward", "Needs", "Fabrikam.Needs-Reverse", isForward: true),
                ACustomRelationTypeEnd("Fabrikam.Needs-Reverse", "Needed By", "Fabrikam.Needs-Forward", isForward: false),
            ];

            // Listed under the base name its two ends share, so a reference to that base matches something listed
            // and still names neither end.
            private static readonly WorkItemRelationType ContosoDependsOnUnderItsBaseName = new()
            {
                ReferenceName = ContosoDependsOn,
                Name = "Depends On",
                Attributes = new Dictionary<string, object> { ["usage"] = "workItemLink", ["directional"] = true, ["topology"] = "dependency" },
            };

            private static WorkItemRelationType AsListed(string referenceName)
                => RelationTypesAnOrganisationAnswered.Concat(CustomRelationTypesAServerDefines).Append(ContosoDependsOnUnderItsBaseName)
                    .FirstOrDefault(type => type.ReferenceName == referenceName)
                    ?? throw new InvalidOperationException($"No organisation lists a relation type called {referenceName}; record it first.");

            private static WorkItemRelationType ACustomRelationTypeEnd(string referenceName, string name, string oppositeEnd, bool isForward)
                => new()
                {
                    ReferenceName = referenceName,
                    Name = name,
                    Attributes = new Dictionary<string, object>
                    {
                        ["usage"] = "workItemLink",
                        ["editable"] = true,
                        ["enabled"] = true,
                        ["directional"] = true,
                        ["topology"] = "dependency",
                        ["isForward"] = isForward,
                        ["oppositeEndReferenceName"] = oppositeEnd,
                    },
                };

            private static List<WorkItemRelationType> TheRecordedRelationTypes()
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
                {
                    directory = directory.Parent;
                }

                var recording = Path.Combine(
                    directory?.FullName ?? throw new InvalidOperationException("Lighthouse.sln could not be found to anchor the recorded relation types."),
                    "Lighthouse.Backend.Tests", "API", "Integration", "ConfigureOncePerConnection", "Fixtures", "azure-devops-services-relation-types.json");

                var answer = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(recording));

                return answer["value"]?.ToObject<List<WorkItemRelationType>>()
                    ?? throw new InvalidOperationException($"{recording} holds no relation types.");
            }

            private sealed class AWorkItemThere(int id)
            {
                public List<(string Rel, int To)> RelationsItCarries { get; } = [];

                public Dictionary<string, string> FieldValues { get; } = [];

                public AdoWorkItem Payload()
                {
                    var fields = new Dictionary<string, object>
                    {
                        [AzureDevOpsFieldNames.Id] = id,
                        [AzureDevOpsFieldNames.State] = "Active",
                        [AzureDevOpsFieldNames.Title] = $"Item {id}",
                        [AzureDevOpsFieldNames.WorkItemType] = AdoFeatureType,
                        [AzureDevOpsFieldNames.CreatedDate] = WhenEverythingWasCreated,
                        [AzureDevOpsFieldNames.ChangedDate] = WhenEverythingWasCreated,
                        [AzureDevOpsFieldNames.StackRank] = $"{id}",
                    };

                    foreach (var (field, value) in FieldValues)
                    {
                        fields[field] = value;
                    }

                    var item = new AdoWorkItem { Id = id, Fields = fields, Links = new ReferenceLinks() };
                    item.Links.AddLink(AzureDevOpsFieldNames.UrlPropertyName, $"https://dev.azure.com/anywhere/_workitems/edit/{id}");

                    return item;
                }

                public AdoWorkItem Relations() => new()
                {
                    Id = id,
                    Relations = [.. RelationsItCarries.Select(relation => new WorkItemRelation
                    {
                        Rel = relation.Rel,
                        Url = $"https://dev.azure.com/anywhere/_apis/wit/workItems/{relation.To}",
                        Attributes = new Dictionary<string, object> { ["name"] = TheNameAzureDevOpsShows(relation.Rel) },
                    })],
                };

                // Azure DevOps hands a relation's display name beside its type; the parent link is the one it calls
                // "Parent", and that name is what the connector finds the native parent by.
                private static string TheNameAzureDevOpsShows(string rel) => rel == Hierarchy ? "Parent" : rel;
            }
        }

        /// <summary>
        /// The production Azure DevOps connector, one instance for every organisation, as the factory caches it.
        /// Only the client is chosen here, by the organisation a connection points at.
        /// </summary>
        private sealed class AzureDevOpsOverEveryOrganisation(
            Dictionary<string, AnAzureDevOpsOrganisation> organisations, ILogger<AzureDevOpsWorkTrackingConnector> logger)
            : AzureDevOpsWorkTrackingConnector(logger, Mock.Of<IWorkTrackingAuthStrategyFactory>(), TimeProvider.System)
        {
            internal override Task<WorkItemTrackingHttpClient> GetWorkItemTrackingHttpClientAsync(WorkTrackingSystemConnection workTrackingSystemConnection)
            {
                var url = workTrackingSystemConnection.FindWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.Url) ?? string.Empty;

                return Task.FromResult(organisations[url].Client);
            }
        }
    }
}
