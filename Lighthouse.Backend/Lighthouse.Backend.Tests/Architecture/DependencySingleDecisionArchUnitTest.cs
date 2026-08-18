using ArchUnitNET.NUnit;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Microsoft.Extensions.Logging;
using Moq;
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
        // Every type this epic introduced sits in a namespace ending in this word, so a rule written against
        // it keeps covering whatever the next slice adds without anyone remembering to widen a list.
        private const string TheNamespaceThisEpicOwns =
            @"^Lighthouse\.Backend\.(Models|Services\.(Implementation|Interfaces))\.Dependencies($|\..*)";

        private const string TheFileThatHandsOutTheCount = "Lighthouse.Backend/API/FeaturesController.cs";

        private const string TheAttributeThatCharges = "LicenseGuard";

        private const string ThePayloadTheCountRidesOn = "FeatureDto";

        private static readonly string[] TheWordsThatWouldMeanAPriceWasAsked =
            ["CanUsePremiumFeatures", "LicenseGuard", "ILicenseService", "useLicenseRestrictions"];

        // Both forecast runs draw from a Random built on this one number, so they see the same sequence.
        private const int TheSeedBothRunsShare = 20260818;

        private static readonly int[] TheThroughputBothRunsForecastFrom =
            [2, 0, 0, 5, 1, 3, 2, 4, 0, 0, 1, 1, 2, 4, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 1, 2, 0, 0];

        private static readonly int[] TheRemainingWorkPerFeature = [7, 3, 11];

        private static readonly int[] ThePercentilesLighthouseShows = [50, 70, 85, 95];

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
                .SelectMany(file => LinesMatching(file, line =>
                    line.Contains(TheWordThatIsAlreadyTaken, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Assert.That(offenders, Is.Empty,
                $"'{TheWordThatIsAlreadyTaken}' already names an item a team has flagged as stuck, which the user " +
                "can rename under Settings. A dependency is a different thing, and a Feature can be both at once " +
                "on the same row. Say what this actually is - waiting on, depends on, not honoured. Found: " +
                string.Join(", ", offenders));
        }

        /// <summary>
        /// The seeded forecast next door shows the days did not move. It cannot show why, and a run that
        /// happens to agree today would still agree the day after someone wires the two halves together.
        /// This says the two halves do not know each other exists, which is the reason the days agree.
        /// </summary>
        [Test]
        public void TheForecastAndThisEpic_KnowNothingOfEachOther()
        {
            var theForecastItself = Types().That()
                .Are(typeof(ForecastService)).Or()
                .Are(typeof(SimulationResult)).Or()
                .Are(typeof(Lighthouse.Backend.Services.Implementation.RandomNumberService));

            var whatThisEpicAdded = Types().That()
                .ResideInNamespaceMatching(TheNamespaceThisEpicOwns).Or()
                .Are(typeof(FeatureDependencyReference));

            const string reason =
                "Neither half of this may reach the other. What a Feature waits on is stored and counted here " +
                "and read by nobody who schedules anything; the simulation still draws from throughput alone, " +
                "exactly as it did before dependencies existed. Letting a dependency change a forecast is a " +
                "separate piece of work with its own decisions to make, and this is the door it has to come " +
                "through rather than arriving as an import nobody noticed.";

            theForecastItself.Should().NotDependOnAny(whatThisEpicAdded).Because(reason).Check(Architecture);
            whatThisEpicAdded.Should().NotDependOnAny(theForecastItself).Because(reason).Check(Architecture);
        }

        /// <summary>
        /// Everything here is free. The premium flag has a place waiting for it in the design so the next
        /// epic need not re-cut the type, and this keeps that place empty until that epic arrives - an
        /// unread field is easy to start reading by accident.
        /// </summary>
        [Test]
        public void NothingThisEpicAdded_AsksWhetherTheLicenceIsPremium()
        {
            var licenceWords = TheSourceThisEpicAdded()
                .SelectMany(file => LinesMatching(file, line =>
                    TheWordsThatWouldMeanAPriceWasAsked.Any(word => line.Contains(word, StringComparison.Ordinal))))
                .ToList();

            var chargedRoutes = LinesMatching(TheFileThatHandsOutTheCount, TheRouteHandsOutTheCount);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(licenceWords, Is.Empty,
                    "Reading what a Feature waits on costs nothing, on the screen as well as on the wire. A " +
                    "licence question here would hide the number from most instances and leave the column blank " +
                    "with no way to tell why. Found: " + string.Join(", ", licenceWords));

                Assert.That(chargedRoutes, Is.Empty,
                    "The count of what a Feature waits on rides along on the Feature payload that every list " +
                    "reads, and that payload is free. Putting a route that hands it out behind a paid gate would " +
                    "take the whole Feature list away from an unlicensed instance to hide one number. Found: " +
                    string.Join(", ", chargedRoutes));
            }
        }

        /// <summary>
        /// A route is charged for by an attribute sitting directly above it, so the paid ones are found by
        /// reading down from each attribute to the signature it guards.
        /// </summary>
        private static bool TheRouteHandsOutTheCount(string line, IReadOnlyList<string> following)
        {
            if (!line.TrimStart().StartsWith($"[{TheAttributeThatCharges}", StringComparison.Ordinal))
            {
                return false;
            }

            var signature = following.FirstOrDefault(next => next.Contains(" public ", StringComparison.Ordinal));

            return signature is not null && signature.Contains(ThePayloadTheCountRidesOn, StringComparison.Ordinal);
        }

        /// <summary>
        /// Every other test in this epic hands the forecast a stand-in. This one runs the real Monte Carlo
        /// simulation, because the claim being checked is precisely that the real simulation lands on the same
        /// days whether or not dependency data is present - a stand-in could never tell you that.
        /// </summary>
        [Test]
        public async Task StoringWhatAFeatureWaitsOn_MovesNoForecastDate()
        {
            var withoutDependencyData = await TheDaysTheForecastLandsOn(storeDependencies: false);
            var withDependencyData = await TheDaysTheForecastLandsOn(storeDependencies: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withoutDependencyData, Has.All.GreaterThan(0),
                    "The fixture forecast produced no days at all, so comparing the two runs would compare nothing.");

                Assert.That(withDependencyData, Is.EqualTo(withoutDependencyData),
                    "Storing what a Feature waits on moved a forecast date. Nothing in this epic may reach the " +
                    "simulation - the day Lighthouse shows has to be the same day it showed before dependencies " +
                    "existed. Deciding what a dependency does to a date is a separate piece of work.");
            }
        }

        private static async Task<int[]> TheDaysTheForecastLandsOn(bool storeDependencies)
        {
            var team = new Team { Id = 1, Name = "Team", FeatureWIP = 3 };
            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData(TheThroughputBothRunsForecastFrom));

            var teamMetricsService = new Mock<ITeamMetricsService>();
            teamMetricsService
                .Setup(service => service.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting))
                .Returns(new ForecastThroughputStatus(throughput, false, null));

            var features = TheFixturePortfolio(team, storeDependencies);

            var featureRepository = new Mock<IRepository<Feature>>();
            featureRepository.Setup(repository => repository.GetAll()).Returns(features);

            var portfolio = new Portfolio { Id = 1, Name = "Portfolio" };
            portfolio.UpdateFeatures(features);

            var forecastService = new ForecastService(
                new SeededRandomNumberService(TheSeedBothRunsShare),
                Mock.Of<ILogger<ForecastService>>(),
                teamMetricsService.Object,
                featureRepository.Object);

            await forecastService.UpdateForecastsForPortfolio(portfolio);

            return features
                .SelectMany(feature => ThePercentilesLighthouseShows.Select(feature.Forecast.GetProbability))
                .ToArray();
        }

        private static List<Feature> TheFixturePortfolio(Team team, bool storeDependencies)
        {
            var features = TheRemainingWorkPerFeature
                .Select((remainingItems, index) => new Feature(team, remainingItems)
                {
                    Id = index + 1,
                    Name = $"Feature {index + 1}",
                    ReferenceId = $"F-{index + 1}",
                })
                .ToList();

            if (storeDependencies)
            {
                foreach (var feature in features)
                {
                    feature.ReplaceDependsOnReferences(features
                        .Where(other => other != feature)
                        .Select(other => new FeatureDependencyReference(feature.Id, other.ReferenceId, DependencySource.TrackerLink)));
                }
            }

            return features;
        }

        private static List<string> LinesMatching(string relativePath, Func<string, IReadOnlyList<string>, bool> matches)
        {
            var file = Path.Combine(SolutionRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.That(File.Exists(file), Is.True,
                $"{relativePath} was moved or deleted, so the rule it carries is no longer being enforced.");

            var lines = File.ReadAllLines(file);

            return lines
                .Select((line, index) => new { Line = line, Number = index + 1, Following = lines.Skip(index + 1).ToList() })
                .Where(entry => matches(entry.Line, entry.Following))
                .Select(entry => $"{relativePath}:{entry.Number}: {entry.Line.Trim()}")
                .ToList();
        }

        private static List<string> LinesMatching(SourceFile file, Func<string, bool> matches)
        {
            return file.Source
                .Split('\n')
                .Select((line, index) => new { Line = line, Number = index + 1 })
                .Where(entry => matches(entry.Line))
                .Select(entry => $"{file.RelativePath}:{file.FirstLine + entry.Number - 1}: {entry.Line.Trim()}")
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();
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

        /// <summary>
        /// The shipped randomness source builds a fresh, unseeded Random for every single draw, so two runs of
        /// it can never be compared to each other. This one starts from a fixed number and therefore draws the
        /// same sequence every time, on any machine, which is what makes "the same forecast with and without
        /// dependency data" a statement about the code rather than about luck. Only one team is forecast here,
        /// and the simulation runs one thread per team, so the draws are handed out in a fixed order.
        /// </summary>
        private sealed class SeededRandomNumberService(int seed) : IRandomNumberService
        {
            private readonly Random random = new(seed);

            public int GetRandomNumber(int maxValue) => random.Next(maxValue);
        }

        private sealed record SourceFile(string RelativePath, string Source, int FirstLine);
    }
}
