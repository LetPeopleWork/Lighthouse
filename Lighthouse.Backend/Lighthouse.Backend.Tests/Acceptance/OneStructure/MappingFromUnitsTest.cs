using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;

namespace Lighthouse.Backend.Tests.Acceptance.OneStructure
{
    /// <summary>
    /// How the upgrade works out each connection's item types per level from the Teams and Portfolios on it, asked of
    /// the planner the start-up step uses, with no database: every type a level's units read, once whatever the case,
    /// in the spelling most of them use, in the order they first appear; today's create defaults for a level no unit
    /// of the connection sits on.
    /// </summary>
    [TestFixture]
    public class MappingFromUnitsTest
    {
        private const int GeneratedInstallations = 200;

        private static readonly string[] TypePool = ["User Story", "Bug", "Epic", "Feature", "Task", "Change"];

        private static readonly WorkTrackingSystems[] Trackers = [WorkTrackingSystems.Jira, WorkTrackingSystems.AzureDevOps, WorkTrackingSystems.Csv, WorkTrackingSystems.Linear];

        private static readonly string[] JustBug = ["Bug"];

        private static readonly string[] JustLowerCaseBug = ["bug"];

        private static readonly string[] BugUserStoryTask = ["Bug", "User Story", "Task"];

        private static readonly string[] JustEpic = ["Epic"];

        private static readonly TerminologyWords SeededWords = new(
            new TermPair("Team", "Teams"), new TermPair("Work Item", "Work Items"), new TermPair("Portfolio", "Portfolios"), new TermPair("Feature", "Features"));

        /// <summary>
        /// Each level of a connection lists the union of its own units' types on that connection, once each
        /// ignoring case; a type both levels read sits on both, so no unit loses a type at the upgrade.
        /// </summary>
        [Test]
        public void Each_level_of_a_connection_lists_every_type_its_units_read_once_whatever_the_case()
        {
            var seed = TestContext.CurrentContext.Random.Next();
            var random = new Random(seed);
            var installations = new[] { TheInstallationEveryRunIncludes() }
                .Concat(Enumerable.Range(0, GeneratedInstallations).Select(_ => AnyInstallation(random)));

            foreach (var installation in installations)
            {
                var wrong = WhatThePlanGetsWrong(installation);
                if (wrong.Count > 0)
                {
                    var smallest = Smallest(installation);
                    Assert.Fail(
                        $"With seed {seed} the upgrade maps the types of an installation wrongly:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", WhatThePlanGetsWrong(smallest))}{Environment.NewLine}Smallest installation that shows it: {smallest}{Environment.NewLine}Each level of a connection must list every type its units on that level read, once whatever the case; a type a Team and a Portfolio of one connection both read sits on both levels, and neither level takes it from the other. Run again with this seed to see the same installation.");
                }
            }
        }

        [Test]
        public void Two_Teams_spelling_Bug_and_one_spelling_bug_give_the_level_the_spelling_most_of_them_use()
        {
            var jira = AConnection(1, WorkTrackingSystems.Jira);

            var mapped = TheTypesPlannedOn(jira, LevelId.Team, teams: [ATeam(1, jira, "bug"), ATeam(2, jira, "Bug"), ATeam(3, jira, "Bug")], portfolios: []);

            Assert.That(mapped, Is.EqualTo(JustBug),
                $"The Team level of a connection whose Teams 1, 2 and 3 spell bug, Bug and Bug lists [{string.Join(", ", mapped)}]. A type is listed once, in the spelling most units on that level use.");
        }

        [Test]
        public void A_tie_in_spelling_goes_to_the_Team_with_the_lowest_number()
        {
            var jira = AConnection(1, WorkTrackingSystems.Jira);

            var mapped = TheTypesPlannedOn(jira, LevelId.Team, teams: [ATeam(2, jira, "Bug"), ATeam(1, jira, "bug")], portfolios: []);

            Assert.That(mapped, Is.EqualTo(JustLowerCaseBug),
                $"Team 1 spells bug and Team 2 Bug, and the Team level lists [{string.Join(", ", mapped)}]. When as many units use one spelling as the other, the unit with the lowest number decides.");
        }

