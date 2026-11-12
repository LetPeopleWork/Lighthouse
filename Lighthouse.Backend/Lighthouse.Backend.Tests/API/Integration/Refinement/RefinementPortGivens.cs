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
    /// refines, from the named inputs below. A scenario on a web host chooses these through the settings form,
    /// whose save tidies what it is sent; a port scenario writes the Team directly, so it could store something
    /// no save ever produces and pass on it. Two host rows guard that: one sends every input in
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

        public const string BillingExport = "GR-040";

        public const string AdvancedReporting = "GR-051";

        public const string ApiVersioning = "GR-054";

        public const string UserActivityTracking = "GR-058";

        public const string AdvancedSearch = "GR-059";

        public const string ConfigurationManagement = "GR-073";

        public const string LoadTesting = "GR-074";

        private const string TagsField = "workitem.tags";

        private const string ContainsOperator = "contains";

        /// <summary>Wednesday 7 October 2026, nine in the morning: the moment the tracker's dates are counted back from.</summary>
        private static readonly DateTime TrackerReadOn = new(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc);

        /// <summary>The Gravity demo Team's tracker states: To Do "Backlog"; Doing "Next", "Analysing" and "Implementation"; Done "Done".</summary>
        public static TeamStates GravitysStates { get; } = new([Backlog], [Next, Analysing, Implementation], [Done]);

        /// <summary>The Team refines in Backlog, Analysing and Next.</summary>
        public static RefinementInput RefinesInBacklogAnalysingAndNext { get; } = new(
            "States chosen: Backlog, Analysing, Next",
            settings => settings.States = [.. new[] { "Backlog", "Analysing", "Next" }.Select(state => new RefinementStateSetting { State = state })]);

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
                    Conditions = [new WorkItemRuleCondition { FieldKey = TagsField, Operator = ContainsOperator, Value = "ready" }],
                },
                BeingRefined = null,
            });

        /// <summary>The Team refines every Thursday.</summary>
        public static RefinementInput RefinesOnThursdays { get; } = new(
            "Cadence: Thursday, every week, no starting week",
            settings => settings.Cadence = RefinementCadence.Of([DayOfWeek.Thursday], 1, null));

        /// <summary>The Team refines every Monday and every Thursday.</summary>
        public static RefinementInput RefinesOnMondaysAndThursdays { get; } = new(
            "Cadence: Monday and Thursday, every week, no starting week",
            settings => settings.Cadence = RefinementCadence.Of([DayOfWeek.Monday, DayOfWeek.Thursday], 1, null));

        /// <summary>Every input a port scenario may give a Team. A new input is added here, or the host rows never see it.</summary>
        public static IReadOnlyList<RefinementInput> EveryInput { get; } =
        [
            RefinesInBacklogAnalysingAndNext,
            RefinesInNoState,
            ReadyWhenTaggedReady,
            RefinesOnThursdays,
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
        {
            var gravity = ATeam("Team Gravity", GravitysStates, ThroughputHistoryDays, sleProbability: 85, sleRange: 7);

            TheTrackerHolds(gravity, tags,
            [
                new TrackerWorkItem(BillingExport, "Billing export", Implementation, StateCategories.Doing, "1", StartedDaysAgo: 9),
                new TrackerWorkItem(UserActivityTracking, "User activity tracking", Next, StateCategories.Doing, "2", StartedDaysAgo: 1),
                new TrackerWorkItem(AdvancedSearch, "Advanced search filters", Next, StateCategories.Doing, "3", StartedDaysAgo: 1),
                new TrackerWorkItem(AdvancedReporting, "Advanced reporting module", Analysing, StateCategories.Doing, "4", StartedDaysAgo: 3),
                new TrackerWorkItem(ApiVersioning, "Public API versioning", Analysing, StateCategories.Doing, "5", StartedDaysAgo: 2),
                new TrackerWorkItem(ConfigurationManagement, "Configuration management", Backlog, StateCategories.ToDo, "6"),
                new TrackerWorkItem(LoadTesting, "Load testing framework", Backlog, StateCategories.ToDo, "7"),
            ]);

            TheTeamRefines(gravity, RefinesInBacklogAnalysingAndNext);
            return gravity;
        }

        /// <summary>
        /// A Team with Gravity's states and Throughput history and this SLE refines in Backlog, Analysing and Next,
        /// and its tracker holds, in backlog order: GR-040 being implemented (not in refinement), GR-058 in Next,
        /// GR-051 and GR-054 in Analysing, GR-073 and GR-074 in Backlog - five Work Items in refinement.
        /// </summary>
        public int GravityRefinesFiveWorkItems(string name = "Team Gravity", int sleProbability = 85, int sleRange = 7)
        {
            var gravity = ATeam(name, GravitysStates, ThroughputHistoryDays, sleProbability, sleRange);

            TheTrackerHolds(gravity, [],
            [
                new TrackerWorkItem(BillingExport, "Billing export", Implementation, StateCategories.Doing, "1", StartedDaysAgo: 9),
                new TrackerWorkItem(UserActivityTracking, "User activity tracking", Next, StateCategories.Doing, "2", StartedDaysAgo: 1),
                new TrackerWorkItem(AdvancedReporting, "Advanced reporting module", Analysing, StateCategories.Doing, "3", StartedDaysAgo: 3),
                new TrackerWorkItem(ApiVersioning, "Public API versioning", Analysing, StateCategories.Doing, "4", StartedDaysAgo: 2),
                new TrackerWorkItem(ConfigurationManagement, "Configuration management", Backlog, StateCategories.ToDo, "5"),
                new TrackerWorkItem(LoadTesting, "Load testing framework", Backlog, StateCategories.ToDo, "6"),
            ]);

            TheTeamRefines(gravity, RefinesInBacklogAnalysingAndNext);
            return gravity;
        }

        /// <summary>
        /// The Team finished these Work Items, each at noon the given number of days before the instance's today,
        /// having taken its cycle time: a cycle time of N days started N - 1 days before the day it finished.
        /// </summary>
        public void TheTeamFinished(int teamId, params FinishedWorkItem[] finished)
        {
            using var scope = services.CreateScope();
            var owner = TheStoredTeam(scope, teamId);
            var today = scope.ServiceProvider.GetRequiredService<ILighthouseClock>().TodayAsUtcMidnight;
            var workItemRepository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            foreach (var item in finished)
            {
                var closed = today.AddDays(-item.FinishedDaysAgo).AddHours(12);

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

        /// <summary>
        /// The Team finished this many Work Items on each of the last <paramref name="days"/> days before the tracker
        /// was read, that day included, each taking one day. A Team that finishes the same number every day makes the
        /// shipped forecast engine answer one number, whatever it draws.
        /// </summary>
        public void TheTeamFinishedWorkEveryDay(int teamId, int perDay, int days = ThroughputHistoryDays)
        {
            using var scope = services.CreateScope();
            var owner = TheStoredTeam(scope, teamId);
            var workItemRepository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            foreach (var daysAgo in Enumerable.Range(0, days))
            {
                foreach (var index in Enumerable.Range(0, perDay))
                {
                    var referenceId = $"DONE-{daysAgo}-{index}";
                    var closed = TrackerReadOn.Date.AddDays(-daysAgo).AddHours(12);

                    workItemRepository.Add(new WorkItem
                    {
                        Team = owner,
                        TeamId = owner.Id,
                        ReferenceId = referenceId,
                        Name = $"Finished {referenceId}",
                        Type = "User Story",
                        State = Done,
                        StateCategory = StateCategories.Done,
                        CreatedDate = closed.AddDays(-11),
                        StartedDate = closed,
                        ClosedDate = closed,
                        Order = "99",
                        Url = TrackerAddressOf(referenceId),
                    });
                }
            }

            workItemRepository.Save().GetAwaiter().GetResult();
        }

        private void TheTrackerHolds(int teamId, (string WorkItem, string Tag)[] tags, List<TrackerWorkItem> workItems)
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
                    CreatedDate = TrackerReadOn.AddDays(-60),
                    StartedDate = tracked.Category == StateCategories.Doing ? TrackerReadOn.Date.AddDays(-tracked.StartedDaysAgo).AddHours(12) : null,
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
