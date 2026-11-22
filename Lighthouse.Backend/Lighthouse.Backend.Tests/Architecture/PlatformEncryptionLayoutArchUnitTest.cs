using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The key ring and secret custody are the first platform area: every type the platform membership map gives the
    /// key-ring move lives in Lighthouse.Backend.Platform.Encryption and none is left where it lived before, the unit tests
    /// of those types moved with them into Tests/Platform/Encryption, and the test-time budget and its check name the moved
    /// test classes in their new place. Every production type name is read from the membership map and looked up in the
    /// compiled assembly, so a name that matches no type fails and names itself.
    /// </summary>
    [TestFixture]
    public class PlatformEncryptionLayoutArchUnitTest
    {
        private const string TheMovedTestsNamespace = "Lighthouse.Backend.Tests.Platform.Encryption";

        private const string WhereTheTestsLivedBefore = @"^Lighthouse\.Backend\.Tests\.Services\.Implementation(\.BackgroundServices|\.Encryption)?$";

        private const string TheOldTestsNamespace = "Lighthouse.Backend.Tests.Services.Implementation.Encryption";

        private const string TheTimeBudgetFolder = "Scripts/test-timings/";

        private static readonly string[] TheMovedTestClasses =
        [
            "KeyRingFileWatcherTests",
            "CryptoServiceTests",
            "ConfiguredKeyRingSourceTests",
            "ConnectionSecretsTests",
            "CryptoServiceNamedKeyTests",
            "EncryptionKeyRingBootstrapperTests",
            "EncryptionKeyRingHolderTests",
            "GeneratedKeyRingStoreTests",
            "KeyRingSerializerTests",
            "KeyStoreFileTests",
            "KeyStoreResolverTests",
            "LegacyDefaultEncryptionKeyTests",
            "PublishedKeySecretCountTests",
            "ReadableSecretsNotOnTheActiveKeyTests",
            "ReferencedKeyIdsTests",
            "SecretCustodyServiceTests",
            "SecretEnvelopeTests",
            "SecretStateClassifierTests",
            "StoredSecretReadabilityProbeTests",
            "StoredSecretSummaryReaderTests",
            "WhereTheKeyCameFromTests",
        ];

        // These test shared Models types, which stay where they are, so their tests stay too.
        private static readonly string[] TheTestsWhoseSubjectStays = ["EncryptionKeyRingTests", "SecretReadabilityReportTests"];

        private static readonly string[] TheBudgetedEncryptionTestClasses =
            ["PublishedKeySecretCountTests", "ReadableSecretsNotOnTheActiveKeyTests", "ReferencedKeyIdsTests", "SecretCustodyServiceTests"];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PlatformEncryptionLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryKeyRingType_LivesInItsPlatformArea_AndNoneIsLeftInItsOldNamespace()
        {
            var keyRingTypes = GivenTheTypesTheMapGivesTheKeyRingMove();

            var verdicts = WhenEachAreaIsCheckedAgainstWhereItsTypesLivedBefore(keyRingTypes);

            ThenEveryVerdictPasses(verdicts);
        }

        [Test]
        public void TheUnitTestsOfTheMovedTypes_LiveInTestsPlatformEncryption_AndNoneIsLeftInItsOldNamespace()
        {
            var verdict = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, TheMovedTestsNamespace, TheMovedTestClasses, WhereTheTestsLivedBefore);

            verdict.Check();
        }

        [Test]
        public void TheTestsOfTheModelsTypesThatStay_StayInTheirOldNamespace()
        {
            var misplaced = TheTestsWhoseSubjectStays
                .Where(name => TestModule.Value.GetType($"{TheOldTestsNamespace}.{name}") is null || TestModule.Value.GetType($"{TheMovedTestsNamespace}.{name}") is not null)
                .ToList();

            Assert.That(
                misplaced,
                Is.Empty,
                $"[{string.Join(", ", misplaced)}] should stay exactly once in {TheOldTestsNamespace} and not appear in {TheMovedTestsNamespace}. " +
                "Why it matters: these classes test shared Models types that do not move, and a test lives next to its subject, so moving them " +
                "would put tests of Models code under the platform area. How to fix: leave these test files where they are.");
        }

        [Test]
        public void TheTimeBudgetAndItsCheck_NameTheBudgetedEncryptionTestClassesInTheirNewPlace()
        {
            var budgetRows = GivenTheBudgetStringsTheKeyRingMoveRenames();

            var stale = WhenEachBudgetFileIsReadForItsRenamedStrings(budgetRows);

            ThenEveryBudgetFileNamesOnlyTheNewPlace(budgetRows, stale);
        }

        private static List<PlatformLaneType> GivenTheTypesTheMapGivesTheKeyRingMove()
        {
            var keyRingTypes = PlatformLaneMembership.MovingTypes
                .Where(type => PlatformLaneMembership.Areas[type.Area] == PlatformMove.KeyRing)
                .ToList();

            Assert.That(
                keyRingTypes.Select(type => type.Name),
                Does.Contain("CryptoService").And.Contain("ICryptoService").And.Contain("KeyRingFileWatcher").And.Contain("SecretCustodyService"),
                "The membership map gave the key-ring move none of CryptoService, ICryptoService, KeyRingFileWatcher or SecretCustodyService. " +
                "Why it matters: the layout check would pass over a list that misses the types the move is about. How to fix: read the map's moving types whose area belongs to the key-ring move.");
            return keyRingTypes;
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

        private static List<StringTheMoveRenames> GivenTheBudgetStringsTheKeyRingMoveRenames()
        {
            var rows = PlatformLaneMembership.StringsTheMoveRenames
                .Where(row => row.RenamedBy == PlatformMove.KeyRing && row.File.StartsWith(TheTimeBudgetFolder, StringComparison.Ordinal))
                .ToList();

            var named = rows.Select(row => row.NewText.Split('.')[^1]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
            Assert.That(
                named,
                Is.EqualTo(TheBudgetedEncryptionTestClasses),
                "The membership map's renamed strings under Scripts/test-timings do not cover exactly the four budgeted Encryption test classes. " +
                "Why it matters: a budget key the check does not look at can stay on the old name and the budget check then finds no such test. " +
                "How to fix: the map lists each of the four classes once for test-time-budget.json and once for its Python test.");
            return rows;
        }

        private static List<string> WhenEachBudgetFileIsReadForItsRenamedStrings(IEnumerable<StringTheMoveRenames> rows)
        {
            var root = RepositoryRoot();

            return [.. from row in rows
                let text = File.ReadAllText(Path.Combine(root, row.File))
                where text.Contains(row.OldText, StringComparison.Ordinal) || !text.Contains(row.NewText, StringComparison.Ordinal)
                select $"{row.File}: expected \"{row.NewText}\" instead of \"{row.OldText}\""];
        }

        private static void ThenEveryBudgetFileNamesOnlyTheNewPlace(IReadOnlyCollection<StringTheMoveRenames> rows, IReadOnlyCollection<string> stale)
        {
            using var scope = Assert.EnterMultipleScope();
            Assert.That(
                rows.Select(row => row.File).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(2),
                "The renamed strings under Scripts/test-timings should name two files, the budget and its Python test, so both are read.");
            Assert.That(
                stale,
                Is.Empty,
                $"These budget strings still name an Encryption test class in its old place:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}{Environment.NewLine}" +
                "Why it matters: the budget check finds a test by its full name, so a key on the old namespace matches no test after the move and its " +
                "budget silently stops applying. How to fix: in the move commit, rewrite each key to the class's new full name, as each line above says.");
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not find the repository root (the folder holding Lighthouse.Backend/Lighthouse.sln) above the test directory, so the budget files cannot be read.");
            return directory!.FullName;
        }
    }
}
