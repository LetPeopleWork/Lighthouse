namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Nothing but <c>UpdateQueueService</c> sets the ambient refresh round.
    ///
    /// The context is an <c>AsyncLocal</c>, so a second writer does not announce itself. It silently
    /// changes which round every frame below it stages into, and the damage is not a wrong log line: a
    /// round can be handed over and written while work that belonged to it is still staging, so fields
    /// the operator asked to have written back to their tracker never arrive, and nothing says so.
    ///
    /// The queue is the only component that can know, because it is the one that admits the work and
    /// decides whether a piece of it opens a new round or joins the one that asked for it. Everything
    /// else reads.
    ///
    /// A source scanner rather than ArchUnitNET, matching <c>UpdateCancellationContextWriterArchUnitTest</c>:
    /// this is a rule about a property assignment, and no dependency rule can express "may read but not
    /// write".
    /// </summary>
    [TestFixture]
    public class WriteBackRoundContextWriterSourceStructureTest
    {
        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private const string ContextTypeName = "WriteBackRoundContext";

        /// <summary>The one component allowed to write it: it is the only one that knows whose round this is.</summary>
        private const string QueueRelativePath = "Services/Implementation/BackgroundServices/Update/UpdateQueueService.cs";

        /// <summary>
        /// The declaration itself assigns the backing field, which is not the rule's target. It sits
        /// beside the round rather than beside the queue, which is worth knowing before looking for it.
        /// </summary>
        private const string ContextRelativePath = "Services/Implementation/WriteBackRoundContext.cs";

        [Test]
        public void OnlyTheUpdateQueue_SetsTheAmbientRound()
        {
            var productionRoot = Path.Combine(RepositoryRoot(), ProductionProjectDirectory);
            var allowed = new[] { QueueRelativePath, ContextRelativePath }
                .Select(relative => Path.Combine(productionRoot, relative.Replace('/', Path.DirectorySeparatorChar)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var offenders = new List<string>();

            foreach (var file in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (allowed.Contains(file) || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var lines = File.ReadAllLines(file);

                // A file can only write the context if it names the type somewhere - to declare a field,
                // take it as a parameter, or resolve it. The assignment line itself says only
                // "something.Current = ...", so the type name has to be looked for across the file rather
                // than on the line.
                if (!lines.Any(line => line.Contains(ContextTypeName, StringComparison.Ordinal)))
                {
                    continue;
                }

                for (var line = 0; line < lines.Length; line++)
                {
                    if (WritesTheContext(lines[line]))
                    {
                        offenders.Add($"{Path.GetRelativePath(productionRoot, file)}:{line + 1}  {lines[line].Trim()}");
                    }
                }
            }

            Assert.That(offenders, Is.Empty,
                "Only the update queue may set the ambient refresh round - it is the only component that knows "
                + "whether new work opens a round or joins the one that asked for it. A second writer changes "
                + "which round every frame below it stages into without saying so, and the write that goes "
                + "missing as a result leaves no trace.\n" + string.Join("\n", offenders));
        }

        /// <summary>
        /// An assignment to the context's <c>Current</c>, however the instance is named. Reads are the point
        /// of the thing and are not matched.
        /// </summary>
        private static bool WritesTheContext(string line)
        {
            var assignment = line.IndexOf(".Current", StringComparison.Ordinal);
            if (assignment < 0)
            {
                return false;
            }

            var after = line[(assignment + ".Current".Length)..].TrimStart();
            return after.StartsWith('=') && !after.StartsWith("==", StringComparison.Ordinal);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the write-back-round-context scan.");
            return directory!.FullName;
        }
    }
}
