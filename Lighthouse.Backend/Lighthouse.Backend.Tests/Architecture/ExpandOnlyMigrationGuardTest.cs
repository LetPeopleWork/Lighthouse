namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ExpandOnlyMigrationGuardTest
    {
        // Three deliberate contract releases sit below this line.
        //
        // RemoveLegacyBlockedConfiguration dropped the legacy BlockedStates and BlockedTags columns, and
        // ran only after the BackfillBlockedRuleSetJson migrations had populated BlockedRuleSetJson for
        // every configured owner.
        //
        // RemovePortfolioTeamJoinTable drops the PortfolioTeam table, which had become impossible to
        // trust. A portfolio's list of teams is computed from the features those teams work on, yet the
        // table was still mapped as if it were stored, so rows appeared only as a side effect of saving a
        // portfolio that happened to have its feature graph loaded. Nothing chose to write them and
        // nothing could rely on them being there. Every reader now derives the link from the feature work
        // instead. Leaving the drop out of a migration was the more dangerous option, because the model
        // no longer describes the table, so the next migration generated for any unrelated reason would
        // have carried this drop along with it, unannounced.
        //
        // DropUnusedForecastHowMany drops a column created on DeliveryMetricSnapshot that nothing ever
        // wrote, in any release: a Delivery forecast answers when, not how many, so no value was ever
        // computed for it. Verified null in every row of a real database before dropping. There is no
        // expand phase because there is nothing to carry over.
        private const long ExpandOnlyBaselineTimestamp = 20260822121253;

        private static readonly string[] DropAndRenameTableOperations = { "DropTable", "RenameTable" };

        private static readonly string[] MigrationProjectRelativePaths =
        {
            Path.Combine("Lighthouse.Migrations.Postgres", "Migrations"),
            Path.Combine("Lighthouse.Migrations.Sqlite", "Migrations"),
        };

        [Test]
        public void MigrationGuard_DropColumnInRelease_FailsCheck()
        {
            var source = MigrationWith(
                up: "migrationBuilder.DropColumn(name: \"Obsolete\", table: \"Teams\");",
                down: "migrationBuilder.AddColumn<string>(name: \"Obsolete\", table: \"Teams\");");

            Assert.That(
                ExpandOnlyMigrationGuard.FindDestructiveOperationsInUp(source),
                Does.Contain("DropColumn"));
        }

        [Test]
        public void MigrationGuard_DropOrRenameTable_FailsCheck()
        {
            var source = MigrationWith(
                up: "migrationBuilder.DropTable(name: \"LegacyForecast\");\n" +
                    "migrationBuilder.RenameTable(name: \"Old\", newName: \"New\");",
                down: "// reverted elsewhere");

            Assert.That(
                ExpandOnlyMigrationGuard.FindDestructiveOperationsInUp(source),
                Is.EquivalentTo(DropAndRenameTableOperations));
        }

        [Test]
        public void MigrationGuard_AdditiveOnlyMigration_PassesCheck()
        {
            var source = MigrationWith(
                up: "migrationBuilder.AddColumn<string>(name: \"NewField\", table: \"Teams\");",
                down: "migrationBuilder.DropColumn(name: \"NewField\", table: \"Teams\");");

            Assert.That(
                ExpandOnlyMigrationGuard.FindDestructiveOperationsInUp(source),
                Is.Empty,
                "A migration whose Up only adds is additive-only; the DropColumn in its Down reverts the add and must be ignored.");
        }

        [Test]
        public void RealMigrations_AddedAfterBaseline_AreAdditiveOnly()
        {
            var violations = MigrationFilesNewerThanBaseline()
                .Select(file => new
                {
                    File = Path.GetFileName(file),
                    Destructive = ExpandOnlyMigrationGuard.FindDestructiveOperationsInUp(File.ReadAllText(file)),
                })
                .Where(result => result.Destructive.Count > 0)
                .Select(result => $"{result.File}: {string.Join(", ", result.Destructive)}")
                .ToList();

            Assert.That(
                violations,
                Is.Empty,
                "Expand-only discipline: migrations added after the baseline must not drop/rename columns or " +
                "tables in their Up method. Destructive (contract) migrations belong in a separate, conscious later release " +
                "(expand now, contract later) so old pods never depend on a dropped column during a rolling update. " +
                "If a contract migration is genuinely intended, bump ExpandOnlyBaselineTimestamp past it and document the " +
                "contract release. Offending migrations: " + string.Join("; ", violations));
        }

        private static List<string> MigrationFilesNewerThanBaseline()
        {
            var repoRoot = FindRepositoryRoot();

            return MigrationProjectRelativePaths
                .Select(relative => Path.Combine(repoRoot, relative))
                .Where(Directory.Exists)
                .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs"))
                .Where(file => !file.EndsWith(".Designer.cs", StringComparison.Ordinal))
                .Where(file => ExpandOnlyMigrationGuard.TimestampOf(file) is long timestamp
                    && timestamp > ExpandOnlyBaselineTimestamp)
                .ToList();
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate the Lighthouse.sln to anchor the migration scan.");
            return directory!.FullName;
        }

        private static string MigrationWith(string up, string down) =>
            "namespace Lighthouse.Migrations.Test\n" +
            "{\n" +
            "    public partial class Sample : Migration\n" +
            "    {\n" +
            "        protected override void Up(MigrationBuilder migrationBuilder)\n" +
            "        {\n" +
            $"            {up}\n" +
            "        }\n" +
            "        protected override void Down(MigrationBuilder migrationBuilder)\n" +
            "        {\n" +
            $"            {down}\n" +
            "        }\n" +
            "    }\n" +
            "}\n";
    }
}
