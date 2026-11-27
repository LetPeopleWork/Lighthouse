using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Domain;
using Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Services;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.Compliant.Application;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.HoldingADbSet.Application;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnAdapter.Application;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnApiType.Application;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnUnlistedAccessType.Application;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnUnlistedPlatformType.Application;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnUnlistedRepository.Application;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnUnlistedSetUpType.Application;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingTheData.Application;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingTrackerIntegration.Application;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.TouchingTheUpdateCode.Application;
using Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.TouchingTheUpdateContracts.Application;
using Lighthouse.Backend.SizingBoundariesFixtures.ApplicationNewcomer;
using Lighthouse.Backend.SizingBoundariesFixtures.CompositionRoot;
using Lighthouse.Backend.SizingBoundariesFixtures.DomainNewcomer;
using Lighthouse.Backend.SizingBoundariesFixtures.ListedCallers;
using Lighthouse.Backend.Tests.Architecture.RefinementLookalikeFixtures;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Refinement is a module with rules of its own, checked by the shared module rules with Refinement's namespaces and
    /// Refinement's wording, and read from the compiled production assembly: each Refinement type lives in its layer and
    /// none is left under Services; its Domain reaches only the shared kernel and the base library; code outside the module
    /// reaches its Domain only from the API, the composition root and its own Application, and its Application only from the
    /// API and the composition root; its Application reaches no EF Core, data-layer, API, module adapter, Tracker integration
    /// or update-code type; and it reaches Access, Organisation set-up, the Platform lane and the repositories only along a
    /// list measured from the compiled code. Every list is compared both ways and only shrinks. The same rules run on
    /// made-up types: each rule and each list position names its made-up offender and passes its compliant twin.
    /// </summary>
    /// <remarks>
    /// The IL reader does not see types used only in a catch clause, an event or a named attribute argument.
    /// </remarks>
    [TestFixture]
    public class RefinementModuleBoundariesArchUnitTest
    {
        private const string RefinementDomainNamespace = "Lighthouse.Backend.Modules.Refinement.Domain";

        private const string RefinementApplicationNamespace = "Lighthouse.Backend.Modules.Refinement.Application";

        private const string RefinementDomain = @"^Lighthouse\.Backend\.Modules\.Refinement\.Domain(\.|$)";

        private const string RefinementApplication = @"^Lighthouse\.Backend\.Modules\.Refinement\.Application(\.|$)";

        private const string TheCompositionRootAndItsNestedTypes = @"^Lighthouse\.Backend\.Program(\+|$)";

        private const string EntityFrameworkCore = @"^Microsoft\.EntityFrameworkCore(\.|$)";

        private const string AnyModuleAdapters = @"^Lighthouse\.Backend\.Modules\.[^.]+\.Adapters(\.|$)";

        private const string TrackerIntegration = @"^Lighthouse\.Backend\.Modules\.TrackerIntegration(\.|$)";

        private const string TheFormerAccessHome = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Auth|Authorization)(\.|$)";

        private const string MadeUpModule = @"^Lighthouse\.Backend\.Modules\.SizingFixtures\.Boundaries(\.|$)";

        private const string MadeUpDomainNamespace = "Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Domain";

        private const string MadeUpDomain = @"^Lighthouse\.Backend\.Modules\.SizingFixtures\.Boundaries\.Domain(\.|$)";

        private const string MadeUpFormerHome = @"^Lighthouse\.Backend\.Modules\.SizingFixtures\.Boundaries\.Services(\.|$)";

        private const string MadeUpApplication = @"^Lighthouse\.Backend\.Modules\.SizingFixtures\.Boundaries\.Application(\.|$)";

        private const string MadeUpOutside = "Lighthouse.Backend.SizingBoundariesFixtures";

        private const string MadeUpApi = @"^Lighthouse\.Backend\.SizingBoundariesFixtures\.Api(\.|$)";

        private const string MadeUpController = "Lighthouse.Backend.SizingBoundariesFixtures.Api.AVotesController";

        private const string AGoneEntry = "Lighthouse.Backend.SizingBoundariesFixtures.Gone.AFormerVotesCaller";

        private const string AGoneEdge = "Lighthouse.Backend.Modules.Refinement.Application.AFormerCalendar -> Lighthouse.Backend.Platform.Host.ILighthouseClock";

        private const int MinimumRefinementRuleConstants = 7;

        private static readonly string[] TypesAModuleLookalikeMightHold = ["Domain.NeedBand", "Application.RefinementResolution", "Application.RefinementCadenceCalendar"];

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly ModuleWording RefinementWording = new("Refinement", "the API");

        // The types each Refinement layer holds, and the namespaces they moved out of. The lists name every type the
        // layer holds, so a type added to the module is placed here on purpose. Generic types carry their arity.
        private static readonly Dictionary<string, string[]> TypesByLayer = new(StringComparer.Ordinal)
        {
            [RefinementDomainNamespace] = ["NeedBand"],
            [RefinementApplicationNamespace] =
            [
                "IRefinementCalendar",
                "IRefinementViewQuery",
                "ISizingLogCommands",
                "NeedRange",
                "ReadySource",
                "RefinementCadenceCalendar",
                "RefinementCalendar",
                "RefinementCalendarFacts",
                "RefinementCycle",
                "RefinementList",
                "RefinementNeed",
                "RefinementNeedCalculator",
                "RefinementOutlook",
                "RefinementResolution",
                "RefinementRow",
                "RefinementView",
                "RefinementViewQuery",
                "RowConversation",
                "RowStanding",
                "RowVotes",
                "SizingComment",
                "SizingLog",
                "SizingLogCommands",
                "SizingLogLine",
                "SizingOutcome",
                "SizingRefusal",
                "SizingVote",
                "SleYardstickResolver",
                "StageMatches",
                "StageRuleHealing",
                "StageRuleMatcher",
                "Voter",
                "VoterIdentityKind",
                "VoterIdentityResolver",
                "VoterKeyResolution",
                "VoterNames",
                "VoterRefusal",
                "VoterResolution",
                "VoteSplit",
                "Yardstick",
            ],
        };

        // The outside types, besides the API and the composition root, that reach Refinement's Domain. The list only shrinks.
        private static readonly string[] OutsideTypesReachingTheDomain =
        [
        ];

        // The outside types, besides the API and the composition root, that reach Refinement's Application. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheApplication =
        [
        ];

        // The edges from Refinement's Application into EF Core, the data layer, the API, any module's adapters, Tracker
        // integration, the update code or Organisation set-up behind its Contracts. The cadence arithmetic still takes the
        // weekly recurrence from set-up's Domain, which set-up does not publish.
        private static readonly string[] EdgesOfTheApplicationIntoForbiddenCode =
        [
            "Lighthouse.Backend.Modules.Refinement.Application.RefinementCadenceCalendar -> Lighthouse.Backend.Modules.OrganisationSetup.Domain.WeeklyRecurrence",
        ];

        // The edges from Refinement's Application into Access, Organisation set-up, the Platform lane and the repositories.
        // Refinement asks Access which identity votes, reads the clock and the blackout periods, takes its cadence arithmetic
        // from the recurrence that Organisation set-up shares with the blackout rules, and reads Work Items and its sizing
        // log through the repository ports. The list only shrinks.
        private static readonly string[] EdgesOfTheApplicationIntoAccessSetUpPlatformAndRepositories =
        [
            "Lighthouse.Backend.Modules.Refinement.Application.RefinementCalendar -> Lighthouse.Backend.Platform.Host.ILighthouseClock",
            "Lighthouse.Backend.Modules.Refinement.Application.RefinementList -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Modules.Refinement.Application.RefinementList -> Lighthouse.Backend.Services.Interfaces.Repositories.IWorkItemRepository",
            "Lighthouse.Backend.Modules.Refinement.Application.RefinementViewQuery -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Modules.Refinement.Application.RefinementViewQuery -> Lighthouse.Backend.Services.Interfaces.Repositories.ISizingLogRepository",
            "Lighthouse.Backend.Modules.Refinement.Application.SizingLogCommands -> Lighthouse.Backend.Platform.Host.ILighthouseClock",
            "Lighthouse.Backend.Modules.Refinement.Application.SizingLogCommands -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Modules.Refinement.Application.SizingLogCommands -> Lighthouse.Backend.Services.Interfaces.Repositories.ISizingLogRepository",
            "Lighthouse.Backend.Modules.Refinement.Application.SleYardstickResolver -> Lighthouse.Backend.Platform.Host.ILighthouseClock",
            "Lighthouse.Backend.Modules.Refinement.Application.VoterIdentityResolver -> Lighthouse.Backend.Modules.Access.Application.CurrentUserProfileService",
            "Lighthouse.Backend.Modules.Refinement.Application.VoterIdentityResolver -> Lighthouse.Backend.Modules.Access.Contracts.IAuthModeResolver",
        ];

        private const string TheUpdateCode = @"^Lighthouse\.Backend\.Services\.(Implementation\.BackgroundServices\.Update|Interfaces\.Update)(\.|$)|^Lighthouse\.Backend\.Platform\.UpdateQueue(\.|$)";

        private static readonly string TheForbiddenCode =
            $"(?:{EntityFrameworkCore})|(?:{ApplicationCoreRules.TheData})|(?:{ApplicationCoreRules.TheApi})|(?:{AnyModuleAdapters})|(?:{TrackerIntegration})|(?:{TheUpdateCode})";

        // Apart from the declared pattern, so that this one is a property and no rule pattern the move laws read.
        private static string TheForbiddenCodeAndSetUpBehindItsContracts =>
            $"(?:{TheForbiddenCode})|(?:{ModuleRules.SetUpBehindItsContracts})";

        // Organisation set-up has no namespace of its own yet: its settings, blackout periods and recurrence sit in the
        // top-level Services namespaces, and the Platform lane and the repositories sit under Services and Platform too.
        private static readonly string AccessSetUpPlatformAndRepositories =
            $"(?:{ModuleRules.AccessModule})|(?:{TheFormerAccessHome})|(?:{KernelRules.TheServices})";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(RefinementModuleBoundariesArchUnitTest).Assembly));

        public enum ListedRule
        {
            OutsideTypesReachingTheDomain,
            OutsideTypesReachingTheApplication,
            ApplicationEdgesIntoForbiddenCode,
            ApplicationEdgesIntoAccessSetUpPlatformAndRepositories,
        }

        private static IEnumerable<string> Layers => TypesByLayer.Keys;

        private static string MadeUpCompositionRoot => typeof(ASizingCompositionRoot).FullName!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(Layers))]
        public void EveryRefinementType_LivesInItsLayer_AndNoneIsLeftUnderServices(string layer)
        {
            var types = TypesByLayer[layer];

            var placement = ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, layer, types);

            ThenTheRuleHolds(placement.Check, $"Refinement's layer {layer}");
            ThenTheRuleHolds(() => LayerHoldsExactly(ProductionModule.Value, layer, types), $"what Refinement's layer {layer} holds");
        }

        [Test]
        public void TheRefinementDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, RefinementDomain);

            ThenTheRuleHolds(verdict.Check, "Refinement's Domain");
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.ApplicationEdgesIntoForbiddenCode)]
        [TestCase(ListedRule.ApplicationEdgesIntoAccessSetUpPlatformAndRepositories)]
        public void ARefinementListedRule_MatchesWhatTheCompiledCodeReaches_InBothDirections(ListedRule rule)
        {
            var (check, list) = TheProductRule(rule);

            ThenTheRuleHolds(() => check(list), rule.ToString());
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.ApplicationEdgesIntoForbiddenCode)]
        [TestCase(ListedRule.ApplicationEdgesIntoAccessSetUpPlatformAndRepositories)]
        public void EveryEntryDeletedFromARefinementList_IsNamedAsOffending_AndAnEntryWithNoEdgeIsNamedForDeletion(ListedRule rule)
        {
            var (check, list) = TheProductRule(rule);

            ThenEachDeletedEntryIsNamedAndAGoneEntryIsNamedForDeletion(rule, check, list);
        }

        [Test]
        public void ARefinementTypeLeftUnderServices_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twin = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpDomainNamespace, [nameof(AnEstimateBand)], MadeUpFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpDomainNamespace, [nameof(ALeftBehindEstimateBand)], MadeUpFormerHome);

            ThenTheRuleNamesTheOffenderAndPassesItsTwin(leftBehind.Check, typeof(ALeftBehindEstimateBand).FullName!, twin.Check, "the layer rule");
        }

        [Test]
        public void ATypeInARefinementLayerThatIsNotOnItsList_IsNamed_AndTheFullListPasses()
        {
            ThenTheRuleNamesTheOffenderAndPassesItsTwin(
                () => LayerHoldsExactly(TestModule.Value, MadeUpDomainNamespace, [nameof(AnEstimateBand)]),
                typeof(AnEstimateBandAskingTheSettings).FullName!,
                () => LayerHoldsExactly(TestModule.Value, MadeUpDomainNamespace, [nameof(AnEstimateBand), nameof(AnEstimateBandAskingTheSettings)]),
                "what the layer holds");
        }

        [Test]
        public void ARefinementDomainTypeReachingAService_IsNamed_AndItsCompliantTwinIsNot()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(TestModule.Value, MadeUpDomain);

            ThenTheFailureNamesOnly(verdict.Check, typeof(AnEstimateBandAskingTheSettings).FullName!, [typeof(AnEstimateBand).FullName!], "the Domain rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingTheRefinementDomain_IsNamed_AndTheApiTheCompositionRootAndTheListedCallerAreNot()
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
            ThenTheFailureSpeaksOf(verdict.Check, "Refinement's Domain holds the module's own calculations");
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
        public void AnOutsideTypeOffTheList_ReachingTheRefinementApplication_IsNamed_AndTheApiTheCompositionRootAndTheListedCallerAreNot()
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
            ThenTheFailureSpeaksOf(verdict.Check, "Refinement's Application is the module's front door for the API");
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
        public void ARefinementApplicationReachingOnlyALegalNeighbourAndTheClock_PassesBothOutboundRules()
        {
            var compliant = NamespacePatternOf(typeof(ACompliantSizingService));
            var clock = Edge(typeof(ACompliantSizingService), "Lighthouse.Backend.Platform.Host.ILighthouseClock");

            var forbidden = ForbiddenCodeRule(TestModule.Value, compliant, []);
            var listedEdges = AccessSetUpPlatformAndRepositoriesRule(TestModule.Value, compliant, [clock]);

            Assert.That(
                listedEdges.Edges,
                Is.EquivalentTo(new[] { clock }),
                $"Expected the outbound rule to find exactly [{clock}]. Why it matters: a rule that misses an edge proves nothing by passing, and one that counts " +
                "Work Items' or Forecasting's Application flags a call Refinement may make. How to fix: report every edge into the namespaces the rule is handed, " +
                "and nothing in a legal module's Application.");
            ThenTheRuleHolds(forbidden.Check, "Refinement's Application towards forbidden code");
            ThenTheRuleHolds(listedEdges.Check, "Refinement's Application towards Access, set-up, the Platform lane and the repositories");
        }

        [TestCaseSource(nameof(RefinementApplicationTypesReachingForbiddenCode))]
        public void ARefinementApplicationTypeReachingForbiddenCode_IsNamedWithTheEdge(Type offender, string forbidden)
        {
            var verdict = ForbiddenCodeRule(TestModule.Value, NamespacePatternOf(offender), []);

            ThenTheFailureNamesOnly(verdict.Check, $"Not on the list, and offending: {Edge(offender, forbidden)}", [], "the forbidden-code rule");
            ThenTheFailureSpeaksOf(verdict.Check, "Refinement's Application");
        }

        [TestCaseSource(nameof(RefinementApplicationTypesReachingUnlistedEdges))]
        public void ARefinementApplicationTypeReachingAnUnlistedAccessSetUpPlatformOrRepositoryType_IsNamedWithTheEdge(Type offender, string unlisted)
        {
            var verdict = AccessSetUpPlatformAndRepositoriesRule(TestModule.Value, NamespacePatternOf(offender), []);

            ThenTheFailureNamesOnly(verdict.Check, $"Not on the list, and offending: {Edge(offender, unlisted)}", [], "the Access, set-up, Platform and repositories rule");
            ThenTheFailureSpeaksOf(verdict.Check, "Refinement's Application");
        }

        [Test]
        public void AListedEdgeThatNoLongerExists_IsNamedForDeletion_AndEachListedEdgeDeletedIsNamedAsOffending()
        {
            var offender = typeof(AnApplicationTypeReachingAnUnlistedAccessType);
            var listed = Edge(offender, "Lighthouse.Backend.Modules.Access.Contracts.IAuthModeResolver");
            var namespaces = NamespacePatternOf(offender);

            ThenTheRuleHolds(() => AccessSetUpPlatformAndRepositoriesRule(TestModule.Value, namespaces, [listed]).Check(), "the made-up listed edge");
            ThenTheFailureNamesOnly(AccessSetUpPlatformAndRepositoriesRule(TestModule.Value, namespaces, []).Check, $"Not on the list, and offending: {listed}", [], "the rule without its listed edge");
            ThenTheGoneEntryIsNamedForDeletion(AccessSetUpPlatformAndRepositoriesRule(TestModule.Value, namespaces, [listed, AGoneEdge]).Check, AGoneEdge, "the Access, set-up, Platform and repositories rule");
        }

        [Test]
        public void NoRefinementAlternativeOfTheRuleConstantsMatchesALookalikeNamespace()
        {
            var constants = RefinementRuleConstants(TypesOfTheTestAssemblyOutsideFixtures());

            var loose = ConstantsMatchingALookalike(constants);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(constants, Has.Count.GreaterThanOrEqualTo(MinimumRefinementRuleConstants),
                    $"Only {constants.Count} rule constants name Modules.Refinement and at least {MinimumRefinementRuleConstants} exist (the shared pattern, the Tracker integration, Access and Licensing composites, the per-type patterns and this file's own). " +
                    "Why it matters: a constant that vanished from the check would pass it for free. How to fix: keep every Refinement rule constant in the population the check reads.");
                Assert.That(loose, Is.Empty,
                    $"{loose.Count} of the {constants.Count} Refinement rule constants also match a namespace that only starts like the module's:" + Environment.NewLine +
                    string.Join(Environment.NewLine, loose) + Environment.NewLine +
                    "Why it matters: a rule that names the module without anchoring its end also constrains a neighbouring namespace, so a type there would be held to Refinement's rules, or let through them. " +
                    "How to fix: end the alternative with (\\.|$) or an exact type name.");
            }
        }

        [Test]
        public void AMadeUpRefinementConstantWithoutAnAnchoredEnd_IsNamedAsMatchingALookalike_AndItsAnchoredTwinIsNot()
        {
            var constants = RefinementRuleConstants(TypesOfTheTestAssemblyInTheLookalikeFixtures());

            var loose = ConstantsMatchingALookalike(constants);

            Assert.That(
                loose,
                Has.Count.EqualTo(1).And.All.StartWith($"{nameof(AnUnanchoredRefinementRule)}.{nameof(AnUnanchoredRefinementRule.Rule)}"),
                $"Of {constants.Count} made-up Refinement rule constants the check named [{string.Join("; ", loose)}], and should name only the one whose end is not anchored. " +
                "Why it matters: a check blind to a loose end lets a rule spill over onto a neighbouring namespace. How to fix: match every alternative against the lookalike namespace.");
        }

        private static IEnumerable<TestCaseData> RefinementApplicationTypesReachingForbiddenCode()
        {
            yield return new TestCaseData(typeof(ASizingServiceHoldingADbSet), "Microsoft.EntityFrameworkCore.DbSet`1")
                .SetName("ARefinementApplicationTypeHoldingAnEntityFrameworkCoreDbSet_IsNamed");
            yield return new TestCaseData(typeof(ASizingServiceReachingTheData), "Lighthouse.Backend.Data.LighthouseAppContext")
                .SetName("ARefinementApplicationTypeReachingTheDataLayer_IsNamed");
            yield return new TestCaseData(typeof(ASizingServiceReachingAnApiType), "Lighthouse.Backend.API.DTO.YardstickDto")
                .SetName("ARefinementApplicationTypeReachingTheApi_IsNamed");
            yield return new TestCaseData(typeof(ASizingServiceReachingAnAdapter), "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater")
                .SetName("ARefinementApplicationTypeReachingAModuleAdapter_IsNamed");
            yield return new TestCaseData(typeof(ASizingServiceReachingTrackerIntegration), "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IWorkTrackingConnectorFactory")
                .SetName("ARefinementApplicationTypeReachingTrackerIntegration_IsNamed");
            yield return new TestCaseData(typeof(ASizingServiceTouchingTheUpdateCode), "Lighthouse.Backend.Platform.UpdateQueue.UpdateKey")
                .SetName("ARefinementApplicationTypeReachingTheUpdateCode_IsNamed");
            yield return new TestCaseData(typeof(ASizingServiceReachingTheUpdateContracts), "Lighthouse.Backend.Services.Interfaces.Update.IUpdateService")
                .SetName("ARefinementApplicationTypeReachingTheUpdateContracts_IsNamed");
        }

        private static IEnumerable<TestCaseData> RefinementApplicationTypesReachingUnlistedEdges()
        {
            yield return new TestCaseData(typeof(AnApplicationTypeReachingAnUnlistedAccessType), "Lighthouse.Backend.Modules.Access.Contracts.IAuthModeResolver")
                .SetName("ARefinementApplicationTypeReachingAnUnlistedAccessType_IsNamed");
            yield return new TestCaseData(typeof(AnApplicationTypeReachingAnUnlistedSetUpType), typeof(global::Lighthouse.Backend.Services.Interfaces.IFeatureListService).FullName!)
                .SetName("ARefinementApplicationTypeReachingAnUnlistedSetUpType_IsNamed");
            yield return new TestCaseData(typeof(AnApplicationTypeReachingAnUnlistedPlatformType), "Lighthouse.Backend.Platform.Host.ILighthouseClock")
                .SetName("ARefinementApplicationTypeReachingAnUnlistedPlatformLaneType_IsNamed");
            yield return new TestCaseData(typeof(AnApplicationTypeReachingAnUnlistedRepository), "Lighthouse.Backend.Services.Interfaces.Repositories.IWorkItemRepository")
                .SetName("ARefinementApplicationTypeReachingAnUnlistedRepository_IsNamed");
        }

        private static (Action<IReadOnlyCollection<string>> Check, string[] List) TheProductRule(ListedRule rule)
        {
            var module = ProductionModule.Value;
            var theApi = ApplicationCoreRules.TheApi;

            return rule switch
            {
                ListedRule.OutsideTypesReachingTheDomain => (
                    listed => DomainInboundRule(module, listed, ModuleRules.AnyNamespace, RefinementDomain, ModuleBoundariesArchUnitTest.RefinementPattern, theApi, TheCompositionRootAndItsNestedTypes).Check(),
                    OutsideTypesReachingTheDomain),
                ListedRule.OutsideTypesReachingTheApplication => (
                    listed => ApplicationInboundRule(module, listed, ModuleRules.AnyNamespace, RefinementApplication, ModuleBoundariesArchUnitTest.RefinementPattern, theApi, TheCompositionRootAndItsNestedTypes).Check(),
                    OtherOutsideTypesReachingTheApplication),
                ListedRule.ApplicationEdgesIntoForbiddenCode => (
                    listed => ForbiddenCodeRule(module, RefinementApplication, listed).Check(),
                    EdgesOfTheApplicationIntoForbiddenCode),
                ListedRule.ApplicationEdgesIntoAccessSetUpPlatformAndRepositories => (
                    listed => AccessSetUpPlatformAndRepositoriesRule(module, RefinementApplication, listed).Check(),
                    EdgesOfTheApplicationIntoAccessSetUpPlatformAndRepositories),
                _ => throw new ArgumentOutOfRangeException(nameof(rule)),
            };
        }

        private static InboundVerdict DomainInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string outside)
            => DomainInboundRule(module, listed, outside, MadeUpDomain, MadeUpModule, MadeUpApi, RootPattern(MadeUpCompositionRoot));

        // The API and the composition root reach the Domain by right, so they are set aside by namespace and by type name.
        private static InboundVerdict DomainInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string outside, string domain, string moduleNamespaces, string legalCallers, string compositionRoot)
        {
            var verdict = ModuleRules.DomainIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, domain, outside, RefinementWording);
            var legal = IlDependencies.TypesIn(module, legalCallers).ToHashSet(StringComparer.Ordinal);

            return verdict with
            {
                Dependents = [.. verdict.Dependents.Where(dependent => !legal.Contains(dependent.TypeName) && !Regex.IsMatch(dependent.TypeName, compositionRoot, RegexOptions.None, PatternTimeout))],
            };
        }

        private static InboundVerdict ApplicationInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string outside)
            => ApplicationInboundRule(module, listed, outside, MadeUpApplication, MadeUpModule, MadeUpApi, RootPattern(MadeUpCompositionRoot));

        private static InboundVerdict ApplicationInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string outside, string application, string moduleNamespaces, string legalCallers, string compositionRoot)
            => ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, application, legalCallers, compositionRoot, outside, RefinementWording);

        private static OutboundVerdict ForbiddenCodeRule(ModuleDefinition module, string application, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                application,
                TheForbiddenCodeAndSetUpBehindItsContracts,
                listed,
                $"Types in Refinement's Application, namespaces matching \"{application}\", should not depend on any type in namespaces matching " +
                $"\"{TheForbiddenCodeAndSetUpBehindItsContracts}\" except through the listed edges, because Refinement's Application decides what a team's refinement needs " +
                "and who voted, and returns Models and its own result types. An Application type that holds a DbSet or the database context, shapes " +
                "an API type, runs a module's adapter, calls Tracker integration or queues an update ties Refinement to how the rest of the system " +
                "stores, shows and fetches its data. Read through a repository port, ask Work Items, Forecasting or Flow metrics through their " +
                "Application, and return a Models type.");

        private static OutboundVerdict AccessSetUpPlatformAndRepositoriesRule(ModuleDefinition module, string application, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                application,
                AccessSetUpPlatformAndRepositories,
                listed,
                $"Types in Refinement's Application, namespaces matching \"{application}\", should reach Access, Organisation set-up, the Platform lane and the " +
                $"repositories, namespaces matching \"{AccessSetUpPlatformAndRepositories}\", only through the listed edges, because each of them is another " +
                "context's decision: who is signed in, which clock and blackout periods apply, how a recurrence is counted and how records are stored. " +
                "Every new edge spreads one of those decisions into Refinement. Ask where the listed edge already asks, or add the port to the owning context.");

        private static void LayerHoldsExactly(ModuleDefinition module, string layerNamespace, IReadOnlyCollection<string> typeNames)
        {
            var held = IlDependencies.TypesIn(module, $"^{Regex.Escape(layerNamespace)}$")
                .Select(fullName => fullName.Split('+')[0])
                .Distinct(StringComparer.Ordinal);

            InlineBaseline.AssertMatches(
                held,
                typeNames.Select(name => $"{layerNamespace}.{name}"),
                $"Namespace \"{layerNamespace}\" should hold exactly the types its list names, because the list is where a type is placed in " +
                "Refinement's layer on purpose. A type that lands in the layer unlisted was placed there without that decision. Add it to the " +
                "layer's list if it belongs to Refinement, or move it to the module that owns it.");
        }

        // The old-path sweep keeps a bare module prefix to pick out the alternatives it checks; it is a selector, not a rule.
        private static List<Type> TypesOfTheTestAssemblyOutsideFixtures()
            => [.. typeof(RefinementModuleBoundariesArchUnitTest).Assembly.GetTypes()
                .Where(type => type.Namespace?.Split('.').Any(part => part.EndsWith("Fixtures", StringComparison.Ordinal)) != true)
                .Where(type => (type.DeclaringType ?? type) != typeof(RefinementRulesNameOnlyTheModuleArchUnitTest))];

        private static List<Type> TypesOfTheTestAssemblyInTheLookalikeFixtures()
            => [.. typeof(RefinementModuleBoundariesArchUnitTest).Assembly.GetTypes().Where(type => type.Namespace == typeof(AnUnanchoredRefinementRule).Namespace)];

        private static List<(string Field, string Value)> RefinementRuleConstants(IEnumerable<Type> types)
            => [.. from type in types
                   where !type.ContainsGenericParameters
                   from field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                   where field.FieldType == typeof(string) && (field.IsLiteral || field.IsInitOnly)
                   let value = (field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null)) as string
                   where value is not null && value.Contains(@"Modules\.Refinement", StringComparison.Ordinal)
                   select (Field: $"{(type.DeclaringType ?? type).Name}.{field.Name}", Value: value)];

        private static List<string> ConstantsMatchingALookalike(IEnumerable<(string Field, string Value)> constants)
        {
            var lookalikes = TypesAModuleLookalikeMightHold
                .Select(type => string.Concat("Lighthouse.Backend.Modules.", "Refinement", "Lookalike", ".", type))
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
                $"The rule for {what} failed; the failure above names each type or edge to fix. Why it matters: Refinement keeps its shape only " +
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
                Does.Contain(phrase).And.Not.Contain("Flow metrics' ").And.Not.Contain("Flow metrics's "),
                $"A failing Refinement rule must speak of Refinement in its own words (\"{phrase}\"), not of another module. Why it matters: the reader is sent " +
                "to the wrong module. How to fix: hand the rule Refinement's wording.");
        }
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Domain
{
    internal sealed record AnEstimateBand(int Low, int High)
    {
        public int Width => High - Low;
    }

    internal sealed class AnEstimateBandAskingTheSettings(global::Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService settings)
    {
        public int Describe() => settings.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Services
{
    internal sealed record ALeftBehindEstimateBand(int Low, int High);
}

namespace Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Application
{
    internal static class ASizingAnswerService
    {
        public static int Answer(int low) => new global::Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Domain.AnEstimateBand(low, low + 3).Width;
    }
}

namespace Lighthouse.Backend.SizingBoundariesFixtures.Api
{
    internal static class AVotesController
    {
        public static int Get(int low) => global::Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Application.ASizingAnswerService.Answer(low)
            + new global::Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Domain.AnEstimateBand(low, low + 1).Width;
    }
}

namespace Lighthouse.Backend.SizingBoundariesFixtures.CompositionRoot
{
    internal static class ASizingCompositionRoot
    {
        public static int Register(int low) => global::Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Application.ASizingAnswerService.Answer(low);

        internal static class Registrations
        {
            public static int Register(int low) => new global::Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Domain.AnEstimateBand(low, low + 2).Width;
        }
    }
}

namespace Lighthouse.Backend.SizingBoundariesFixtures.ListedCallers
{
    internal static class AListedDomainCaller
    {
        public static int Answer(int low) => new global::Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Domain.AnEstimateBand(low, low + 4).Width;
    }

    internal static class AListedApplicationCaller
    {
        public static int Answer(int low) => global::Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Application.ASizingAnswerService.Answer(low);
    }
}

namespace Lighthouse.Backend.SizingBoundariesFixtures.DomainNewcomer
{
    internal static class ANewcomerReachingTheDomain
    {
        public static int Answer(int low) => new global::Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Domain.AnEstimateBand(low, low + 5).Width;
    }
}

namespace Lighthouse.Backend.SizingBoundariesFixtures.ApplicationNewcomer
{
    internal static class ANewcomerReachingTheApplication
    {
        public static async Task<int> AnswerAsync(int low)
        {
            await Task.Yield();

            return global::Lighthouse.Backend.Modules.SizingFixtures.Boundaries.Application.ASizingAnswerService.Answer(low);
        }
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.Compliant.Application
{
    internal sealed class ACompliantSizingService(
        global::Lighthouse.Backend.Platform.Host.ILighthouseClock clock,
        global::Lighthouse.Backend.Modules.WorkItems.Application.ITeamDataService teams,
        global::Lighthouse.Backend.Modules.Forecasting.Contracts.IForecastService forecasts)
    {
        public string Describe() => $"{clock.GetHashCode()} {teams.GetHashCode()} {forecasts.GetHashCode()}";
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.HoldingADbSet.Application
{
    internal sealed class ASizingServiceHoldingADbSet(global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Delivery> deliveries)
    {
        public global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Delivery> Deliveries { get; } = deliveries;
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingTheData.Application
{
    internal sealed class ASizingServiceReachingTheData(global::Lighthouse.Backend.Data.LighthouseAppContext context)
    {
        public int Describe() => context.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnApiType.Application
{
    internal static class ASizingServiceReachingAnApiType
    {
        public static string Describe() => typeof(global::Lighthouse.Backend.API.DTO.YardstickDto).Name;
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnAdapter.Application
{
    internal static class ASizingServiceReachingAnAdapter
    {
        public static string Describe() => typeof(global::Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater).Name;
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingTrackerIntegration.Application
{
    internal sealed class ASizingServiceReachingTrackerIntegration(global::Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IWorkTrackingConnectorFactory connectors)
    {
        public int Describe() => connectors.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.TouchingTheUpdateCode.Application
{
    internal static class ASizingServiceTouchingTheUpdateCode
    {
        public static string Describe() => typeof(global::Lighthouse.Backend.Platform.UpdateQueue.UpdateKey).Name;
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.TouchingTheUpdateContracts.Application
{
    internal sealed class ASizingServiceReachingTheUpdateContracts(global::Lighthouse.Backend.Services.Interfaces.Update.IUpdateService updates)
    {
        public int Describe() => updates.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnUnlistedAccessType.Application
{
    internal sealed class AnApplicationTypeReachingAnUnlistedAccessType(global::Lighthouse.Backend.Modules.Access.Contracts.IAuthModeResolver authMode)
    {
        public int Describe() => authMode.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnUnlistedSetUpType.Application
{
    internal sealed class AnApplicationTypeReachingAnUnlistedSetUpType(global::Lighthouse.Backend.Services.Interfaces.IFeatureListService settings)
    {
        public int Describe() => settings.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnUnlistedPlatformType.Application
{
    internal sealed class AnApplicationTypeReachingAnUnlistedPlatformType(global::Lighthouse.Backend.Platform.Host.ILighthouseClock clock)
    {
        public int Describe() => clock.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.SizingFixtures.BoundariesOutbound.ReachingAnUnlistedRepository.Application
{
    internal sealed class AnApplicationTypeReachingAnUnlistedRepository(global::Lighthouse.Backend.Services.Interfaces.Repositories.IWorkItemRepository workItems)
    {
        public int Describe() => workItems.GetHashCode();
    }
}

namespace Lighthouse.Backend.Tests.Architecture.RefinementLookalikeFixtures
{
    // Made-up rule constants; the unanchored one is assembled from pieces so its text is not a rule pattern of its own.
    internal static class AnUnanchoredRefinementRule
    {
        public const string Rule = @"^Lighthouse\.Backend\.Modules\." + "Refinement";
    }

    internal static class AnAnchoredRefinementRule
    {
        public const string Rule = @"^Lighthouse\.Backend\.Modules\." + @"Refinement(\.|$)";
    }
}
