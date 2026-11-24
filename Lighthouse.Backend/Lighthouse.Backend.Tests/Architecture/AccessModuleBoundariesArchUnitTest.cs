using System.Text.RegularExpressions;
using Lighthouse.Backend.AccessBoundariesFixtures.Api;
using Lighthouse.Backend.AccessBoundariesFixtures.ApplicationNewcomer;
using Lighthouse.Backend.AccessBoundariesFixtures.AttributeOnlyNewcomer;
using Lighthouse.Backend.AccessBoundariesFixtures.CompositionRoot;
using Lighthouse.Backend.AccessBoundariesFixtures.DomainNewcomer;
using Lighthouse.Backend.AccessBoundariesFixtures.ListedCallers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Domain;
using Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Services;
using Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.AskingTheAppSettings.Application;
using Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.AskingWorkItems.Application;
using Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.Compliant.Application;
using Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.HoldingADbSet.Application;
using Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.RunningAForecastingAdapter.Application;
using Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.RunningTheWriteBackRound.Application;
using Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.TouchingTheUpdateCode.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Access is a module with rules of its own, checked by the shared module rules with Access's namespaces and Access's
    /// wording, and read from the compiled production assembly: each Access type lives in its layer and none is left in
    /// the old Services Auth or Authorization namespaces; its Domain reaches only the shared kernel; outside code reaches
    /// its Domain and Application only from the API, the composition root or as listed, and its Http adapters only from
    /// the API or as listed; its Application reaches the data layer, the platform and EF Core only through the listed
    /// edges; and Access reaches no other context, Licensing aside. Every list is compared both ways. The same rules run
    /// on made-up types: each rule names its made-up offender, one of which reaches Access only through an attribute it
    /// applies, and passes its compliant twin.
    /// </summary>
    /// <remarks>
    /// The IL reader does not see types used only in a catch clause, an event or a named attribute argument.
    /// </remarks>
    [TestFixture]
    public class AccessModuleBoundariesArchUnitTest
    {
        private const string AccessDomainNamespace = "Lighthouse.Backend.Modules.Access.Domain";

        private const string AccessApplicationNamespace = "Lighthouse.Backend.Modules.Access.Application";

        private const string AccessAdaptersHttpNamespace = "Lighthouse.Backend.Modules.Access.Adapters.Http";

        private const string AccessDomain = @"^Lighthouse\.Backend\.Modules\.Access\.Domain(\.|$)";

        private const string AccessApplication = @"^Lighthouse\.Backend\.Modules\.Access\.Application(\.|$)";

        private const string AccessAdaptersHttp = @"^Lighthouse\.Backend\.Modules\.Access\.Adapters\.Http(\.|$)";

        private const string TheOldAccessNamespaces = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Auth|Authorization)(\.|$)";

        private const string TheCompositionRootAndItsNestedTypes = @"^Lighthouse\.Backend\.Program(\+|$)";

        private const string EntityFrameworkCore = @"^Microsoft\.EntityFrameworkCore(\.|$)";

        private const string AnyModuleAdapters = @"^Lighthouse\.Backend\.Modules\.[^.]+\.Adapters(\.|$)";

        private const string ThePlatform = @"^Lighthouse\.Backend\.(Services|Platform)(\.|$)";

        private const string TheOtherModules = @"^Lighthouse\.Backend\.Modules\.(WorkItems|Forecasting|FlowMetrics|TrackerIntegration|Promises)(\.|$)";

        private const string TheUpdateCode = @"^Lighthouse\.Backend\.Services\.(Implementation\.BackgroundServices\.Update|Interfaces\.Update)(\.|$)"
            + @"|^Lighthouse\.Backend\.Platform\.UpdateQueue(\.|$)";

        private const string RefinementAndReports = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Reports(\.|$)|^Lighthouse\.Backend\.Modules\.Refinement(\.|$)|^Lighthouse\.Backend\.Modules\.Reports(\.|$)";

        private const string MadeUpModule = @"^Lighthouse\.Backend\.Modules\.AccessFixtures\.Boundaries(\.|$)";

        private const string MadeUpDomainNamespace = "Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Domain";

        private const string MadeUpDomain = @"^Lighthouse\.Backend\.Modules\.AccessFixtures\.Boundaries\.Domain(\.|$)";

        private const string MadeUpApplication = @"^Lighthouse\.Backend\.Modules\.AccessFixtures\.Boundaries\.Application(\.|$)";

        private const string MadeUpFormerHome = @"^Lighthouse\.Backend\.Modules\.AccessFixtures\.Boundaries\.Services(\.|$)";

        private const string MadeUpOutside = "Lighthouse.Backend.AccessBoundariesFixtures";

        private const string AGoneEntry = "Lighthouse.Backend.AccessBoundariesFixtures.Gone.AFormerAccessCaller";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly ModuleWording AccessWording = new("Access", "the API");

        // The types each Access layer holds, measured from the compiled code. The Domain also holds the shared Models
        // types Access took with it.
        private static readonly Dictionary<string, IReadOnlyList<string>> TypesByLayer = new(StringComparer.Ordinal)
        {
            [AccessDomainNamespace] = [.. AccessAndLicensingMembership.AccessDomain, .. AccessAndLicensingMembership.ModelsTypesThatMoveIntoAccess],
            [AccessApplicationNamespace] = AccessAndLicensingMembership.AccessApplication,
            [AccessAdaptersHttpNamespace] = AccessAndLicensingMembership.AccessAdaptersHttp,
        };

        // The outside types, besides the API and the composition root, that reach Access's Domain. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheDomain =
        [
            "Lighthouse.Backend.Services.Implementation.FeatureListService",
        ];

        // The outside types, besides the API and the composition root, that reach Access's Application. Each of them
        // asks Access who the user is or what they may see. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheApplication =
        [
            "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.FeatureDependencyRendering",
            "Lighthouse.Backend.Services.Implementation.FeatureListActiveWork",
            "Lighthouse.Backend.Services.Implementation.FeatureListService",
            "Lighthouse.Backend.Modules.Refinement.Application.VoterIdentityResolver",
            "Lighthouse.Backend.Platform.UsageData.UsageDataInstanceProperties",
        ];

        // The outside types, besides the API, that reach Access's Http adapters: the composition root registers the
        // authentication handlers, the scheme selector and the blocked-mode filter. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheHttpAdapters =
        [
            "Lighthouse.Backend.Program",
        ];

        // The edges from Access's Application into EF Core, the data layer, the API, any module's adapters or the
        // platform. Access keeps its API keys and embed sessions through platform repositories, asks the platform which
        // host it runs on, and keeps profiles, group snapshots and role assignments in the database context directly.
        // The list only shrinks.
        private static readonly string[] EdgesOfTheApplicationBelowItOrIntoThePlatform =
        [
            "Lighthouse.Backend.Modules.Access.Application.ApiKeyService -> Lighthouse.Backend.Services.Interfaces.Repositories.IApiKeyRepository",
            "Lighthouse.Backend.Modules.Access.Application.ApiKeyService -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Modules.Access.Application.AuthModeResolver -> Lighthouse.Backend.Platform.Host.IPlatformService",
            "Lighthouse.Backend.Modules.Access.Application.CurrentUserProfileService -> Lighthouse.Backend.Data.LighthouseAppContext",
            "Lighthouse.Backend.Modules.Access.Application.CurrentUserProfileService -> Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry`1",
            "Lighthouse.Backend.Modules.Access.Application.CurrentUserProfileService -> Microsoft.EntityFrameworkCore.DbContext",
            "Lighthouse.Backend.Modules.Access.Application.CurrentUserProfileService -> Microsoft.EntityFrameworkCore.DbSet`1",
            "Lighthouse.Backend.Modules.Access.Application.CurrentUserProfileService -> Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions",
            "Lighthouse.Backend.Modules.Access.Application.CurrentUserProfileService -> Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder`1",
            "Lighthouse.Backend.Modules.Access.Application.EmbedSessionTokenService -> Lighthouse.Backend.Services.Interfaces.Repositories.IEmbedSessionTokenRepository",
            "Lighthouse.Backend.Modules.Access.Application.OidcGroupSnapshotWriter -> Lighthouse.Backend.Data.LighthouseAppContext",
            "Lighthouse.Backend.Modules.Access.Application.OidcGroupSnapshotWriter -> Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry`1",
            "Lighthouse.Backend.Modules.Access.Application.OidcGroupSnapshotWriter -> Microsoft.EntityFrameworkCore.DbContext",
            "Lighthouse.Backend.Modules.Access.Application.OidcGroupSnapshotWriter -> Microsoft.EntityFrameworkCore.DbSet`1",
            "Lighthouse.Backend.Modules.Access.Application.OidcGroupSnapshotWriter -> Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions",
            "Lighthouse.Backend.Modules.Access.Application.OidcGroupSnapshotWriter -> Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder`1",
            "Lighthouse.Backend.Modules.Access.Application.RbacAdministrationService -> Lighthouse.Backend.Data.LighthouseAppContext",
            "Lighthouse.Backend.Modules.Access.Application.RbacAdministrationService -> Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry`1",
            "Lighthouse.Backend.Modules.Access.Application.RbacAdministrationService -> Microsoft.EntityFrameworkCore.DbContext",
            "Lighthouse.Backend.Modules.Access.Application.RbacAdministrationService -> Microsoft.EntityFrameworkCore.DbSet`1",
            "Lighthouse.Backend.Modules.Access.Application.RbacAdministrationService -> Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions",
        ];

        // The edges from anywhere in Access into another context. Asking Licensing is allowed and is not counted here.
        // The list only shrinks.
        private static readonly string[] EdgesOfTheModuleIntoAnotherContext =
        [
        ];

        // The write-back round has no namespace of its own, so its types are named one by one.
        private static readonly string[] TheWriteBackRound =
        [
            "Lighthouse.Backend.Services.Implementation.WriteBackCollector",
            "Lighthouse.Backend.Services.Implementation.WriteBackRound",
            "Lighthouse.Backend.Services.Implementation.WriteBackRoundContext",
            "Lighthouse.Backend.Services.Implementation.WriteBackService",
            "Lighthouse.Backend.Services.Implementation.WriteBackTriggerService",
            "Lighthouse.Backend.Services.Interfaces.IWriteBackCollector",
            "Lighthouse.Backend.Services.Interfaces.IWriteBackService",
            "Lighthouse.Backend.Services.Interfaces.IWriteBackTriggerService",
        ];

        private static readonly string TheLayersBelowTheApplicationAndThePlatform =
            $"(?:{EntityFrameworkCore})|(?:{ApplicationCoreRules.TheData})|(?:{ApplicationCoreRules.TheApi})|(?:{AnyModuleAdapters})|(?:{ThePlatform})";

        private static readonly string TheOtherContexts = $"(?:{TheOtherModules})|(?:{TheUpdateCode})|(?:{RefinementAndReports})";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(AccessModuleBoundariesArchUnitTest).Assembly));

        public enum ListedRule
        {
            OutsideTypesReachingTheDomain,
            OutsideTypesReachingTheApplication,
            OutsideTypesReachingTheHttpAdapters,
            ApplicationEdgesBelowItOrIntoThePlatform,
            ModuleEdgesIntoAnotherContext,
        }

        public enum OutboundTarget
        {
            BelowOrPlatform,
            AnotherContext,
        }

        private static IEnumerable<string> Layers => TypesByLayer.Keys;

        private static string MadeUpCompositionRoot => typeof(AnAccessCompositionRoot).FullName!;

        private static string MadeUpController => typeof(AnAccessController).FullName!;

        private static string MadeUpGuardedController => typeof(AGuardedController).FullName!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(Layers))]
        public void EveryAccessType_LivesInItsLayer_AndNoneIsLeftInTheOldServicesNamespaces(string layer)
        {
            var placement = ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, layer, TypesByLayer[layer], TheOldAccessNamespaces);

            ThenTheRuleHolds(placement.Check, $"Access's layer {layer}");
        }

        [Test]
        public void TheAccessDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, AccessDomain);

            ThenTheRuleHolds(verdict.Check, "Access's Domain");
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.OutsideTypesReachingTheHttpAdapters)]
        [TestCase(ListedRule.ApplicationEdgesBelowItOrIntoThePlatform)]
        [TestCase(ListedRule.ModuleEdgesIntoAnotherContext)]
        public void AnAccessListedRule_MatchesWhatTheCompiledCodeReaches_InBothDirections(ListedRule rule)
        {
            var (check, list) = TheProductRule(rule);

            ThenTheRuleHolds(() => check(list), rule.ToString());
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.OutsideTypesReachingTheHttpAdapters)]
        [TestCase(ListedRule.ApplicationEdgesBelowItOrIntoThePlatform)]
        [TestCase(ListedRule.ModuleEdgesIntoAnotherContext)]
        public void EveryEntryDeletedFromAnAccessList_IsNamedAsOffending_AndAnEntryWithNoEdgeIsNamedForDeletion(ListedRule rule)
        {
            var (check, list) = TheProductRule(rule);

            ModuleBoundaryChecks.ThenEachDeletedEntryIsNamedAndAGoneEntryIsNamedForDeletion(rule.ToString(), check, list, AGoneEntry);
        }

        [Test]
        public void AnAccessTypeLeftInItsFormerHome_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twin = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpDomainNamespace, [nameof(AnAccessAnswer)], MadeUpFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpDomainNamespace, [nameof(ALeftBehindAccessAnswer)], MadeUpFormerHome);

            ModuleBoundaryChecks.ThenTheRuleNamesTheOffenderAndPassesItsTwin(leftBehind.Check, typeof(ALeftBehindAccessAnswer).FullName!, twin.Check, "the Access layer rule");
        }

        [Test]
        public void AnAccessDomainTypeReachingThePlatform_IsNamed_AndItsCompliantTwinIsNot()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(TestModule.Value, MadeUpDomain);

            ModuleBoundaryChecks.ThenTheFailureNamesOnly(verdict.Check, typeof(AnAccessAnswerAskingThePlatform).FullName!, [typeof(AnAccessAnswer).FullName!], "Access's Domain rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingTheAccessDomain_IsNamed_AndTheApiTheCompositionRootAndTheListedCallerAreNot()
        {
            var verdict = DomainInboundRule(
                TestModule.Value,
                [typeof(AListedDomainCaller).FullName!],
                MadeUpModule,
                MadeUpDomain,
                OutsideFixture("Api"),
                ModuleBoundaryChecks.RootPattern(MadeUpCompositionRoot),
                OutsideFixtures("Api", "CompositionRoot", "ListedCallers", "DomainNewcomer"));

            ModuleBoundaryChecks.ThenTheFailureNamesOnly(
                verdict.Check,
                typeof(ANewcomerReachingTheDomain).FullName!,
                [typeof(AListedDomainCaller).FullName!, MadeUpController, MadeUpCompositionRoot],
                "Access's Domain inbound rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingTheAccessApplication_IsNamed_AndTheApiTheCompositionRootAndTheListedCallerAreNot()
        {
            var verdict = ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(
                TestModule.Value,
                [typeof(AListedApplicationCaller).FullName!],
                MadeUpModule,
                MadeUpApplication,
                OutsideFixture("Api"),
                ModuleBoundaryChecks.RootPattern(MadeUpCompositionRoot),
                OutsideFixtures("Api", "CompositionRoot", "ListedCallers", "ApplicationNewcomer"),
                AccessWording);

            ModuleBoundaryChecks.ThenTheFailureNamesOnly(
                verdict.Check,
                typeof(ANewcomerReachingTheApplication).FullName!,
                [typeof(AListedApplicationCaller).FullName!, MadeUpController, MadeUpCompositionRoot],
                "Access's Application inbound rule");
        }

        [Test]
        public void AnOutsideTypeReachingAccessOnlyThroughAnAttributeItApplies_IsNamedByTheHttpAdapterRule_AndTheApiAndTheListedCompositionRootAreNot()
        {
            var verdict = ModuleRulesAskedByName.HttpAdaptersAreReachedFromOutsideOnlyBy(
                TestModule.Value,
                [MadeUpCompositionRoot],
                ModuleRules.AccessModule,
                AccessAdaptersHttp,
                OutsideFixture("Api"),
                OutsideFixtures("Api", "CompositionRoot", "AttributeOnlyNewcomer"),
                AccessWording);

            ModuleBoundaryChecks.ThenTheFailureNamesOnly(
                verdict.Check,
                typeof(AReportExportGuardedOnlyByAnAttribute).FullName!,
                [MadeUpGuardedController, MadeUpCompositionRoot],
                "Access's Http adapter rule");
            ModuleBoundaryChecks.ThenTheFailureSpeaksOf(verdict.Check, "Access", "Access's Http adapter rule");
        }

        [Test]
        public void AnAccessApplicationReachingOnlyAListedPlatformEdgeAndLicensing_PassesBothOutboundRules()
        {
            var listedEdge = ModuleBoundaryChecks.Edge(typeof(ACompliantAccessService), "Lighthouse.Backend.Platform.Host.ILighthouseClock");
            var namespaces = ModuleBoundaryChecks.NamespacePatternOf(typeof(ACompliantAccessService));

            var belowOrPlatform = ApplicationOutboundRule(TestModule.Value, namespaces, [listedEdge]);
            var anotherContext = ModuleIntoAnotherContextRule(TestModule.Value, namespaces, []);

            Assert.That(
                belowOrPlatform.Edges,
                Is.EquivalentTo(new[] { listedEdge }),
                $"Expected the outbound rule to find exactly [{listedEdge}]. Why it matters: a rule that misses an edge proves nothing by passing, " +
                "and one that counts Licensing flags a question Access may ask. How to fix: report every edge into the namespaces the rule is " +
                "handed, and nothing in Licensing.");
            ThenTheRuleHolds(belowOrPlatform.Check, "Access's Application towards the layers below it and the platform");
            ThenTheRuleHolds(anotherContext.Check, "Access towards the other contexts");
        }

        [TestCaseSource(nameof(AccessTypesReachingForbiddenCode))]
        public void AnAccessTypeReachingForbiddenCode_IsNamedWithTheEdge(Type offender, OutboundTarget target, string forbidden)
        {
            var namespaces = ModuleBoundaryChecks.NamespacePatternOf(offender);
            var verdict = target == OutboundTarget.BelowOrPlatform
                ? ApplicationOutboundRule(TestModule.Value, namespaces, [])
                : ModuleIntoAnotherContextRule(TestModule.Value, namespaces, []);

            ModuleBoundaryChecks.ThenTheFailureNamesOnly(verdict.Check, $"Not on the list, and offending: {ModuleBoundaryChecks.Edge(offender, forbidden)}", [], $"the {target} rule");
        }

        private static IEnumerable<TestCaseData> AccessTypesReachingForbiddenCode()
        {
            yield return new TestCaseData(typeof(AnAccessServiceHoldingADbSet), OutboundTarget.BelowOrPlatform, "Microsoft.EntityFrameworkCore.DbSet`1")
                .SetName("AnAccessApplicationTypeHoldingAnEntityFrameworkCoreDbSet_IsNamed");
            yield return new TestCaseData(typeof(AnAccessServiceAskingTheAppSettings), OutboundTarget.BelowOrPlatform, "Lighthouse.Backend.Services.Interfaces.IAppSettingService")
                .SetName("AnAccessApplicationTypeReachingAnUnlistedPlatformType_IsNamed");
            yield return new TestCaseData(typeof(AnAccessServiceRunningAForecastingAdapter), OutboundTarget.BelowOrPlatform, "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater")
                .SetName("AnAccessApplicationTypeReachingAModuleAdapter_IsNamedByTheBelowRule");
            yield return new TestCaseData(typeof(AnAccessServiceAskingWorkItems), OutboundTarget.AnotherContext, "Lighthouse.Backend.Modules.WorkItems.Application.IWorkItemService")
                .SetName("AnAccessTypeReachingAnotherModule_IsNamed");
            yield return new TestCaseData(typeof(AnAccessServiceTouchingTheUpdateCode), OutboundTarget.AnotherContext, "Lighthouse.Backend.Platform.UpdateQueue.UpdateKey")
                .SetName("AnAccessTypeReachingTheUpdateCode_IsNamed");
            yield return new TestCaseData(typeof(AnAccessServiceRunningTheWriteBackRound), OutboundTarget.AnotherContext, "Lighthouse.Backend.Services.Interfaces.IWriteBackTriggerService")
                .SetName("AnAccessTypeReachingTheWriteBackRound_IsNamed");
        }

        private static (Action<IReadOnlyCollection<string>> Check, string[] List) TheProductRule(ListedRule rule)
        {
            var module = ProductionModule.Value;

            return rule switch
            {
                ListedRule.OutsideTypesReachingTheDomain => (
                    listed => DomainInboundRule(module, listed, ModuleRules.AccessModule, AccessDomain, ApplicationCoreRules.TheApi, TheCompositionRootAndItsNestedTypes, ModuleRules.AnyNamespace).Check(),
                    OtherOutsideTypesReachingTheDomain),
                ListedRule.OutsideTypesReachingTheApplication => (
                    listed => ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(module, listed, ModuleRules.AccessModule, AccessApplication, ApplicationCoreRules.TheApi, TheCompositionRootAndItsNestedTypes, ModuleRules.AnyNamespace, AccessWording).Check(),
                    OtherOutsideTypesReachingTheApplication),
                ListedRule.OutsideTypesReachingTheHttpAdapters => (
                    listed => ModuleRulesAskedByName.HttpAdaptersAreReachedFromOutsideOnlyBy(module, listed, ModuleRules.AccessModule, AccessAdaptersHttp, ApplicationCoreRules.TheApi, ModuleRules.AnyNamespace, AccessWording).Check(),
                    OtherOutsideTypesReachingTheHttpAdapters),
                ListedRule.ApplicationEdgesBelowItOrIntoThePlatform => (
                    listed => ApplicationOutboundRule(module, AccessApplication, listed).Check(),
                    EdgesOfTheApplicationBelowItOrIntoThePlatform),
                ListedRule.ModuleEdgesIntoAnotherContext => (
                    listed => ModuleIntoAnotherContextRule(module, ModuleRules.AccessModule, listed).Check(),
                    EdgesOfTheModuleIntoAnotherContext),
                _ => throw new ArgumentOutOfRangeException(nameof(rule)),
            };
        }

        // The shared Domain rule takes no legal callers, so the API and the composition root, which build the principal and
        // register the parsers, are set aside from what it found; its text and its comparison with the list stay its own.
        private static InboundVerdict DomainInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string moduleNamespaces, string domain, string legalCallers, string compositionRoot, string outside)
        {
            var verdict = ModuleRules.DomainIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, domain, outside, AccessWording);

            return verdict with
            {
                Dependents = [.. verdict.Dependents.Where(dependent => !IsMatch(dependent.TypeName, legalCallers) && !IsMatch(dependent.TypeName, compositionRoot))],
            };
        }

        private static OutboundVerdict ApplicationOutboundRule(ModuleDefinition module, string application, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                application,
                TheLayersBelowTheApplicationAndThePlatform,
                listed,
                $"Types in Access's Application, namespaces matching \"{application}\", should not depend on any type in namespaces matching " +
                $"\"{TheLayersBelowTheApplicationAndThePlatform}\" except through the listed edges, because Access's Application decides who a user " +
                "is and what they may do, and reaches storage and the platform only through the listed ports and its own tables. An Application " +
                "type that holds a new DbSet or the database context, runs a module's adapter, shapes an API type or reaches a new platform " +
                "service ties every permission answer to how the rest of the system stores and shows its data. Read through a listed port and " +
                "return a Models type.");

        private static OutboundVerdict ModuleIntoAnotherContextRule(ModuleDefinition module, string moduleNamespaces, IReadOnlyCollection<string> listed)
            => new(
                IlDependencies.TypesIn(module, moduleNamespaces),
                [.. IlDependencies.DependentsIn(module, moduleNamespaces, IsAnotherContext)
                    .SelectMany(dependent => dependent.Targets.Select(reached => $"{dependent.TypeName} -> {reached}"))],
                listed,
                $"Types in namespaces matching \"{moduleNamespaces}\" should not depend on another context (Work Items, Forecasting, Flow metrics, " +
                "Tracker integration, Promises, the update code, Refinement, Reports or the write-back round) except through the listed edges, " +
                "because every context asks Access who the user is and what they may do. An Access type that reaches one of them turns that " +
                "dependency around, so a permission answer would depend on the code it guards. Only Licensing may be asked. Hand Access what it " +
                "needs as a value, or let the other context react to a domain event.",
                moduleNamespaces);

        private static bool IsAnotherContext(TypeReference type)
            => ModuleBoundaryChecks.IsATypeIn(type, TheOtherContexts)
                || (type.DeclaringType is null && TheWriteBackRound.Contains(type.FullName, StringComparer.Ordinal));

        private static bool IsMatch(string typeName, string pattern) => Regex.IsMatch(typeName, pattern, RegexOptions.None, PatternTimeout);

        private static string OutsideFixture(string name) => $"^{Regex.Escape(MadeUpOutside)}\\.{name}(\\.|$)";

        private static string OutsideFixtures(params string[] namespaces)
            => $"^{Regex.Escape(MadeUpOutside)}\\.({string.Join('|', namespaces)})(\\.|$)";

        private static void ThenTheRuleHolds(Action check, string what)
            => Assert.DoesNotThrow(
                () => check(),
                $"The rule for {what} failed; the failure above names each type or edge to fix. Why it matters: Access keeps its shape only " +
                "while every list matches what the compiled code reaches, in both directions. How to fix: undo the new dependency, or delete " +
                "the entry the failure names as no longer offending.");
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Domain
{
    internal sealed record AnAccessAnswer(int Level)
    {
        public bool Allows(int needed) => Level >= needed;
    }

    internal sealed class AnAccessAnswerAskingThePlatform(global::Lighthouse.Backend.Platform.Host.ILighthouseClock clock)
    {
        public int Describe() => clock.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Services
{
    internal sealed record ALeftBehindAccessAnswer(int Level);
}

namespace Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Application
{
    internal static class AnAccessAnswerService
    {
        public static bool Allows(int level, int needed) => new global::Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Domain.AnAccessAnswer(level).Allows(needed);
    }
}

namespace Lighthouse.Backend.AccessBoundariesFixtures.Api
{
    internal static class AnAccessController
    {
        public static bool Get(int level) =>
            new global::Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Domain.AnAccessAnswer(level).Allows(1)
            && global::Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Application.AnAccessAnswerService.Allows(level, 1);
    }

    [global::Lighthouse.Backend.Modules.Access.Adapters.Http.RbacGuard]
    internal sealed record AGuardedController(string Name);
}

namespace Lighthouse.Backend.AccessBoundariesFixtures.CompositionRoot
{
    internal static class AnAccessCompositionRoot
    {
        public static string Register(int level) => string.Join(
            ' ',
            typeof(global::Lighthouse.Backend.Modules.Access.Adapters.Http.ApiKeyAuthenticationHandler).Name,
            new global::Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Domain.AnAccessAnswer(level).Allows(1),
            global::Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Application.AnAccessAnswerService.Allows(level, 1));
    }
}

namespace Lighthouse.Backend.AccessBoundariesFixtures.ListedCallers
{
    internal static class AListedDomainCaller
    {
        public static bool Ask(int level) => new global::Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Domain.AnAccessAnswer(level).Allows(2);
    }

    internal static class AListedApplicationCaller
    {
        public static bool Ask(int level) => global::Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Application.AnAccessAnswerService.Allows(level, 2);
    }
}

namespace Lighthouse.Backend.AccessBoundariesFixtures.DomainNewcomer
{
    internal static class ANewcomerReachingTheDomain
    {
        public static bool Ask(int level) => new global::Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Domain.AnAccessAnswer(level).Allows(3);
    }
}

namespace Lighthouse.Backend.AccessBoundariesFixtures.ApplicationNewcomer
{
    internal static class ANewcomerReachingTheApplication
    {
        public static async Task<bool> AskAsync(int level)
        {
            await Task.Yield();
            return global::Lighthouse.Backend.Modules.AccessFixtures.Boundaries.Application.AnAccessAnswerService.Allows(level, 3);
        }
    }
}

namespace Lighthouse.Backend.AccessBoundariesFixtures.AttributeOnlyNewcomer
{
    [global::Lighthouse.Backend.Modules.Access.Adapters.Http.RbacGuard]
    internal sealed record AReportExportGuardedOnlyByAnAttribute(string Name);
}

namespace Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.Compliant.Application
{
    internal sealed class ACompliantAccessService(
        global::Lighthouse.Backend.Platform.Host.ILighthouseClock clock,
        global::Lighthouse.Backend.Modules.Licensing.Application.ILicenseService licence)
    {
        public string Describe() => $"{clock.GetHashCode()} {licence.CanUsePremiumFeatures()}";
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.HoldingADbSet.Application
{
    internal sealed class AnAccessServiceHoldingADbSet(global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Feature> features)
    {
        public global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Feature> Features { get; } = features;
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.AskingTheAppSettings.Application
{
    internal sealed class AnAccessServiceAskingTheAppSettings(global::Lighthouse.Backend.Services.Interfaces.IAppSettingService settings)
    {
        public int Describe() => settings.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.RunningAForecastingAdapter.Application
{
    internal static class AnAccessServiceRunningAForecastingAdapter
    {
        public static string Describe() => typeof(global::Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater).Name;
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.AskingWorkItems.Application
{
    internal sealed class AnAccessServiceAskingWorkItems(global::Lighthouse.Backend.Modules.WorkItems.Application.IWorkItemService workItems)
    {
        public int Describe() => workItems.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.TouchingTheUpdateCode.Application
{
    internal static class AnAccessServiceTouchingTheUpdateCode
    {
        public static string Describe() => typeof(global::Lighthouse.Backend.Platform.UpdateQueue.UpdateKey).Name;
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.BoundariesOutbound.RunningTheWriteBackRound.Application
{
    internal sealed class AnAccessServiceRunningTheWriteBackRound(global::Lighthouse.Backend.Services.Interfaces.IWriteBackTriggerService trigger)
    {
        public int Describe() => trigger.GetHashCode();
    }
}
