namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// ADR-183's stated enforcement: nothing but <c>UpdateQueueService</c> writes
    /// <c>UpdateCancellationContext</c>.
    ///
    /// The context is an <c>AsyncLocal</c>, which means a second writer does not announce itself. It
    /// silently changes what "has this been cancelled" answers for every frame below it, and the failure
    /// shows up as a refresh that stops when nobody asked it to, or one that does not stop when somebody
    /// did — neither of which points back at the line that caused it.
    ///
    /// The queue is the only component that knows which execution is which, because it is the one that
    /// admitted the work and owns the token source. Everything else reads.
    ///
    /// A source scanner rather than ArchUnitNET, matching <c>CalendarDayAnchorSeamArchUnitTest</c>: this is
    /// a rule about a property assignment, and no dependency rule can express "may read but not write".
    /// </summary>
    [TestFixture]
    public class UpdateCancellationContextWriterArchUnitTest
    {
        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private const string ContextTypeName = "UpdateCancellationContext";

        /// <summary>The one component allowed to write it: it is the only one that knows whose execution this is.</summary>
        private const string QueueRelativePath = "Services/Implementation/BackgroundServices/Update/UpdateQueueService.cs";

        /// <summary>The declaration itself assigns the backing field, which is not the rule's target.</summary>
        private const string ContextRelativePath = "Services/Implementation/BackgroundServices/Update/UpdateCancellationContext.cs";

        [Test]
        public void OnlyTheUpdateQueue_WritesTheCancellationContext()
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
                // than on the line: looking for it on the line matches nothing at all, including the one
                // real writer.
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
                "Only the update queue may set the cancellation context - it is the only component that knows "
                + "which execution a token belongs to. A second writer changes the answer for every frame "
                + "below it without saying so.\n" + string.Join("\n", offenders));
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

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the cancellation-context scan.");
            return directory!.FullName;
        }
    }
}
