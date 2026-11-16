using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors
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
        private const string IncrementalSourceCapabilityName = "Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.IIncrementalSource";

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

        private static Type GivenTheIncrementalSourceCapability()
        {
            var capability = typeof(IWorkTrackingConnector).Assembly.GetType(IncrementalSourceCapabilityName);

            Assert.That(
                capability,
                Is.Not.Null,
                $"WHAT: there is no {IncrementalSourceCapabilityName} in Lighthouse.Backend. WHY: without a sweeping capability of its own, " +
                "every connector is asked to sweep and to fetch by id, and CSV, Linear and ServiceNow can only refuse what they claim to do. " +
                "HOW: add the interface in Services/Interfaces/WorkTrackingConnectors, move the per-connection probe, the three sweeps and " +
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
