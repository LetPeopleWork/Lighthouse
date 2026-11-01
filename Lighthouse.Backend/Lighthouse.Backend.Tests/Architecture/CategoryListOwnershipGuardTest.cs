using System.Text.RegularExpressions;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Who touches a To Do, Doing or Done list. Only the owner turns a mapping's name into its states when it reads
    /// the lists (plus the few readers of a setting that names a group: a wait state, a cycle time boundary, a
    /// refinement state). Only the settings sync, the start-up seeder and the seed-data factory write the lists, so
    /// every stored list has been through the owner's classifier.
    /// </summary>
    [TestFixture]
    public partial class CategoryListOwnershipGuardTest
    {
        private const string Pending = "pending: category lists hold raw states";

        private const string ProductionProject = "Lighthouse.Backend";

        private static readonly string[] FilesThatReadSettingsNamingAGroup =
        [
            "Models/WorkTrackingSystemOptionsOwner.cs",
            "Models/IWorkItemQueryOwner.cs",
            "Services/Implementation/Refinement/RefinementList.cs",
            "Services/Implementation/BaseMetricsService.cs",
            "Services/Implementation/TeamMetricsService.cs",
            "Services/Implementation/PortfolioMetricsService.cs",
        ];

        // Only the sync of a settings save and the start-up seeder put a list on an owner: both run the owner's
        // classifier first, and a third writer would skip it.
        private static readonly string[] FilesThatWriteTheCategoryLists =
        [
            "API/Helpers/TeamExtensions.cs",
            "API/Helpers/PortfolioExtensions.cs",
            "Services/Implementation/Seeding/CategoryListNormalisationSeeder.cs",

            // Builds the demo Teams and Portfolios with lists of plain states and no mappings, so there is nothing to normalise.
            "Factories/DemoDataFactory.cs",

            // Fills a board description handed back to the settings page (not an owner) with the states it found.
            "Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs",
        ];

        // Catches a call with the list inline. A call through a variable or a parameter (the ServiceNow mapper
        // passes one down) is caught by the second test, which allows only the files that read settings naming a group.
        [GeneratedRegex(@"GetRawStatesForCategory\(\s*[\w.]*\b(ToDoStates|DoingStates|DoneStates)\b", RegexOptions.CultureInvariant)]
        private static partial Regex ExpandsACategoryList();

        // Two shapes: any assignment to, or mutation of, one of the three lists through a member access, whatever the
        // receiver is called, and a list set inside the initialiser of a new Team or Portfolio.
        [GeneratedRegex(@"\.\s*(?:ToDoStates|DoingStates|DoneStates)\s*(?:=(?!=)|\.(?:Add|AddRange|Clear|Remove|RemoveAll|Insert)\()|\bnew\s+(?:Team|Portfolio)\b[^;]*?\b(?:ToDoStates|DoingStates|DoneStates)\s*=(?!=)", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
        private static partial Regex PutsAListOnAnOwner();

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate the Lighthouse.sln that anchors the source scan.");
            return directory!.FullName;
        }

        private static IEnumerable<string> ProductionSources()
        {
            var root = Path.Combine(FindRepositoryRoot(), ProductionProject.Replace('/', Path.DirectorySeparatorChar));
            return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
        }

        [Test]
        [Ignore(Pending)]
        public void NoCodeExpandsACategoryList()
        {
            var offenders = ProductionSources()
                .Where(file => ExpandsACategoryList().IsMatch(File.ReadAllText(file)))
                .Select(file => Path.GetRelativePath(FindRepositoryRoot(), file).Replace(Path.DirectorySeparatorChar, '/'))
                .ToList();

            Assert.That(offenders, Is.Empty, "These files still expand the To Do, Doing or Done list through a mapping.");
        }

        [Test]
        public void OnlyTheSettingsSyncAndTheSeederPutAListOnAnOwner()
        {
            var root = Path.Combine(FindRepositoryRoot(), ProductionProject.Replace('/', Path.DirectorySeparatorChar));
            var writers = ProductionSources()
                .Where(file => PutsAListOnAnOwner().IsMatch(File.ReadAllText(file)))
                .Select(file => Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/'))
                .ToList();

            Assert.That(writers.Except(FilesThatWriteTheCategoryLists), Is.Empty, "These files put a To Do, Doing or Done list on an owner without the classifier.");
        }

        [Test]
        [Ignore(Pending)]
        public void OnlyCodeThatReadsSettingsNamingAGroupCallsTheExpansion()
        {
            var root = Path.Combine(FindRepositoryRoot(), ProductionProject.Replace('/', Path.DirectorySeparatorChar));
            var callers = ProductionSources()
                .Where(file => File.ReadAllText(file).Contains("GetRawStatesForCategory(", StringComparison.Ordinal))
                .Select(file => Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/'))
                .ToList();

            Assert.That(callers.Except(FilesThatReadSettingsNamingAGroup), Is.Empty);
        }
    }
}
