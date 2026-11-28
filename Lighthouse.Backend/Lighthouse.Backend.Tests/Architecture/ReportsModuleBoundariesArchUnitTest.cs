using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Domain;
using Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Services;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.Compliant.Application;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.HoldingADbSet.Application;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.OnlyCatching.Application;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingAnAdapter.Application;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingAnApiType.Application;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingAnUnlistedPlatformType.Application;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingAnUnlistedSetUpType.Application;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingTheData.Application;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingTheRbacService.Application;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingTrackerIntegration.Application;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.TouchingTheUpdateCode.Application;
using Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.TouchingTheUpdateContracts.Application;
using Lighthouse.Backend.ReportsBoundariesFixtures.ApplicationNewcomer;
using Lighthouse.Backend.ReportsBoundariesFixtures.CompositionRoot;
using Lighthouse.Backend.ReportsBoundariesFixtures.DomainNewcomer;
using Lighthouse.Backend.ReportsBoundariesFixtures.ListedCallers;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.Architecture.ReportsLookalikeFixtures;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Reports is a module with rules of its own, checked by the shared module rules with Reports' namespaces and
    /// Reports' wording, and read from the compiled production assembly: each Reports type lives in its layer and
    /// none is left under Services; its Domain reaches only the shared kernel and the base library; code outside the module
    /// reaches its Domain only from the API, the composition root and its own Application, and its Application only from the
    /// API, the composition root and the report repository; its Application reaches no data-layer, API, module adapter,
    /// Tracker integration, RBAC or update-code type and EF Core only along a measured list; and it reaches Flow metrics'
    /// Domain, Licensing, Organisation set-up and the platform infrastructure only along a list measured from the compiled
    /// code. Every list is compared both ways and only shrinks. The same rules run on made-up types: each rule and each
    /// list position names its made-up offender and passes its compliant twin.
    /// </summary>
    [TestFixture]
    public class ReportsModuleBoundariesArchUnitTest
    {
        private const string ReportsDomainNamespace = "Lighthouse.Backend.Modules.Reports.Domain";

        private const string ReportsApplicationNamespace = "Lighthouse.Backend.Modules.Reports.Application";

        private const string ReportsMetricsNamespace = "Lighthouse.Backend.Modules.Reports.Application.Metrics";

        private const string ReportsModule = @"^Lighthouse\.Backend\.Modules\.Reports(\.|$)";

        private const string ReportsDomain = @"^Lighthouse\.Backend\.Modules\.Reports\.Domain(\.|$)";

        private const string ReportsApplication = @"^Lighthouse\.Backend\.Modules\.Reports\.Application(\.|$)";

        private const string TheCompositionRootAndItsNestedTypes = @"^Lighthouse\.Backend\.Program(\+|$)";

        private const string EntityFrameworkCore = @"^Microsoft\.EntityFrameworkCore(\.|$)";

        private const string AnyModuleAdapters = @"^Lighthouse\.Backend\.Modules\.[^.]+\.Adapters(\.|$)";

        private const string TrackerIntegration = @"^Lighthouse\.Backend\.Modules\.TrackerIntegration(\.|$)";

        private const string MadeUpModule = @"^Lighthouse\.Backend\.Modules\.ReportFixtures\.Boundaries(\.|$)";

        private const string MadeUpDomainNamespace = "Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Domain";

        private const string MadeUpDomain = @"^Lighthouse\.Backend\.Modules\.ReportFixtures\.Boundaries\.Domain(\.|$)";

        private const string MadeUpFormerHome = @"^Lighthouse\.Backend\.Modules\.ReportFixtures\.Boundaries\.Services(\.|$)";

        private const string MadeUpApplication = @"^Lighthouse\.Backend\.Modules\.ReportFixtures\.Boundaries\.Application(\.|$)";

        private const string MadeUpOutside = "Lighthouse.Backend.ReportsBoundariesFixtures";

        private const string MadeUpApi = @"^Lighthouse\.Backend\.ReportsBoundariesFixtures\.Api(\.|$)";

        private const string MadeUpController = "Lighthouse.Backend.ReportsBoundariesFixtures.Api.AReportsController";

        private const string AGoneEntry = "Lighthouse.Backend.ReportsBoundariesFixtures.Gone.AFormerReportsCaller";

        private const string AGoneEdge = "Lighthouse.Backend.Modules.Reports.Application.AFormerReportCalendar -> Lighthouse.Backend.Platform.Host.ILighthouseClock";

        private const string ReportRepositoryType = "Lighthouse.Backend.Services.Implementation.Repositories.ReportRepository";

        private const int MinimumReportsRuleConstants = 7;

        private static readonly string[] TypesAModuleLookalikeMightHold = ["Domain.ReportCapPolicy", "Application.ReportQueries", "Application.Metrics.ReportMetricCatalog"];

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly ModuleWording ReportsWording = new("Reports", "the API");

        // The types each Reports layer holds. The lists name every type the layer holds, so a type added to the module is
        // placed here on purpose. The report repository is the one Reports type that stays outside the module.
        private static readonly Dictionary<string, string[]> TypesByLayer = new(StringComparer.Ordinal)
        {
            [ReportsDomainNamespace] = ["ReportCapPolicy", "ReportNoteCaller", "ReportNoteRead", "ReportOwnerGoneException", "ReportSeriesReading", "ReportWindowPolicy"],
            [ReportsApplicationNamespace] =
            [
                "CycleTimePercentiles",
                "IReportBulkCommands",
                "IReportBulkRemover",
                "IReportCommands",
                "IReportMetric",
                "IReportMetricSeries",
                "IReportNoteCommands",
                "IReportNoteQueries",
                "IReportNoteStore",
                "IReportOwnerLookup",
                "IReportOwnerWriteAccess",
                "IReportQueries",
                "IReportReader",
                "IReportTemplate",
                "IReportWriter",
                "PortfolioReportMetricSeries",
                "ReportBulkCommands",
                "ReportBulkRemover",
                "ReportCalendar",
                "ReportCommands",
                "ReportEdit",
                "ReportMetricSeriesByOwner",
                "ReportNoteCommands",
                "ReportNoteQueries",
                "ReportOwnerFacts",
                "ReportOwnerLookup",
                "ReportOwnerRow",
                "ReportQueries",
                "TeamReportMetricSeries",
                "ThenAndNowTemplate",
            ],
            [ReportsMetricsNamespace] =
            [
                "ArrivalsReportMetric",
                "CycleTimeReportMetric",
                "DailySeriesSummary",
                "ReportMetricCatalog",
                "ThroughputReportMetric",
                "WindowedReportMetric",
                "WipReportMetric",
                "WorkItemAgeReportMetric",
            ],
        };

        // The outside types, besides the API and the composition root, that reach Reports' Domain. The list only shrinks.
        private static readonly string[] OutsideTypesReachingTheDomain =
        [
        ];

        // The outside types, besides the API and the composition root, that reach Reports' Application: the repository that
        // implements the Reports reader and writer ports. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheApplication =
        [
            ReportRepositoryType,
        ];

        // The edges from Reports' Application into EF Core, the data layer, the API, any module's adapters, Tracker
        // integration, the RBAC service or the update code: the bulk commands and the bulk remover name the database
        // exceptions they translate into a refusal, the remover also the one it only catches. The list only shrinks.
        private static readonly string[] EdgesOfTheApplicationIntoForbiddenCode =
        [
            "Lighthouse.Backend.Modules.Reports.Application.ReportBulkCommands -> Microsoft.EntityFrameworkCore.DbUpdateException",
            "Lighthouse.Backend.Modules.Reports.Application.ReportBulkRemover -> Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException",
            "Lighthouse.Backend.Modules.Reports.Application.ReportBulkRemover -> Microsoft.EntityFrameworkCore.DbUpdateException",
        ];

        // The edges from Reports' Application into Flow metrics' Domain, Licensing, Organisation set-up and the platform
        // infrastructure: the summary takes its percentile from Flow metrics, the commands and queries ask Licensing whether
        // reports may be used, the owner lookup and the two series read teams and portfolios through the repository port,
        // and six types read the clock. The list only shrinks.
        private static readonly string[] EdgesOfTheApplicationIntoFlowMetricsLicensingSetUpAndPlatform =
        [
            "Lighthouse.Backend.Modules.Reports.Application.PortfolioReportMetricSeries -> Lighthouse.Backend.Platform.Host.ILighthouseClock",
            "Lighthouse.Backend.Modules.Reports.Application.PortfolioReportMetricSeries -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Modules.Reports.Application.ReportBulkCommands -> Lighthouse.Backend.Platform.Host.ILighthouseClock",
            "Lighthouse.Backend.Modules.Reports.Application.ReportCommands -> Lighthouse.Backend.Platform.Host.ILighthouseClock",
            "Lighthouse.Backend.Modules.Reports.Application.ReportOwnerLookup -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Modules.Reports.Application.ReportQueries -> Lighthouse.Backend.Platform.Host.ILighthouseClock",
            "Lighthouse.Backend.Modules.Reports.Application.TeamReportMetricSeries -> Lighthouse.Backend.Platform.Host.ILighthouseClock",
            "Lighthouse.Backend.Modules.Reports.Application.TeamReportMetricSeries -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Modules.Reports.Application.ThenAndNowTemplate -> Lighthouse.Backend.Platform.Host.ILighthouseClock",
        ];

        // Access holds the RBAC administration service, which Reports never asks: who may see a report is the API's decision.
        // The update code is the kernel's update-queue key plus the namespace of the update contracts, read from the contract
        // itself so it follows the contracts wherever they live.
        private static string TheUpdateCode => $"(?:{KernelRules.TheUpdateQueue})|(?:^{Regex.Escape(typeof(IUpdateService).Namespace!)}(\\.|$))";

        private static string TheForbiddenCode =>
            $"(?:{EntityFrameworkCore})|(?:{ApplicationCoreRules.TheData})|(?:{ApplicationCoreRules.TheApi})|(?:{AnyModuleAdapters})|(?:{TrackerIntegration})|(?:{ModuleRules.AccessModule})|(?:{TheUpdateCode})|(?:{ModuleRules.SetUpBehindItsContracts})";

        // Organisation set-up has no namespace of its own yet: its settings and the repositories sit in the top-level
        // Services namespaces, and the platform infrastructure sits under Platform.
        private static string FlowMetricsLicensingSetUpAndPlatform =>
            $"(?:{ModuleRules.FlowMetricsDomain})|(?:{ModuleRules.LicensingModule})|(?:{KernelRules.TheServices})";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(ReportsModuleBoundariesArchUnitTest).Assembly));

        public enum ListedRule
        {
            OutsideTypesReachingTheDomain,
            OutsideTypesReachingTheApplication,
            ApplicationEdgesIntoForbiddenCode,
            ApplicationEdgesIntoFlowMetricsLicensingSetUpAndPlatform,
        }

        private static IEnumerable<string> Layers => TypesByLayer.Keys;

        private static string MadeUpCompositionRoot => typeof(AReportsCompositionRoot).FullName!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(Layers))]
        public void EveryReportsType_LivesInItsLayer_AndNoneIsLeftUnderServices(string layer)
        {
            var types = TypesByLayer[layer];

            var placement = ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, layer, types);

            ThenTheRuleHolds(placement.Check, $"Reports' layer {layer}");
            ThenTheRuleHolds(() => LayerHoldsExactly(ProductionModule.Value, layer, types), $"what Reports' layer {layer} holds");
        }

        [Test]
        public void TheReportsDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, ReportsDomain);

            ThenTheRuleHolds(verdict.Check, "Reports' Domain");
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.ApplicationEdgesIntoForbiddenCode)]
        [TestCase(ListedRule.ApplicationEdgesIntoFlowMetricsLicensingSetUpAndPlatform)]
        public void AReportsListedRule_MatchesWhatTheCompiledCodeReaches_InBothDirections(ListedRule rule)
        {
            var (check, list) = TheProductRule(rule);

            ThenTheRuleHolds(() => check(list), rule.ToString());
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.ApplicationEdgesIntoForbiddenCode)]
        [TestCase(ListedRule.ApplicationEdgesIntoFlowMetricsLicensingSetUpAndPlatform)]
        public void EveryEntryDeletedFromAReportsList_IsNamedAsOffending_AndAnEntryWithNoEdgeIsNamedForDeletion(ListedRule rule)
        {
            var (check, list) = TheProductRule(rule);

            ThenEachDeletedEntryIsNamedAndAGoneEntryIsNamedForDeletion(rule, check, list);
        }

        [Test]
        public void AReportsTypeLeftUnderServices_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twin = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpDomainNamespace, [nameof(AReportBand)], MadeUpFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpDomainNamespace, [nameof(ALeftBehindReportBand)], MadeUpFormerHome);

            ThenTheRuleNamesTheOffenderAndPassesItsTwin(leftBehind.Check, typeof(ALeftBehindReportBand).FullName!, twin.Check, "the layer rule");
        }

        [Test]
        public void ATypeInAReportsLayerThatIsNotOnItsList_IsNamed_AndTheFullListPasses()
        {
            ThenTheRuleNamesTheOffenderAndPassesItsTwin(
                () => LayerHoldsExactly(TestModule.Value, MadeUpDomainNamespace, [nameof(AReportBand)]),
                typeof(AReportBandAskingTheSettings).FullName!,
                () => LayerHoldsExactly(TestModule.Value, MadeUpDomainNamespace, [nameof(AReportBand), nameof(AReportBandAskingTheSettings)]),
                "what the layer holds");
        }

        [Test]
        public void AReportsDomainTypeReachingAService_IsNamed_AndItsCompliantTwinIsNot()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(TestModule.Value, MadeUpDomain);

            ThenTheFailureNamesOnly(verdict.Check, typeof(AReportBandAskingTheSettings).FullName!, [typeof(AReportBand).FullName!], "the Domain rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingTheReportsDomain_IsNamed_AndTheApiTheCompositionRootAndTheListedCallerAreNot()
        {
            var verdict = DomainInboundRule(
                TestModule.Value,
                [typeof(AListedDomainCaller).FullName!],
                OutsideFixtures("Api", "CompositionRoot", "ListedCallers", "DomainNewcomer"));

            ThenTheFailureNamesOnly(
                verdict.Check,
                typeof(ANewcomerReachingTheDomain).FullName!,
                [typeof(AListedDomainCaller).FullName!, MadeUpController, MadeUpCompositionRoot],
                "the Domain inbound rule");
            ThenTheFailureSpeaksOf(verdict.Check, "Reports' Domain holds the module's own calculations");
        }

        [Test]
        public void AListedDomainCallerDeletedFromTheList_IsNamedAsOffending_AndAListedCallerThatNoLongerReachesTheDomainIsNamedForDeletion()
        {
            var outside = OutsideFixtures("Api", "CompositionRoot", "ListedCallers");
            var listed = typeof(AListedDomainCaller).FullName!;

            ThenTheFailureNamesOnly(DomainInboundRule(TestModule.Value, [], outside).Check, listed, [MadeUpController, MadeUpCompositionRoot], "the Domain inbound rule without its listed caller");
            ThenTheGoneEntryIsNamedForDeletion(DomainInboundRule(TestModule.Value, [listed, AGoneEntry], outside).Check, AGoneEntry, "the Domain inbound rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingTheReportsApplication_IsNamed_AndTheApiTheCompositionRootAndTheListedCallerAreNot()
        {
            var verdict = ApplicationInboundRule(
                TestModule.Value,
                [typeof(AListedApplicationCaller).FullName!],
                OutsideFixtures("Api", "CompositionRoot", "ListedCallers", "ApplicationNewcomer"));

            ThenTheFailureNamesOnly(
                verdict.Check,
                typeof(ANewcomerReachingTheApplication).FullName!,
                [typeof(AListedApplicationCaller).FullName!, MadeUpController, MadeUpCompositionRoot],
                "the Application inbound rule");
            ThenTheFailureSpeaksOf(verdict.Check, "Reports' Application is the module's front door for the API");
        }

        [Test]
        public void AListedApplicationCallerDeletedFromTheList_IsNamedAsOffending_AndAListedCallerThatNoLongerReachesTheApplicationIsNamedForDeletion()
        {
            var outside = OutsideFixtures("Api", "CompositionRoot", "ListedCallers");
            var listed = typeof(AListedApplicationCaller).FullName!;

            ThenTheFailureNamesOnly(ApplicationInboundRule(TestModule.Value, [], outside).Check, listed, [MadeUpController, MadeUpCompositionRoot], "the Application inbound rule without its listed caller");
            ThenTheGoneEntryIsNamedForDeletion(ApplicationInboundRule(TestModule.Value, [listed, AGoneEntry], outside).Check, AGoneEntry, "the Application inbound rule");
        }

        [Test]
        public void ATypeThatOnlyCatchesAnExceptionInACatchClause_IsSeenReferencingIt()
        {
            var offender = typeof(AReportServiceOnlyCatchingAnEntityFrameworkCoreException);
            var concurrency = typeof(Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException).FullName!;

            var verdict = ForbiddenCodeRule(TestModule.Value, NamespacePatternOf(offender), []);

            ThenTheFailureNamesOnly(verdict.Check, $"Not on the list, and offending: {Edge(offender, concurrency)}", [], "the forbidden-code rule over a catch-only reference");
        }

        [Test]
        public void AReportsApplicationReachingOnlyALegalNeighbourAndTheClock_PassesBothOutboundRules()
        {
            var compliant = NamespacePatternOf(typeof(ACompliantReportService));
            var clock = Edge(typeof(ACompliantReportService), "Lighthouse.Backend.Platform.Host.ILighthouseClock");

            var forbidden = ForbiddenCodeRule(TestModule.Value, compliant, []);
            var listedEdges = FlowMetricsLicensingSetUpAndPlatformRule(TestModule.Value, compliant, [clock]);

            Assert.That(
                listedEdges.Edges,
                Is.EquivalentTo(new[] { clock }),
                $"Expected the outbound rule to find exactly [{clock}]. Why it matters: a rule that misses an edge proves nothing by passing, and one that counts " +
                "Flow metrics' or Work Items' Application flags a call Reports may make. How to fix: report every edge into the namespaces the rule is handed, " +
                "and nothing in a legal module's Application.");
            ThenTheRuleHolds(forbidden.Check, "Reports' Application towards forbidden code");
            ThenTheRuleHolds(listedEdges.Check, "Reports' Application towards Flow metrics' Domain, Licensing, set-up and the platform infrastructure");
        }

        [TestCaseSource(nameof(ReportsApplicationTypesReachingForbiddenCode))]
        public void AReportsApplicationTypeReachingForbiddenCode_IsNamedWithTheEdge(Type offender, string forbidden)
        {
            var verdict = ForbiddenCodeRule(TestModule.Value, NamespacePatternOf(offender), []);

            ThenTheFailureNamesOnly(verdict.Check, $"Not on the list, and offending: {Edge(offender, forbidden)}", [], "the forbidden-code rule");
            ThenTheFailureSpeaksOf(verdict.Check, "Reports' Application");
        }

        [TestCaseSource(nameof(ReportsApplicationTypesReachingUnlistedEdges))]
        public void AReportsApplicationTypeReachingAnUnlistedFlowMetricsLicensingSetUpOrPlatformType_IsNamedWithTheEdge(Type offender, string unlisted)
        {
            var verdict = FlowMetricsLicensingSetUpAndPlatformRule(TestModule.Value, NamespacePatternOf(offender), []);

            ThenTheFailureNamesOnly(verdict.Check, $"Not on the list, and offending: {Edge(offender, unlisted)}", [], "the Flow metrics, Licensing, set-up and platform rule");
            ThenTheFailureSpeaksOf(verdict.Check, "Reports' Application");
        }

        [Test]
        public void AListedEdgeThatNoLongerExists_IsNamedForDeletion_AndEachListedEdgeDeletedIsNamedAsOffending()
        {
            var offender = typeof(AnApplicationTypeReachingAnUnlistedPlatformType);
            var listed = Edge(offender, "Lighthouse.Backend.Platform.Host.ILighthouseClock");
            var namespaces = NamespacePatternOf(offender);

            ThenTheRuleHolds(() => FlowMetricsLicensingSetUpAndPlatformRule(TestModule.Value, namespaces, [listed]).Check(), "the made-up listed edge");
            ThenTheFailureNamesOnly(FlowMetricsLicensingSetUpAndPlatformRule(TestModule.Value, namespaces, []).Check, $"Not on the list, and offending: {listed}", [], "the rule without its listed edge");
            ThenTheGoneEntryIsNamedForDeletion(FlowMetricsLicensingSetUpAndPlatformRule(TestModule.Value, namespaces, [listed, AGoneEdge]).Check, AGoneEdge, "the Flow metrics, Licensing, set-up and platform rule");
        }

        [Test]
        public void ARuleOverNothingFails_InsteadOfPassingForFree()
        {
            var emptyDomain = @"^Lighthouse\.Backend\.Modules\.ReportFixtures\.Nowhere\.Domain(\.|$)";
            var emptyApplication = @"^Lighthouse\.Backend\.Modules\.ReportFixtures\.Nowhere\.Application(\.|$)";

            var domain = Assert.Throws<AssertionException>(() => ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(TestModule.Value, emptyDomain).Check(), "The Domain rule passed over a namespace that holds no type.");
            var application = Assert.Throws<AssertionException>(() => ForbiddenCodeRule(TestModule.Value, emptyApplication, []).Check(), "The outbound rule passed over a namespace that holds no type.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(domain!.Message, Does.Contain("any type in a namespace matching").And.Contain("Nowhere"), "The Domain rule over nothing must say it checked no type. Why it matters: a rule pointed at a renamed namespace stays green while it guards nothing. How to fix: fail when the examined set is empty.");
                Assert.That(application!.Message, Does.Contain("checked nothing").And.Contain("Nowhere"), "The outbound rule over nothing must say it checked nothing. Why it matters: a rule pointed at a renamed namespace stays green while it guards nothing. How to fix: fail when the examined set is empty.");
            }
        }

        [Test]
        public void NoReportsAlternativeOfTheRuleConstantsMatchesALookalikeNamespace()
        {
            var constants = ReportsRuleConstants(TypesOfTheTestAssemblyOutsideFixtures());

            var loose = ConstantsMatchingALookalike(constants);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(constants, Has.Count.GreaterThanOrEqualTo(MinimumReportsRuleConstants),
                    $"Only {constants.Count} rule constants name Modules.Reports and at least {MinimumReportsRuleConstants} exist (the shared pattern, the Tracker integration, Access and Licensing composites, the membership law and this file's own). " +
                    "Why it matters: a constant that vanished from the check would pass it for free. How to fix: keep every Reports rule constant in the population the check reads.");
                Assert.That(loose, Is.Empty,
                    $"{loose.Count} of the {constants.Count} Reports rule constants also match a namespace that only starts like the module's:" + Environment.NewLine +
                    string.Join(Environment.NewLine, loose) + Environment.NewLine +
                    "Why it matters: a rule that names the module without anchoring its end also constrains a neighbouring namespace, so a type there would be held to Reports' rules, or let through them. " +
                    "How to fix: end the alternative with (\\.|$) or an exact type name.");
            }
        }

        [Test]
        public void AMadeUpReportsConstantWithoutAnAnchoredEnd_IsNamedAsMatchingALookalike_AndItsAnchoredTwinIsNot()
        {
            var constants = ReportsRuleConstants(TypesOfTheTestAssemblyInTheLookalikeFixtures());

            var loose = ConstantsMatchingALookalike(constants);

            Assert.That(
                loose,
                Has.Count.EqualTo(1).And.All.StartWith($"{nameof(AnUnanchoredReportsRule)}.{nameof(AnUnanchoredReportsRule.Rule)}"),
                $"Of {constants.Count} made-up Reports rule constants the check named [{string.Join("; ", loose)}], and should name only the one whose end is not anchored. " +
                "Why it matters: a check blind to a loose end lets a rule spill over onto a neighbouring namespace. How to fix: match every alternative against the lookalike namespace.");
        }

        private static IEnumerable<TestCaseData> ReportsApplicationTypesReachingForbiddenCode()
        {
            yield return new TestCaseData(typeof(AReportServiceHoldingADbSet), "Microsoft.EntityFrameworkCore.DbSet`1")
                .SetName("AReportsApplicationTypeHoldingAnEntityFrameworkCoreDbSet_IsNamed");
            yield return new TestCaseData(typeof(AReportServiceReachingTheData), "Lighthouse.Backend.Data.LighthouseAppContext")
                .SetName("AReportsApplicationTypeReachingTheDataLayer_IsNamed");
            yield return new TestCaseData(typeof(AReportServiceReachingAnApiType), "Lighthouse.Backend.API.DTO.YardstickDto")
                .SetName("AReportsApplicationTypeReachingTheApi_IsNamed");
            yield return new TestCaseData(typeof(AReportServiceReachingAnAdapter), "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater")
                .SetName("AReportsApplicationTypeReachingAModuleAdapter_IsNamed");
            yield return new TestCaseData(typeof(AReportServiceReachingTrackerIntegration), "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IWorkTrackingConnectorFactory")
                .SetName("AReportsApplicationTypeReachingTrackerIntegration_IsNamed");
            yield return new TestCaseData(typeof(AReportServiceReachingTheRbacService), "Lighthouse.Backend.Modules.Access.Contracts.IRbacAdministrationService")
                .SetName("AReportsApplicationTypeReachingTheRbacService_IsNamed");
            yield return new TestCaseData(typeof(AReportServiceTouchingTheUpdateCode), "Lighthouse.Backend.Platform.UpdateQueue.UpdateKey")
                .SetName("AReportsApplicationTypeReachingTheUpdateCode_IsNamed");
            yield return new TestCaseData(typeof(AReportServiceReachingTheUpdateContracts), "Lighthouse.Backend.Services.Interfaces.Update.IUpdateService")
                .SetName("AReportsApplicationTypeReachingTheUpdateContracts_IsNamed");
        }

        private static IEnumerable<TestCaseData> ReportsApplicationTypesReachingUnlistedEdges()
        {
            yield return new TestCaseData(typeof(AnApplicationTypeReachingAnUnlistedSetUpType), "Lighthouse.Backend.Services.Interfaces.IFeatureListService")
                .SetName("AReportsApplicationTypeReachingAnUnlistedSetUpType_IsNamed");
            yield return new TestCaseData(typeof(AnApplicationTypeReachingAnUnlistedPlatformType), "Lighthouse.Backend.Platform.Host.ILighthouseClock")
                .SetName("AReportsApplicationTypeReachingAnUnlistedPlatformType_IsNamed");
        }

        private static (Action<IReadOnlyCollection<string>> Check, string[] List) TheProductRule(ListedRule rule)
        {
            var module = ProductionModule.Value;
            var theApi = ApplicationCoreRules.TheApi;

            return rule switch
            {
                ListedRule.OutsideTypesReachingTheDomain => (
                    listed => DomainInboundRule(module, listed, ModuleRules.AnyNamespace, ReportsDomain, ReportsModule, theApi, TheCompositionRootAndItsNestedTypes).Check(),
                    OutsideTypesReachingTheDomain),
                ListedRule.OutsideTypesReachingTheApplication => (
                    listed => ApplicationInboundRule(module, listed, ModuleRules.AnyNamespace, ReportsApplication, ReportsModule, theApi, TheCompositionRootAndItsNestedTypes).Check(),
                    OtherOutsideTypesReachingTheApplication),
                ListedRule.ApplicationEdgesIntoForbiddenCode => (
                    listed => ForbiddenCodeRule(module, ReportsApplication, listed).Check(),
                    EdgesOfTheApplicationIntoForbiddenCode),
                ListedRule.ApplicationEdgesIntoFlowMetricsLicensingSetUpAndPlatform => (
                    listed => FlowMetricsLicensingSetUpAndPlatformRule(module, ReportsApplication, listed).Check(),
                    EdgesOfTheApplicationIntoFlowMetricsLicensingSetUpAndPlatform),
                _ => throw new ArgumentOutOfRangeException(nameof(rule)),
            };
        }

        private static InboundVerdict DomainInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string outside)
            => DomainInboundRule(module, listed, outside, MadeUpDomain, MadeUpModule, MadeUpApi, RootPattern(MadeUpCompositionRoot));

        // The API and the composition root reach the Domain by right, so they are set aside by namespace and by type name.
        private static InboundVerdict DomainInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string outside, string domain, string moduleNamespaces, string legalCallers, string compositionRoot)
        {
            var verdict = ModuleRules.DomainIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, domain, outside, ReportsWording);
            var legal = IlDependencies.TypesIn(module, legalCallers).ToHashSet(StringComparer.Ordinal);

            return verdict with
            {
                Dependents = [.. verdict.Dependents.Where(dependent => !legal.Contains(dependent.TypeName) && !Regex.IsMatch(dependent.TypeName, compositionRoot, RegexOptions.None, PatternTimeout))],
            };
        }

        private static InboundVerdict ApplicationInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string outside)
            => ApplicationInboundRule(module, listed, outside, MadeUpApplication, MadeUpModule, MadeUpApi, RootPattern(MadeUpCompositionRoot));

        private static InboundVerdict ApplicationInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string outside, string application, string moduleNamespaces, string legalCallers, string compositionRoot)
            => ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, application, legalCallers, compositionRoot, outside, ReportsWording);

        private static OutboundVerdict ForbiddenCodeRule(ModuleDefinition module, string application, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                application,
                TheForbiddenCode,
                listed,
                $"Types in Reports' Application, namespaces matching \"{application}\", should not depend on any type in namespaces matching " +
                $"\"{TheForbiddenCode}\" except through the listed edges, because Reports' Application builds, reads and removes a team's or portfolio's " +
                "reports and returns Models and its own result types. An Application type that holds a DbSet or the database context, shapes " +
                "an API type, runs a module's adapter, calls Tracker integration, asks the RBAC service who may do what or queues an update ties Reports " +
                "to how the rest of the system stores, shows, guards and fetches its data. Read through a repository port, ask Flow metrics through its " +
                "Application, and return a Models type.");

        private static OutboundVerdict FlowMetricsLicensingSetUpAndPlatformRule(ModuleDefinition module, string application, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                application,
                ModuleRules.WithoutTheContextsOf(FlowMetricsLicensingSetUpAndPlatform, "FlowMetrics", "Licensing"),
                listed,
                $"Types in Reports' Application, namespaces matching \"{application}\", should reach Flow metrics' Domain, Licensing, Organisation set-up and the " +
                $"platform infrastructure, namespaces matching \"{FlowMetricsLicensingSetUpAndPlatform}\", only through the listed edges, because each of them is another " +
                "context's decision: how a percentile is calculated, whether reports may be used, which teams and portfolios exist and which clock applies. " +
                "Every new edge spreads one of those decisions into Reports. Ask where the listed edge already asks, or add the port to the owning context.");

        private static void LayerHoldsExactly(ModuleDefinition module, string layerNamespace, IReadOnlyCollection<string> typeNames)
        {
            var held = IlDependencies.TypesIn(module, $"^{Regex.Escape(layerNamespace)}$")
                .Select(fullName => fullName.Split('+')[0])
                .Distinct(StringComparer.Ordinal);

            InlineBaseline.AssertMatches(
                held,
                typeNames.Select(name => $"{layerNamespace}.{name}"),
                $"Namespace \"{layerNamespace}\" should hold exactly the types its list names, because the list is where a type is placed in " +
                "Reports' layer on purpose. A type that lands in the layer unlisted was placed there without that decision. Add it to the " +
                "layer's list if it belongs to Reports, or move it to the module that owns it.");
        }

        // The sweep keeps a bare module prefix to pick out the alternatives it checks; it is a selector, not a rule.
        private static List<Type> TypesOfTheTestAssemblyOutsideFixtures()
            => [.. typeof(ReportsModuleBoundariesArchUnitTest).Assembly.GetTypes()
                .Where(type => type.Namespace?.Split('.').Any(part => part.EndsWith("Fixtures", StringComparison.Ordinal)) != true)
                .Where(type => (type.DeclaringType ?? type) != typeof(ReportsRulesNameOnlyTheModuleArchUnitTest))];

        private static List<Type> TypesOfTheTestAssemblyInTheLookalikeFixtures()
            => [.. typeof(ReportsModuleBoundariesArchUnitTest).Assembly.GetTypes().Where(type => type.Namespace == typeof(AnUnanchoredReportsRule).Namespace)];

        private static List<(string Field, string Value)> ReportsRuleConstants(IEnumerable<Type> types)
            => [.. from type in types
                   where !type.ContainsGenericParameters
                   from field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                   where field.FieldType == typeof(string) && (field.IsLiteral || field.IsInitOnly)
                   let value = (field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null)) as string
                   where value is not null && value.Contains(@"Modules\.Reports", StringComparison.Ordinal)
                   select (Field: $"{(type.DeclaringType ?? type).Name}.{field.Name}", Value: value)];

        private static List<string> ConstantsMatchingALookalike(IEnumerable<(string Field, string Value)> constants)
        {
            var lookalikes = TypesAModuleLookalikeMightHold
                .Select(type => string.Concat("Lighthouse.Backend.Modules.", "Reports", "Lookalike", ".", type))
                .ToList();

            return [.. from constant in constants
                       let lookalike = lookalikes.FirstOrDefault(name => Regex.IsMatch(name, constant.Value, RegexOptions.CultureInvariant, PatternTimeout))
                       where lookalike is not null
                       select $"{constant.Field} = {constant.Value} matches {lookalike}"];
        }

        private static string OutsideFixtures(params string[] namespaces)
            => $"^{Regex.Escape(MadeUpOutside)}\\.({string.Join('|', namespaces)})(\\.|$)";

        private static string RootPattern(string compositionRoot) => $"^{Regex.Escape(compositionRoot)}(\\+|$)";

        private static string NamespacePatternOf(Type type) => $"^{Regex.Escape(type.Namespace!)}(\\.|$)";

        private static string Edge(Type dependent, string target) => $"{dependent.FullName} -> {target}";

        private static void ThenTheRuleHolds(Action check, string what)
            => Assert.DoesNotThrow(
                () => check(),
                $"The rule for {what} failed; the failure above names each type or edge to fix. Why it matters: Reports keeps its shape only " +
                "while every list matches what the compiled code reaches, in both directions. How to fix: undo the new dependency, or delete " +
                "the entry the failure names as no longer offending.");

        private static void ThenEachDeletedEntryIsNamedAndAGoneEntryIsNamedForDeletion(ListedRule rule, Action<IReadOnlyCollection<string>> check, string[] list)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var entry in list)
                {
                    var failure = Assert.Throws<AssertionException>(
                        () => check([.. list.Where(listed => listed != entry)]),
                        $"{rule} passed with {entry} deleted from its list. Why it matters: an edge the list misses would go in unseen. How to " +
                        "fix: report every offender that is not on the list.");

                    Assert.That(failure?.Message, Does.Contain("Not on the list, and offending: ").And.Contain(entry), $"{rule} must name {entry} as offending once it is off the list.");
                }

                ThenTheGoneEntryIsNamedForDeletion(() => check([.. list, AGoneEntry]), AGoneEntry, rule.ToString());
            }
        }

        private static void ThenTheGoneEntryIsNamedForDeletion(Action check, string gone, string what)
        {
            var failure = Assert.Throws<AssertionException>(
                () => check(),
                $"{what} passed with {gone} on its list although nothing like it is in the code. Why it matters: a list that keeps a gone entry " +
                "lets that dependency come back unseen. How to fix: compare the list with what was found in both directions.");

            Assert.That(failure?.Message, Does.Contain($"On the list, but no longer offending: {gone}. Good - delete it"), $"{what} must name {gone} for deletion.");
        }

        private static void ThenTheFailureNamesOnly(Action check, string offender, IReadOnlyCollection<string> notNamed, string what)
        {
            var failure = Assert.Throws<AssertionException>(
                () => check(),
                $"{what} passed although {offender} breaks it. Why it matters: a rule that cannot fail on a made-up offender guards nothing. " +
                "How to fix: report every type or edge the rule forbids.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Message, Does.Contain(offender), $"{what} must name {offender}.");

                foreach (var compliant in notNamed)
                {
                    Assert.That(failure.Message, Does.Not.Match($"{Regex.Escape(compliant)}(?![A-Za-z0-9+`])"), $"{what} must not name {compliant}, which is allowed.");
                }
            }
        }

        private static void ThenTheRuleNamesTheOffenderAndPassesItsTwin(Action offending, string offender, Action twin, string what)
        {
            ThenTheFailureNamesOnly(offending, offender, [], what);

            Assert.DoesNotThrow(() => twin(), $"{what} failed on a compliant twin. Why it matters: a rule that fails on compliant code gets switched off. How to fix: report only what the rule forbids.");
        }

        private static void ThenTheFailureSpeaksOf(Action check, string phrase)
        {
            var failure = Assert.Throws<AssertionException>(() => check(), "The rule passed with an offender; there is no failure text to read.");

            Assert.That(
                failure!.Message,
                Does.Contain(phrase).And.Not.Contain("Flow metrics' Application is").And.Not.Contain("Flow metrics' Domain holds"),
                $"A failing Reports rule must speak of Reports in its own words (\"{phrase}\"), not of another module. Why it matters: the reader is sent " +
                "to the wrong module. How to fix: hand the rule Reports' wording.");
        }
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Domain
{
    internal sealed record AReportBand(int Low, int High)
    {
        public int Width => High - Low;
    }

    internal sealed class AReportBandAskingTheSettings(global::Lighthouse.Backend.Services.Interfaces.IFeatureListService settings)
    {
        public int Describe() => settings.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Services
{
    internal sealed record ALeftBehindReportBand(int Low, int High);
}

namespace Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Application
{
    internal static class AReportAnswerService
    {
        public static int Answer(int low) => new global::Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Domain.AReportBand(low, low + 3).Width;
    }
}

namespace Lighthouse.Backend.ReportsBoundariesFixtures.Api
{
    internal static class AReportsController
    {
        public static int Get(int low) => global::Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Application.AReportAnswerService.Answer(low)
            + new global::Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Domain.AReportBand(low, low + 1).Width;
    }
}

namespace Lighthouse.Backend.ReportsBoundariesFixtures.CompositionRoot
{
    internal static class AReportsCompositionRoot
    {
        public static int Register(int low) => global::Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Application.AReportAnswerService.Answer(low);

        internal static class Registrations
        {
            public static int Register(int low) => new global::Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Domain.AReportBand(low, low + 2).Width;
        }
    }
}

namespace Lighthouse.Backend.ReportsBoundariesFixtures.ListedCallers
{
    internal static class AListedDomainCaller
    {
        public static int Answer(int low) => new global::Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Domain.AReportBand(low, low + 4).Width;
    }

    internal static class AListedApplicationCaller
    {
        public static int Answer(int low) => global::Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Application.AReportAnswerService.Answer(low);
    }
}

namespace Lighthouse.Backend.ReportsBoundariesFixtures.DomainNewcomer
{
    internal static class ANewcomerReachingTheDomain
    {
        public static int Answer(int low) => new global::Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Domain.AReportBand(low, low + 5).Width;
    }
}

namespace Lighthouse.Backend.ReportsBoundariesFixtures.ApplicationNewcomer
{
    internal static class ANewcomerReachingTheApplication
    {
        public static async Task<int> AnswerAsync(int low)
        {
            await Task.Yield();

            return global::Lighthouse.Backend.Modules.ReportFixtures.Boundaries.Application.AReportAnswerService.Answer(low);
        }
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.Compliant.Application
{
    internal sealed class ACompliantReportService(
        global::Lighthouse.Backend.Platform.Host.ILighthouseClock clock,
        global::Lighthouse.Backend.Modules.FlowMetrics.Contracts.ITeamMetricsService teamMetrics,
        global::Lighthouse.Backend.Modules.FlowMetrics.Contracts.IPortfolioMetricsService portfolioMetrics)
    {
        public string Describe() => $"{clock.GetHashCode()} {teamMetrics.GetHashCode()} {portfolioMetrics.GetHashCode()}";
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.HoldingADbSet.Application
{
    internal sealed class AReportServiceHoldingADbSet(global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Delivery> deliveries)
    {
        public global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Delivery> Deliveries { get; } = deliveries;
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.OnlyCatching.Application
{
    internal static class AReportServiceOnlyCatchingAnEntityFrameworkCoreException
    {
        public static bool Attempt(Action work)
        {
            try
            {
                work();
                return true;
            }
            catch (global::Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                return false;
            }
        }
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingTheData.Application
{
    internal sealed class AReportServiceReachingTheData(global::Lighthouse.Backend.Data.LighthouseAppContext context)
    {
        public int Describe() => context.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingAnApiType.Application
{
    internal static class AReportServiceReachingAnApiType
    {
        public static string Describe() => typeof(global::Lighthouse.Backend.API.DTO.YardstickDto).Name;
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingAnAdapter.Application
{
    internal static class AReportServiceReachingAnAdapter
    {
        public static string Describe() => typeof(global::Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater).Name;
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingTrackerIntegration.Application
{
    internal sealed class AReportServiceReachingTrackerIntegration(global::Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IWorkTrackingConnectorFactory connectors)
    {
        public int Describe() => connectors.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingTheRbacService.Application
{
    internal sealed class AReportServiceReachingTheRbacService(global::Lighthouse.Backend.Modules.Access.Contracts.IRbacAdministrationService rbac)
    {
        public int Describe() => rbac.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.TouchingTheUpdateCode.Application
{
    internal static class AReportServiceTouchingTheUpdateCode
    {
        public static string Describe() => typeof(global::Lighthouse.Backend.Platform.UpdateQueue.UpdateKey).Name;
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.TouchingTheUpdateContracts.Application
{
    internal sealed class AReportServiceReachingTheUpdateContracts(global::Lighthouse.Backend.Services.Interfaces.Update.IUpdateService updates)
    {
        public int Describe() => updates.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingAnUnlistedSetUpType.Application
{
    internal sealed class AnApplicationTypeReachingAnUnlistedSetUpType(global::Lighthouse.Backend.Services.Interfaces.IFeatureListService settings)
    {
        public int Describe() => settings.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.ReportFixtures.BoundariesOutbound.ReachingAnUnlistedPlatformType.Application
{
    internal sealed class AnApplicationTypeReachingAnUnlistedPlatformType(global::Lighthouse.Backend.Platform.Host.ILighthouseClock clock)
    {
        public int Describe() => clock.GetHashCode();
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ReportsLookalikeFixtures
{
    // Made-up rule constants; the unanchored one is assembled from pieces so its text is not a rule pattern of its own.
    internal static class AnUnanchoredReportsRule
    {
        public const string Rule = @"^Lighthouse\.Backend\.Modules\." + "Reports";
    }

    internal static class AnAnchoredReportsRule
    {
        public const string Rule = @"^Lighthouse\.Backend\.Modules\." + @"Reports(\.|$)";
    }
}
