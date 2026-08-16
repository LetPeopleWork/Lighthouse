using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Moving stored secrets onto a new key is the only thing in the product that rewrites a credential
    /// column outside the ordinary save pipeline. What it may write, and that nothing else grows a second
    /// way to write it, are read off the source: a dependency rule cannot see which column a statement
    /// targets, and a behavioural test cannot see a second write path that nothing calls yet.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class SecretRotationWriteSurfaceTest
    {
        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private const string TheOnePassThatWrites = "Services/Implementation/Encryption/SecretCustodyService.cs";

        private const string GuardedWrite = "ExecuteUpdateAsync";

        // The three columns this feature may write, and the whole of what a rotation is allowed to touch.
        private static readonly string[] TheOnlyColumnsARotationMayWrite =
        [
            "option => option.Value",
            "credential => credential.AccessToken",
            "credential => credential.RefreshToken",
        ];

        private static readonly string[] ColumnsThatHoldACredential = ["AccessToken", "RefreshToken", "option.Value"];

        // Lighthouse holds no permission on any Kubernetes Secret and must not be able to acquire one. The
        // rule is that nothing can compile against a client at all, so there is nothing left to probe.
        private static readonly string[] WaysToTalkToKubernetes = ["k8s.", "KubernetesClient"];

        // Whether the application may make a key is read off where the key in force came from. A setting
        // saying so would be a second answer to the same question, and the failure mode of getting it wrong
        // is a key minted into a store that does not survive a restart.
        private static readonly string[] SettingsThatWouldContradictCustody =
        [
            "canMint",
            "canRotate",
            "mayRotate",
            "allowRotation",
        ];

        private static readonly string[] DirectoriesThatAreNotSource = ["/obj/", "/bin/"];

        [Test]
        public void TheRotation_WritesTheThreeColumnsItIsAllowedToAndNoOthers()
        {
            var written = CodeLines(ProductionSourceOf(TheOnePassThatWrites))
                .Where(line => line.Contains("SetProperty(", StringComparison.Ordinal))
                .Select(line => line.Trim())
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(written, Has.Count.EqualTo(TheOnlyColumnsARotationMayWrite.Length),
                    "A column was added to or removed from what a rotation writes. Found: " + string.Join(" | ", written));

                foreach (var column in TheOnlyColumnsARotationMayWrite)
                {
                    Assert.That(written, Has.One.Contains(column),
                        $"A rotation no longer writes {column}, or writes it twice. Found: " + string.Join(" | ", written));
                }
            }
        }

        /// <summary>
        /// Writing a column directly is an ordinary enough thing to do in this codebase, and several places
        /// do it. Doing it to a column that holds a credential is not: it goes around the pipeline that
        /// encrypts on save, so a value written that way is stored exactly as it was handed over. The one
        /// place that is allowed to is the pass that builds the envelope itself.
        /// </summary>
        [Test]
        public void NothingElseInTheProduct_WritesAColumnThatHoldsACredential()
        {
            var offenders = ProductionSourceFiles()
                .Where(file => !string.Equals(file.RelativePath, TheOnePassThatWrites, StringComparison.Ordinal))
                .SelectMany(file => CodeLines(file.Source)
                    .Where(line => line.Contains("SetProperty(", StringComparison.Ordinal))
                    .Where(line => ColumnsThatHoldACredential.Any(column => line.Contains(column, StringComparison.Ordinal)))
                    .Select(line => $"{file.RelativePath}: {line.Trim()}"))
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();

            Assert.That(offenders, Is.Empty,
                $"A second place now writes a stored credential with {GuardedWrite}, which goes around the save " +
                "pipeline that encrypts secrets. Whatever is written that way is stored exactly as it was handed " +
                "over. Found: " + string.Join(", ", offenders));
        }

        [Test]
        public void NothingInTheProduct_CanTalkToKubernetesAtAll()
        {
            var offenders = LighthouseArchitecture.Production.Types
                .SelectMany(type => type.Dependencies.Select(dependency => new
                {
                    Type = type.FullName,
                    Target = dependency.Target.FullName,
                }))
                .Where(dependency => WaysToTalkToKubernetes.Any(
                    client => dependency.Target.StartsWith(client, StringComparison.Ordinal)))
                .Select(dependency => $"{dependency.Type} -> {dependency.Target}")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();

            Assert.That(offenders, Is.Empty,
                "Lighthouse writing to its own Kubernetes Secret would need a permission nobody should grant, " +
                "and an external secret store would overwrite whatever it wrote on the next sync. The operator " +
                "puts the new key there; Lighthouse only moves the stored secrets onto it. Found: " +
                string.Join(", ", offenders));
        }

        [Test]
        public void NoSetting_DecidesWhetherThisInstanceMayMakeAKey()
        {
            var offenders = ProductionSourceFiles()
                .SelectMany(file => CodeLines(file.Source)
                    .Where(line => SettingsThatWouldContradictCustody.Any(
                        setting => line.Contains($"\"{setting}", StringComparison.OrdinalIgnoreCase)))
                    .Select(line => $"{file.RelativePath}: {line.Trim()}"))
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();

            Assert.That(offenders, Is.Empty,
                "Whether this instance may make a key is read off where the key in force came from, with " +
                "certainty. A setting saying so asks a person to keep a fact in sync that the application " +
                "already knows, and getting it wrong mints a key into a store that does not survive a " +
                "restart. Found: " + string.Join(", ", offenders));
        }

        private static IEnumerable<string> CodeLines(string source)
        {
            return source.Split('\n').Select(WithoutComment);
        }

        private static string WithoutComment(string line)
        {
            var start = line.IndexOf("//", StringComparison.Ordinal);

            return start < 0 ? line : line[..start];
        }

        private static List<SourceFile> ProductionSourceFiles()
        {
            var productionRoot = ProductionRoot();

            var files = Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
                .Select(file => new
                {
                    RelativePath = Path.GetRelativePath(productionRoot, file).Replace('\\', '/'),
                    FullPath = file,
                })
                .Where(file => !DirectoriesThatAreNotSource.Any(directory => ("/" + file.RelativePath).Contains(directory, StringComparison.Ordinal)))
                .Select(file => new SourceFile(file.RelativePath, File.ReadAllText(file.FullPath)))
                .ToList();

            Assert.That(files, Is.Not.Empty, "Found no production sources to scan; the scan is anchored at the wrong directory.");

            return files;
        }

        private static string ProductionSourceOf(string relativePath)
        {
            var file = Path.Combine(ProductionRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.That(File.Exists(file), Is.True,
                $"{relativePath} was moved or deleted, so the rule it carries is no longer being enforced.");

            return File.ReadAllText(file);
        }

        private static string ProductionRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the rotation write surface scan.");

            return Path.Combine(directory!.FullName, ProductionProjectDirectory);
        }

        private sealed record SourceFile(string RelativePath, string Source);
    }
}
