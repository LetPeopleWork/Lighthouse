namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class PublishOutputOutsideProjectGuardTest
    {
        private const string PendingReason = "pending: the build files still publish into the backend project folder; the consumers move first, then this guard is switched on";

        // Publishing inside the project folder makes the SDK pick up the output's *.json as content, so the
        // output nests inside itself on the next publish.
        private const string InProjectPublishFolder = "Lighthouse.Backend/Lighthouse.Backend/publish";

        // Folders the scan skips: build output, version-control data, the artifacts folder and nested worktrees.
        private static readonly string[] PruneDirectories =
        [
            "node_modules", "bin", "obj", ".git", "artifacts", ".claude", "target",
            "StrykerOutput", ".stryker-tmp", "dist", "coverage", "playwright-report", "test-results",
        ];

        [Test]
        [Ignore(PendingReason)]
        public void NoBuildFileWritesOrReadsTheBackendPublishOutputInsideTheBackendProjectFolder()
        {
            var offenders = BuildFiles()
                .SelectMany(file => File.ReadLines(file.FullPath)
                    .Select((text, index) => (file.RelativePath, Line: index + 1, Text: text)))
                .Where(line => MentionsInProjectPublishFolder(line.Text))
                .Select(line => $"{line.RelativePath}:{line.Line}")
                .ToList();

            Assert.That(
                offenders,
                Is.Empty,
                "Publish the backend to artifacts/publish/<rid> at the repository root, not into the project folder, " +
                "where the next publish would nest inside this one. Offenders: " + string.Join(", ", offenders));
        }

        private static readonly string[] KnownConsumers =
        [
            ".github/actions/build-backend/action.yml",
            ".github/actions/package-app/action.yml",
            ".github/workflows/ci_package-macos-standalone.yml",
            "Lighthouse.Frontend/package.json",
            "Dockerfile",
        ];

        [Test]
        public void TheScanSeesTheFilesThatConsumeThePublishOutput()
        {
            var scanned = BuildFiles().Select(file => file.RelativePath).ToHashSet();

            Assert.That(
                scanned,
                Is.SupersetOf(KnownConsumers),
                "The scan must read the files that publish and package the backend; otherwise an empty offender list proves nothing.");
        }

        [TestCase("-o ./Lighthouse.Backend/Lighthouse.Backend/publish/win-x64 ./Lighthouse.Backend/Lighthouse.Backend", true)]
        [TestCase("ARM64_DIR=\"./Lighthouse.Backend/Lighthouse.Backend/publish/osx-arm64\"", true)]
        [TestCase("-o ../Lighthouse.Backend/Lighthouse.Backend/publish/linux-x64 ../Lighthouse.Backend/Lighthouse.Backend", true)]
        [TestCase(@"-o .\Lighthouse.Backend\Lighthouse.Backend\publish\win-x64", true)]
        [TestCase(@"-o lighthouse.backend\lighthouse.backend\publish\win-x64", true)]
        [TestCase(@"""..\\Lighthouse.Backend\\Lighthouse.Backend\\publish""", true)]
        [TestCase("-o ./artifacts/publish/linux-x64 ./Lighthouse.Backend/Lighthouse.Backend", false)]
        [TestCase("path: ./publish/", false)]
        [TestCase("tail -f publish/lighthouse.pid", false)]
        [TestCase("RUN dotnet publish -c Release -o /app/publish", false)]
        [TestCase("dotnet publish -o ./temp_publish ../Lighthouse.Backend/Lighthouse.Backend", false)]
        public void OnlyThePathInsideTheBackendProjectFolderIsRecognised(string line, bool expected)
        {
            Assert.That(MentionsInProjectPublishFolder(line), Is.EqualTo(expected));
        }

        private static bool MentionsInProjectPublishFolder(string line)
        {
            var normalised = string.Join('/', line.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries));
            return normalised.Contains(InProjectPublishFolder, StringComparison.OrdinalIgnoreCase);
        }

        private static List<(string FullPath, string RelativePath)> BuildFiles()
        {
            var root = RepositoryRoot();
            var found = new List<(string FullPath, string RelativePath)>();
            Collect(root, root, found);
            return found.OrderBy(file => file.RelativePath, StringComparer.Ordinal).ToList();
        }

        private static void Collect(string root, string directory, List<(string FullPath, string RelativePath)> found)
        {
            foreach (var file in Directory.EnumerateFiles(directory).Where(IsBuildFile))
            {
                found.Add((file, Path.GetRelativePath(root, file).Replace('\\', '/')));
            }

            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                var isLink = new DirectoryInfo(child).Attributes.HasFlag(FileAttributes.ReparsePoint);
                if (!isLink && !PruneDirectories.Contains(Path.GetFileName(child), StringComparer.Ordinal))
                {
                    Collect(root, child, found);
                }
            }
        }

        private static bool IsBuildFile(string path)
        {
            var name = Path.GetFileName(path);
            var extension = Path.GetExtension(path);
            var inGithubFolder = path.Replace('\\', '/').Contains("/.github/", StringComparison.Ordinal);

            return name.StartsWith("Dockerfile", StringComparison.Ordinal)
                || name == "package.json"
                || extension is ".sh" or ".ps1"
                || (inGithubFolder && extension is ".yml" or ".yaml");
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory?.Parent, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the build file scan.");
            return directory!.Parent!.FullName;
        }
    }
}
