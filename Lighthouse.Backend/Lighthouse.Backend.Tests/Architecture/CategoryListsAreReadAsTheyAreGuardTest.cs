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

        [GeneratedRegex(@"GetRawStatesForCategory\(\s*[\w.]*\b(ToDoStates|DoingStates|DoneStates)\b", RegexOptions.CultureInvariant)]
        private static partial Regex ExpandsACategoryList();

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
