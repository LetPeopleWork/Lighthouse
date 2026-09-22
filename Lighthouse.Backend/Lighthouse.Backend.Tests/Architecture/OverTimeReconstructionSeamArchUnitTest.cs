namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Which trailing windows a cycle-time percentile is recorded over is one decision, and a day
    /// rebuilt from history has to agree with the day that was recorded live or the chart shows a
    /// step where nothing happened. Every copy of that list is a place the two can be changed apart,
    /// so the copies are counted here and a new one fails the build. Read off the source text rather
    /// than expressed as a dependency rule, because an int array initialiser is not a reference to
    /// anything and no dependency rule can see it.
    /// </summary>
    [TestFixture]
    public class OverTimeReconstructionSeamArchUnitTest
    {
        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        /// <summary>
        /// The one type allowed to decide a percentile day. Both write policies live on it, so a day
        /// filled in behind the series and a day recorded this afternoon run the same computation.
        /// </summary>
        private const string TheWriter = "Services/Implementation/PercentileSnapshotWriter.cs";

        /// <summary>
        /// The demo synthesiser invents a backdated history for a freshly-loaded demo instead of
        /// computing one, so it never calls the writer, yet the horizons it invents rows for must
        /// still match the ones a real instance records - a demo showing a 45-day line nobody else
        /// has would be a bug in the screenshot, not in the data. It is named here rather than
        /// pattern-matched so that a third copy has to be argued for instead of just appearing.
        /// </summary>
        private const string TheDemoSynthesiser = "Services/Implementation/DomainEvents/DemoPercentilesBackfillHandler.cs";

        private static readonly string[] FilesAllowedToDeclareTheHorizons = [TheWriter, TheDemoSynthesiser];

        /// <summary>The port a chart load asks through, and the thing it asks.</summary>
        private const string TheGapReconcilerPort = "Services/Interfaces/IOverTimeGapReconciler.cs";

        /// <summary>Everything a chart load runs through on the thread the reader is waiting on.</summary>
        private static readonly string[] TheReadPath =
        [
            "Services/Implementation/GapAskingPercentilesOverTimeSeriesQuery.cs",
            "Services/Implementation/GapAskingProcessBehaviorSeriesQuery.cs",
            "Services/Implementation/OverTimeGapReconciler.cs",
        ];

        /// <summary>Every way an over-time day gets written, percentile and process limit alike.</summary>
        private static readonly string[] TheWriteSide =
        [
            "IPercentileSnapshotWriter",
            "IPercentilesOverTimeSnapshotRepository",
            "IProcessBehaviorSnapshotWriter",
            "IProcessBehaviorSnapshotRepository",
        ];

        /// <summary>
        /// The cycle-time trailing windows, spelled as they appear in a collection expression. Written
        /// out in the two spacings C# formatting produces, so a copy does not slip through on a comma.
        /// </summary>
        private static readonly string[] CycleTimeHorizonListSpellings = ["[30, 60, 90]", "[30,60,90]"];

        /// <summary>
        /// The one type allowed to decide a process-behaviour day. Both write policies live on it, so
        /// a day filled in behind the series and a day recorded this afternoon run the same computation.
        /// </summary>
        private const string TheProcessBehaviorWriter = "Services/Implementation/ProcessBehaviorSnapshotWriter.cs";

        private static readonly string[] FilesAllowedToDeclareTheProcessBehaviorFamilies = [TheProcessBehaviorWriter];

        /// <summary>
        /// Feature Size is the one process-behaviour family a portfolio has and a team does not, so
        /// naming it is something only code assembling the portfolio family set ever does. The other
        /// five are spelled out elsewhere for unrelated reasons - a controller query default, the demo
        /// synthesiser - and a rule built on those would be a list of exemptions instead of a rule.
        /// </summary>
        private const string ThePortfolioOnlyFamilySpelling = "ProcessBehaviorMetricType.FeatureSize";

        /// <summary>A chart being built, as it is spelled where one is built.</summary>
        private const string AChartBeingBuilt = "new ProcessBehaviourChart";

        /// <summary>
        /// How many places build a chart carrying a status other than Ready: the NotReady factory, and
        /// the five refusals the two metrics services hand back when a baseline cannot be used. Pinned
        /// so that the scan going blind - the type renamed, the code moved out of this tree - reads as
        /// a failure rather than as an empty list of offenders, which is what a rule of this shape
        /// otherwise quietly degrades into. A seventh place is welcome; it just has to be argued for
        /// here rather than appear.
        /// </summary>
        private const int PlacesThatBuildAChartWithoutAReadyStatus = 6;

        private static readonly string[] DirectoriesThatAreNotSource = ["/obj/", "/bin/", "/StrykerOutput"];

        [Test]
        public void TheCycleTimeHorizonList_IsDeclaredOnlyWhereItIsAllowedToBe()
        {
            var declarations = ProductionSourceFiles()
                .Where(file => CycleTimeHorizonListSpellings.Any(spelling =>
                    file.Source.Contains(spelling, StringComparison.Ordinal)))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            Assert.That(declarations, Is.EqualTo(FilesAllowedToDeclareTheHorizons.OrderBy(path => path, StringComparer.Ordinal).ToList()),
                "The cycle-time horizons decide which windows a percentile day covers. A copy outside " +
                $"{TheWriter} can be changed without the recorded and the reconstructed day changing " +
                "together, which is how a series gains a step at the date the two stopped agreeing. Take " +
                "the horizons from the writer instead. Declared in: " + string.Join(", ", declarations));
        }

        [Test]
        public void TheWriter_OffersBothWritePoliciesUnderTheirOwnNames()
        {
            var source = ProductionSourceOf(TheWriter);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(source, Does.Contain("void RecordToday("),
                    "Recording today overwrites, because today's value keeps changing until the day ends.");

                Assert.That(source, Does.Contain("void FillDayIfAbsent("),
                    "Filling a day leaves a day that already carries a value alone, because that value " +
                    "was measured when the day was current and a recomputation from today's data is worse.");

                Assert.That(source, Does.Not.Contain("bool overwrite"),
                    "The two policies differ in what they do to a day that already has a value, and that " +
                    "difference is the whole point. Behind a boolean, a call site no longer says which one " +
                    "it meant.");
            }
        }

        [Test]
        public void TheProcessBehaviorFamilySets_AreDeclaredOnlyWhereTheyAreAllowedToBe()
        {
            var declarations = ProductionSourceFiles()
                .Where(file => file.Source.Contains(ThePortfolioOnlyFamilySpelling, StringComparison.Ordinal))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            Assert.That(declarations, Is.EqualTo(FilesAllowedToDeclareTheProcessBehaviorFamilies.OrderBy(path => path, StringComparer.Ordinal).ToList()),
                "Which families a scope records is one decision, and a stretch rebuilt from history has to " +
                "cover the same families as the days recorded live, or a family goes missing from part of " +
                $"the chart with nothing to show for it. A copy outside {TheProcessBehaviorWriter} can be " +
                "changed without the other moving. Take the family set from the writer instead. Declared " +
                "in: " + string.Join(", ", declarations));
        }

        [Test]
        public void TheProcessBehaviorWriter_OffersBothWritePoliciesUnderTheirOwnNames()
        {
            var source = ProductionSourceOf(TheProcessBehaviorWriter);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(source, Does.Contain("void RecordToday("),
                    "Recording today overwrites, because today's limits keep moving until the day ends.");

                Assert.That(source, Does.Contain("void FillDayIfAbsent("),
                    "Filling a day leaves a day that already carries limits alone, because those limits " +
                    "were measured when the day was current and a recomputation from today's data is worse.");

                Assert.That(source, Does.Not.Contain("bool overwrite"),
                    "The two policies differ in what they do to a day that already has a value, and that " +
                    "difference is the whole point. Behind a boolean, a call site no longer says which one " +
                    "it meant.");
            }
        }

        /// <summary>
        /// A chart that is not ready reports no centre and no upper limit. That convention is what makes
        /// the two honesty gates in the process-behaviour writer interchangeable today: the gate that
        /// refuses a chart on its status refuses nothing the gate that refuses a collapsed band would
        /// not also refuse, which is why no scenario can tell them apart and why removing the status
        /// gate on its own breaks no test.
        ///
        /// Nothing in the type requires it. A builder that one day hands back a live band alongside a
        /// status of "not ready" would make that status gate the only thing standing between the band
        /// and the stored chart, and the gate would go from redundant to load-bearing with no test
        /// noticing. No acceptance scenario can pin this, because the object it would need - not ready,
        /// and carrying a band - is precisely the one the product deliberately never constructs and
        /// nothing a test can run will produce. So it is read off the source, as the rules above are,
        /// and for the same reason: a literal in an object initialiser is not a reference to anything a
        /// dependency rule could follow.
        /// </summary>
        [Test]
        public void AChartThatIsNotReady_ReportsNoCentreAndNoUpperLimit()
        {
            var builtWithoutAReadyStatus = ProductionSourceFiles()
                .SelectMany(file => ChartsBuiltIn(file.Source).Select(chart => new { file.RelativePath, Chart = chart }))
                .Where(built => !built.Chart.Contains("Status = BaselineStatus.Ready", StringComparison.Ordinal))
                .ToList();

            var carryingABandAnyway = builtWithoutAReadyStatus
                .Where(built => !built.Chart.Contains("Average = 0,", StringComparison.Ordinal)
                             || !built.Chart.Contains("UpperNaturalProcessLimit = 0,", StringComparison.Ordinal))
                .Select(built => built.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(builtWithoutAReadyStatus, Has.Count.EqualTo(PlacesThatBuildAChartWithoutAReadyStatus),
                    $"{builtWithoutAReadyStatus.Count} places build a chart without a ready status, and " +
                    $"{PlacesThatBuildAChartWithoutAReadyStatus} were expected. Fewer usually means the scan has gone blind - a " +
                    "rename, a move - and a rule scanning for nothing reports no offenders forever. More means a new refusal " +
                    "arrived: check it leaves the band empty and say so here.");

                Assert.That(carryingABandAnyway, Is.Empty,
                    "A chart that is not ready must report an average and an upper limit of zero. The process-behaviour writer " +
                    "refuses to store a collapsed band, and that refusal is the only one either of its honesty gates can be shown " +
                    "to make - a chart that is not ready but carries a band would walk straight past it and be stored as limits " +
                    "the owner never had. Carrying one anyway: " + string.Join(", ", carryingABandAnyway));
            }
        }

        [Test]
        public void TheDemoSynthesiserExemption_StillDescribesRealCode()
        {
            var source = ProductionSourceOf(TheDemoSynthesiser);

            Assert.That(
                CycleTimeHorizonListSpellings.Any(spelling => source.Contains(spelling, StringComparison.Ordinal)),
                Is.True,
                $"{TheDemoSynthesiser} is the one file exempted from the rule above but no longer declares " +
                "the horizons. Remove its exemption, so it cannot go on excusing a copy that moves into " +
                "this file later.");
        }

        /// <summary>
        /// Opening a chart must not be able to write a day while the person who opened it waits. The
        /// shipped read does reach the snapshot table - that is the read. What is ruled out is the
        /// write: the types this story puts between the endpoint and that read may not so much as
        /// name the writer or the snapshot store, which leaves "a GET wrote to the database on the
        /// request thread" unavailable rather than merely untested.
        /// </summary>
        [Test]
        public void TheReadPath_CannotWriteAnOverTimeDay()
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var path in TheReadPath)
                {
                    var source = ProductionSourceOf(path);

                    foreach (var writeSide in TheWriteSide)
                    {
                        Assert.That(source, Does.Not.Contain(writeSide),
                            $"{path} runs while a reader waits, so it may not reach {writeSide}. Hand the " +
                            "missing days to the filler and let a pass with its own scope write them.");
                    }
                }
            }
        }

        /// <summary>
        /// The ask gives the reader nothing to wait on. A result here is something a caller would
        /// eventually be tempted to await, and awaiting it is the defect this whole seam avoids.
        /// </summary>
        [Test]
        public void AskingForTheMissingDays_HandsBackNothing()
        {
            var source = ProductionSourceOf(TheGapReconcilerPort);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(source, Does.Contain("void AskForTheDaysThatAreMissing("),
                    "The one operation on this port returns void.");

                Assert.That(source, Does.Not.Contain("Task"),
                    "A Task here is a handle on the filling in, and a reader handed one waits for it.");
            }
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

        /// <summary>
        /// Every object initialiser in this source that builds a chart, as text. Anything that is not
        /// followed by an initialiser is skipped: the data-point record's name begins the same way, and
        /// a construction that sets nothing declares no band either way.
        /// </summary>
        private static IEnumerable<string> ChartsBuiltIn(string source)
        {
            var at = source.IndexOf(AChartBeingBuilt, StringComparison.Ordinal);

            while (at >= 0)
            {
                var opening = at + AChartBeingBuilt.Length;
                while (opening < source.Length && char.IsWhiteSpace(source[opening]))
                {
                    opening++;
                }

                if (opening < source.Length && source[opening] == '{')
                {
                    yield return InitialiserStartingAt(source, opening);
                }

                at = source.IndexOf(AChartBeingBuilt, at + AChartBeingBuilt.Length, StringComparison.Ordinal);
            }
        }

        private static string InitialiserStartingAt(string source, int opening)
        {
            var depth = 0;

            for (var i = opening; i < source.Length; i++)
            {
                if (source[i] == '{')
                {
                    depth++;
                }
                else if (source[i] == '}')
                {
                    depth--;

                    if (depth == 0)
                    {
                        return source[opening..(i + 1)];
                    }
                }
            }

            Assert.Fail("A chart initialiser runs to the end of its file without closing, so the scan cannot read what it sets.");

            return string.Empty;
        }

        private static string ProductionSourceOf(string relativePath)
        {
            var file = Path.Combine(ProductionRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.That(File.Exists(file), Is.True, $"{relativePath} was moved or deleted, so the rule it carries is no longer being enforced.");

            return File.ReadAllText(file);
        }

        // Anchored on the solution file rather than on a relative hop out of the test binary: the depth
        // from the assembly to the repository differs between a local build and the runner.
        private static string ProductionRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the percentile horizon scan.");

            return Path.Combine(directory!.FullName, ProductionProjectDirectory);
        }

        private sealed record SourceFile(string RelativePath, string Source);
    }
}
