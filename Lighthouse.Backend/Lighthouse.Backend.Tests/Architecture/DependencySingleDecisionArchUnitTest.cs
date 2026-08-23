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

        // The trackers that return Features and can carry something for one to wait on. ServiceNow returns
        // no Features at all, so a dependency has nothing to run between there and it is not listed.
        private static readonly string[] TrackersThatCarryDependencies =
        [
            "Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs",
            "Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs",
            "Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingConnector.cs",
            "Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Csv/CsvWorkTrackingConnector.cs",
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

        private const string TheFrontendsCopyOfTheReasons =
            "Lighthouse.Frontend/src/models/FeatureDependency.ts";

        private const string TheNameOfTheFrontendsList = "NOT_HONOURED_REASONS";

        private const string TheAttributeThatCharges = "LicenseGuard";

        private const string ThePayloadTheCountRidesOn = "FeatureDto";

        // Declaring it is legitimate and reading it anywhere but the decision is not, so the rule keys on
        // the dot that tells them apart.
        private const string TheLicenceFlagOnlyTheDecisionMayRead = "HasPremiumLicence";

        private const string TheOnlyFileThatMayReadTheLicenceFlag =
            "Lighthouse.Backend/Services/Implementation/Dependencies/DependencyHonourPolicy.cs";

        private const string TheOnlyFileThatMayAskWhetherTheInstanceHasPaid =
            "Lighthouse.Backend/Services/Implementation/Dependencies/DependencyDecision.cs";

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
        /// The forecast now acts on dependencies, and reaches exactly two things to do it: the port it asks,
        /// and the plain answer that port hands back. Everything else this Epic owns stays out of reach - the
        /// decision itself, the walk that finds circles, the reconciler, the licence. A simulation that could
        /// reach any of those could decide for itself what a dependency means, and then the warning a reader
        /// is shown and the date they are given come from two different answers to one question.
        /// </summary>
        /// <remarks>
        /// This replaced a rule saying the forecast and this Epic knew nothing of each other at all. That
        /// rule was the door this work had to come through, and it said so: it held while storing a
        /// dependency was not allowed to move a date. What replaces it is not weaker, it is narrower - two
        /// named types instead of none, and nothing between them that could answer a question twice.
        /// </remarks>
        [Test]
        public void TheForecast_ReachesOnlyThePortItAsksAndTheAnswerItIsGiven()
        {
            var theForecastItself = Types().That()
                .Are(typeof(ForecastService)).Or()
                .Are(typeof(SimulationResult)).Or()
                .Are(typeof(Lighthouse.Backend.Services.Implementation.RandomNumberService));

            var whatTheForecastMayReach = Types().That()
                .Are(typeof(IWhatTheForecastWaitsFor)).Or()
                .Are(typeof(ForecastWaits));

            var whatThisEpicAdded = Types().That()
                .ResideInNamespaceMatching(TheNamespaceThisEpicOwns).And()
                .AreNot(typeof(IWhatTheForecastWaitsFor)).And()
                .AreNot(typeof(ForecastWaits));

            const string reason =
                "The forecast asks one port and reads one plain answer. Anything else this Epic added is a " +
                "second opinion waiting to happen: a licence read, a circle walk or the decision itself, " +
                "sitting inside a simulation, is a place where what a dependency does gets worked out for a " +
                "second time. Ask through the port, and let the one decision behind it answer.";

            theForecastItself.Should().NotDependOnAny(whatThisEpicAdded).Because(reason).Check(Architecture);

            whatThisEpicAdded.Should().NotDependOnAny(theForecastItself)
                .Because(
                    "What a Feature waits on is decided from facts a caller already holds. A decision that " +
                    "could reach into the simulation could answer differently depending on how far a run had " +
                    "got, and a screen asking the same question seconds later would be told something else.")
                .Check(Architecture);

            // Without this the rule above would pass most loudly on a forecast that had stopped asking about
            // dependencies altogether - which is the one outcome it is not there to permit.
            Types().That().Are(typeof(ForecastService))
                .Should().DependOnAny(whatTheForecastMayReach)
                .Because(
                    "The forecast has to ask, or the dates it produces ignore every dependency the product " +
                    "shows on screen and nothing in this file would say so.")
                .Check(Architecture);
        }

        /// <summary>
        /// The licence flag arrives on the decision's input, and the decision is the only thing that may read
        /// it. Declaring it is fine; reading it anywhere else is not, and the difference is exactly one dot.
        /// Nothing else catches this - the four names the licence scan looks for are all about asking a
        /// service, and this one is a property already sitting in the room.
        /// </summary>
        /// <remarks>
        /// This replaced a rule saying the flag was read by nobody at all, which held while a dependency
        /// could not change a date. Now that one can, the invariant is not that the licence goes unread but
        /// that it is read once: a second reader is how a warning promising that a purchase moves a date and
        /// a forecast that never moves it come to be shipped together.
        /// </remarks>
        [Test]
        public void NothingButTheOneDecision_ReadsTheLicenceFlagItWasHanded()
        {
            var theDecisionsOwnRead = TheSourceThisEpicAdded()
                .Where(file => file.RelativePath == TheOnlyFileThatMayReadTheLicenceFlag)
                .SelectMany(file => LinesMatching(file, ReadsTheLicenceFlag))
                .ToList();

            var readsElsewhere = TheSourceThisEpicAdded()
                .Where(file => file.RelativePath != TheOnlyFileThatMayReadTheLicenceFlag)
                .SelectMany(file => LinesMatching(file, ReadsTheLicenceFlag))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(theDecisionsOwnRead, Is.Not.Empty,
                    $"{TheOnlyFileThatMayReadTheLicenceFlag} no longer reads " +
                    $"'{TheLicenceFlagOnlyTheDecisionMayRead}', so this scan is hunting something nothing in the " +
                    "code does and would pass whatever anybody else read. Point it at wherever the decision " +
                    "reads the licence now.");

                Assert.That(readsElsewhere, Is.Empty,
                    $"'{TheLicenceFlagOnlyTheDecisionMayRead}' says whether this instance has paid for a " +
                    "dependency to change a date, and one place answers that. A second reader is a second " +
                    "answer, and the day they differ a reader is told a purchase would move a date that stays " +
                    "where it is. Found: " + string.Join(", ", readsElsewhere));
            }
        }

        private static bool ReadsTheLicenceFlag(string line)
            => line.Contains($".{TheLicenceFlagOnlyTheDecisionMayRead}", StringComparison.Ordinal);

        private static bool AsksThePrice(string line)
            => TheWordsThatWouldMeanAPriceWasAsked.Any(word => line.Contains(word, StringComparison.Ordinal));

        /// <summary>
        /// One place decides whether Lighthouse can act on a dependency. A second implementation would let
        /// the warning a user reads and the forecast that acts on the same dependency disagree, and the two
        /// would be discovered by a person noticing a screenshot did not match a date.
        /// </summary>
        /// <remarks>
        /// Exactly one, tightened from at most one now that the second reader has shipped. The weaker form
        /// was waiting for the forecast to start consuming this decision; it does, so a run with no decider
        /// at all is no longer a state anybody is working towards - it is a forecast that quietly ignores
        /// every dependency on screen.
        /// </remarks>
        [Test]
        public void ExactlyOnePlace_DecidesWhetherADependencyCanBeActedOn()
        {
            var deciders = Architecture.Classes
                .Where(candidate => candidate.ImplementedInterfaces
                    .Any(implemented => implemented.FullName == typeof(IDependencyHonourPolicy).FullName))
                .Select(candidate => candidate.FullName)
                .ToList();

            Assert.That(deciders, Has.Count.EqualTo(1),
                "A second place deciding whether a dependency can be acted on is how a warning on screen ends " +
                "up disagreeing with what a forecast actually did, and no place at all is a forecast ignoring " +
                "every dependency the product shows. Found: " + string.Join(", ", deciders));
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
        /// Reading what a Feature waits on is free; letting it change a date is not. One file asks the
        /// instance whether it has paid, and hands the answer to the decision as a fact among the others.
        /// Everything else - every screen, every refresh, the forecast itself - is told what was decided and
        /// never asks the price for itself.
        /// </summary>
        /// <remarks>
        /// This replaced a rule saying nothing in this epic asked at all, which held while every screen it
        /// owned was free. The half that has not changed is the second assertion: the count still rides on a
        /// payload every list reads, and putting a paid gate in front of that route would take the whole
        /// Feature list away from an unlicensed instance to hide one number.
        /// </remarks>
        [Test]
        public void NothingButTheOnePlace_AsksWhetherTheInstanceHasPaid()
        {
            var theOnePlacesOwnAsk = TheSourceThisEpicAdded()
                .Where(file => file.RelativePath == TheOnlyFileThatMayAskWhetherTheInstanceHasPaid)
                .SelectMany(file => LinesMatching(file, AsksThePrice))
                .ToList();

            var licenceWords = TheSourceThisEpicAdded()
                .Where(file => file.RelativePath != TheOnlyFileThatMayAskWhetherTheInstanceHasPaid)
                .SelectMany(file => LinesMatching(file, AsksThePrice))
                .ToList();

            var chargedRoutes = LinesMatching(TheFileThatHandsOutTheCount, TheRouteHandsOutTheCount);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(theOnePlacesOwnAsk, Is.Not.Empty,
                    $"{TheOnlyFileThatMayAskWhetherTheInstanceHasPaid} no longer asks whether this instance has " +
                    "paid, so this scan is hunting something nothing in the code does and would stay green " +
                    "whoever started asking. Point it at whatever asks now.");

                Assert.That(licenceWords, Is.Empty,
                    "One file asks the instance whether it has paid and hands the answer to the decision. A " +
                    "second asker is a second answer to a question that has one, and the two of them differ the " +
                    "first time either one changes. Found: " + string.Join(", ", licenceWords));

                Assert.That(chargedRoutes, Is.Empty,
                    "The count of what a Feature waits on rides along on the Feature payload that every list " +
                    "reads, and that payload is free. Putting a route that hands it out behind a paid gate would " +
                    "take the whole Feature list away from an unlicensed instance to hide one number. Found: " +
                    string.Join(", ", chargedRoutes));
            }
        }

        /// <summary>
        /// The reasons exist twice - once as the enum the server sends, once as the list the browser accepts -
        /// and the browser's copy is a closed set it validates against. So a reason added on the server and
        /// not here does not degrade: the payload fails to decode and the whole Features view goes blank,
        /// with a console error naming a value rather than anything about dependencies.
        ///
        /// This Epic churns the set on purpose - it added two reasons and the next slice deletes one - which
        /// is exactly when a pair of lists drifts.
        /// </summary>
        [Test]
        public void TheReasonsTheServerSends_AreTheOnesTheBrowserAccepts()
        {
            var asTheBrowserHasThem = TheFrontendsListOfReasons();

            Assert.That(asTheBrowserHasThem, Is.EquivalentTo(Enum.GetNames<NotHonouredReason>()),
                $"{TheFrontendsCopyOfTheReasons} and the {nameof(NotHonouredReason)} enum have drifted apart. " +
                "The browser validates against its copy, so a reason only the server knows about does not " +
                "read oddly - it stops the Feature list decoding at all. Found in the browser's copy: " +
                string.Join(", ", asTheBrowserHasThem));
        }

        /// <summary>
        /// Read out of the source rather than off a response, because the drift this catches is committed
        /// long before anything is running.
        /// </summary>
        private static List<string> TheFrontendsListOfReasons()
        {
            var source = WholeFile(TheFrontendsCopyOfTheReasons).Source;
            var declaration = source.IndexOf(TheNameOfTheFrontendsList, StringComparison.Ordinal);

            Assert.That(declaration, Is.GreaterThanOrEqualTo(0),
                $"{TheNameOfTheFrontendsList} is no longer in {TheFrontendsCopyOfTheReasons}, so this scan is " +
                "looking for something that is not there and would pass whatever the two sides said. Point it " +
                "at whatever the browser validates against now.");

            var opening = source.IndexOf('[', declaration);
            var closing = source.IndexOf(']', opening);

            // Splitting on the quote leaves every quoted name in an odd position and everything between
            // them - commas, newlines, indentation - in an even one.
            return source[opening..closing]
                .Split('"')
                .Where((_, position) => position % 2 == 1)
                .ToList();
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
                new Lighthouse.Backend.Tests.TestDoubles.SeededRandomNumberService(TheSeedBothRunsShare),
                Mock.Of<ILogger<ForecastService>>(),
                teamMetricsService.Object,
                featureRepository.Object,
                new Lighthouse.Backend.Tests.TestDoubles.NothingWaitsForAnything(),
                new Lighthouse.Backend.Tests.TestDoubles.DrawsFromAPinnedStartingNumber(TheSeedBothRunsShare),
                Lighthouse.Backend.Models.Forecast.ForecastSimulationLimits.Default);

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

        private sealed record SourceFile(string RelativePath, string Source, int FirstLine);
    }
}
