namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class PostgresContainerSharingGuardTest
    {
        private const string SingleTestReason = "its only Postgres test in the class, so there is nothing to share a container with";
        private const string ServerLevelReason = "exercises server-level behaviour (locks, concurrent writers, replicas, substrate health) and needs a server of its own per test";

        // Starting a container takes seconds. A test class that needs Postgres starts one SharedPostgresContainer
        // and hands each test a fresh database inside it. Starting a container per test is allowed only here.
        private static readonly Dictionary<string, string> AllowedFileReasons = new Dictionary<string, string>
        {
            ["SharedPostgresContainer.cs"] = "the wrapper that starts the one container a test class shares",
            ["BlockedCountSnapshotMigrationTests.cs"] = SingleTestReason,
            ["BlockedDurationMigrationTests.cs"] = SingleTestReason,
            ["BlockedStalenessThresholdMigrationTests.cs"] = SingleTestReason,
            ["FeatureDependencyReferenceMigrationTests.cs"] = SingleTestReason,
            ["PredefinedAdditionalFieldMigrationTests.cs"] = SingleTestReason,
            ["LongSecretRoundTripTests.cs"] = SingleTestReason,
            ["ReEncryptionCompareAndSwapProbeTests.cs"] = SingleTestReason,
            ["ClusterSubstrateHealthCheckTests.cs"] = ServerLevelReason,
            ["EmbedSessionSingleUseConcurrencyTests.cs"] = ServerLevelReason,
            ["PostgresUpdateExecutionLockTests.cs"] = ServerLevelReason,
            ["ScalabilityTests.cs"] = ServerLevelReason,
            ["SubstrateProbeTests.cs"] = ServerLevelReason,
        };

        // Split so that this file does not match its own scan.
        private const string StartFresh = "PostgresContainerFixture" + ".StartFreshAsync(";

        [Test]
        public void NoTestFileStartsItsOwnPostgresContainerWithoutBeingOnTheAllowlist()
        {
            var offenders = FilesStartingAContainer()
                .Where(name => !AllowedFileReasons.ContainsKey(name))
                .OrderBy(name => name)
                .ToList();

            Assert.That(
                offenders,
                Is.Empty,
                "Starting a Postgres container per test costs seconds each. Start one SharedPostgresContainer in the " +
                "fixture's OneTimeSetUp and take a fresh database per test from it. If a test genuinely needs a server " +
                "of its own, add its file to the allowlist with the reason. Offenders: " + string.Join(", ", offenders));
        }

        [Test]
        public void EveryAllowlistedFileStillStartsItsOwnPostgresContainer()
        {
            var starting = FilesStartingAContainer().ToHashSet();

            var stale = AllowedFileReasons.Keys
                .Where(name => !starting.Contains(name))
                .OrderBy(name => name)
                .ToList();

            Assert.That(
                stale,
                Is.Empty,
                "These files no longer start a Postgres container themselves. Remove them from the allowlist. " +
                "Stale entries: " + string.Join(", ", stale));
        }

        private static IEnumerable<string> FilesStartingAContainer()
        {
            var testProjectDirectory = Path.Combine(FindSolutionDirectory(), "Lighthouse.Backend.Tests");
            var separator = Path.DirectorySeparatorChar;

            return Directory.EnumerateFiles(testProjectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains($"{separator}obj{separator}", StringComparison.Ordinal)
                    && !file.Contains($"{separator}bin{separator}", StringComparison.Ordinal))
                .Where(file => File.ReadAllText(file).Contains(StartFresh, StringComparison.Ordinal))
                .Select(file => Path.GetFileName(file));
        }

        private static string FindSolutionDirectory()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the source scan.");
            return directory!.FullName;
        }
    }
}