        [Test]
        public void Types_are_listed_in_the_order_they_first_appear_by_unit_number()
        {
            var azureDevOps = AConnection(1, WorkTrackingSystems.AzureDevOps);

            var mapped = TheTypesPlannedOn(azureDevOps, LevelId.Team,
                teams: [ATeam(7, azureDevOps, "User Story", "Task"), ATeam(3, azureDevOps, "Bug", "User Story")], portfolios: []);

            Assert.That(mapped, Is.EqualTo(BugUserStoryTask),
                $"Team 3 reads Bug, User Story and Team 7 User Story, Task, and the Team level lists [{string.Join(", ", mapped)}]. Types are listed in the order they first appear, going through the units by number.");
        }

        [Test]
        public void A_Jira_connection_with_Teams_and_no_Portfolio_gets_Epic_on_the_Portfolio_level()
        {
            var jira = AConnection(1, WorkTrackingSystems.Jira);

            var mapped = TheTypesPlannedOn(jira, LevelId.Portfolio, teams: [ATeam(1, jira, "User Story")], portfolios: []);

            Assert.That(mapped, Is.EqualTo(JustEpic),
                $"A Jira connection with a Team and no Portfolio lists [{string.Join(", ", mapped)}] on the Portfolio level. A level no unit of the connection sits on gets today's create default, Epic for a Portfolio.");
        }

        [Test]
        public void A_Linear_connection_with_a_Portfolio_and_no_Team_lists_nothing_on_the_Team_level()
        {
            var linear = AConnection(1, WorkTrackingSystems.Linear);

            var mapped = TheTypesPlannedOn(linear, LevelId.Team, teams: [], portfolios: [APortfolio(1, linear)]);

            Assert.That(mapped, Is.Empty,
                $"A Linear connection with a Portfolio and no Team lists [{string.Join(", ", mapped)}] on the Team level. Linear has no item types, so a level there gets none, not today's User Story and Bug.");
        }

        private static IReadOnlyList<string> TheTypesPlannedOn(WorkTrackingSystemConnection connection, LevelId level, Team[] teams, Portfolio[] portfolios)
        {
            var plan = OrganisationStructureMigrationPlan.From(teams, portfolios, [connection], SeededWords);
            return plan.Mappings.SingleOrDefault(mapping => mapping.ConnectionId == connection.Id && mapping.Level == level)?.Types ?? [];
        }

        private static List<string> WhatThePlanGetsWrong(PlannedInstallation installation)
        {
            var plan = OrganisationStructureMigrationPlan.From(installation.Teams, installation.Portfolios, installation.Connections, SeededWords);
            var wrong = new List<string>();

            foreach (var connection in installation.Connections.Where(installation.HasUnitsOn))
            {
                foreach (var level in new[] { LevelId.Team, LevelId.Portfolio })
                {
                    var expected = installation.TypesExpectedOn(connection, level);
                    var planned = plan.Mappings.SingleOrDefault(mapping => mapping.ConnectionId == connection.Id && mapping.Level == level)?.Types ?? [];
                    var sameTypesOnceEach = planned.Count == expected.Count && expected.All(type => planned.Contains(type, StringComparer.OrdinalIgnoreCase));
                    if (!sameTypesOnceEach)
                    {
                        wrong.Add($"connection {connection.Id} ({connection.WorkTrackingSystem}), {level.Key} level: planned [{string.Join(", ", planned)}], expected [{string.Join(", ", expected)}]");
                    }
                }
            }

            return wrong;
        }

        // There is no shrinker to lean on, so a failing installation is cut down by halving its units for as long as
        // the smaller one still fails.
        private static PlannedInstallation Smallest(PlannedInstallation failing)
        {
            var current = failing;
            var smaller = SmallerStillFailing(current);
            while (smaller is not null)
            {
                current = smaller;
                smaller = SmallerStillFailing(current);
            }

            return current;
        }

        private static PlannedInstallation? SmallerStillFailing(PlannedInstallation installation)
            => installation.Halves().FirstOrDefault(half => WhatThePlanGetsWrong(half).Count > 0);

        private static PlannedInstallation TheInstallationEveryRunIncludes()
        {
            var jira = AConnection(1, WorkTrackingSystems.Jira);
            var azureDevOps = AConnection(2, WorkTrackingSystems.AzureDevOps);
            var linear = AConnection(3, WorkTrackingSystems.Linear);

            return new PlannedInstallation(
                [jira, azureDevOps, linear],
                [ATeam(1, jira, "User Story", "Epic"), ATeam(2, azureDevOps, "bug"), ATeam(3, azureDevOps, "Bug", "User Story"), ATeam(4, linear)],
                [APortfolio(1, jira, "Epic", "Change")]);
        }

