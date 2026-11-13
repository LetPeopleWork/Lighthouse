using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// The one way a Refinement scenario at the port sets up a Team: it stores the Team, and then how the Team
    /// refines, from the named inputs below. In the product these are chosen through the settings form, whose
    /// save tidies what it is sent; this builder writes the Team directly, so it could store something no save
    /// ever produces and a scenario would pass on a Team no user can have. Two host rows guard that: one sends every input in
    /// <see cref="EveryInput"/> through the real save and compares what is stored, and one compares the settings
    /// the port stands in for with a seeded database. Both run on a host, so the fast loop does not see them.
    /// After changing an input or adding one, run them by name and check the count, one case per input and one
    /// per port fixture, because a filter that matches nothing still passes:
    /// <code>
    /// dotnet test --filter "FullyQualifiedName~A_Team_set_up_through_its_settings_holds_what_a_port_scenario_sets_up|FullyQualifiedName~Every_setting_a_port_scenario_is_given_is_what_a_seeded_database_holds"
    /// </code>
    /// </summary>
    public sealed class RefinementPortGivens(IServiceProvider services)
    {
        public const string Backlog = "Backlog";

        public const string Next = "Next";

        public const string Analysing = "Analysing";

        public const string Implementation = "Implementation";

        public const string Done = "Done";

        public const int ThroughputHistoryDays = 30;

        /// <summary>The tag that makes a Work Item Ready under <see cref="ReadyWhenTaggedReady"/>.</summary>
        public const string ReadyTag = "ready";

        public const string BillingExport = "GR-040";

        public const string AdvancedReporting = "GR-051";

        public const string ApiVersioning = "GR-054";

        public const string UserActivityTracking = "GR-058";

        public const string AdvancedSearch = "GR-059";

        public const string ConfigurationManagement = "GR-073";

        public const string LoadTesting = "GR-074";

        private const string TeamNamePrefix = "Team ";

        private const string TagsField = "workitem.tags";

        private const string ContainsOperator = "contains";

        /// <summary>Wednesday 7 October 2026, nine in the morning: the moment the tracker's dates are counted back from.</summary>
        private static readonly DateTime TrackerReadOn = new(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc);

        /// <summary>The Gravity demo Team's tracker states: To Do "Backlog"; Doing "Next", "Analysing" and "Implementation"; Done "Done".</summary>
        public static TeamStates GravitysStates { get; } = new([Backlog], [Next, Analysing, Implementation], [Done]);

        /// <summary>The Gravity demo Team's tracker, in backlog order.</summary>
        private static readonly TrackerWorkItem[] GravitysTracker =
        [
            new(BillingExport, "Billing export", Implementation, StateCategories.Doing, "1", StartedDaysAgo: 9),
            new(UserActivityTracking, "User activity tracking", Next, StateCategories.Doing, "2", StartedDaysAgo: 1),
            new(AdvancedSearch, "Advanced search filters", Next, StateCategories.Doing, "3", StartedDaysAgo: 1),
            new(AdvancedReporting, "Advanced reporting module", Analysing, StateCategories.Doing, "4", StartedDaysAgo: 3),
            new(ApiVersioning, "Public API versioning", Analysing, StateCategories.Doing, "5", StartedDaysAgo: 2),
            new(ConfigurationManagement, "Configuration management", Backlog, StateCategories.ToDo, "6"),
            new(LoadTesting, "Load testing framework", Backlog, StateCategories.ToDo, "7"),
        ];

        /// <summary>The Team refines in Backlog, Analysing and Next.</summary>
        public static RefinementInput RefinesInBacklogAnalysingAndNext { get; } = new(
            "States chosen: Backlog, Analysing, Next",
            settings => settings.States = [.. new[] { "Backlog", "Analysing", "Next" }.Select(state => new RefinementStateSetting { State = state })]);

        /// <summary>The Team refines in Backlog.</summary>
        public static RefinementInput RefinesInBacklog { get; } = new(
            "States chosen: Backlog",
            settings => settings.States = [new RefinementStateSetting { State = Backlog }]);

        /// <summary>A Team that has chosen no state to refine in.</summary>
        public static RefinementInput RefinesInNoState { get; } = new(
            "No states chosen",
            settings => settings.States = []);

        /// <summary>A Work Item whose tags contain "ready" is Ready; no rule says what is being refined.</summary>
        public static RefinementInput ReadyWhenTaggedReady { get; } = new(
            "Ready rule: tags contain ready, no Being-refined rule",
            settings => settings.StageRules = new StageRules
            {
                Ready = new WorkItemRuleSet
                {
                    Mode = WorkItemRuleSet.ModeAnd,
                    Conditions = [new WorkItemRuleCondition { FieldKey = TagsField, Operator = ContainsOperator, Value = ReadyTag }],
                },
                BeingRefined = null,
            });

        /// <summary>The Team refines every Thursday.</summary>
        public static RefinementInput RefinesOnThursdays { get; } = new(
            "Cadence: Thursday, every week, no starting week",
            settings => settings.Cadence = RefinementCadence.Of([DayOfWeek.Thursday], 1, null));

        /// <summary>The Team refines every Tuesday.</summary>
        public static RefinementInput RefinesOnTuesdays { get; } = new(
            "Cadence: Tuesday, every week, no starting week",
            settings => settings.Cadence = RefinementCadence.Of([DayOfWeek.Tuesday], 1, null));

        /// <summary>The Team refines every Monday and every Thursday.</summary>
        public static RefinementInput RefinesOnMondaysAndThursdays { get; } = new(
            "Cadence: Monday and Thursday, every week, no starting week",
            settings => settings.Cadence = RefinementCadence.Of([DayOfWeek.Monday, DayOfWeek.Thursday], 1, null));

        /// <summary>Every input a port scenario may give a Team. A new input is added here, or the host rows never see it.</summary>
        public static IReadOnlyList<RefinementInput> EveryInput { get; } =
        [
            RefinesInBacklogAnalysingAndNext,
            RefinesInBacklog,
            RefinesInNoState,
            ReadyWhenTaggedReady,
            RefinesOnThursdays,
            RefinesOnTuesdays,
            RefinesOnMondaysAndThursdays,
        ];

        /// <summary>A Team on a Jira connection with these states, which has not yet said how it refines.</summary>
        public int ATeam(string name, TeamStates states, int throughputHistoryDays, int sleProbability = 0, int sleRange = 0)
        {
            using var scope = services.CreateScope();
            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DataRetrievalValue = "project = GRAVITY",
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = [.. states.ToDo],
                DoingStates = [.. states.Doing],
                DoneStates = [.. states.Done],
                DoneItemsCutoffDays = 365,
                ThroughputHistory = throughputHistoryDays,
                ServiceLevelExpectationProbability = sleProbability,
                ServiceLevelExpectationRange = sleRange,
            };

            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();
            return team.Id;
        }

        /// <summary>The Team refines as these inputs say, each over what the Team already stores.</summary>
        public void TheTeamRefines(int teamId, params RefinementInput[] inputs)
        {
            using var scope = services.CreateScope();
            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            var team = teamRepository.GetById(teamId) ?? throw new InvalidOperationException($"no Team {teamId} is stored to give a refinement setting");

            var settings = team.RefinementSettings ?? new RefinementSettings();
            foreach (var input in inputs)
            {
                input.ApplyTo(settings);
            }

            team.RefinementSettings = settings;
            teamRepository.Update(team);
            teamRepository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Team Gravity refines in Backlog, Analysing and Next, and its tracker holds, in backlog order: GR-040
        /// being implemented (not in refinement), GR-058 and GR-059 in Next, GR-051 and GR-054 in Analysing,
        /// GR-073 and GR-074 in Backlog - six Work Items in refinement. The given Work Items carry the given
        /// tags. No stage, cadence or band has been chosen.
        /// </summary>
        public int GravityRefinesSixWorkItems(params (string WorkItem, string Tag)[] tags)
            => GravityRefines("Team Gravity", sleProbability: 85, sleRange: 7, tags, GravitysTracker, TrackerReadOn);

        /// <summary>
        /// Team Gravity as in <see cref="GravityRefinesSixWorkItems"/>, with the given Work Items carrying the given
        /// tags, and a Work Item tagged <see cref="ReadyTag"/> is Ready under <see cref="ReadyWhenTaggedReady"/>.
        /// </summary>
        public int GravityWithItsReadyRule(params (string WorkItem, string Tag)[] tags)
        {
            var gravity = GravityRefinesSixWorkItems(tags);
            TheTeamRefines(gravity, ReadyWhenTaggedReady);
            return gravity;
        }

        /// <summary>
        /// A Team with Gravity's states and Throughput history and this SLE refines in Backlog, Analysing and Next,
        /// and its tracker holds Gravity's Work Items without GR-059: GR-040 being implemented (not in refinement),
        /// GR-058 in Next, GR-051 and GR-054 in Analysing, GR-073 and GR-074 in Backlog - five Work Items in refinement.
        /// </summary>
        public int GravityRefinesFiveWorkItems(string name = "Team Gravity", int sleProbability = 85, int sleRange = 7)
            => GravityRefines(name, sleProbability, sleRange, [], GravitysTracker.Where(tracked => tracked.ReferenceId != AdvancedSearch), TrackerReadOn);

        /// <summary>
        /// Team Gravity as in <see cref="GravityRefinesFiveWorkItems"/>, and its tracker holds one more Work Item
        /// in Backlog, ranked 7, under this reference, such as one that holds a slash. Days are counted back from
        /// the instance's today, so the Given also serves a host whose clock is the instance's.
        /// </summary>
        public int GravityRefinesFiveWorkItemsAndOneReferencedAs(string reference)
        {
            using var scope = services.CreateScope();
            var today = scope.ServiceProvider.GetRequiredService<ILighthouseClock>().TodayAsUtcMidnight.AddHours(9);
            var referenced = new TrackerWorkItem(reference, "Work Item whose reference holds a slash", Backlog, StateCategories.ToDo, "7");
            return GravityRefines("Team Gravity", sleProbability: 85, sleRange: 7, [], GravitysTracker.Where(tracked => tracked.ReferenceId != AdvancedSearch).Append(referenced), today);
        }

        /// <summary>
        /// The Team finished these Work Items, each at noon the given number of days before the instance's today,
        /// having taken its cycle time: a cycle time of N days started N - 1 days before the day it finished.
        /// </summary>
        public void TheTeamFinished(int teamId, params FinishedWorkItem[] finished)
        {
            using var scope = services.CreateScope();
            var today = scope.ServiceProvider.GetRequiredService<ILighthouseClock>().TodayAsUtcMidnight;
            TheTrackerReportsFinished(scope, teamId, today, finished);
        }

        /// <summary>
        /// The Team finished this many Work Items on each of the last <paramref name="days"/> days before the tracker
        /// was read, that day included, each taking one day. A Team that finishes the same number every day makes the
        /// shipped forecast engine answer one number, whatever it draws.
        /// </summary>
        public void TheTeamFinishedWorkEveryDay(int teamId, int perDay, int days = ThroughputHistoryDays)
        {
            using var scope = services.CreateScope();
            TheTrackerReportsFinished(scope, teamId, TrackerReadOn.Date,
                Enumerable.Range(0, days).SelectMany(daysAgo => Enumerable.Range(0, perDay)
                    .Select(index => new FinishedWorkItem($"DONE-{daysAgo}-{index}", CycleTimeDays: 1, FinishedDaysAgo: daysAgo))));
        }

        /// <summary>
        /// A Team with the states Backlog, Implementation and Done and a Throughput history of thirty days, whose
        /// tracker holds this many Work Items in Backlog, named after the Team: for "Team Nebula", NB-001
        /// "Nebula Work Item 1" ranked first, and so on. It has not yet said how it refines.
        /// </summary>
        public int ATeamWhoseBacklogHolds(string name, int workItems)
        {
            var team = ATeam(name, new TeamStates([Backlog], [Implementation], [Done]), ThroughputHistoryDays);
            var shortName = name.StartsWith(TeamNamePrefix, StringComparison.Ordinal) ? name[TeamNamePrefix.Length..] : name;
            var keyPrefix = TrackerKeyPrefixOf(shortName);
            TheTrackerHolds(team, [], Enumerable.Range(1, workItems).Select(number => new TrackerWorkItem(
                $"{keyPrefix}-{number:D3}", $"{shortName} Work Item {number}", Backlog, StateCategories.ToDo, $"{number}")), TrackerReadOn);
            return team;
        }

        /// <summary>The tracker's key for a project is the first two consonants of its name, as GR is for Gravity and NB for Nebula.</summary>
        private static string TrackerKeyPrefixOf(string shortName)
            => string.Concat(shortName.Where(letter => char.IsLetter(letter) && !"aeiouAEIOU".Contains(letter)).Take(2)).ToUpperInvariant();

        private int GravityRefines(string name, int sleProbability, int sleRange, (string WorkItem, string Tag)[] tags, IEnumerable<TrackerWorkItem> workItems, DateTime readOn)
        {
            var gravity = ATeam(name, GravitysStates, ThroughputHistoryDays, sleProbability, sleRange);
            TheTrackerHolds(gravity, tags, workItems, readOn);
            TheTeamRefines(gravity, RefinesInBacklogAnalysingAndNext);
            return gravity;
        }

        private static void TheTrackerReportsFinished(IServiceScope scope, int teamId, DateTime countedBackFrom, IEnumerable<FinishedWorkItem> finished)
        {
            var owner = TheStoredTeam(scope, teamId);
            var workItemRepository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            foreach (var item in finished)
            {
                var closed = countedBackFrom.AddDays(-item.FinishedDaysAgo).AddHours(12);

                workItemRepository.Add(new WorkItem
                {
                    Team = owner,
                    TeamId = owner.Id,
                    ReferenceId = item.ReferenceId,
                    Name = $"Finished {item.ReferenceId}",
                    Type = "User Story",
                    State = Done,
                    StateCategory = StateCategories.Done,
                    CreatedDate = closed.AddDays(-item.CycleTimeDays - 10),
                    StartedDate = closed.AddDays(-(item.CycleTimeDays - 1)),
                    ClosedDate = closed,
                    Order = "99",
                    Url = TrackerAddressOf(item.ReferenceId),
                });
            }

            workItemRepository.Save().GetAwaiter().GetResult();
        }

        private void TheTrackerHolds(int teamId, (string WorkItem, string Tag)[] tags, IEnumerable<TrackerWorkItem> workItems, DateTime readOn)
        {
            using var scope = services.CreateScope();
            var owner = TheStoredTeam(scope, teamId);
            var workItemRepository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            foreach (var tracked in workItems)
            {
                workItemRepository.Add(new WorkItem
                {
                    Team = owner,
                    TeamId = owner.Id,
                    ReferenceId = tracked.ReferenceId,
                    Name = tracked.Name,
                    Type = "User Story",
                    State = tracked.State,
                    StateCategory = tracked.Category,
                    CreatedDate = readOn.AddDays(-60),
                    StartedDate = tracked.Category == StateCategories.Doing ? readOn.Date.AddDays(-tracked.StartedDaysAgo).AddHours(12) : null,
                    Order = tracked.Rank,
                    Url = TrackerAddressOf(tracked.ReferenceId),
                    ParentReferenceId = string.Empty,
                    Tags = [.. tags.Where(tag => tag.WorkItem == tracked.ReferenceId).Select(tag => tag.Tag)],
                });
            }

            workItemRepository.Save().GetAwaiter().GetResult();
        }

        private static Team TheStoredTeam(IServiceScope scope, int teamId)
            => scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)
                ?? throw new InvalidOperationException($"no Team {teamId} is stored to give Work Items");

        private static string TrackerAddressOf(string referenceId) => $"https://tracker.example/browse/{referenceId}";

        private sealed record TrackerWorkItem(string ReferenceId, string Name, string State, StateCategories Category, string Rank, int StartedDaysAgo = 0);
    }

    /// <summary>A Work Item the Team finished: its cycle time in days, and how many days before today it finished.</summary>
    public sealed record FinishedWorkItem(string ReferenceId, int CycleTimeDays, int FinishedDaysAgo);

    /// <summary>A Team's states by category, in the tracker's order.</summary>
    public sealed record TeamStates(IReadOnlyList<string> ToDo, IReadOnlyList<string> Doing, IReadOnlyList<string> Done);

    /// <summary>One thing a port scenario can say about how a Team refines, named as a reader of a test case sees it.</summary>
    public sealed class RefinementInput(string name, Action<RefinementSettings> applyTo)
    {
        public void ApplyTo(RefinementSettings settings) => applyTo(settings);

        public override string ToString() => name;
    }
}
