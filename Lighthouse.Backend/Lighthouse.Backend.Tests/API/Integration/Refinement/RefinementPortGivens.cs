using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Models.WorkItemRules;
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
        private const string TagsField = "workitem.tags";

        private const string ContainsOperator = "contains";

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
    }

    /// <summary>A Team's states by category, in the tracker's order.</summary>
    public sealed record TeamStates(IReadOnlyList<string> ToDo, IReadOnlyList<string> Doing, IReadOnlyList<string> Done);

    /// <summary>One thing a port scenario can say about how a Team refines, named as a reader of a test case sees it.</summary>
    public sealed class RefinementInput(string name, Action<RefinementSettings> applyTo)
    {
        public void ApplyTo(RefinementSettings settings) => applyTo(settings);

        public override string ToString() => name;
    }
}
