namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A wrong encryption key looked like an expired credential for years because the failure was caught
    /// somewhere and turned into an ordinary-looking answer. The classifier now decides what a stored value
    /// is by looking at it rather than by running something and seeing whether it blew up, and these tests
    /// keep it that way by reading the source itself - a dependency rule cannot see a catch block, and a
    /// behavioural test cannot see a catch that has not swallowed anything yet.
    /// </summary>
    [TestFixture]
    public class SecretReadPathSourceStructureTest
    {
        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private const string EncryptionImplementationDirectory = "Services/Implementation/Encryption/";

        private const string ClassifierRelativePath = EncryptionImplementationDirectory + "SecretStateClassifier.cs";

        private const string EnvelopeRelativePath = EncryptionImplementationDirectory + "SecretEnvelope.cs";

        private const string TheOnlyFailureTheEnvelopeMayCatch = "AuthenticationTagMismatchException";

        // The backup archive is unlocked with a password the operator types, not with a key from the ring,
        // so nothing on the secret read path passes through here and this catch cannot absorb an unreadable
        // secret. It is named rather than pattern-matched so that a second one has to be argued for.
        private const string BackupArchiveReader = "Services/Implementation/DatabaseManagement/DatabaseManagementService.cs";

        private static readonly string[] DirectoriesThatAreNotSource = ["/obj/", "/bin/"];

        [Test]
        public void TheClassifier_DecidesWhatAStoredValueIsWithoutCatchingAnything()
        {
            var source = ProductionSourceOf(ClassifierRelativePath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(LinesWithACatchClause(source), Is.Empty,
                    "The classifier answers every question by inspecting the stored value. A catch here means " +
                    "some question is now answered by running something and seeing whether it threw, which is " +
                    "exactly how an unreadable secret came to be reported as a working one.");

                Assert.That(LinesWithAnExceptionFilter(source), Is.Empty,
                    "An exception filter is a catch that reads as a condition. It hides the same thing a catch " +
                    "hides, and it hides it better.");
            }
        }

        /// <summary>
        /// The envelope is the one place a catch is unavoidable: the platform offers no authenticated decrypt
        /// that reports a failed tag by returning rather than throwing. That single catch is allowed and its
        /// exact type is asserted, because widening it by one base class - to CryptographicException, say -
        /// would silently swallow a key of the wrong size or a fault in the platform itself.
        /// </summary>
        [Test]
        public void TheEnvelope_CatchesTheFailedAuthenticationTagAndNothingElse()
        {
            var source = ProductionSourceOf(EnvelopeRelativePath);
            var catches = LinesWithACatchClause(source);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(catches, Has.Count.EqualTo(1),
                    $"The envelope may catch exactly one thing, a failed authentication tag. Catch clauses found: {Describe(catches)}");

                Assert.That(catches.All(entry => entry.Text.Contains(TheOnlyFailureTheEnvelopeMayCatch, StringComparison.Ordinal)), Is.True,
                    $"The one permitted catch must name {TheOnlyFailureTheEnvelopeMayCatch} and no broader type. Catch clauses found: {Describe(catches)}");

                Assert.That(LinesWithAnExceptionFilter(source), Is.Empty,
                    "An exception filter would let the permitted catch quietly grow a second job.");
            }
        }

        /// <summary>
        /// An unreadable secret is reported as a CryptographicException, so any catch of that type upstream
        /// would swallow it and put back the behaviour this work removed - and it would look like a fixed bug
        /// regressing on its own. Nothing outside the encryption code catches it today; this keeps it so.
        /// Expressed by reading the source because a dependency rule cannot tell a catch from a throw, and
        /// every caller that reports an unreadable secret legitimately mentions the type by name.
        /// </summary>
        [Test]
        public void NothingOutsideTheEncryptionCode_CatchesACryptographicFailure()
        {
            var offenders = ProductionSourceFiles()
                .Where(file => !file.RelativePath.StartsWith(EncryptionImplementationDirectory, StringComparison.Ordinal))
                .Where(file => !string.Equals(file.RelativePath, BackupArchiveReader, StringComparison.Ordinal))
                .SelectMany(file => LinesWithACatchClause(file.Source)
                    .Where(entry => entry.Text.Contains("CryptographicException", StringComparison.Ordinal))
                    .Select(entry => $"{file.RelativePath}:{entry.Line}"))
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();

            Assert.That(offenders, Is.Empty,
                "A stored secret nobody can read is reported as a CryptographicException. Catching that type " +
                "here turns it back into an ordinary error, which is how a wrong encryption key spent years " +
                "looking like a work tracking system rejecting a credential. Catch the specific failure this " +
                "code actually handles instead. Found at: " + string.Join(", ", offenders));
        }

        [Test]
        public void TheBackupArchiveExemption_StillDescribesRealCode()
        {
            var source = ProductionSourceOf(BackupArchiveReader);

            Assert.That(
                LinesWithACatchClause(source).Any(entry => entry.Text.Contains("CryptographicException", StringComparison.Ordinal)),
                Is.True,
                $"{BackupArchiveReader} is exempted from the rule above but no longer catches a cryptographic " +
                "failure. Remove the exemption, so it cannot go on excusing something else that moves into " +
                "this file later.");
        }

        private static List<CatchClause> LinesWithACatchClause(string source)
        {
            return CodeLines(source)
                .Where(line => ContainsToken(line.Text, "catch"))
                .Select(line => new CatchClause(line.Number, line.Text.Trim()))
                .ToList();
        }

        private static List<CatchClause> LinesWithAnExceptionFilter(string source)
        {
            return LinesWithACatchClause(source)
                .Where(entry => ContainsToken(entry.Text, "when"))
                .ToList();
        }

        private static string Describe(List<CatchClause> catches)
        {
            return catches.Count == 0
                ? "none"
                : string.Join(" | ", catches.Select(entry => $"line {entry.Line}: {entry.Text}"));
        }

        // Comments are dropped before anything is matched, because the code being guarded here says in prose
        // that it contains no catch, and a guard that reads its own justification as a violation is useless.
        private static IEnumerable<SourceLine> CodeLines(string source)
        {
            return source
                .Split('\n')
                .Select((text, offset) => new SourceLine(offset + 1, WithoutComment(text)));
        }

        private static string WithoutComment(string line)
        {
            var start = line.IndexOf("//", StringComparison.Ordinal);

            return start < 0 ? line : line[..start];
        }

        private static bool ContainsToken(string line, string token)
        {
            for (var index = line.IndexOf(token, StringComparison.Ordinal); index >= 0; index = line.IndexOf(token, index + 1, StringComparison.Ordinal))
            {
                var after = index + token.Length;
                var startsCleanly = index == 0 || !IsIdentifierCharacter(line[index - 1]);
                var endsCleanly = after >= line.Length || !IsIdentifierCharacter(line[after]);

                if (startsCleanly && endsCleanly)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsIdentifierCharacter(char character)
        {
            return char.IsLetterOrDigit(character) || character == '_';
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

            Assert.That(File.Exists(file), Is.True, $"{relativePath} was moved or deleted, so the rule it carries is no longer being enforced.");

            return File.ReadAllText(file);
        }

        // Anchored on the solution file rather than on a relative hop out of the test binary, which is what
        // the migration and calendar-anchor scans already do: the depth from the assembly to the repository
        // differs between a local build and the runner.
        private static string ProductionRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the secret read path scan.");

            return Path.Combine(directory!.FullName, ProductionProjectDirectory);
        }

        private sealed record SourceLine(int Number, string Text);

        private sealed record CatchClause(int Line, string Text);

        private sealed record SourceFile(string RelativePath, string Source);
    }
}
