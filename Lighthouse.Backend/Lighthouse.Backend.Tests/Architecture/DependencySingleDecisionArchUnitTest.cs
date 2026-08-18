using ArchUnitNET.NUnit;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using NUnit.Framework;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// What a Feature waits on is decided in one place. The collection is exposed read-only and is changed
    /// through an internal seam, which stops the accident; this file stops someone widening the seam back
    /// open later, which a type cannot.
    /// </summary>
    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class DependencySingleDecisionArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private const string TheSeam = "ReplaceDependsOnReferences";

        private const string TheWordThatIsAlreadyTaken = "blocked";

        private const string BackendProjectDirectory = "Lighthouse.Backend";

        private const string DependsOnColumn = "createDependsOnColumn";

        private const string DependsOnColumnFile =
            "Lighthouse.Frontend/src/components/Common/FeatureListDataGrid/columns.tsx";

        // Everything this epic added on the backend lives under these three folders, so the terminology rule
        // can be scoped by folder rather than by file and still cover whatever the next slice adds.
        private static readonly string[] WhatThisEpicAddedToTheBackend =
        [
            "Models/Dependencies/",
            "Services/Implementation/Dependencies/",
            "Services/Interfaces/Dependencies/",
        ];

        [Test]
        public void NothingButTheReconciler_ChangesWhatAFeatureWaitsOn()
        {
            var theSeamOnFeature = MethodMembers().That()
                .AreDeclaredIn(typeof(Feature)).And()
                .HaveNameStartingWith(TheSeam);

            MethodMembers().That()
                .AreNotDeclaredIn(typeof(DependencyReconciler)).And()
                .AreNotDeclaredIn(typeof(AzureDevOpsWorkTrackingConnector))
                .Should().NotCallAny(theSeamOnFeature)
                .Because(
                    "Reconciling is a wholesale replacement, so a second caller does not add to what a Feature " +
                    "waits on - it silently discards whatever the first one wrote. Two callers exist and each " +
                    "earns it: DependencyReconciler decides what is stored, and the Azure DevOps connector fills " +
                    "in the links it just read off a work item that has no row yet, which the reconciler then " +
                    "re-keys and de-duplicates onto the Feature that is saved. Anything else - WorkItemService " +
                    "reaching past the reconciler it already calls, most of all - is the regression this catches. " +
                    "If a third caller is genuinely needed, take IDependencyReconciler instead.")
                .Check(Architecture);
        }

        /// <summary>
        /// "Blocked" already names a different thing in this product - one the user can rename, and does. Two
        /// meanings on one renameable word would follow the same rename and land side by side on the same row,
        /// where nobody could tell which one they had renamed.
        /// </summary>
        [Test]
        public void NothingThisEpicAdded_CallsAnythingBlocked()
        {
            var offenders = TheSourceThisEpicAdded()
                .SelectMany(file => file.Source
                    .Split('\n')
                    .Select((line, index) => new { Line = line, Number = index + 1 })
                    .Where(line => line.Line.Contains(TheWordThatIsAlreadyTaken, StringComparison.OrdinalIgnoreCase))
                    .Select(line => $"{file.RelativePath}:{file.FirstLine + line.Number - 1}: {line.Line.Trim()}"))
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();

            Assert.That(offenders, Is.Empty,
                $"'{TheWordThatIsAlreadyTaken}' already names an item a team has flagged as stuck, which the user " +
                "can rename under Settings. A dependency is a different thing, and a Feature can be both at once " +
                "on the same row. Say what this actually is - waiting on, depends on, not honoured. Found: " +
                string.Join(", ", offenders));
        }

        private static List<SourceFile> TheSourceThisEpicAdded()
        {
            var files = BackendSourceThisEpicAdded();
            files.Add(TheDependsOnColumn());

            return files;
        }

        private static List<SourceFile> BackendSourceThisEpicAdded()
        {
            var solutionRoot = SolutionRoot();
            var backendRoot = Path.Combine(solutionRoot, BackendProjectDirectory);

            var files = WhatThisEpicAddedToTheBackend
                .Select(folder => Path.Combine(backendRoot, folder.Replace('/', Path.DirectorySeparatorChar)))
                .SelectMany(folder =>
                {
                    Assert.That(Directory.Exists(folder), Is.True,
                        $"{folder} was moved or deleted, so the rule it carries is no longer being enforced.");

                    return Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories);
                })
                .Select(file => new SourceFile(
                    Path.GetRelativePath(solutionRoot, file).Replace('\\', '/'),
                    File.ReadAllText(file),
                    FirstLine: 1))
                .ToList();

            Assert.That(files, Is.Not.Empty, "Found no backend sources to scan; the scan is anchored at the wrong directory.");

            return files;
        }

        /// <summary>
        /// The column shares a file with every other column on the Feature lists, and one of those may one day
        /// legitimately show whether an item is stuck. Only the column this epic added is read, so such a
        /// column would not trip this.
        /// </summary>
        private static SourceFile TheDependsOnColumn()
        {
            var file = Path.Combine(
                RepositoryRoot(), DependsOnColumnFile.Replace('/', Path.DirectorySeparatorChar));

            Assert.That(File.Exists(file), Is.True,
                $"{DependsOnColumnFile} was moved or deleted, so the rule it carries is no longer being enforced.");

            var source = File.ReadAllText(file);
            var start = source.IndexOf($"export const {DependsOnColumn}", StringComparison.Ordinal);

            Assert.That(start, Is.GreaterThanOrEqualTo(0),
                $"{DependsOnColumn} was renamed or removed, so the rule it carries is no longer being enforced.");

            var next = source.IndexOf("\nexport ", start + 1, StringComparison.Ordinal);

            return new SourceFile(
                DependsOnColumnFile,
                next < 0 ? source[start..] : source[start..next],
                FirstLine: source[..start].Count(character => character == '\n') + 1);
        }

        private static string RepositoryRoot()
        {
            return Directory.GetParent(SolutionRoot())!.FullName;
        }

        private static string SolutionRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the dependency source scan.");

            return directory!.FullName;
        }

        private sealed record SourceFile(string RelativePath, string Source, int FirstLine);
    }
}
