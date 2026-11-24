using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Database management, demo data, the seeder port and the race-tolerant save are the second platform move: every type the
    /// platform membership map gives that move lives in Lighthouse.Backend.Platform.&lt;its area&gt; and none is left where it
    /// lived before, the unit tests of those types moved with them into Tests/Platform/&lt;area&gt;, the seven context seeders
    /// stayed where they are, and the suppression and the strings that name a moved type name its new place. Every production
    /// type name is read from the membership map and looked up in the compiled assembly, so a name that matches no type fails
    /// and names itself.
    /// </summary>
    [TestFixture]
    public class PlatformDatabaseAndSeedingLayoutArchUnitTest
    {
        private const string TheTestsNamespace = "Lighthouse.Backend.Tests.Platform";

        private const string WhereTheTestsLivedBefore = @"^Lighthouse\.Backend\.Tests\.Services\.Implementation$";

        private static readonly (string Area, string[] Classes)[] TheMovedTestClasses =
        [
            ("DatabaseManagement",
            [
                "DatabaseMaintenanceGateTest",
                "DatabaseManagementServiceTest",
                "DatabaseOperationTrackerTest",
                "PostgresDatabaseManagementProviderTest",
                "SqliteDatabaseManagementProviderTest",
            ]),
            ("DemoData", ["DemoDataServiceTest"]),
            ("Persistence", ["LostRaceTolerantSaveTest"]),
        ];

        private static readonly string[] TheAreasOfTheMove = ["DatabaseManagement", "DemoData", "Persistence", "Seeding"];

        private static readonly string[] TheContextSeeders =
        [
            "ApiKeyOwnerReconciliationSeeder", "AppSettingSeeder", "CategoryListNormalisationSeeder", "EveryoneSignedInSeeder",
            "OptionalFeatureSeeder", "RefreshLogSeeder", "TerminologySeeder",
        ];

        private static readonly string[] TheSuppressedTypes = ["PostgresDatabaseManagementProvider"];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PlatformDatabaseAndSeedingLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryDatabaseAndSeedingType_LivesInItsPlatformArea_AndNoneIsLeftInItsOldNamespace()
        {
            var movingTypes = GivenTheTypesTheMapGivesTheDatabaseAndSeedingMove();

            var verdicts = WhenEachAreaIsCheckedAgainstWhereItsTypesLivedBefore(movingTypes);

            ThenEveryVerdictPasses(verdicts);
        }

        [Test]
        public void TheUnitTestsOfTheMovedTypes_LiveInTestsPlatformTheirArea_AndNoneIsLeftInItsOldNamespace()
        {
            var verdicts = TheMovedTestClasses
                .Select(area => ModuleRules.TypesLiveInTheirLayer(TestModule.Value, $"{TheTestsNamespace}.{area.Area}", area.Classes, WhereTheTestsLivedBefore))
                .ToList();

            ThenEveryVerdictPasses(verdicts);
        }

        [Test]
        public void TheSevenContextSeeders_StayInServicesImplementationSeeding_ReachingTheSeederPortInItsNewPlace()
        {
            var theSeeder = PlatformLaneMembership.MovingTypes.Single(type => type.Name == "InstallTimestampSeeder");
            var seeders = GivenTheContextSeedersTheMapKeeps(theSeeder.OldNamespace);

            var misplaced = WhenEachSeederIsLookedUpInItsOldAndItsNewPlace(seeders, theSeeder.NewNamespace);

            Assert.That(
                misplaced,
                Is.Empty,
                $"These context seeders are not exactly once in {theSeeder.OldNamespace}, or appear in {theSeeder.NewNamespace}: [{string.Join(", ", misplaced)}]. " +
                "Why it matters: each seeder belongs to the context whose data it seeds, and only the seeder port and the install-timestamp seeder are platform. " +
                "How to fix: leave the context seeders where they are; they reach ISeeder through a using for its new namespace.");
        }

        [Test]
        public void TheSuppressionOnThePostgresProvider_NamesItsNewFullName()
        {
            var rows = GivenTheSuppressionTargetsTheMoveRewrites();

            var stale = WhenEachSuppressionFileIsReadForItsRewrittenTarget(rows);

            Assert.That(
                stale,
                Is.Empty,
                $"These suppression targets still name a moved type in its old place, or miss its new place:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}{Environment.NewLine}" +
                "Why it matters: a suppression is matched by the type's full name, so one left on the old name suppresses nothing after the move and the build then fails on the very finding it excused. " +
                "How to fix: in the move commit, change the target in GlobalSuppressions.cs to the type's new full name, as each line above says.");
        }

        [Test]
        public void TheRuleAndListStringsThatNameAMovedType_NameItsNewPlaceOnly()
        {
            var rows = GivenTheStringsTheDatabaseAndSeedingMoveRenames();

            var stale = WhenEachFileIsReadForItsRenamedStrings(rows);

            Assert.That(
                stale,
                Is.Empty,
                $"These strings still name a moved type in its old place, or miss its new place:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}{Environment.NewLine}" +
                "Why it matters: an architecture rule or source check that names a type by its old full name or path silently stops looking at it after the move, so the rule is loosened without anyone deciding to. " +
                "How to fix: in the move commit, rewrite each string to the text the line above expects; do not delete or widen it.");
        }

        private static List<PlatformLaneType> GivenTheTypesTheMapGivesTheDatabaseAndSeedingMove()
        {
            var movingTypes = PlatformLaneMembership.MovingTypes
                .Where(type => PlatformLaneMembership.Areas[type.Area] == PlatformMove.DatabaseAndSeeding)
                .ToList();

            using var scope = Assert.EnterMultipleScope();
            Assert.That(
                movingTypes.Select(type => type.Area).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
                Is.EqualTo(TheAreasOfTheMove),
                "The membership map gave the database-and-seeding move areas other than DatabaseManagement, DemoData, Persistence and Seeding. " +
                "Why it matters: the layout check would pass over an area the move is not about, or skip one it is about. How to fix: read the map's moving types whose area belongs to this move.");
            Assert.That(
                movingTypes.Select(type => type.Name),
                Does.Contain("DatabaseManagementService").And.Contain("IDatabaseManagementService").And.Contain("DemoDataService")
                    .And.Contain("ISeeder").And.Contain("InstallTimestampSeeder").And.Contain("LostRaceTolerantSave"),
                "The membership map gave the database-and-seeding move none of DatabaseManagementService, IDatabaseManagementService, DemoDataService, ISeeder, InstallTimestampSeeder or LostRaceTolerantSave. " +
                "Why it matters: the layout check would pass over a list that misses the types the move is about. How to fix: read the map's moving types whose area belongs to this move.");
            return movingTypes;
        }

        private static List<KernelRuleVerdict> WhenEachAreaIsCheckedAgainstWhereItsTypesLivedBefore(IEnumerable<PlatformLaneType> types)
            => [.. types
                .GroupBy(type => type.NewNamespace, StringComparer.Ordinal)
                .Select(area => ModuleRules.TypesLiveInTheirLayer(
                    ProductionModule.Value,
                    area.Key,
                    [.. area.Select(type => type.Name)],
                    $"^({string.Join("|", area.Select(type => Regex.Escape(type.OldNamespace)).Distinct(StringComparer.Ordinal))})$"))];

        private static void ThenEveryVerdictPasses(IEnumerable<KernelRuleVerdict> verdicts)
        {
            foreach (var verdict in verdicts)
            {
                verdict.Check();
            }
        }

        private static List<TypeThatStays> GivenTheContextSeedersTheMapKeeps(string seedersNamespace)
        {
            var seeders = PlatformLaneMembership.TypesThatStay
                .Where(type => type.Namespace == seedersNamespace)
                .ToList();

            Assert.That(
                seeders.Select(type => type.Name).Order(StringComparer.Ordinal),
                Is.EqualTo(TheContextSeeders),
                "The membership map does not keep exactly the seven context seeders in Services.Implementation.Seeding. " +
                "Why it matters: the check would look at a different set than the seeders that belong to their contexts. How to fix: list the seven context seeders as types that stay.");
            return seeders;
        }

        private static List<string> WhenEachSeederIsLookedUpInItsOldAndItsNewPlace(IEnumerable<TypeThatStays> seeders, string platformSeedingNamespace)
            => [.. seeders
                .Where(seeder => ProductionModule.Value.GetType(seeder.FullName) is null
                    || ProductionModule.Value.GetType($"{platformSeedingNamespace}.{seeder.Name}") is not null)
                .Select(seeder => seeder.Name)];

        private static List<SuppressionTargetToRewrite> GivenTheSuppressionTargetsTheMoveRewrites()
        {
            var rows = PlatformLaneMembership.SuppressionTargets
                .Where(row => row.RewrittenBy == PlatformMove.DatabaseAndSeeding)
                .ToList();

            Assert.That(
                rows.Select(row => row.Target),
                Is.EqualTo(TheSuppressedTypes),
                "The membership map does not give the database-and-seeding move exactly the PostgresDatabaseManagementProvider suppression. " +
                "Why it matters: the check would skip a suppression the move must rewrite. How to fix: list the suppression target the move rewrites.");
            return rows;
        }

        private static List<string> WhenEachSuppressionFileIsReadForItsRewrittenTarget(IEnumerable<SuppressionTargetToRewrite> rows)
        {
            var root = RepositoryRoot();

            return [.. from row in rows
                let type = PlatformLaneMembership.MovingTypes.Single(moving => moving.Name == row.Target)
                let text = File.ReadAllText(Path.Combine(root, row.File))
                where text.Contains($"~T:{type.OldFullName}", StringComparison.Ordinal) || !text.Contains($"~T:{type.NewFullName}", StringComparison.Ordinal)
                select $"{row.File}: expected \"~T:{type.NewFullName}\" instead of \"~T:{type.OldFullName}\""];
        }

        private static List<StringTheMoveRenames> GivenTheStringsTheDatabaseAndSeedingMoveRenames()
        {
            var rows = PlatformLaneMembership.StringsTheMoveRenames
                .Where(row => row.RenamedBy == PlatformMove.DatabaseAndSeeding)
                .ToList();

            Assert.That(
                rows,
                Is.Not.Empty,
                "The membership map lists no renamed string for the database-and-seeding move. " +
                "Why it matters: the check would pass over rule and list strings that name a moved type. How to fix: list each string the move rewrites, with the text it reads after the move.");
            return rows;
        }

        private static List<string> WhenEachFileIsReadForItsRenamedStrings(IEnumerable<StringTheMoveRenames> rows)
        {
            var root = RepositoryRoot();

            return [.. from row in rows
                let text = File.ReadAllText(Path.Combine(root, row.File))
                where text.Contains(row.OldText, StringComparison.Ordinal) || !text.Contains(row.NewText, StringComparison.Ordinal)
                select $"{row.File}: expected \"{row.NewText}\" instead of \"{row.OldText}\""];
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not find the repository root (the folder holding Lighthouse.Backend/Lighthouse.sln) above the test directory, so the files cannot be read.");
            return directory!.FullName;
        }
    }
}
