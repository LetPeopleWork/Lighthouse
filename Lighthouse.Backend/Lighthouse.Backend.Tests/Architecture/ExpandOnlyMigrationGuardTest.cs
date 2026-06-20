namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ExpandOnlyMigrationGuardTest
    {
        private const long ExpandOnlyBaselineTimestamp = 20260609073926;

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
                "Expand-only discipline (DISCUSS D4): migrations added after the baseline must not drop/rename columns or " +
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
