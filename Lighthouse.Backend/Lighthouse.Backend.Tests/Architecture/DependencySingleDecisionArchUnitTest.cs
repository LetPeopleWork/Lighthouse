using ArchUnitNET.NUnit;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
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
    /// What a Feature waits on is decided in one place. The collection is exposed read-only and a stored
    /// one is changed through a single internal seam, which stops the accident; this file stops someone
    /// widening the seam back open later, which a type cannot.
    /// </summary>
    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class DependencySingleDecisionArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private const string TheSeam = "ReplaceDependsOnReferences";

        private const string TheOnlyFileThatMayChangeIt =
            "Lighthouse.Backend/Services/Implementation/Dependencies/DependencyReconciler.cs";

        private const string TheOnlyPlaceThatMayRecordTheSource =
            "Lighthouse.Backend/Services/Implementation/Dependencies/";

        private const string TheFileThatChoosesTheSource =
            "Lighthouse.Backend/Services/Implementation/Dependencies/DependencySourceSelector.cs";

        // Azure DevOps, Jira and Linear return Features and can carry something for one to wait on.
        // ServiceNow and CSV return no Features at all, so a dependency has nothing to run between there.
        private static readonly string[] TrackersThatCarryDependencies =
        [
            "Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs",
            "Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs",
            "Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingConnector.cs",
        ];

        private const string TheWordThatIsAlreadyTaken = "blocked";

        private const string BackendProjectDirectory = "Lighthouse.Backend";

        private const string DependsOnColumn = "createDependsOnColumn";

        private const string DependsOnColumnFile =
            "Lighthouse.Frontend/src/components/Common/FeatureListDataGrid/columns.tsx";

        // The two files that write the words a reader actually sees about a dependency: the sentences
        // themselves, and the indicator that shows them. Read whole rather than scoped to a region -
        // anything either of them grows is this epic's vocabulary by construction.
        private static readonly string[] TheFrontendFilesThisEpicWrites =
        [
            "Lighthouse.Frontend/src/utils/dependencies/dependencySentences.ts",
            "Lighthouse.Frontend/src/components/Common/FeatureListDataGrid/WarningsIndicator.tsx",
        ];

        // Everything this epic added on the backend lives under these three folders, so the terminology rule
        // can be scoped by folder rather than by file and still cover whatever the next slice adds.
        // Every type this epic introduced sits in a namespace ending in this word, so a rule written against
        // it keeps covering whatever the next slice adds without anyone remembering to widen a list.
        private const string TheNamespaceThisEpicOwns =
            @"^Lighthouse\.Backend\.(Models|Services\.(Implementation|Interfaces))\.Dependencies($|\..*)";

        private const string TheFileThatHandsOutTheCount = "Lighthouse.Backend/API/FeaturesController.cs";

        private const string TheAttributeThatCharges = "LicenseGuard";

        private const string ThePayloadTheCountRidesOn = "FeatureDto";

        // Declaring it is legitimate and reading it is not, so the rule keys on the dot that tells them apart.
        private const string TheLicenceFlagThatMustStayUnread = "HasPremiumLicence";

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
                .AreNotDeclaredIn(typeof(DependencyReconciler))
                .Should().NotCallAny(theSeamOnFeature)
                .Because(
                    "Reconciling is a wholesale replacement, so a second caller does not add to what a Feature " +
                    "waits on - it silently discards whatever the first one wrote. DependencyReconciler is the " +
                    "only thing that decides what a Feature already on file waits on. A connector reading links " +
                    "off a work item that has no row here yet hands them to the Feature constructor instead, " +
                    "which can only ever fill in an object being built and cannot reach a stored one. Anything " +
                    "else - WorkItemService reaching past the reconciler it already calls, most of all - is the " +
                    "regression this catches. If a second writer is genuinely needed, take IDependencyReconciler.")
                .Check(Architecture);
        }

        /// <summary>
        /// The rule above reads the compiled assembly, so it sees a call however many methods it travels
        /// through, and even a delegate handed the seam rather than called. What it cannot see is a call
        /// that compiles to no edge at all: assign the Feature to a dynamic local and the seam's name
        /// becomes a string the runtime looks up, invisible to anything reading the assembly and plain to
        /// anything reading the file. That is not a hypothetical - it was tried against both rules, and
        /// only this one went red. Both stay: one catches what is compiled, the other what is written.
        /// </summary>
        [Test]
        public void NoBackendSourceOutsideTheReconciler_CallsTheSeam()
        {
            var secondWriters = TheBackendSourceOutsideTheReconciler()
                .SelectMany(file => LinesMatching(file, ACallToTheSeam))
                .ToList();

            Assert.That(secondWriters, Is.Empty,
                "Reconciling is a wholesale replacement, so a second caller does not add to what a Feature " +
                "waits on - it discards whatever the first one wrote, with nothing logged and nothing left to " +
                "see but a count that is short. DependencyReconciler is the only thing that may change what a " +
                "Feature already on file waits on; a Feature the tracker has only just handed over takes its " +
                "references through the constructor, which cannot reach a stored one. If a second writer is " +
                "genuinely needed, take IDependencyReconciler. Found: " + string.Join(", ", secondWriters));
        }

        // The seam's name anywhere it is written as code, rather than only where a bracket follows it: a
        // name handed to a delegate, or to reflection as a string, reaches the seam without ever looking
        // like a call. The line that declares it is not a use of it, and prose about it is not code.
        private static bool ACallToTheSeam(string line)
        {
            var code = line.TrimStart();

            return code.Contains(TheSeam, StringComparison.Ordinal)
                && !code.StartsWith("//", StringComparison.Ordinal)
                && !code.StartsWith('*')
                && !code.Contains($"void {TheSeam}(", StringComparison.Ordinal);
        }

        /// <summary>
        /// Everything the scan is allowed to find nothing in. Reading the reconciler's own call first is what
        /// keeps the rest honest: rename the seam and the scan would be hunting a name no longer in the code,
        /// finding nothing everywhere and passing for the wrong reason. Finding it there says the name is live.
        /// </summary>
        private static List<SourceFile> TheBackendSourceOutsideTheReconciler()
        {
            var backendSource = TheWholeBackendSource();

            var theReconcilersOwnCall = backendSource
                .Where(file => file.RelativePath == TheOnlyFileThatMayChangeIt)
                .SelectMany(file => LinesMatching(file, ACallToTheSeam))
                .ToList();

            Assert.That(theReconcilersOwnCall, Is.Not.Empty,
                $"{TheOnlyFileThatMayChangeIt} no longer calls {TheSeam} - it was renamed, moved or deleted - so " +
                "this scan is looking for a name nothing in the code uses and would stay green whatever the rest " +
                "of the source did. Point it at whatever the single writer and its seam are called now.");

            return backendSource.Where(file => file.RelativePath != TheOnlyFileThatMayChangeIt).ToList();
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
        /// The licence flag has a place waiting for it on the decision's input so the next epic need not
        /// re-cut the type. Declaring it is fine; reading it here is not, and the difference is exactly one
        /// dot. Nothing else catches this - the four names the licence scan looks for are all about asking
        /// a service, and this one is a property already sitting in the room.
        /// </summary>
        [Test]
        public void NothingThisEpicAdded_ReadsTheLicenceFlagItWasHanded()
        {
            var reads = TheSourceThisEpicAdded()
                .SelectMany(file => LinesMatching(file, line =>
                    line.Contains($".{TheLicenceFlagThatMustStayUnread}", StringComparison.Ordinal)))
                .ToList();

            Assert.That(reads, Is.Empty,
                $"'{TheLicenceFlagThatMustStayUnread}' is declared for the epic that turns paid behaviour on and is " +
                "read by nothing until then. A read here would make what a dependency does depend on an instance's " +
                "licence while every screen in this epic is free, and the two would disagree. Found: " +
                string.Join(", ", reads));
        }

        /// <summary>
        /// One place decides whether Lighthouse can act on a dependency. A second implementation would let
        /// the warning a user reads and the forecast that acts on the same dependency disagree, and the two
        /// would be discovered by a person noticing a screenshot did not match a date.
        /// </summary>
        /// <remarks>
        /// At most one, rather than exactly one, only because the second reader has not shipped yet: the
        /// forecast that consumes this decision arrives with its own epic and tightens the word then. The
        /// weaker form is deliberate, not an oversight.
        /// </remarks>
        [Test]
        public void AtMostOnePlace_DecidesWhetherADependencyCanBeActedOn()
        {
            var deciders = Architecture.Classes
                .Where(candidate => candidate.ImplementedInterfaces
                    .Any(implemented => implemented.FullName == typeof(IDependencyHonourPolicy).FullName))
                .Select(candidate => candidate.FullName)
                .ToList();

            Assert.That(deciders, Has.Count.LessThanOrEqualTo(1),
                "A second place deciding whether a dependency can be acted on is how a warning on screen ends " +
                "up disagreeing with what a forecast actually did. Found: " + string.Join(", ", deciders));
        }

        /// <summary>
        /// Where a dependency was read from is a fact about the Portfolio's settings, not about the tracker,
        /// so no tracker works it out for itself. A tracker that did would be right until the day the rule
        /// changed and nobody remembered it had a copy - which is how the setting came to be honoured on one
        /// tracker while two others accepted it and ignored it, looking from the outside exactly like a field
        /// everyone had left empty.
        ///
        /// The scan reads for the reference being built at all, rather than only for the field-backed source.
        /// A tracker that never builds one cannot record the wrong source, and one that builds them by hand
        /// has taken the decision back whichever source it happens to write today.
        /// </summary>
        [Test]
        public void NothingButTheOneSelector_RecordsWhereADependencyWasReadFrom()
        {
            var secondDeciders = TheBackendSourceOutsideTheSelector()
                .SelectMany(file => LinesMatching(file, AReferenceBuiltByHand))
                .ToList();

            Assert.That(secondDeciders, Is.Empty,
                "DependencySourceSelector decides whether a Feature's dependencies came from the tracker's own " +
                "link or from a field the Portfolio named, and stamps each reference accordingly. A tracker that " +
                "builds references itself has taken that decision back, and the next tracker after it inherits " +
                "the rule only if somebody remembers to tell it. Ask the selector instead - and a tracker that " +
                "cannot serve a named field at all says so by name, through TheTrackersOwnLinksOnly. Found: " +
                string.Join(", ", secondDeciders));
        }

        /// <summary>
        /// Every tracker that can carry dependencies has to ask. The rule above stops a tracker recording the
        /// wrong answer; on its own it would not stop one that never asks at all, reads only its own links and
        /// passes clean - which is the bug this epic already shipped once, in exactly that shape.
        ///
        /// This one reads coarsely, for the selector being named anywhere in the tracker at all. A tracker
        /// that asks about one thing and decides another for itself is not caught here - it is caught by the
        /// rule above, which is the load-bearing one. Both were watched to fail before being trusted: with
        /// Jira stamping its own references again, the rule above names the file and line, and this one stays
        /// green because Jira still names the selector where it decides what to report about links.
        /// </summary>
        [Test]
        public void EveryTrackerThatReadsDependencies_AsksTheSelectorWhereToReadThemFrom()
        {
            var trackersThatDoNotAsk = TheTrackersThatCarryDependencies()
                .Where(file => !file.Source.Contains(nameof(DependencySourceSelector), StringComparison.Ordinal))
                .Select(file => file.RelativePath)
                .ToList();

            Assert.That(trackersThatDoNotAsk, Is.Empty,
                "A tracker that hands a Feature its dependencies without asking where they should be read from " +
                "reads its own links and nothing else, however the Portfolio is configured. That failure is " +
                "silent: the setting is accepted, saved, and ignored, and the column reads the same as it would " +
                "for a Portfolio that had never named a field. Found: " + string.Join(", ", trackersThatDoNotAsk));
        }

        /// <summary>
        /// A reference built anywhere but the selector. The selector's own line is not one, and neither is the
        /// declaration of the type itself.
        /// </summary>
        private static bool AReferenceBuiltByHand(string line)
        {
            var code = line.TrimStart();

            return code.Contains($"new {nameof(FeatureDependencyReference)}(", StringComparison.Ordinal)
                && !code.StartsWith("//", StringComparison.Ordinal)
                && !code.StartsWith('*');
        }

        /// <summary>
        /// Everything outside the one folder that may build a reference. The folder rather than the single
        /// file, because the reconciler rebuilds each reference against the row its Feature landed on - it
        /// carries the source it was handed rather than choosing one, so it decides nothing.
        ///
        /// Reading the selector's own use first is what keeps the scan honest: rename or move the reference
        /// type and the scan would be hunting a name no longer in the code, finding nothing everywhere and
        /// passing for the wrong reason.
        /// </summary>
        private static List<SourceFile> TheBackendSourceOutsideTheSelector()
        {
            var backendSource = TheWholeBackendSource();

            var theSelectorsOwnUse = backendSource
                .Where(file => file.RelativePath == TheFileThatChoosesTheSource)
                .SelectMany(file => LinesMatching(file, AReferenceBuiltByHand))
                .ToList();

            Assert.That(theSelectorsOwnUse, Is.Not.Empty,
                $"{TheFileThatChoosesTheSource} no longer builds a dependency reference - it was renamed, " +
                "moved or deleted - so this scan is looking for something nothing in the code does and would stay " +
                "green whatever the trackers did. Point it at whatever the single selector is called now.");

            return backendSource
                .Where(file => !file.RelativePath.StartsWith(TheOnlyPlaceThatMayRecordTheSource, StringComparison.Ordinal))
                .ToList();
        }

        /// <summary>
        /// The trackers that can hand a Feature something to wait on. A tracker with no Features at all has
        /// nothing for a dependency to run between and is not one of them, so it is not listed and does not
        /// have to ask.
        /// </summary>
        private static List<SourceFile> TheTrackersThatCarryDependencies()
        {
            var trackers = TheWholeBackendSource()
                .Where(file => TrackersThatCarryDependencies.Contains(file.RelativePath))
                .ToList();

            Assert.That(trackers, Has.Count.EqualTo(TrackersThatCarryDependencies.Length),
                "A tracker named here was moved or renamed, so the rule it carries is no longer being enforced. " +
                "Expected: " + string.Join(", ", TrackersThatCarryDependencies));

            return trackers;
        }

        /// <summary>
        /// Finding the circles is half of that decision, so only the place making the decision may ask. A
        /// caller working it out for itself has a second answer whether or not it means to have one.
        /// </summary>
        [Test]
        public void NothingButTheOneDecider_AsksWhichFeaturesWaitOnEachOtherInACircle()
        {
            Types().That().AreNot(typeof(DependencyHonourPolicy)).And()
                .ResideInNamespaceMatching(@"^Lighthouse\.Backend($|\..*)").And()
                // The walk is written as one object with the bookkeeping that has to survive between
                // starting points held inside it, so its own parts depend on it.
                .DoNotHaveFullNameContaining(nameof(DependencyCycleDetector))
                .Should().NotDependOnAny(Types().That().Are(typeof(DependencyCycleDetector)))
                .Because(
                    "Whether a dependency is caught in a circle is part of one decision, made in one place. " +
                    "Anything walking the circles for itself is a second opinion, and the two of them differ " +
                    "the first time either one changes.")
                .Check(Architecture);
        }

        /// <summary>
        /// The decision reads plain facts and nothing else. Something that loads, logs or stores could answer
        /// differently on the second asking - and the whole point is that the screen and the forecast ask the
        /// same question and are told the same thing.
        /// </summary>
        [Test]
        public void TheOneDecider_ReachesNothingThatLoadsOrStoresAnything()
        {
            Types().That().Are(typeof(DependencyHonourPolicy)).Or().Are(typeof(DependencyCycleDetector))
                .Should().NotDependOnAny(Types().That()
                    .ResideInNamespaceMatching(@"^Lighthouse\.Backend\.(Data|Services\.(Implementation|Interfaces)\.Repositories)($|\..*)").Or()
                    .ResideInNamespaceMatching(@"^Microsoft\.EntityFrameworkCore($|\..*)").Or()
                    .ResideInNamespaceMatching(@"^Microsoft\.Extensions\.Logging($|\..*)"))
                .Because(
                    "Everything this decision may see arrives as the facts handed to it. A repository, a " +
                    "database or a log would let it answer one way for the screen and another for a forecast " +
                    "run seconds later, which is the disagreement having one decision exists to prevent.")
                .Check(Architecture);
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
            files.AddRange(TheFrontendFilesThisEpicWrites.Select(WholeFile));

            return files;
        }

        /// <summary>
        /// Every line of backend production source, build output left out. The single-writer scan has to read
        /// all of it rather than the folders this epic added, because the writer it is looking for would be
        /// somewhere else by definition - a connector, the work item service, a controller.
        /// </summary>
        private static List<SourceFile> TheWholeBackendSource()
        {
            var solutionRoot = SolutionRoot();
            var backendRoot = Path.Combine(solutionRoot, BackendProjectDirectory);

            Assert.That(Directory.Exists(backendRoot), Is.True,
                $"{backendRoot} was moved or deleted, so the rule it carries is no longer being enforced.");

            return Directory.EnumerateFiles(backendRoot, "*.cs", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(solutionRoot, file).Replace('\\', '/'))
                .Where(relativePath => !TheBuildWroteIt(relativePath))
                .Select(relativePath => new SourceFile(
                    relativePath,
                    File.ReadAllText(Path.Combine(solutionRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))),
                    FirstLine: 1))
                .ToList();
        }

        private static bool TheBuildWroteIt(string relativePath)
        {
            return relativePath.Contains("/obj/", StringComparison.Ordinal)
                || relativePath.Contains("/bin/", StringComparison.Ordinal);
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

        private static SourceFile WholeFile(string relativePath)
        {
            var file = Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.That(File.Exists(file), Is.True,
                $"{relativePath} was moved or deleted, so the rule it carries is no longer being enforced.");

            return new SourceFile(relativePath, File.ReadAllText(file), FirstLine: 1);
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
