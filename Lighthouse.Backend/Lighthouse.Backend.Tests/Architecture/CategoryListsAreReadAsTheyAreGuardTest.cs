using System.Text.RegularExpressions;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The To Do, Doing and Done lists hold the tracker's own states, so reading them never expands a mapping name.
    /// Only a name a person typed into a setting that refers to a group (a wait state, a cycle time boundary, a
    /// refinement state) is expanded, and only the code that reads those settings may do so.
    /// </summary>
    [TestFixture]
    public partial class CategoryListsAreReadAsTheyAreGuardTest
    {
        private const string Pending = "pending: category lists hold raw states";

        private const string ProductionProject = "Lighthouse.Backend";

        private static readonly string[] FilesThatReadSettingsNamingAGroup =
        [
            "Models/WorkTrackingSystemOptionsOwner.cs",
            "Models/IWorkItemQueryOwner.cs",
            "API/Helpers/CycleTimeDefinitionValidator.cs",
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
        ];

        // Catches a call with the list inline. A call through a variable or a parameter (the ServiceNow mapper
        // passes one down) is caught by the second test, which allows only the files that read settings naming a group.
        [GeneratedRegex(@"GetRawStatesForCategory\(\s*[\w.]*\b(ToDoStates|DoingStates|DoneStates)\b", RegexOptions.CultureInvariant)]
        private static partial Regex ExpandsACategoryList();

        [GeneratedRegex(@"\b(?:team|project|portfolio|owner|workItemQueryOwner|workTrackingSystemOptionsOwner)\.(?:ToDoStates|DoingStates|DoneStates)\s*(?:=(?!=)|\.(?:Add|AddRange|Clear|Remove|RemoveAll|Insert)\()", RegexOptions.CultureInvariant)]
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
