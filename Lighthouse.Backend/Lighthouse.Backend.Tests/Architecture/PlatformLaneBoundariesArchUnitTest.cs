using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.API;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Modules.Harbour.Adapters;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.Beacons;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.Compliant;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.Lanterns;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.ReachesAnAdapter;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.ReachesTheApi;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Platform.ReachesUnlistedServices;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Services;
using Lighthouse.Backend.Tests.Architecture.PlatformLaneBoundaryFixtures.Services.Lanterns;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The platform lane, every type under Lighthouse.Backend.Platform, is infrastructure below the contexts, and its
    /// direction is checked from the compiled code: each type the platform membership map lists lives in its
    /// Platform area and none is left under Services; the lane reaches no API type and no context's Adapters; and it
    /// reaches code still in Services only through the edges listed here, compared both ways. Each rule also runs over
    /// made-up types, so a rule that stopped seeing an offender, or a list entry that stopped mattering, fails here.
    /// </summary>
    /// <remarks>
    /// The namespace patterns are properties rather than fields, so the check that every rule pattern field sees the
    /// platform move does not read them as old homes of the moved types.
    /// </remarks>
    [TestFixture]
    public class PlatformLaneBoundariesArchUnitTest
    {
        // The edges from the platform lane into code still in Services, as the compiled code shows them. The list only shrinks.
        private static readonly string[] TheLanesEdgesIntoServices =
        [
            "Lighthouse.Backend.Platform.DemoData.DemoDataService -> Lighthouse.Backend.Services.Interfaces.Repositories.IDeliveryMetricSnapshotRepository",
            "Lighthouse.Backend.Platform.DemoData.DemoDataService -> Lighthouse.Backend.Services.Interfaces.Repositories.IDeliveryRepository",
            "Lighthouse.Backend.Platform.DemoData.DemoDataService -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Platform.DemoData.DemoDataService -> Lighthouse.Backend.Services.Interfaces.Repositories.ISizingLogRepository",
            "Lighthouse.Backend.Platform.Host.SystemInfoService -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService",
            "Lighthouse.Backend.Platform.Seeding.InstallTimestampSeeder -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService",
            "Lighthouse.Backend.Platform.UpdateQueue.UpdateTaskNaming -> Lighthouse.Backend.Services.Interfaces.Repositories.IPortfolioRepository",
            "Lighthouse.Backend.Platform.UpdateQueue.UpdateTaskNaming -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Platform.UsageData.UsageDataConsentPruningService -> Lighthouse.Backend.Services.Interfaces.Repositories.IUsageDataConsentRepository",
            "Lighthouse.Backend.Platform.UsageData.UsageDataConsentService -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService",
            "Lighthouse.Backend.Platform.UsageData.UsageDataConsentService -> Lighthouse.Backend.Services.Interfaces.Repositories.IUsageDataConsentRepository",
            "Lighthouse.Backend.Platform.UsageData.UsageDataGate -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Platform.UsageData.UsageDataGate -> Lighthouse.Backend.Services.Interfaces.Repositories.IUsageDataConsentRepository",
            "Lighthouse.Backend.Platform.UsageData.UsageDataMasterSwitch -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PlatformLaneBoundariesArchUnitTest).Assembly));

        private static string ThePlatformLane => @"^Lighthouse\.Backend\.Platform(\.|$)";

        private static string TheApi => @"^Lighthouse\.Backend\.API(\.|$)";

        private static string AnyContextsAdapters => @"^Lighthouse\.Backend\.Modules\.[^.]+\.Adapters(\.|$)";

        private static string TheServices => @"^Lighthouse\.Backend\.Services(\.|$)";

        private static string TheServicesAndOrganisationSetUp => @"^Lighthouse\.Backend\.Services(\.|$)|^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.|$)";

        private static string MadeUpRoot => typeof(AnEndpoint).Namespace![..^".API".Length];

        private static string MadeUpApi => NamespaceAndBelow(typeof(AnEndpoint));

        private static string MadeUpContextsAdapters => $@"^{Regex.Escape(MadeUpRoot)}\.Modules\.[^.]+\.Adapters(\.|$)";

        private static string MadeUpServices => NamespaceAndBelow(typeof(AListedServicesHelper));

        private static string MadeUpLanterns => typeof(ALanternTrimmer).Namespace!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryTypeTheMapListsForThePlatformLane_LivesInItsPlatformArea_AndNoneIsLeftUnderServices()
        {
            var verdicts = PlatformLaneMembership.MovingTypes
                .GroupBy(type => type.NewNamespace, StringComparer.Ordinal)
                .Select(area => (Area: area.Key, Verdict: EachListedTypeLivesInItsArea(ProductionModule.Value, area.Key, [.. area.Select(type => type.Name)], TheServices)))
                .ToList();

            ThenEveryAreaPasses(verdicts);
        }

        [Test]
        public void ThePlatformLane_ReachesNoApiType()
        {
            var verdict = ReachesNoApiType(ProductionModule.Value, ThePlatformLane, TheApi);

            verdict.Check();
        }

        [Test]
        public void ThePlatformLane_ReachesNoContextsAdapters()
        {
            var verdict = ReachesNoContextsAdapters(ProductionModule.Value, ThePlatformLane, AnyContextsAdapters);

            verdict.Check();
        }

        [Test]
        public void ThePlatformLane_ReachesCodeStillInServices_OnlyThroughTheListedEdges()
        {
            var verdict = ReachesServicesOnlyThroughTheList(ProductionModule.Value, ThePlatformLane, TheServicesAndOrganisationSetUp, TheLanesEdgesIntoServices);

            verdict.Check();
        }

        [Test]
        public void AMadeUpLaneThatOnlyDoesWhatIsAllowed_PassesEveryLaneRule()
        {
            var lane = NamespaceAndBelow(typeof(ACompliantLaneService));
            string[] listed = [Edge(typeof(ACompliantLaneService), typeof(AListedServicesHelper)), Edge(typeof(ACompliantLaneService.AHeldWork), typeof(AListedServicesHelper))];

            var layout = EachListedTypeLivesInItsArea(TestModule.Value, MadeUpLanterns, [nameof(ALanternTrimmer)], MadeUpServices);
            var noApi = ReachesNoApiType(TestModule.Value, lane, MadeUpApi);
            var noAdapters = ReachesNoContextsAdapters(TestModule.Value, lane, MadeUpContextsAdapters);
            var services = ReachesServicesOnlyThroughTheList(TestModule.Value, lane, MadeUpServices, listed);

            ThenTheEdgesFoundAreExactly(services, listed);
            ThenEveryRulePasses(layout.Check, noApi.Check, noAdapters.Check, services.Check);
        }

        [Test]
        public void AMadeUpListedTypeLeftUnderServices_FailsTheLayoutRule_NamingItsExpectedArea()
        {
            var verdict = EachListedTypeLivesInItsArea(TestModule.Value, MadeUpLanterns, [nameof(ALanternKeeper)], MadeUpServices);

            var failure = ThenTheLaneRuleFails(verdict.Check, typeof(ALanternKeeper).FullName!);

            Assert.That(
                failure,
                Does.Contain($"{typeof(ALanternKeeper).FullName} is left behind instead of moving to {MadeUpLanterns}.{nameof(ALanternKeeper)}"),
                $"Expected the failure to name {typeof(ALanternKeeper).FullName} as left behind, with its expected place {MadeUpLanterns}.{nameof(ALanternKeeper)}. " +
                "Why it matters: a reader must see which type to move and into which Platform area. How to fix: report each listed type " +
                "found under Services with the Platform area the map gives it.");
        }

        [Test]
        public void AMadeUpListedTypeInAnotherArea_FailsTheLayoutRule_NamingItMissingFromItsExpectedArea()
        {
            var verdict = EachListedTypeLivesInItsArea(TestModule.Value, MadeUpLanterns, [nameof(ABeaconLighter)], MadeUpServices);

            var failure = ThenTheLaneRuleFails(verdict.Check, $"{MadeUpLanterns}.{nameof(ABeaconLighter)}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure,
                    Does.Contain($"{MadeUpLanterns}.{nameof(ABeaconLighter)} is named by the rule but does not exist"),
                    $"Expected the failure to name {MadeUpLanterns}.{nameof(ABeaconLighter)} as missing, since the type sits in {typeof(ABeaconLighter).Namespace}. " +
                    "Why it matters: a listed type in the wrong Platform area hides which area owns it. How to fix: report every listed type " +
                    "not found at its expected Platform area by that expected full name.");
                Assert.That(
                    failure,
                    Does.Not.Contain("is left behind"),
                    "Nothing of that name is under Services, so the failure must not name a type as left behind.");
            }
        }

        [Test]
        public void AMadeUpLaneTypeReachingAnApiType_FailsTheNoApiRule_NamingTheEdge()
        {
            var verdict = ReachesNoApiType(TestModule.Value, NamespaceAndBelow(typeof(ALaneServiceReachingTheApi)), MadeUpApi);

            ThenTheLaneRuleFailsNamingTheNewcomer(verdict.Check, Edge(typeof(ALaneServiceReachingTheApi), typeof(AnEndpoint)));
        }

        [Test]
        public void AMadeUpLaneTypeReachingAContextsAdapter_FailsTheNoAdaptersRule_NamingTheEdge()
        {
            var verdict = ReachesNoContextsAdapters(TestModule.Value, NamespaceAndBelow(typeof(ALaneServiceReachingAnAdapter)), MadeUpContextsAdapters);

            ThenTheLaneRuleFailsNamingTheNewcomer(verdict.Check, Edge(typeof(ALaneServiceReachingAnAdapter), typeof(AHarbourStore)));
        }

        [Test]
        public void AMadeUpLaneTypeReachingAnUnlistedServicesType_FailsTheServicesRule_NamingItNotOnTheList()
        {
            var lane = NamespaceAndBelow(typeof(ALaneServiceWithANewServicesEdge));
            var verdict = ReachesServicesOnlyThroughTheList(TestModule.Value, lane, MadeUpServices, [Edge(typeof(ALaneServiceWithANewServicesEdge), typeof(AListedServicesHelper))]);

            ThenTheLaneRuleFailsNamingTheNewcomer(verdict.Check, Edge(typeof(ALaneServiceWithANewServicesEdge.ANewcomer), typeof(AnUnlistedServicesHelper)));
        }

        [TestCaseSource(nameof(EveryListPosition))]
        public void EachListedEdgeOfTheLane_LeftOffTheList_FailsTheServicesRule_NamingItNotOnTheList(int position)
        {
            var listMinusTheEntry = TheLanesEdgesIntoServices.Where((_, index) => index != position).ToList();

            var verdict = ReachesServicesOnlyThroughTheList(ProductionModule.Value, ThePlatformLane, TheServicesAndOrganisationSetUp, listMinusTheEntry);

            ThenTheLaneRuleFailsNamingTheNewcomer(verdict.Check, TheLanesEdgesIntoServices[position]);
        }

        [TestCaseSource(nameof(EveryListPosition))]
        public void EachListedEdgeOfTheLane_WithAGoneEdgeOfItsDependentAdded_FailsTheServicesRule_NamingItNoLongerOffending(int position)
        {
            var dependent = TheLanesEdgesIntoServices[position].Split(" -> ")[0];
            var goneEdge = $"{dependent} -> {typeof(AGoneServicesHelper).FullName}";

            var verdict = ReachesServicesOnlyThroughTheList(ProductionModule.Value, ThePlatformLane, TheServicesAndOrganisationSetUp, [.. TheLanesEdgesIntoServices, goneEdge]);

            ThenTheRuleFailsNamingTheStaleEntryForDeletion(verdict.Check, goneEdge);
        }

        [Test]
        public void ALaneRuleHandedALaneHoldingNoType_FailsAsHavingCheckedNothing()
        {
            var emptyLane = $@"^{Regex.Escape(MadeUpRoot)}\.Platform\.HoldsNoType(\.|$)";

            var verdict = ReachesServicesOnlyThroughTheList(TestModule.Value, emptyLane, MadeUpServices, []);

            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                "The lane rule passed although the lane it was handed holds no type. Why it matters: a lane namespace that was renamed or " +
                "emptied would leave the rule green while it guards nothing. How to fix: fail when the lane examines no type.");

            Assert.That(
                failure!.Message,
                Does.Contain("so the rule checked nothing"),
                "Expected the failure to say the rule checked nothing, so the reader points the rule at the lane's namespace again.");
        }

        private static IEnumerable<int> EveryListPosition() => Enumerable.Range(0, TheLanesEdgesIntoServices.Length);

        private static KernelRuleVerdict EachListedTypeLivesInItsArea(ModuleDefinition module, string areaNamespace, IReadOnlyCollection<string> typeNames, string services)
            => ModuleRules.TypesLiveInTheirLayer(module, areaNamespace, typeNames, services) with
            {
                Rule =
                    $"Each of these types of the platform lane should live in namespace \"{areaNamespace}\", and none should be left in " +
                    $"namespaces matching \"{services}\", because the platform lane shows where its parts live only when every type the " +
                    "platform membership map lists sits in the Platform area the map gives it. A type left behind under Services, or a " +
                    "second copy of it, keeps its callers on the old home. Move the file into its Platform/<Area> folder and change its " +
                    "namespace line to match, nothing else.",
            };

        private static OutboundVerdict ReachesNoApiType(ModuleDefinition module, string lane, string api)
            => ModuleRules.OutboundEdges(
                module,
                lane,
                api,
                [],
                $"Types of the platform lane, in namespaces matching \"{lane}\", should reach no type in namespaces matching \"{api}\", " +
                "because the platform lane is infrastructure below the contexts and the API sits on top of them: a lane type that reaches " +
                "a controller or an API contract ties the lane to how requests are answered. Move what the lane needs out of the API, or " +
                "let the API hand it in; this list stays empty.");

        private static OutboundVerdict ReachesNoContextsAdapters(ModuleDefinition module, string lane, string adapters)
            => ModuleRules.OutboundEdges(
                module,
                lane,
                adapters,
                [],
                $"Types of the platform lane, in namespaces matching \"{lane}\", should reach no type in namespaces matching \"{adapters}\", " +
                "because a context's Adapters are how that context stores its data and runs in the background, and the platform lane " +
                "below the contexts must not depend on them. Reach the context through its Application contract or a domain event it " +
                "publishes, or move the shared part into the platform lane; this list stays empty.");

        private static OutboundVerdict ReachesServicesOnlyThroughTheList(ModuleDefinition module, string lane, string services, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                lane,
                services,
                listed,
                $"Types of the platform lane, in namespaces matching \"{lane}\", should reach types in namespaces matching \"{services}\" " +
                "only through the edges on the list, because code still in Services sits above the platform lane until it moves, and " +
                "every new edge ties the lane to it. Depend on a type of the platform lane or on a context's Application contract " +
                "instead; the list only shrinks.");

        private static void ThenEveryAreaPasses(IReadOnlyList<(string Area, KernelRuleVerdict Verdict)> verdicts)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var (area, verdict) in verdicts)
                {
                    Assert.DoesNotThrow(verdict.Check, $"The platform lane's area {area} does not hold exactly the types the membership map gives it; the reason is above.");
                }
            }
        }

        private static void ThenTheEdgesFoundAreExactly(OutboundVerdict verdict, string[] expected)
            => Assert.That(
                verdict.Edges,
                Is.EquivalentTo(expected),
                $"Expected the lane rule to find exactly [{string.Join(", ", expected)}] in the made-up lane, found [{string.Join(", ", verdict.Edges)}]. " +
                "Why it matters: a rule that reads only top-level types misses the nested one, and one that matches short names without " +
                "their namespace also counts the same-named type outside Services or the context's Application. How to fix: read every " +
                "type of the lane, nested ones included, and match each reached type by its namespace.");

        private static void ThenEveryRulePasses(params Action[] checks)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var check in checks)
                {
                    Assert.DoesNotThrow(
                        () => check(),
                        "A lane rule failed although the made-up lane only does what is allowed. Why it matters: a rule that fails on " +
                        "compliant code gets switched off. How to fix: compare what was found with the list, nothing more.");
                }
            }
        }

        private static string ThenTheLaneRuleFails(Action check, string offender)
        {
            var failure = Assert.Throws<AssertionException>(
                () => check(),
                $"The lane rule passed with {offender} in the made-up lane. Why it matters: new platform code would break the lane's " +
                "direction unseen. How to fix: report everything the rule finds in the namespaces it is handed.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Message,
                    Does.Contain("the platform lane"),
                    "Expected the failure to speak of the platform lane, so the reader knows which rule set broke.");
                Assert.That(
                    failure.Message,
                    Does.Not.Contain("module"),
                    "Expected the failure to name no module: the platform lane is infrastructure below the contexts, not a module.");
            }

            return failure.Message;
        }

        private static void ThenTheLaneRuleFailsNamingTheNewcomer(Action check, string newcomer)
        {
            var failure = ThenTheLaneRuleFails(check, newcomer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure,
                    Does.Contain($"Not on the list, and offending: {newcomer}. "),
                    $"Expected the failure to name {newcomer}, and only it, as not on the list, so the reader knows which edge to undo.");
                Assert.That(
                    failure,
                    Does.Not.Contain("no longer offending"),
                    "Every listed entry is still there, so the failure must not ask to delete one from the list.");
            }
        }

        private static void ThenTheRuleFailsNamingTheStaleEntryForDeletion(Action check, string stale)
        {
            var failure = Assert.Throws<AssertionException>(
                () => check(),
                $"The lane rule passed with {stale} on its list although that edge does not exist. Why it matters: a list that keeps a " +
                "gone edge lets it come back unseen. How to fix: compare the list with what was found in both directions.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Message,
                    Does.Contain($"On the list, but no longer offending: {stale}. Good - delete it"),
                    $"Expected the failure to name {stale}, and only it, with 'delete', so the reader knows which list entry to remove.");
                Assert.That(
                    failure.Message,
                    Does.Not.Contain("Not on the list"),
                    "Everything found is on the list, so the failure must not name a newcomer.");
            }
        }

        private static string NamespaceAndBelow(Type type) => $@"^{Regex.Escape(type.Namespace!)}(\.|$)";

        private static string Edge(Type dependent, Type reached) => $"{dependent.FullName} -> {reached.FullName}";
    }
}
