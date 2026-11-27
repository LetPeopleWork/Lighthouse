using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.AzureDevOps;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Jira;
using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.TrackerIntegration.Application
{
    /// <summary>
    /// Only Jira and Azure DevOps can sweep a query for identities and change stamps and then fetch just the
    /// records that moved; CSV, Linear and ServiceNow always download everything. A connector that cannot
    /// sweep must not claim it can, so sweeping is its own capability, and whether a connection can be swept
    /// right now is asked of that capability: Azure DevOps always can, Jira only once Lighthouse has reached
    /// the site and learned which deployment answers there. Every refresh therefore takes the same path,
    /// sweep or full download, for each connector and connection as before.
    /// </summary>
    [TestFixture]
    public class IncrementalSourceCapabilityTest
    {
        private const string IncrementalSourceCapabilityName = "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IIncrementalSource";

        private const string ProbeMember = "SupportsIncrementalSync";

        private static readonly string[] ConnectorsThatSweep =
        [
            "JiraWorkTrackingConnector",
            "AzureDevOpsWorkTrackingConnector",
        ];

        private static readonly string[] EveryKnownConnector =
        [
            "JiraWorkTrackingConnector",
            "AzureDevOpsWorkTrackingConnector",
            "LinearWorkTrackingConnector",
            "CsvWorkTrackingConnector",
            "ServiceNowWorkTrackingConnector",
        ];

        private static readonly MemberShape[] TheSweepingMembers =
        [
            new(ProbeMember, typeof(bool), [typeof(WorkTrackingSystemConnection)]),
            new("SweepWorkItemsForTeam", typeof(Task<IReadOnlyList<RemoteRecordStamp>>), [typeof(Team), typeof(CancellationToken)]),
            new("GetWorkItemsForTeam", typeof(Task<IEnumerable<WorkItem>>), [typeof(Team), typeof(IReadOnlyCollection<string>), typeof(CancellationToken)]),
            new("SweepFeaturesForPortfolio", typeof(Task<IReadOnlyList<RemoteRecordStamp>>), [typeof(Portfolio), typeof(CancellationToken)]),
            new("GetFeaturesForPortfolio", typeof(Task<List<Feature>>), [typeof(Portfolio), typeof(IReadOnlyCollection<string>), typeof(CancellationToken)]),
            new("SweepParentFeatures", typeof(Task<IReadOnlyList<RemoteRecordStamp>>), [typeof(Portfolio), typeof(IEnumerable<string>), typeof(CancellationToken)]),
        ];

        private static readonly MemberShape[] WhatEveryConnectorServes =
        [
            new("SupportsTransitionHistory", typeof(bool), [typeof(WorkTrackingSystemConnection)]),
            new("GetPredefinedAdditionalFields", typeof(IReadOnlyList<AdditionalFieldDefinition>), [typeof(WorkTrackingSystemConnection)]),
            new("GetWorkItemsForTeam", typeof(Task<IEnumerable<WorkItem>>), [typeof(Team), typeof(CancellationToken)]),
            new("ValidateConnection", typeof(Task<ConnectionValidationResult>), [typeof(WorkTrackingSystemConnection)]),
            new("ValidateTeamSettings", typeof(Task<ConnectionValidationResult>), [typeof(Team)]),
            new("ValidatePortfolioSettings", typeof(Task<ConnectionValidationResult>), [typeof(Portfolio)]),
        ];

        [Test]
        public void SweepingIsItsOwnCapability_AndEveryConnectorIsAskedOnlyForWhatAllFiveServe()
        {
            var capability = GivenTheIncrementalSourceCapability();

            ThenTheCapabilityDeclaresTheProbeTheSweepsAndTheFetchesById(capability);
            ThenEveryConnectorIsAskedForExactlyWhatAllFiveServe();
        }

        [Test]
        public void OnlyJiraAndAzureDevOpsCanSweep_CsvLinearAndServiceNowDownloadEverythingAsBefore()
        {
            var capability = GivenTheIncrementalSourceCapability();

            var connectors = WhenEveryConnectorIsRead();

            ThenEveryKnownConnectorWasRead(connectors);
            ThenExactlyJiraAndAzureDevOpsTakeOnTheCapability(connectors, capability);
        }

        [Test]
        public async Task AJiraSiteIsSweptOnlyOnceLighthouseHasReachedIt()
        {
            var capability = GivenTheIncrementalSourceCapability();
            var jira = GivenAJiraConnector();
            var reachedTeam = await GivenATeamWhoseJiraSiteLighthouseHasReached(jira);
            var unreachedTeam = GivenATeamOnAJiraSiteNoTestHasReached();

            var answers = WhenTheCapabilityIsAskedAboutBothSites(capability, jira, reachedTeam, unreachedTeam);

            ThenOnlyTheReachedSiteIsSwept(answers);
        }

        [Test]
        public void AnAzureDevOpsConnectionIsAlwaysSwept()
        {
            var capability = GivenTheIncrementalSourceCapability();
            var azureDevOps = GivenAnAzureDevOpsConnector();

            var answer = WhenTheCapabilityIsAskedAbout(capability, azureDevOps, AnAzureDevOpsConnection());

            ThenTheAzureDevOpsConnectionIsSwept(answer);
        }

        [Test]
        public async Task ATeamRefreshOnAConnectorThatCannotSweep_DownloadsTheWholeQuery()
        {
            var connector = GivenAConnectorThatCannotSweep();
            var team = GivenATeamWhoseQueryHoldsOneWorkItem(connector);

            await WhenTheTeamIsRefreshed(connector, team);

            ThenTheWholeQueryWasDownloadedOnceAndNothingWasSwept(connector, TeamDownload);
        }

        [Test]
        public async Task APortfolioRefreshOnAConnectorThatCannotSweep_DownloadsEveryFeature()
        {
            var connector = GivenAConnectorThatCannotSweep();
            var portfolio = GivenAPortfolioWhoseQueryHoldsOneFeature(connector, parentReferenceId: null);

            await WhenThePortfolioIsRefreshed(connector, portfolio);

            ThenTheWholeQueryWasDownloadedOnceAndNothingWasSwept(connector, PortfolioDownload);
        }

        [Test]
        public async Task AParentFeatureReadOnAConnectorThatCannotSweep_ReadsEveryParentInFull()
        {
            var connector = GivenAConnectorThatCannotSweep();
            var portfolio = GivenAPortfolioWhoseQueryHoldsOneFeature(connector, parentReferenceId: "PARENT-1");

            await WhenThePortfolioIsRefreshed(connector, portfolio);

            ThenTheWholeQueryWasDownloadedOnceAndNothingWasSwept(connector, ParentDownload);
        }

        private static Type GivenTheIncrementalSourceCapability()
        {
            var capability = typeof(IWorkTrackingConnector).Assembly.GetType(IncrementalSourceCapabilityName);

            Assert.That(
                capability,
                Is.Not.Null,
                $"WHAT: there is no {IncrementalSourceCapabilityName} in Lighthouse.Backend. WHY: without a sweeping capability of its own, " +
                "every connector is asked to sweep and to fetch by id, and CSV, Linear and ServiceNow can only refuse what they claim to do. " +
                "HOW: add the interface in Modules/TrackerIntegration/Contracts, move the per-connection probe, the three sweeps and " +
                "the two fetches by id onto it, and let only the Jira and Azure DevOps connectors take it on.");
            Assert.That(capability!.IsInterface, Is.True, $"WHAT: {IncrementalSourceCapabilityName} is not an interface. WHY: a connector takes a capability on by implementing it. HOW: declare it as an interface.");

            return capability;
        }

        private static List<Type> WhenEveryConnectorIsRead()
            => [.. typeof(IWorkTrackingConnector).Assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IWorkTrackingConnector).IsAssignableFrom(type))];

        private static JiraWorkTrackingConnector GivenAJiraConnector()
            => JiraConnectorTestSetup.AConnectorOver(new JiraInstanceStub().Handler());

        private static async Task<Team> GivenATeamWhoseJiraSiteLighthouseHasReached(JiraWorkTrackingConnector jira)
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();

            // Lighthouse learns which deployment answers at a site only by asking it, and a first full
            // download is what asks.
            await jira.GetWorkItemsForTeam(team, CancellationToken.None);

            return team;
        }

        private static Team GivenATeamOnAJiraSiteNoTestHasReached() => JiraConnectorTestSetup.ATeamOnJiraCloud();

        private static AzureDevOpsWorkTrackingConnector GivenAnAzureDevOpsConnector()
            => new AzureDevOpsWorkTrackingConnector(
                Mock.Of<ILogger<AzureDevOpsWorkTrackingConnector>>(),
                TestAuthStrategyFactory.CreateRealFactory(new FakeCryptoService()),
                TimeProvider.System);

        private static WorkTrackingSystemConnection AnAzureDevOpsConnection()
            => new() { Id = 41, Name = "An Azure DevOps connection", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };

        private static (bool Reached, bool Unreached) WhenTheCapabilityIsAskedAboutBothSites(
            Type capability, JiraWorkTrackingConnector jira, Team reachedTeam, Team unreachedTeam)
            => (WhenTheCapabilityIsAskedAbout(capability, jira, reachedTeam.WorkTrackingSystemConnection),
                WhenTheCapabilityIsAskedAbout(capability, jira, unreachedTeam.WorkTrackingSystemConnection));

        private static bool WhenTheCapabilityIsAskedAbout(Type capability, IWorkTrackingConnector connector, WorkTrackingSystemConnection connection)
        {
            Assert.That(
                capability.IsInstanceOfType(connector),
                Is.True,
                $"WHAT: the {connector.GetType().Name} does not take on {IncrementalSourceCapabilityName}. WHY: its Teams and Portfolios " +
                "would never be swept again and every refresh would download everything. HOW: list the capability on its connector interface.");

            var probe = capability.GetMethod(ProbeMember, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, [typeof(WorkTrackingSystemConnection)]);

            Assert.That(
                probe,
                Is.Not.Null,
                $"WHAT: {IncrementalSourceCapabilityName} does not declare bool {ProbeMember}(WorkTrackingSystemConnection). WHY: whether a " +
                "connection can be swept now is a per-connection answer, and only a connector that can sweep at all has one. HOW: move the " +
                "probe from IWorkTrackingConnector onto the capability unchanged.");

            return (bool)probe!.Invoke(connector, [connection])!;
        }

        private static void ThenTheCapabilityDeclaresTheProbeTheSweepsAndTheFetchesById(Type capability)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var member in TheSweepingMembers)
                {
                    Assert.That(
                        DeclaredReturnTypeOf(capability, member),
                        Is.EqualTo(member.ReturnType),
                        $"WHAT: {IncrementalSourceCapabilityName} does not declare {member}. WHY: a refresh that sweeps asks the same " +
                        "connector for the stamps and then for the records that moved, so both halves belong to the capability. HOW: move " +
                        "the member from IWorkTrackingConnector onto the capability unchanged.");
                }
            }
        }

        private static void ThenEveryConnectorIsAskedForExactlyWhatAllFiveServe()
        {
            var declared = typeof(IWorkTrackingConnector)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(method => new MemberShape(method.Name, method.ReturnType, [.. method.GetParameters().Select(parameter => parameter.ParameterType)]).ToString())
                .ToList();

            var expected = WhatEveryConnectorServes.Select(member => member.ToString()).ToList();

            Assert.That(
                declared,
                Is.EquivalentTo(expected),
                $"WHAT: IWorkTrackingConnector declares [{string.Join("; ", declared)}], expected exactly [{string.Join("; ", expected)}]. " +
                "WHY: any further member is one CSV, Linear or ServiceNow must claim and can only refuse, and a missing one is a question " +
                "every refresh asks. HOW: remove the probe, the three sweeps and the two fetches by id from IWorkTrackingConnector; they " +
                "live on the sweeping capability.");
        }

        private static void ThenEveryKnownConnectorWasRead(List<Type> connectors)
        {
            var read = connectors.Select(type => type.Name).ToList();

            Assert.That(
                read,
                Is.SupersetOf(EveryKnownConnector),
                $"WHAT: the connectors read were [{string.Join(", ", read)}], which does not hold all five of [{string.Join(", ", EveryKnownConnector)}]. " +
                "WHY: a connector that is not read is never checked, and the answer below would hold without looking. HOW: each connector " +
                "must remain a concrete class in Lighthouse.Backend implementing IWorkTrackingConnector.");
        }

        private static void ThenExactlyJiraAndAzureDevOpsTakeOnTheCapability(List<Type> connectors, Type capability)
        {
            var sweeping = connectors.Where(capability.IsAssignableFrom).Select(type => type.Name).ToList();

            Assert.That(
                sweeping,
                Is.EquivalentTo(ConnectorsThatSweep),
                $"WHAT: the connectors that take on the sweeping capability are [{string.Join(", ", sweeping)}], expected exactly " +
                $"[{string.Join(", ", ConnectorsThatSweep)}]. WHY: CSV, Linear and ServiceNow cannot sweep, so taking the capability on " +
                "would claim a sweep they can only refuse, and dropping Jira or Azure DevOps would make every one of their refreshes a " +
                "full download. HOW: list the capability on IJiraWorkTrackingConnector and IAzureDevOpsWorkTrackingConnector, and remove " +
                "the probe, sweep and fetch-by-id members from the CSV, Linear and ServiceNow connectors.");
        }

        private static void ThenOnlyTheReachedSiteIsSwept((bool Reached, bool Unreached) answers)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    answers.Reached,
                    Is.True,
                    "WHAT: a Jira site Lighthouse has already reached is not swept. WHY: its deployment is known, so every refresh after " +
                    "the first would download everything again. HOW: keep the Jira probe answering from the deployment Lighthouse has " +
                    "already learned for the site.");
                Assert.That(
                    answers.Unreached,
                    Is.False,
                    "WHAT: a Jira site no one has reached yet is swept. WHY: until Lighthouse has asked the site, it does not know which " +
                    "search answers there and cannot page a sweep, so the first refresh must download everything. HOW: keep the Jira " +
                    "probe answering no while the site's deployment is unknown; do not make it a constant.");
            }
        }

        private static void ThenTheAzureDevOpsConnectionIsSwept(bool answer)
        {
            Assert.That(
                answer,
                Is.True,
                "WHAT: an Azure DevOps connection is not swept. WHY: every Azure DevOps refresh would download the whole query and re-read " +
                "every item's history, which sweeping exists to avoid. HOW: keep the Azure DevOps probe answering yes for every connection.");
        }

        private static Type? DeclaredReturnTypeOf(Type owner, MemberShape member)
            => owner.GetMethod(member.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, member.Parameters)?.ReturnType;

        private static Mock<IWorkTrackingConnector> GivenAConnectorThatCannotSweep()
        {
            var connector = new Mock<IWorkTrackingConnector>();
            connector.As<IFeatureSource>();

            var interfaces = connector.Object.GetType().GetInterfaces().Select(type => type.FullName).ToList();

            Assert.That(
                interfaces,
                Does.Not.Contain(IncrementalSourceCapabilityName),
                "WHAT: the connector standing in for CSV, Linear or ServiceNow takes on the sweeping capability. WHY: this case is about a " +
                "connector that cannot sweep at all. HOW: build the stand-in from the general port and the Feature-reading capability only.");

            return connector;
        }

        private static Team GivenATeamWhoseQueryHoldsOneWorkItem(Mock<IWorkTrackingConnector> connector)
        {
            var team = new Team { Id = 51, Name = "A Team on a tracker that cannot sweep", WorkTrackingSystemConnection = ACsvConnection() };
            team.WorkItemTypes.Add("User Story");

            connector
                .Setup(source => source.GetWorkItemsForTeam(team, It.IsAny<CancellationToken>()))
                .ReturnsAsync([new WorkItem { ReferenceId = "ITEM-1", Name = "The only item", Type = "User Story", Team = team }]);

            return team;
        }

        private static Portfolio GivenAPortfolioWhoseQueryHoldsOneFeature(Mock<IWorkTrackingConnector> connector, string? parentReferenceId)
        {
            var portfolio = new Portfolio { Id = 52, Name = "A Portfolio on a tracker that cannot sweep", WorkTrackingSystemConnection = ACsvConnection() };
            portfolio.WorkItemTypes.Add("Feature");

            var features = connector.As<IFeatureSource>();
            features
                .Setup(source => source.GetFeaturesForPortfolio(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([new Feature(new WorkItemBase { ReferenceId = "FEATURE-1", Name = "The only Feature", Type = "Feature" }) { ParentReferenceId = parentReferenceId ?? string.Empty }]);
            features
                .Setup(source => source.GetParentFeaturesDetails(portfolio, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            return portfolio;
        }

        private static WorkTrackingSystemConnection ACsvConnection()
            => new() { Id = 53, Name = "A CSV connection", WorkTrackingSystem = WorkTrackingSystems.Csv };

        private static Task<SyncOutcome> WhenTheTeamIsRefreshed(Mock<IWorkTrackingConnector> connector, Team team)
            => new WorkItemServiceTestBuilder().WithConnector(connector.Object).Build().UpdateWorkItemsForTeam(team);

        private static Task<SyncOutcome> WhenThePortfolioIsRefreshed(Mock<IWorkTrackingConnector> connector, Portfolio portfolio)
            => new WorkItemServiceTestBuilder().WithConnector(connector.Object).Build().UpdateFeaturesForPortfolio(portfolio);

        private static void ThenTheWholeQueryWasDownloadedOnceAndNothingWasSwept(Mock<IWorkTrackingConnector> connector, FullDownload download)
        {
            var asked = connector.Invocations
                .Select(invocation => new MemberShape(invocation.Method.Name, invocation.Method.ReturnType, [.. invocation.Method.GetParameters().Select(parameter => parameter.ParameterType)]).ToString())
                .ToList();

            var expected = download.Member.ToString();
            var sweepsOrFetchesById = asked
                .Where(member => member.Contains(" Sweep", StringComparison.Ordinal) || member.Contains("IReadOnlyCollection<String>", StringComparison.Ordinal))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    asked.Count(member => member == expected),
                    Is.EqualTo(1),
                    $"WHAT: the refresh did not ask for {expected} exactly once; it asked for [{string.Join("; ", asked)}]. WHY: a connector " +
                    $"that cannot sweep is read in full on every refresh, so {download.What} must come from one full download. HOW: when the " +
                    "connector is not a sweeping capability, take the full path, as for a connection whose probe answers no.");
                Assert.That(
                    sweepsOrFetchesById,
                    Is.Empty,
                    $"WHAT: the refresh asked a connector that cannot sweep for [{string.Join("; ", sweepsOrFetchesById)}]. WHY: only a sweeping " +
                    "capability can answer a sweep or a fetch by id; anyone else can only refuse, and the refresh would lose its records. HOW: " +
                    "test the connector for the sweeping capability before any sweep or fetch by id.");
            }
        }

        private static readonly FullDownload TeamDownload = new(
            new("GetWorkItemsForTeam", typeof(Task<IEnumerable<WorkItem>>), [typeof(Team), typeof(CancellationToken)]), "the Team's work items");

        private static readonly FullDownload PortfolioDownload = new(
            new("GetFeaturesForPortfolio", typeof(Task<List<Feature>>), [typeof(Portfolio), typeof(CancellationToken)]), "the Portfolio's Features");

        private static readonly FullDownload ParentDownload = new(
            new("GetParentFeaturesDetails", typeof(Task<List<Feature>>), [typeof(Portfolio), typeof(IEnumerable<string>), typeof(CancellationToken)]), "the parent Features");

        private sealed record FullDownload(MemberShape Member, string What);

        private sealed record MemberShape(string Name, Type ReturnType, Type[] Parameters)
        {
            public override string ToString()
                => $"{Readable(ReturnType)} {Name}({string.Join(", ", Parameters.Select(Readable))})";

            private static string Readable(Type type)
                => type.IsGenericType
                    ? $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GetGenericArguments().Select(Readable))}>"
                    : type.Name;
        }
    }
}
