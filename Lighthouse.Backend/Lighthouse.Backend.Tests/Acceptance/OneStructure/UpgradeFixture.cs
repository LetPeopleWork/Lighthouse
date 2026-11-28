using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Csv;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;
using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lighthouse.Backend.Tests.Acceptance.OneStructure
{
    /// <summary>
    /// An installation shaped like the ones people upgrade from: Teams on Jira, Azure DevOps, Linear, ServiceNow and a
    /// CSV upload, Portfolios on Jira and Azure DevOps, a Team and a Portfolio on one Jira connection that both read
    /// Epic, a Team that spells its type "bug" where the others say "Bug", a Linear Team that reads no types, and an
    /// organisation that calls its Teams Squads and its Portfolios Initiatives.
    ///
    /// The trackers are not called: each answers its queries from the records written out here, and gives back only
    /// the records whose type the unit reads, ignoring case, as Jira, Azure DevOps and ServiceNow do. Linear has no
    /// types, so a Linear unit gets every record of its query. The CSV Team reads its upload through the real CSV
    /// connection.
    /// </summary>
    public static class UpgradeFixture
    {
        private static readonly string[] ToDo = ["To Do"];

        private static readonly string[] Doing = ["In Progress"];

        private static readonly string[] Done = ["Done"];

        /// <summary>The records each tracker query answers with, before the unit's types narrow them.</summary>
        public static TrackerRecords Trackers(DateOnly today) => new(today);

        public static void WriteTheInstallation(IServiceProvider services, DateOnly today)
        {
            NameTheOrganisationsUnits(services);

            var dbContext = services.GetRequiredService<LighthouseAppContext>();

            var jira = Connection(WorkTrackingSystems.Jira, "letpeoplework.atlassian.net");
            var azureDevOps = Connection(WorkTrackingSystems.AzureDevOps, "dev.azure.com/letpeoplework");
            var linear = Connection(WorkTrackingSystems.Linear, "linear.app/letpeoplework");
            var serviceNow = Connection(WorkTrackingSystems.ServiceNow, "letpeoplework.service-now.com");
            var csv = Connection(WorkTrackingSystems.Csv, "Weekly upload");
            csv.Options.Single(option => option.Key == CsvWorkTrackingOptionNames.DateTimeFormat).Value = "yyyy-MM-dd";

            dbContext.WorkTrackingSystemConnections.AddRange(jira, azureDevOps, linear, serviceNow, csv);
            dbContext.SaveChanges();

            dbContext.Teams.AddRange(
                ATeam("Payments", jira, TrackerRecords.PaymentsQuery, ["User Story", "Bug", "Epic"]),
                ATeam("Platform", azureDevOps, TrackerRecords.PlatformQuery, ["User Story", "bug"]),
                ATeam("Mobile", linear, TrackerRecords.MobileQuery, []),
                ATeam("Service Desk", serviceNow, TrackerRecords.ServiceDeskQuery, ["Incident", "Problem"]),
                ATeam("Data", csv, TrackerRecords.DataUpload(today), ["User Story", "Bug"]));
            dbContext.SaveChanges();

            dbContext.Portfolios.AddRange(
                APortfolio("Checkout", jira, TrackerRecords.CheckoutQuery, ["Epic", "Change"]),
                APortfolio("Roadmap", azureDevOps, TrackerRecords.RoadmapQuery, ["Epic"]));
            dbContext.SaveChanges();
        }

        private static void NameTheOrganisationsUnits(IServiceProvider services)
        {
            services.GetRequiredService<TerminologySeeder>().Seed().GetAwaiter().GetResult();

            services.GetRequiredService<ITerminologyService>().UpdateTerminology(
            [
                new TerminologyEntry { Key = "team", Value = "Squad" },
                new TerminologyEntry { Key = "teams", Value = "Squads" },
                new TerminologyEntry { Key = "portfolio", Value = "Initiative" },
                new TerminologyEntry { Key = "portfolios", Value = "Initiatives" },
            ]).GetAwaiter().GetResult();
        }

        private static WorkTrackingSystemConnection Connection(WorkTrackingSystems system, string name)
        {
            var connection = new WorkTrackingSystemFactory(NullLogger<WorkTrackingSystemFactory>.Instance)
                .CreateDefaultConnectionForWorkTrackingSystem(system);
            connection.Name = name;
            return connection;
        }

        private static Team ATeam(string name, WorkTrackingSystemConnection connection, string query, List<string> types) => new()
        {
            Name = name,
            WorkTrackingSystemConnectionId = connection.Id,
            DataRetrievalValue = query,
            WorkItemTypes = types,
            ToDoStates = [.. ToDo],
            DoingStates = [.. Doing],
            DoneStates = [.. Done],
            ThroughputHistory = 30,
        };

        private static Portfolio APortfolio(string name, WorkTrackingSystemConnection connection, string query, List<string> types) => new()
        {
            Name = name,
            WorkTrackingSystemConnectionId = connection.Id,
            DataRetrievalValue = query,
            WorkItemTypes = types,
            ToDoStates = [.. ToDo],
            DoingStates = [.. Doing],
            DoneStates = [.. Done],
        };
    }

    /// <summary>One record as a tracker holds it: days are counted back from today, and no day means not yet.</summary>
    public sealed record TrackerRecord(string Id, string Type, string State, int? StartedDaysAgo, int? ClosedDaysAgo, string Parent = "");

    /// <summary>What each tracker query of the upgrade fixture answers with.</summary>
    public sealed class TrackerRecords(DateOnly today)
    {
        public const string PaymentsQuery = "project = PAY";

        public const string CheckoutQuery = "project = PAY AND parent is EMPTY";

        public const string PlatformQuery = "[System.AreaPath] UNDER 'Platform'";

        public const string RoadmapQuery = "[System.AreaPath] UNDER 'Roadmap'";

        public const string MobileQuery = "team:MOB";

        public const string ServiceDeskQuery = "assignment_group=Service Desk";

        private static readonly Dictionary<string, IReadOnlyList<TrackerRecord>> ByQuery = new Dictionary<string, IReadOnlyList<TrackerRecord>>
        {
            [PaymentsQuery] =
            [
                new("PAY-1", "User Story", "Done", 12, 3, "PAY-100"),
                new("PAY-2", "User Story", "Done", 20, 9, "PAY-100"),
                new("PAY-3", "Bug", "Done", 4, 1, "PAY-100"),
                new("PAY-4", "Bug", "Done", 6, 5, "PAY-101"),
                new("PAY-5", "Bug", "Done", 15, 14, "PAY-101"),
                new("PAY-6", "User Story", "Done", 30, 21),
                new("PAY-7", "User Story", "In Progress", 6, null, "PAY-100"),
                new("PAY-8", "Bug", "In Progress", 2, null, "PAY-101"),
                new("PAY-9", "User Story", "To Do", null, null, "PAY-100"),
                new("PAY-10", "Bug", "To Do", null, null, "PAY-101"),
                new("PAY-11", "Task", "Done", 5, 2, "PAY-100"),
                new("PAY-12", "Epic", "In Progress", 40, null),
            ],
            [CheckoutQuery] =
            [
                new("PAY-100", "Epic", "In Progress", 25, null),
                new("PAY-101", "Change", "In Progress", 16, null),
                new("PAY-102", "Epic", "Done", 80, 30),
                new("PAY-103", "Story", "To Do", null, null),
            ],
            [PlatformQuery] =
            [
                new("4101", "User Story", "Done", 10, 2, "4200"),
                new("4102", "Bug", "Done", 8, 4, "4200"),
                new("4103", "Bug", "Done", 3, 3, "4200"),
                new("4104", "User Story", "Done", 18, 11),
                new("4105", "User Story", "In Progress", 9, null, "4200"),
                new("4106", "Bug", "To Do", null, null, "4200"),
                new("4107", "User Story", "To Do", null, null, "4200"),
            ],
            [RoadmapQuery] =
            [
                new("4200", "Epic", "In Progress", 12, null),
                new("4201", "Epic", "To Do", null, null),
                new("4202", "Feature", "In Progress", 5, null),
            ],
            [MobileQuery] =
            [
                new("MOB-1", "Issue", "Done", 7, 2),
                new("MOB-2", "Issue", "Done", 5, 4),
                new("MOB-3", "Issue", "In Progress", 3, null),
                new("MOB-4", "Issue", "To Do", null, null),
            ],
            [ServiceDeskQuery] =
            [
                new("INC0001", "Incident", "Done", 2, 1),
                new("INC0002", "Incident", "Done", 3, 2),
                new("PRB0001", "Problem", "In Progress", 10, null),
                new("CHG0001", "Change", "Done", 4, 1),
            ],
        };

        /// <summary>The CSV Team's upload, with its dates written out from today the way the upload carries them.</summary>
        public static string DataUpload(DateOnly today) => string.Join('\n',
            "ID,Name,State,Type,Started Date,Closed Date,Created Date,Parent Reference Id,Tags",
            $"DAT-1,Load the ledger,Done,User Story,{Day(today, 9)},{Day(today, 2)},{Day(today, 20)},,",
            $"DAT-2,Fix the import,Done,Bug,{Day(today, 4)},{Day(today, 1)},{Day(today, 20)},,",
            $"DAT-3,Backfill March,Done,User Story,{Day(today, 13)},{Day(today, 8)},{Day(today, 20)},,",
            $"DAT-4,Repair the totals,In Progress,Bug,{Day(today, 3)},,{Day(today, 20)},,",
            $"DAT-5,Archive old runs,To Do,User Story,,,{Day(today, 20)},,",
            $"DAT-6,Spike on caching,Done,Spike,{Day(today, 3)},{Day(today, 1)},{Day(today, 20)},,");

        private static string Day(DateOnly today, int daysAgo) => today.AddDays(-daysAgo).ToString("yyyy-MM-dd");

        public IReadOnlyList<WorkItemBase> Answer(IWorkItemQueryOwner owner, bool trackerHasTypes)
        {
            if (!ByQuery.TryGetValue(owner.DataRetrievalValue, out var records))
            {
                throw new InvalidOperationException(
                    $"The tracker of {owner.Name} was asked \"{owner.DataRetrievalValue}\", a query the upgrade fixture holds no records for. A unit asks its tracker with the query it stores, so a changed query means the unit no longer reads what it read before.");
            }

            var knownStates = owner.AllStates.ToHashSet(StringComparer.OrdinalIgnoreCase);

            return records
                .Where(record => knownStates.Contains(record.State))
                .Where(record => !trackerHasTypes || owner.WorkItemTypes.Contains(record.Type, StringComparer.OrdinalIgnoreCase))
                .Select(record => AsTheTrackerSendsIt(owner, record))
                .ToList();
        }

        private WorkItemBase AsTheTrackerSendsIt(IWorkItemQueryOwner owner, TrackerRecord record) => new()
        {
            ReferenceId = record.Id,
            Name = $"{record.Type} {record.Id}",
            Type = record.Type,
            State = owner.MapRawStateToMappedName(record.State),
            StateCategory = owner.MapStateToStateCategory(record.State),
            StartedDate = At(record.StartedDaysAgo),
            ClosedDate = At(record.ClosedDaysAgo),
            CreatedDate = At(60),
            ParentReferenceId = record.Parent,
            Url = string.Empty,
            Order = record.Id,
        };

        private DateTime? At(int? daysAgo)
            => daysAgo is { } days ? today.AddDays(-days).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc) : null;
    }

    /// <summary>
    /// The trackers as the upgrade fixture holds them. A CSV connection reads its upload through the product's own CSV
    /// connector; every other tracker answers from <see cref="TrackerRecords"/>.
    /// </summary>
    public sealed class TrackersAsTheFixtureHoldsThem(TrackerRecords records, CsvWorkTrackingConnector csv) : IWorkTrackingConnectorFactory
    {
        public IWorkTrackingConnector GetWorkTrackingConnector(WorkTrackingSystems workTrackingSystem) => workTrackingSystem switch
        {
            WorkTrackingSystems.Csv => csv,
            WorkTrackingSystems.Linear => new FixtureTracker(records, trackerHasTypes: false),
            _ => new FixtureTracker(records, trackerHasTypes: true),
        };

        private sealed class FixtureTracker(TrackerRecords records, bool trackerHasTypes) : IWorkTrackingConnector, IFeatureSource
        {
            public bool SupportsTransitionHistory(WorkTrackingSystemConnection connection) => false;

            public IReadOnlyList<AdditionalFieldDefinition> GetPredefinedAdditionalFields(WorkTrackingSystemConnection connection) => [];

            public Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team, CancellationToken cancellationToken)
                => Task.FromResult(records.Answer(team, trackerHasTypes).Select(record => new WorkItem(record, team)));

            public Task<List<Feature>> GetFeaturesForPortfolio(Portfolio portfolio, CancellationToken cancellationToken)
                => Task.FromResult(records.Answer(portfolio, trackerHasTypes).Select(record => new Feature(record)).ToList());

            public Task<List<Feature>> GetParentFeaturesDetails(Portfolio portfolio, IEnumerable<string> parentFeatureIds, CancellationToken cancellationToken)
                => Task.FromResult(new List<Feature>());

            public Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection) => Task.FromResult(ConnectionValidationResult.Success());

            public Task<ConnectionValidationResult> ValidateTeamSettings(Team team) => Task.FromResult(ConnectionValidationResult.Success());

            public Task<ConnectionValidationResult> ValidatePortfolioSettings(Portfolio portfolio) => Task.FromResult(ConnectionValidationResult.Success());
        }
    }
}
