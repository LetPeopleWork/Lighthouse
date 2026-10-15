namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Jira Cloud and Jira Data Center answer different REST routes with different shapes, and a call
    /// written for Cloud alone silently returns nothing against a Data Center instance. Every route
    /// therefore lives in JiraRestRoutes, where each capability must give a Cloud and a Data Center
    /// answer before it compiles. A dependency rule cannot see a string literal, so this reads the
    /// source to keep a route from being written anywhere else.
    /// </summary>
    [TestFixture]
    public class JiraRouteLiteralsTest
    {
        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private const string RoutesRelativePath = "Services/Implementation/WorkTrackingConnectors/Jira/JiraRestRoutes.cs";

        private const string JiraRestRouteMarker = "rest/api/";

        // The production project has hundreds of source files. Far fewer means the scan is reading the
        // wrong directory and would pass on anything.
        private const int FewestSourceFilesAPlausibleScanReads = 100;

        private static readonly string[] DirectoriesThatAreNotSource = ["/obj/", "/bin/"];

        private static readonly string[] PlantedLiteralLocation = ["Services/Implementation/Somewhere/NewJiraCall.cs:5"];

        [Test]
        public void NoProductionSource_OtherThanTheRoutesObject_WritesAJiraRestRoute()
        {
            var files = ProductionSourceFiles();

            var offenders = RouteLiteralsIn(files.Where(file => file.RelativePath != RoutesRelativePath));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(files, Has.Count.GreaterThanOrEqualTo(FewestSourceFilesAPlausibleScanReads),
                    $"Scanned only {files.Count} production sources; the scan is anchored at the wrong directory.");

                Assert.That(RouteLiteralsIn(files.Where(file => file.RelativePath == RoutesRelativePath)), Is.Not.Empty,
                    $"{RoutesRelativePath} holds no \"{JiraRestRouteMarker}\" route, so either it moved or this scan reads nothing.");

                Assert.That(offenders, Is.Empty,
                    $"A Jira REST route is written outside JiraRestRoutes:{Environment.NewLine}"
                    + string.Join(Environment.NewLine, offenders)
                    + $"{Environment.NewLine}Add the route to JiraRestRoutes with both a Cloud and a Data Center answer, "
                    + "and call it from there. A Cloud-only route returns nothing against a Data Center Jira.");
            }
        }

        [Test]
        public void TheScan_FindsARouteLiteralPlantedInASource()
        {
            var planted = new SourceFile(
                "Services/Implementation/Somewhere/NewJiraCall.cs",
                "namespace Somewhere;\n\npublic class NewJiraCall\n{\n    private const string Url = \"rest/api/3/issue\";\n}\n");

            Assert.That(RouteLiteralsIn([planted]), Is.EqualTo(PlantedLiteralLocation));
        }

        private static List<string> RouteLiteralsIn(IEnumerable<SourceFile> files)
        {
            return [.. files.SelectMany(file => file.Source
                .Split('\n')
                .Select((text, index) => new { Number = index + 1, Text = text })
                .Where(line => line.Text.Contains(JiraRestRouteMarker, StringComparison.Ordinal))
                .Select(line => $"{file.RelativePath}:{line.Number}"))];
        }

        private static List<SourceFile> ProductionSourceFiles()
        {
            var productionRoot = ProductionRoot();

            return [.. Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
                .Select(file => new
                {
                    RelativePath = Path.GetRelativePath(productionRoot, file).Replace('\\', '/'),
                    FullPath = file,
                })
                .Where(file => !DirectoriesThatAreNotSource.Any(directory => ("/" + file.RelativePath).Contains(directory, StringComparison.Ordinal)))
                .Select(file => new SourceFile(file.RelativePath, File.ReadAllText(file.FullPath)))];
        }

        // Anchored on the solution file rather than on a relative hop out of the test binary: the depth from
        // the assembly to the repository differs between a local build and the runner.
        private static string ProductionRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the Jira route scan.");

            return Path.Combine(directory!.FullName, ProductionProjectDirectory);
        }

        private sealed record SourceFile(string RelativePath, string Source);
    }
}
