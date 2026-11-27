using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The composition root names the ten contexts only through their start-up modules. Beside the module it names exactly four
    /// kinds of context type, each listed by full name and compared both ways: the domain-event handlers of its one ordered block,
    /// the statements that wire two contexts together, the services it looks up to set the host up, and the types it needs to set
    /// the host up itself (the authentication handlers and selectors, the filter of the controllers, the claim parser of the
    /// sign-in event). Any other context type it names is a registration that has not moved into its module. The listed ten
    /// equal the contexts the application holds, so a new context cannot keep its start-up in the composition root unseen. The
    /// same check runs over made-up roots and a made-up context, one made-up root per way it can be broken.
    /// </summary>
    [TestFixture]
    public class ProgramNamesModulesOnlyArchUnitTest
    {
        private const string TheCompositionRoot = "Lighthouse.Backend.Program";

        private const string TheMadeUpRoot = "Lighthouse.Backend.Tests.Architecture.ProgramNamesFixtures";

        private const string TheMadeUpModulesRoot = TheMadeUpRoot + ".Modules";

        private const string TheMadeUpSigma = TheMadeUpModulesRoot + ".Sigma.Application";

        private const string ModuleCategory = "its start-up module";

        private const string HandlerCategory = "handler of the ordered block";

        private const string WiringCategory = "cross-context wiring";

        private const string LookupCategory = "host set-up lookup";

        private const string HostTypeCategory = "host set-up type";

        private const string ARootNamingATypeOfEachContext = TheMadeUpRoot + ".Roots.ARootNamingATypeOfEachContext";

        private const string ARootNamingATypeInsideALambda = TheMadeUpRoot + ".Roots.ARootNamingATypeInsideALambda";

        private const string ARootStartingSigmaThroughItsModule = TheMadeUpRoot + ".Roots.ARootStartingSigmaThroughItsModule";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(ProgramNamesModulesOnlyArchUnitTest).Assembly));

        private static string TheModulesRoot => ContextContractsMembership.TheModulesRoot;

        private static IReadOnlyList<string> TheTenContexts =>
            ["Access", "FlowMetrics", "Forecasting", "Licensing", "OrganisationSetup", "Promises", "Refinement", "Reports", "TrackerIntegration", "WorkItems"];

        // The dispatcher runs the handlers of an event in the order the composition root registers them, across contexts, so they
        // stay in its one ordered block. The memo of the flow-metrics context is one of them: it stands as a handler of two refresh
        // events by forwarding to its one instance.
        private static IEnumerable<Allowed> TheHandlersOfTheOrderedBlock =>
        [
            Of("FlowMetrics", HandlerCategory, "Application.Policies.BlackoutConfigurationChangedMetricsInvalidationHandler"),
            Of("FlowMetrics", HandlerCategory, "Application.Policies.BlockedCountSnapshotRecordingHandler"),
            Of("FlowMetrics", HandlerCategory, "Application.Policies.DemoBlockedHistoryBackfillHandler"),
            Of("FlowMetrics", HandlerCategory, "Application.Policies.DemoPercentilesBackfillHandler"),
            Of("FlowMetrics", HandlerCategory, "Application.Policies.PercentilesOverTimeRecordingHandler"),
            Of("FlowMetrics", HandlerCategory, "Application.Policies.PortfolioFeaturesRefreshedMetricsInvalidationHandler"),
            Of("FlowMetrics", HandlerCategory, "Application.Policies.ProcessBehaviorRecordingHandler"),
            Of("FlowMetrics", HandlerCategory, "Application.Policies.TeamDataRefreshedTeamMetricsUpdateHandler"),
            Of("FlowMetrics", HandlerCategory, "Application.ReconstructionMemo"),
            Of("Forecasting", HandlerCategory, "Application.Policies.FeatureOrderingPolicyChangedForecastTriggerHandler"),
            Of("Forecasting", HandlerCategory, "Application.Policies.FeatureRankChangedForecastTriggerHandler"),
            Of("Forecasting", HandlerCategory, "Application.Policies.TeamDataRefreshedForecastTriggerHandler"),
            Of("Forecasting", HandlerCategory, "Application.Policies.TeamDeletedForecastRetriggerHandler"),
            Of("Promises", HandlerCategory, "Application.Policies.DeliveryForecastPublishingHandler"),
            Of("Promises", HandlerCategory, "Application.Policies.DeliveryMetricSnapshotRecordingHandler"),
            Of("WorkItems", HandlerCategory, "Application.Policies.ConnectionFetchShapeChangedUpdateTriggerHandler"),
            Of("WorkItems", HandlerCategory, "Application.Policies.FeatureBlockedTransitionCaptureHandler"),
            Of("WorkItems", HandlerCategory, "Application.Policies.FeatureBlockedTransitionCloseHandler"),
            Of("WorkItems", HandlerCategory, "Application.Policies.PortfolioDeletedOrphanedFeatureCleanupHandler"),
            Of("WorkItems", HandlerCategory, "Application.Policies.TeamDeletedRefreshLogCleanupHandler"),
            Of("WorkItems", HandlerCategory, "Application.Policies.WorkItemBlockedTransitionCaptureHandler"),
            Of("WorkItems", HandlerCategory, "Application.Policies.WorkItemBlockedTransitionCloseHandler"),
        ];

        // A statement that builds one context's service from another context's type, or registers it as the other context's
        // service, names two contexts; no context's own module may name another context, so the statement stays here.
        private static IEnumerable<Allowed> TheStatementsWiringTwoContexts =>
        [
            Of("FlowMetrics", WiringCategory, "Application.IForecastDataSufficiency"),
            Of("FlowMetrics", WiringCategory, "Application.IHowManyForecaster"),
            Of("FlowMetrics", WiringCategory, "Application.IThroughputFilter"),
            Of("Forecasting", WiringCategory, "Application.ForecastFilterRuleService"),
            Of("Forecasting", WiringCategory, "Application.ForecastingAnswersToFlowMetrics"),
            Of("Promises", WiringCategory, "Application.DeliveryForecastBlockRenderer"),
            Of("TrackerIntegration", WiringCategory, "Application.IDescriptionBlockMerger"),
        ];

        // The forecast thread count is read for the start-up banner; the group writer is called by the sign-in event of the
        // identity provider. Neither is a registration, and both are asked of the container where the host is set up.
        private static IEnumerable<Allowed> TheServicesLookedUpToSetTheHostUp =>
        [
            Of("Access", LookupCategory, "Application.IOidcGroupSnapshotWriter"),
            Of("Forecasting", LookupCategory, "Application.ForecastLane"),
        ];

        // Types the host needs outside the container to set itself up: the authentication schemes and their selector, the filter
        // of the controllers, and the claim parser the sign-in event applies to the token.
        private static IEnumerable<Allowed> TheTypesThatSetTheHostUp =>
        [
            Of("Access", HostTypeCategory, "Adapters.Http.ApiKeyAuthenticationHandler"),
            Of("Access", HostTypeCategory, "Adapters.Http.BlockedModeFilter"),
            Of("Access", HostTypeCategory, "Adapters.Http.DisabledAuthenticationHandler"),
            Of("Access", HostTypeCategory, "Adapters.Http.SmartAuthSchemeSelector"),
            Of("Access", HostTypeCategory, "Domain.GroupClaimParser"),
        ];

        private static IEnumerable<Allowed> EveryAllowedEntry =>
        [
            .. TheHandlersOfTheOrderedBlock,
            .. TheStatementsWiringTwoContexts,
            .. TheServicesLookedUpToSetTheHostUp,
            .. TheTypesThatSetTheHostUp,
        ];

        private static IEnumerable<TestCaseData> EveryEntryTheCompositionRootMayName
            => TheTenContexts.Select(context => new TestCaseData(context, ModuleCategory, ModuleTypeOf(TheModulesRoot, context)))
                .Concat(EveryAllowedEntry.Select(entry => new TestCaseData(entry.Context, entry.Category, entry.Type)));

        private static IEnumerable<TestCaseData> OneTypeOfEachContextOffTheLists
            =>
            [
                new TestCaseData("Access", typeof(Backend.Modules.Access.Application.ApiKeyService).FullName),
                new TestCaseData("FlowMetrics", typeof(Backend.Modules.FlowMetrics.Application.IPercentilesOverTimeSeriesQuery).FullName),
                new TestCaseData("Forecasting", typeof(Backend.Modules.Forecasting.Application.IForecastRealityCheckService).FullName),
                new TestCaseData("Licensing", typeof(Backend.Modules.Licensing.Contracts.ILicenseService).FullName),
                new TestCaseData("OrganisationSetup", typeof(Backend.Modules.OrganisationSetup.Application.OptionalFeatures.OptionalFeatureApplierRegistry).FullName),
                new TestCaseData("Promises", typeof(Backend.Modules.Promises.Application.IDeliveryRuleService).FullName),
                new TestCaseData("Refinement", typeof(Backend.Modules.Refinement.Application.RefinementList).FullName),
                new TestCaseData("Reports", typeof(Backend.Modules.Reports.Application.Metrics.ReportMetricCatalog).FullName),
                new TestCaseData("TrackerIntegration", typeof(Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthCadence).FullName),
                new TestCaseData("WorkItems", typeof(Backend.Modules.WorkItems.Application.Dependencies.FeatureDependencyRendering).FullName),
            ];

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(TheTenContexts))]
        public void TheCompositionRoot_NamesOfEachContextOnlyItsModuleItsHandlersItsWiringItsHostLookupsAndItsHostSetUpTypes(string context)
        {
            var verdict = WhenWhatTheCompositionRootNamesIsComparedWithTheLists(ProductionModule.Value, TheCompositionRoot, TheModulesRoot, context, MayName(context));

            ThenTheCompositionRootNamesOnlyTheListed(verdict, TheCompositionRoot, context);
        }

        [TestCaseSource(nameof(EveryEntryTheCompositionRootMayName))]
        public void AnEntryTheCompositionRootMayName_IsNamedOnceItIsTakenOffItsList(string context, string category, string entry)
        {
            var listed = MayName(context);
            var full = WhenWhatTheCompositionRootNamesIsComparedWithTheLists(ProductionModule.Value, TheCompositionRoot, TheModulesRoot, context, listed);

            var withoutTheEntry = WhenWhatTheCompositionRootNamesIsComparedWithTheLists(
                ProductionModule.Value, TheCompositionRoot, TheModulesRoot, context, [.. listed.Where(allowed => allowed != entry)]);

            ThenOnlyTheEntryTakenOffIsAddedToWhatIsNamed(entry, category, full, withoutTheEntry);
        }

        [Test]
        public void TheTenListedContexts_AreTheTenPackagedContexts()
        {
            var packaged = PackagedContexts.InTheAssembly(ProductionModule.Value);

            ThenTheListedContextsAreEveryPackagedContextOnce(packaged);
        }

        [TestCaseSource(nameof(OneTypeOfEachContextOffTheLists))]
        public void AMadeUpCompositionRootNamingATypeOfOneContextOffTheLists_IsReportedNamingIt(string context, string namedType)
        {
            var verdict = WhenWhatTheCompositionRootNamesIsComparedWithTheLists(TestModule.Value, ARootNamingATypeOfEachContext, TheModulesRoot, context, [], ProductionModule.Value);

            ThenOnlyTheNamedTypeIsReported(verdict, context, namedType);
        }

        [Test]
        public void AMadeUpCompositionRootNamingATypeOnlyInsideALambda_IsReportedNamingIt()
        {
            var licensingService = typeof(Backend.Modules.Licensing.Contracts.ILicenseService).FullName!;

            var verdict = WhenWhatTheCompositionRootNamesIsComparedWithTheLists(TestModule.Value, ARootNamingATypeInsideALambda, TheModulesRoot, "Licensing", [], ProductionModule.Value);

            ThenOnlyTheNamedTypeIsReported(verdict, "Licensing", licensingService);
        }

        [Test]
        public void AMadeUpCompositionRootStartingAContextOnlyThroughItsModule_IsNotReported()
        {
            var verdict = WhenWhatTheCompositionRootNamesIsComparedWithTheLists(TestModule.Value, ARootStartingSigmaThroughItsModule, TheMadeUpModulesRoot, "Sigma", [ModuleTypeOf(TheMadeUpModulesRoot, "Sigma")]);

            ThenNothingIsReported(verdict);
        }

        [Test]
        public void AListedTypeAMadeUpCompositionRootNoLongerNames_IsReportedAsGone()
        {
            var goneType = $"{TheMadeUpSigma}.AGoneHandler";

            var verdict = WhenWhatTheCompositionRootNamesIsComparedWithTheLists(
                TestModule.Value, ARootStartingSigmaThroughItsModule, TheMadeUpModulesRoot, "Sigma", [ModuleTypeOf(TheMadeUpModulesRoot, "Sigma"), goneType]);

            ThenOnlyTheGoneTypeIsReported(verdict, goneType);
        }

        private static Allowed Of(string context, string category, string typeInTheContext) => new(context, category, $"{TheModulesRoot}.{context}.{typeInTheContext}");

        private static IReadOnlyList<string> MayName(string context)
            => [ModuleTypeOf(TheModulesRoot, context), .. EveryAllowedEntry.Where(entry => entry.Context == context).Select(entry => entry.Type)];

        private static string ModuleTypeOf(string modulesRoot, string context) => $"{modulesRoot}.{context}.{context}Module";

        private static string ContextPattern(string modulesRoot, string context) => $"^{Regex.Escape(modulesRoot)}\\.{context}(\\.|$)";

        private static string Listed(IEnumerable<string> names) => $"[{string.Join(" | ", names)}]";

        // Every top-level type of the context that the root, its nested types and the closures the compiler made for it name.
        private static NamedTypesVerdict WhenWhatTheCompositionRootNamesIsComparedWithTheLists(ModuleDefinition module, string compositionRoot, string modulesRoot, string context, IReadOnlyCollection<string> mayName, ModuleDefinition? contextHeldIn = null)
        {
            var rootNamespace = compositionRoot[..compositionRoot.LastIndexOf('.')];
            var contextPattern = ContextPattern(modulesRoot, context);

            Assert.That(IlDependencies.TypesIn(contextHeldIn ?? module, contextPattern), Is.Not.Empty,
                $"No type lives in {modulesRoot}.{context}, so the check read what the composition root names of a context the code does not hold. " +
                "Why it matters: a check of a context that is not there passes while it guards nothing. How to fix: name a context the compiled code holds.");

            var named = IlDependencies.DependentsIn(module, $"^{Regex.Escape(rootNamespace)}$", type => IlDependencies.ResidesInNamespaceMatching(type, contextPattern))
                .Where(dependent => dependent.TypeName.Split('+')[0] == compositionRoot)
                .SelectMany(dependent => dependent.Targets)
                .Select(target => target.Split('+')[0])
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();

            return new NamedTypesVerdict(
                [.. named.Except(mayName, StringComparer.Ordinal)],
                [.. mayName.Except(named, StringComparer.Ordinal).Order(StringComparer.Ordinal)]);
        }

        private static void ThenTheCompositionRootNamesOnlyTheListed(NamedTypesVerdict verdict, string compositionRoot, string context)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Unlisted, Is.Empty,
                    $"The composition root {compositionRoot} names these types of the {context} context beside its module: {Listed(verdict.Unlisted)}. " +
                    "Why it matters: the context's start-up is meant to live in its own module, so a name left in the composition root is a registration or a lookup its owner cannot see. " +
                    $"How to fix: move the lines that name them behind a method of the {context} module called at the same point; only a handler of the ordered block, a statement naming two contexts, a service the host looks up to set itself up, or a type that sets the host up stays, and is listed here.");
                Assert.That(verdict.Gone, Is.Empty,
                    $"These listed types of the {context} context are no longer named by the composition root: {Listed(verdict.Gone)}. " +
                    "Why it matters: a list entry the code no longer needs lets the same name come back unseen. How to fix: delete the entries from their list.");
            }
        }

        private static void ThenOnlyTheEntryTakenOffIsAddedToWhatIsNamed(string entry, string category, NamedTypesVerdict full, NamedTypesVerdict withoutTheEntry)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(withoutTheEntry.Unlisted, Is.EqualTo(full.Unlisted.Append(entry)),
                    $"With the {category} {entry} taken off its list, the check named {Listed(withoutTheEntry.Unlisted)} as named and not listed. Expected {Listed(full.Unlisted.Append(entry))}. " +
                    "Why it matters: an entry the check never compares lets the same name come back unseen once it is deleted. " +
                    "How to fix: compare every type the composition root names with the lists.");
                Assert.That(withoutTheEntry.Gone, Is.EqualTo(full.Gone),
                    $"With the {category} {entry} taken off its list, the check named {Listed(withoutTheEntry.Gone)} as gone. Why it matters: taking one entry off must change nothing else. How to fix: compare each entry on its own.");
            }
        }

        private static void ThenTheListedContextsAreEveryPackagedContextOnce(IReadOnlyList<string> packaged)
        {
            Assert.That(TheTenContexts.Order(StringComparer.Ordinal), Is.EqualTo(packaged.Order(StringComparer.Ordinal)),
                $"The rule lists {Listed(TheTenContexts.Order(StringComparer.Ordinal))}; the contexts the application holds are {Listed(packaged.Order(StringComparer.Ordinal))}. " +
                "Why it matters: a context the rule does not list can keep its registrations in the composition root unseen. How to fix: list every context the application holds.");
        }

        private static void ThenOnlyTheNamedTypeIsReported(NamedTypesVerdict verdict, string context, string namedType)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Unlisted, Is.EqualTo(new[] { namedType }),
                    $"For the {context} context the check named {Listed(verdict.Unlisted)} as named by a made-up composition root and not listed. Expected exactly {namedType}. " +
                    "Why it matters: a name left in a composition root must be reported under its own context, in whatever way it is named. " +
                    "How to fix: read every type the composition root, its nested types and its closures name and match the context by its whole name.");
                Assert.That(verdict.Gone, Is.Empty,
                    "The check named a gone entry for a made-up composition root checked with an empty list. Why it matters: with nothing listed nothing can be gone. How to fix: compare the list with what is named both ways.");
            }
        }

        private static void ThenNothingIsReported(NamedTypesVerdict verdict)
        {
            Assert.That(verdict.Unlisted.Concat(verdict.Gone), Is.Empty,
                $"The check reported unlisted {Listed(verdict.Unlisted)} and gone {Listed(verdict.Gone)} for a made-up composition root that starts Sigma only through its module. " +
                "Why it matters: a compliant composition root must pass, or the lists grow with entries nobody needs. How to fix: report only what the lists do not allow.");
        }

        private static void ThenOnlyTheGoneTypeIsReported(NamedTypesVerdict verdict, string goneType)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Gone, Is.EqualTo(new[] { goneType }),
                    $"The check named {Listed(verdict.Gone)} as listed and no longer named. Expected exactly the listed type the made-up composition root does not name. " +
                    "Why it matters: a list compared only one way keeps dead entries that hide a later name. How to fix: compare the list both ways.");
                Assert.That(verdict.Unlisted, Is.Empty,
                    $"The check reported {Listed(verdict.Unlisted)} for a made-up composition root that starts Sigma only through its module. Why it matters: a module call is the allowed way. How to fix: list the module with the allowed names.");
            }
        }

        private sealed record Allowed(string Context, string Category, string Type);

        private sealed record NamedTypesVerdict(List<string> Unlisted, List<string> Gone);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ProgramNamesFixtures.Modules.Sigma
{
    public static class SigmaModule
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSigma(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
            => Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<Application.SigmaService>(services);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ProgramNamesFixtures.Modules.Sigma.Application
{
    public sealed class SigmaService
    {
        public int Serve(int days) => days + 31;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ProgramNamesFixtures.Shared
{
    public sealed class AClockOutsideTheContexts
    {
        public int Tick(int days) => days + 32;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ProgramNamesFixtures.Roots
{
    using Lighthouse.Backend.Tests.Architecture.ProgramNamesFixtures.Modules.Sigma;
    using Microsoft.Extensions.DependencyInjection;

    public static class ARootNamingATypeOfEachContext
    {
        public static void Compose(IServiceCollection services)
        {
            services.AddScoped<Lighthouse.Backend.Modules.Access.Application.ApiKeyService>();
            services.AddScoped<Lighthouse.Backend.Modules.FlowMetrics.Application.IPercentilesOverTimeSeriesQuery>(_ => null!);
            services.AddScoped<Lighthouse.Backend.Modules.Forecasting.Application.IForecastRealityCheckService>(_ => null!);
            services.AddScoped<Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService>(_ => null!);
            services.AddScoped<Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.OptionalFeatureApplierRegistry>();
            services.AddScoped<Lighthouse.Backend.Modules.Promises.Application.IDeliveryRuleService>(_ => null!);
            services.AddScoped<Lighthouse.Backend.Modules.Refinement.Application.RefinementList>();
            services.AddScoped<Lighthouse.Backend.Modules.Reports.Application.Metrics.ReportMetricCatalog>();
            services.AddScoped<Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthCadence>();
            services.AddScoped<Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.FeatureDependencyRendering>();
            services.AddSingleton<Shared.AClockOutsideTheContexts>();
        }
    }

    public static class ARootNamingATypeInsideALambda
    {
        public static void Compose(IServiceCollection services)
            => services.AddScoped(provider =>
            {
                _ = provider.GetRequiredService<Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService>();
                return new Shared.AClockOutsideTheContexts();
            });
    }

    public static class ARootStartingSigmaThroughItsModule
    {
        public static void Compose(IServiceCollection services)
        {
            services.AddSigma();
            services.AddSingleton<Shared.AClockOutsideTheContexts>();
        }
    }
}