        private static PlannedInstallation AnyInstallation(Random random)
        {
            var connections = Enumerable.Range(1, random.Next(1, 4))
                .Select(id => AConnection(id, Trackers[random.Next(Trackers.Length)]))
                .ToList();

            var teams = Enumerable.Range(1, random.Next(0, 7))
                .Select(id => { var on = connections[random.Next(connections.Count)]; return ATeam(id, on, AnyTypes(random, on)); })
                .ToList();
            var portfolios = Enumerable.Range(1, random.Next(0, 5))
                .Select(id => { var on = connections[random.Next(connections.Count)]; return APortfolio(id, on, AnyTypes(random, on)); })
                .ToList();

            return new PlannedInstallation(connections, teams, portfolios);
        }

        private static string[] AnyTypes(Random random, WorkTrackingSystemConnection connection)
            => connection.WorkTrackingSystem == WorkTrackingSystems.Linear
                ? []
                : [.. Enumerable.Range(0, random.Next(0, 6)).Select(_ => InAnyCase(random, TypePool[random.Next(TypePool.Length)]))];

        private static string InAnyCase(Random random, string type) => random.Next(3) switch
        {
            0 => type,
            1 => type.ToLowerInvariant(),
            _ => type.ToUpperInvariant(),
        };

        private static WorkTrackingSystemConnection AConnection(int id, WorkTrackingSystems tracker)
            => new() { Id = id, Name = $"{tracker} {id}", WorkTrackingSystem = tracker };

        private static Team ATeam(int id, WorkTrackingSystemConnection connection, params string[] types)
            => new() { Id = id, Name = $"Team {id}", WorkTrackingSystemConnectionId = connection.Id, WorkItemTypes = [.. types] };

        private static Portfolio APortfolio(int id, WorkTrackingSystemConnection connection, params string[] types)
            => new() { Id = id, Name = $"Portfolio {id}", WorkTrackingSystemConnectionId = connection.Id, WorkItemTypes = [.. types] };

        /// <summary>The input of one generated case, with the reference answer worked out here, apart from the planner.</summary>
        private sealed record PlannedInstallation(IReadOnlyList<WorkTrackingSystemConnection> Connections, IReadOnlyList<Team> Teams, IReadOnlyList<Portfolio> Portfolios)
        {
            public bool HasUnitsOn(WorkTrackingSystemConnection connection)
                => Teams.Any(team => team.WorkTrackingSystemConnectionId == connection.Id)
                   || Portfolios.Any(portfolio => portfolio.WorkTrackingSystemConnectionId == connection.Id);

            public IReadOnlyList<string> TypesExpectedOn(WorkTrackingSystemConnection connection, LevelId level)
            {
                IEnumerable<WorkTrackingSystemOptionsOwner> units = level == LevelId.Team ? Teams : Portfolios;
                var onTheLevel = units.Where(unit => unit.WorkTrackingSystemConnectionId == connection.Id).ToList();

                if (onTheLevel.Count > 0)
                {
                    return [.. onTheLevel.SelectMany(unit => unit.WorkItemTypes).Distinct(StringComparer.OrdinalIgnoreCase)];
                }

                if (connection.WorkTrackingSystem == WorkTrackingSystems.Linear)
                {
                    return [];
                }

                return level == LevelId.Team ? ["User Story", "Bug"] : ["Epic"];
            }

            public IEnumerable<PlannedInstallation> Halves()
            {
                if (Teams.Count > 1)
                {
                    yield return this with { Teams = [.. Teams.Take(Teams.Count / 2)] };
                    yield return this with { Teams = [.. Teams.Skip(Teams.Count / 2)] };
                }

                if (Portfolios.Count > 1)
                {
                    yield return this with { Portfolios = [.. Portfolios.Take(Portfolios.Count / 2)] };
                    yield return this with { Portfolios = [.. Portfolios.Skip(Portfolios.Count / 2)] };
                }
            }

            public override string ToString()
                => $"connections [{string.Join(", ", Connections.Select(connection => $"{connection.Id} {connection.WorkTrackingSystem}"))}]; "
                   + $"Teams [{string.Join(", ", Teams.Select(team => $"{team.Id} on {team.WorkTrackingSystemConnectionId}: {string.Join("/", team.WorkItemTypes)}"))}]; "
                   + $"Portfolios [{string.Join(", ", Portfolios.Select(portfolio => $"{portfolio.Id} on {portfolio.WorkTrackingSystemConnectionId}: {string.Join("/", portfolio.WorkItemTypes)}"))}]";
        }
    }
}
