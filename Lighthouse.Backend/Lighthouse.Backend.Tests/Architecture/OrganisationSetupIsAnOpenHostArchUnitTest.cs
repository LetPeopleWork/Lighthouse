using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.OrganisationSetupFixtures.OpenHost.Api;
using Lighthouse.Backend.Modules.OrganisationSetupFixtures.OpenHost.TrackerIntegration;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Organisation set-up is an open host. Work Items, Flow metrics, Forecasting, Promises, Refinement and Reports build on
    /// it, so they reach its Application and Domain by right: none of their outbound rules reads those namespaces as
    /// foreign, and none of their outbound lists names an edge into set-up. Each of those rules still reads a foreign
    /// namespace that is not set-up, so the allowance is set-up's and nobody else's. Tracker integration does not build on
    /// set-up: its rule still reads set-up as another context, and its list holds exactly the one edge its connection-health
    /// cadence takes into the settings, so a made-up Tracker integration type asking the settings is named. Set-up's events
    /// are constructed only in set-up's Application, except where the Team controller still publishes that a Team was
    /// deleted, so a made-up controller publishing a blackout change is named. The rules and lists are read from the rule
    /// classes that apply them, the edges and constructions from the compiled assemblies.
    /// </summary>
    [TestFixture]
    public class OrganisationSetupIsAnOpenHostArchUnitTest
    {
        private static string SetUpModule => "Lighthouse.Backend.Modules.OrganisationSetup";

        private static string SetUpApplication => @"^Lighthouse\.Backend\.Modules\.OrganisationSetup\.Application(\.|$)";

        private static string WorkItemsApplication => @"^Lighthouse\.Backend\.Modules\.WorkItems\.Application(\.|$)";

        private static string TheLooseServices => "Lighthouse.Backend.Services.Implementation";

        private static string TheEvents => "Lighthouse.Backend.Models.Events";

        private static string MadeUpTrackerIntegration => @"^Lighthouse\.Backend\.Modules\.OrganisationSetupFixtures\.OpenHost\.TrackerIntegration(\.|$)";

        private static string MadeUpApi => @"^Lighthouse\.Backend\.Modules\.OrganisationSetupFixtures\.OpenHost\.Api(\.|$)";

        private static string AnyNamespace => "^";

        private static readonly string[] SetUpEvents =
        [
            TheEvents + ".TeamDeleted",
            TheEvents + ".BlackoutConfigurationChanged",
            TheEvents + ".FeatureOrderingPolicyChanged",
            TheEvents + ".FeatureRankChanged",
        ];

        // The Team controller still publishes that a Team was deleted itself. The list only shrinks.
        private static readonly string[] SetUpEventsConstructedOutsideSetUpsApplication =
        [
            "Lighthouse.Backend.API.TeamController -> Lighthouse.Backend.Models.Events.TeamDeleted",
        ];

        private static string TheCadenceEdgeIntoTheSettings =>
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthCadence -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService";

        private static readonly string[] SetUpNamespacesOpenToDownstreamModules =
        [
            ContextContractsMembership.ContractsOf("OrganisationSetup"),
        ];

        // Refinement's cadence still takes the weekly recurrence from set-up's Domain, which set-up does not publish; this one
        // listed edge into set-up is allowed and any other is not.
        private static string TheListedEdgeIntoSetUpsDomain =>
            "Lighthouse.Backend.Modules.Refinement.Application.RefinementCadenceCalendar -> Lighthouse.Backend.Modules.OrganisationSetup.Domain.WeeklyRecurrence";

        // Work Items' Application reaches set-up along these edges in the compiled code; once set-up is open to Work Items
        // they are legal by namespace and no list names them.
        private static readonly string[] WorkItemsEdgesIntoSetUpThatBecomeLegal =
        [
            "Lighthouse.Backend.Modules.WorkItems.Application.RefreshLogService -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService",
            "Lighthouse.Backend.Modules.WorkItems.Application.WorkItemService -> Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IFeatureOrdering",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly (Type RuleClass, string Module, string Rule, string ForeignNamespace)[] DownstreamOutboundRules =
        [
            (typeof(WorkItemsModuleBoundariesArchUnitTest), "Work Items", "AccessLicensingOrganisationSetUpAndTheUpdateQueue", "Lighthouse.Backend.Modules.Licensing.Application"),
            (typeof(WorkItemsModuleBoundariesArchUnitTest), "Work Items", "TheLayersBelowTheApplication", SetUpModule + ".Adapters"),
            (typeof(FlowMetricsApplicationOutboundArchUnitTest), "Flow metrics", "TheLayersBelowTheApplication", "Lighthouse.Backend.Data"),
            (typeof(ForecastingModuleBoundariesArchUnitTest), "Forecasting", "TheLayersBelowTheApplicationAndTheLicence", "Lighthouse.Backend.Modules.Licensing.Application"),
            (typeof(PromisesModuleBoundariesArchUnitTest), "Promises", "TheLayersBelowTheApplicationAndThePlatform", TheLooseServices),
            (typeof(PromisesModuleBoundariesArchUnitTest), "Promises", "TheNamespacedUpstreamCodeAndSetUpBehindItsContracts", "Lighthouse.Backend.Modules.Licensing.Application"),
            (typeof(RefinementModuleBoundariesArchUnitTest), "Refinement", "TheForbiddenCodeAndSetUpBehindItsContracts", SetUpModule + ".Adapters"),
            (typeof(RefinementModuleBoundariesArchUnitTest), "Refinement", "AccessSetUpPlatformAndRepositories", TheLooseServices),
            (typeof(ReportsModuleBoundariesArchUnitTest), "Reports", "TheForbiddenCode", SetUpModule + ".Adapters"),
            (typeof(ReportsModuleBoundariesArchUnitTest), "Reports", "FlowMetricsLicensingSetUpAndPlatform", TheLooseServices),
            (typeof(AccessModuleBoundariesArchUnitTest), "Access", "TheOtherContexts", "Lighthouse.Backend.Modules.WorkItems.Application"),
            (typeof(AccessModuleBoundariesArchUnitTest), "Access", "TheLayersBelowTheApplicationAndThePlatform", TheLooseServices),
            (typeof(LicensingModuleBoundariesArchUnitTest), "Licensing", "AccessAndTheOtherContexts", "Lighthouse.Backend.Modules.Access.Application"),
            (typeof(LicensingModuleBoundariesArchUnitTest), "Licensing", "TheLayersBelowTheApplicationAndThePlatform", TheLooseServices),
        ];

        private static IEnumerable<TestCaseData> DownstreamOutboundTargets()
            => DownstreamOutboundRules.Select(rule => new TestCaseData(rule.RuleClass, rule.Module, rule.Rule).SetArgDisplayNames(rule.Module, rule.Rule));

        private static IEnumerable<TestCaseData> DownstreamOutboundTargetsAndAForeignNamespace()
            => DownstreamOutboundRules.Select(rule => new TestCaseData(rule.RuleClass, rule.Module, rule.Rule, rule.ForeignNamespace).SetArgDisplayNames(rule.Module, rule.Rule));

        private static IEnumerable<TestCaseData> DownstreamOutboundLists()
        {
            yield return OutboundList(typeof(WorkItemsModuleBoundariesArchUnitTest), "Work Items", "EdgesOfTheApplicationIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue");
            yield return OutboundList(typeof(WorkItemsModuleBoundariesArchUnitTest), "Work Items", "EdgesOfTheApplicationIntoFlowMetrics");
            yield return OutboundList(typeof(FlowMetricsApplicationOutboundArchUnitTest), "Flow metrics", "ForecastingEdgesOfTheApplication");
            yield return OutboundList(typeof(ForecastingModuleBoundariesArchUnitTest), "Forecasting", "EdgesOfTheApplicationBelowItOrIntoTheLicence");
            yield return OutboundList(typeof(PromisesModuleBoundariesArchUnitTest), "Promises", "EdgesOfTheApplicationBelowItOrIntoThePlatform");
            yield return OutboundList(typeof(PromisesModuleBoundariesArchUnitTest), "Promises", "EdgesOfTheModuleIntoUpstreamCode");
            yield return OutboundList(typeof(RefinementModuleBoundariesArchUnitTest), "Refinement", "EdgesOfTheApplicationIntoForbiddenCode");
            yield return OutboundList(typeof(RefinementModuleBoundariesArchUnitTest), "Refinement", "EdgesOfTheApplicationIntoAccessSetUpPlatformAndRepositories");
            yield return OutboundList(typeof(ReportsModuleBoundariesArchUnitTest), "Reports", "EdgesOfTheApplicationIntoForbiddenCode");
            yield return OutboundList(typeof(ReportsModuleBoundariesArchUnitTest), "Reports", "EdgesOfTheApplicationIntoFlowMetricsLicensingSetUpAndPlatform");
            yield return OutboundList(typeof(AccessModuleBoundariesArchUnitTest), "Access", "EdgesOfTheApplicationBelowItOrIntoThePlatform");
            yield return OutboundList(typeof(AccessModuleBoundariesArchUnitTest), "Access", "EdgesOfTheModuleIntoAnotherContext");
            yield return OutboundList(typeof(LicensingModuleBoundariesArchUnitTest), "Licensing", "EdgesOfTheApplicationBelowItOrIntoThePlatform");
            yield return OutboundList(typeof(LicensingModuleBoundariesArchUnitTest), "Licensing", "EdgesOfTheModuleIntoAccessOrAnotherContext");
        }

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(OrganisationSetupIsAnOpenHostArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(DownstreamOutboundTargets))]
        public void ADownstreamModulesOutboundRule_DoesNotReadSetUpsApplicationOrDomainAsForeign(Type ruleClass, string module, string rule)
        {
            var target = GivenTheTargetThatTheRuleReads(ruleClass, rule);

            var setUpNamespacesReadAsForeign = WhenTheTargetIsAppliedTo(target, SetUpNamespacesOpenToDownstreamModules);

            ThenNoSetUpNamespaceIsReadAsForeign(module, rule, setUpNamespacesReadAsForeign);
        }

        [TestCaseSource(nameof(DownstreamOutboundTargetsAndAForeignNamespace))]
        public void ADownstreamModulesOutboundRule_StillReadsAForeignNamespaceThatIsNotSetUp(Type ruleClass, string module, string rule, string foreignNamespace)
        {
            var target = GivenTheTargetThatTheRuleReads(ruleClass, rule);

            var namespacesReadAsForeign = WhenTheTargetIsAppliedTo(target, [foreignNamespace]);

            ThenTheForeignNamespaceIsStillRead(module, rule, foreignNamespace, namespacesReadAsForeign);
        }

        [TestCaseSource(nameof(DownstreamOutboundLists))]
        public void ADownstreamModulesOutboundList_NamesNoEdgeIntoSetUp(Type ruleClass, string module, string list)
        {
            var entries = GivenTheEntriesOfTheList(ruleClass, list);

            var entriesIntoSetUp = WhenTheEntriesIntoSetUpAreTaken(entries).Where(entry => entry != TheListedEdgeIntoSetUpsDomain).ToList();

            ThenNoEntryLeadsIntoSetUp(module, list, entriesIntoSetUp);
        }

        [Test]
        public void TheWorkItemsApplication_StillReachesSetUpInTheCompiledCode_SoAnEmptiedListMeansAnAllowanceAndNotALostEdge()
        {
            var production = ProductionModule.Value;

            var edgesIntoSetUp = WhenTheCompiledEdgesAreRead(production, WorkItemsApplication, ModuleRules.SetUpContracts);

            ThenTheEdgesThatBecomeLegalAreStillCompiled(edgesIntoSetUp);
        }

        [Test]
        public void TrackerIntegrationsRule_StillReadsSetUpAsAnotherContext()
        {
            var target = GivenTheTargetThatTheRuleReads(typeof(TrackerIntegrationModuleBoundariesArchUnitTest), "OtherContexts");

            var setUpNamespacesReadAsForeign = WhenTheTargetIsAppliedTo(target, SetUpNamespacesOpenToDownstreamModules);

            ThenEverySetUpNamespaceIsReadAsAnotherContextByTrackerIntegration(setUpNamespacesReadAsForeign);
        }

        [Test]
        public void TrackerIntegrationsList_HoldsExactlyTheCadenceEdgeIntoTheSettings()
        {
            var entries = GivenTheEntriesOfTheList(typeof(TrackerIntegrationModuleBoundariesArchUnitTest), "EdgesOfTheModuleIntoOtherContexts");

            var entriesIntoSetUp = WhenTheEntriesIntoSetUpAreTaken(entries);

            ThenTrackerIntegrationListsExactlyTheCadenceEdge(entries, entriesIntoSetUp);
        }

        [Test]
        public void AMadeUpTrackerIntegrationTypeAskingTheSettings_IsNamedByTrackerIntegrationsRule()
        {
            var target = GivenTheTargetThatTheRuleReads(typeof(TrackerIntegrationModuleBoundariesArchUnitTest), "OtherContexts");
            var listed = GivenTheEntriesOfTheList(typeof(TrackerIntegrationModuleBoundariesArchUnitTest), "EdgesOfTheModuleIntoOtherContexts");

            var unlistedEdges = WhenTheRuleReadsTheMadeUpTrackerIntegration(target, listed);

            ThenTheMadeUpTrackerIntegrationTypeIsNamed(unlistedEdges);
        }

        [Test]
        public void SetUpsEvents_AreConstructedOnlyInSetUpsApplication_ExceptAlongTheListedEdge()
        {
            var production = ProductionModule.Value;

            var constructionsOutsideSetUp = WhenTheConstructionsOfSetUpsEventsAreRead(production, AnyNamespace);

            ThenTheConstructionsOutsideSetUpAreExactlyTheList(constructionsOutsideSetUp);
        }

        [Test]
        public void AMadeUpControllerPublishingABlackoutChange_IsNamed()
        {
            var tests = TestModule.Value;

            var constructionsOutsideSetUp = WhenTheConstructionsOfSetUpsEventsAreRead(tests, MadeUpApi);

            ThenTheMadeUpControllerIsNamed(constructionsOutsideSetUp);
        }

        private static TestCaseData OutboundList(Type ruleClass, string module, string list)
            => new TestCaseData(ruleClass, module, list).SetArgDisplayNames(module, list);

        private static string GivenTheTargetThatTheRuleReads(Type ruleClass, string rule)
        {
            var target = StaticValueOf(ruleClass, rule) as string;

            Assert.That(target, Is.Not.Null.And.Not.Empty,
                $"{ruleClass.Name}.{rule} holds no namespace pattern. It is the target the outbound rule reads, so without it the rule cannot say " +
                "what it forbids. Keep the pattern the rule passes as its target under this name, or name the new member here.");

            return target!;
        }

        private static string[] GivenTheEntriesOfTheList(Type ruleClass, string list)
        {
            var entries = StaticValueOf(ruleClass, list) as string[];

            Assert.That(entries, Is.Not.Null,
                $"{ruleClass.Name}.{list} is not a list of edges. The outbound rule compares the compiled edges with it both ways, so this check " +
                "must read the same list. Keep the list under this name, or name the new member here.");

            return entries!;
        }

        private static object? StaticValueOf(Type ruleClass, string member)
        {
            const BindingFlags anyStatic = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

            var field = ruleClass.GetField(member, anyStatic);
            if (field is not null)
            {
                return field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null);
            }

            var property = ruleClass.GetProperty(member, anyStatic);
            Assert.That(property, Is.Not.Null,
                $"{ruleClass.Name} has no member {member}. This check reads the rule's own target or list, so a renamed member leaves set-up's " +
                "allowance unobserved. Name the member the rule uses here.");

            return property!.GetValue(null);
        }

        private static List<string> WhenTheTargetIsAppliedTo(string target, IEnumerable<string> namespaces)
            => [.. namespaces.Where(candidate => Regex.IsMatch(candidate, target, RegexOptions.None, TimeSpan.FromSeconds(1)))];

        private static List<string> WhenTheEntriesIntoSetUpAreTaken(IEnumerable<string> entries)
            => [.. entries.Where(entry => entry.Contains($" -> {SetUpModule}.", StringComparison.Ordinal))];

        private static List<string> WhenTheCompiledEdgesAreRead(ModuleDefinition production, string dependents, string targets)
            => [.. IlDependencies.DependentsIn(production, dependents, type => IlDependencies.ResidesInNamespaceMatching(type, targets))
                .SelectMany(dependent => dependent.Targets.Select(reached => $"{dependent.TypeName} -> {reached}"))];

        private static List<string> WhenTheRuleReadsTheMadeUpTrackerIntegration(string target, string[] listed)
            => [.. ModuleRules.OutboundEdges(TestModule.Value, MadeUpTrackerIntegration, target, listed, "Tracker integration's other contexts").Edges
                .Where(edge => !listed.Contains(edge, StringComparer.Ordinal))];

        // A record's own copy constructor builds the event too; that is the event, not a publisher.
        private static List<string> WhenTheConstructionsOfSetUpsEventsAreRead(ModuleDefinition module, string publishers)
            => [.. module.GetTypes()
                .Where(type => IlDependencies.ResidesInNamespaceMatching(type, publishers) && !IlDependencies.ResidesInNamespaceMatching(type, SetUpApplication))
                .SelectMany(type => type.Methods.Where(method => method.HasBody).Select(method => (Publisher: OutermostOf(type), method.Body)))
                .SelectMany(site => site.Body.Instructions
                    .Where(instruction => instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference)
                    .Select(instruction => ((MethodReference)instruction.Operand).DeclaringType.FullName)
                    .Where(constructed => SetUpEvents.Contains(constructed, StringComparer.Ordinal) && constructed != site.Publisher)
                    .Select(constructed => $"{site.Publisher} -> {constructed}"))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];

        private static string OutermostOf(TypeDefinition type)
            => type.DeclaringType is null ? type.FullName : OutermostOf(type.DeclaringType);

        private static void ThenNoSetUpNamespaceIsReadAsForeign(string module, string rule, List<string> setUpNamespacesReadAsForeign)
            => Assert.That(setUpNamespacesReadAsForeign, Is.Empty,
                $"{new ModuleWording(module, string.Empty).Of("outbound rule")} {rule} still reads these Organisation set-up namespaces as foreign: " +
                $"{string.Join(", ", setUpNamespacesReadAsForeign)}. {module} builds on set-up, so it reaches set-up's Application and Domain by " +
                "right, not through a listed edge. Let the rule's target leave set-up's Application and Domain out by namespace, and take the " +
                "list's set-up entries off.");

        private static void ThenTheForeignNamespaceIsStillRead(string module, string rule, string foreignNamespace, List<string> namespacesReadAsForeign)
            => Assert.That(namespacesReadAsForeign, Is.EqualTo(new[] { foreignNamespace }),
                $"{new ModuleWording(module, string.Empty).Of("outbound rule")} {rule} no longer reads {foreignNamespace} as foreign. The allowance belongs to set-up's Application and " +
                $"Domain only, so a type of {module} reaching anything else is still named. Admit set-up by its own namespaces and keep the rest " +
                "of the target as it was.");

        private static void ThenNoEntryLeadsIntoSetUp(string module, string list, List<string> entriesIntoSetUp)
            => Assert.That(entriesIntoSetUp, Is.Empty,
                $"{new ModuleWording(module, string.Empty).Of("outbound list")} {list} still names these edges into Organisation set-up: {string.Join("; ", entriesIntoSetUp)}. " +
                $"{module} reaches set-up through its Contracts by right, and set-up's Application and Domain stay foreign to it, so a listed set-up edge is " +
                "either an exception for something allowed or a new way round the Contracts. Take these entries off, or reach set-up through its Contracts.");

        private static void ThenTheEdgesThatBecomeLegalAreStillCompiled(List<string> edgesIntoSetUp)
            => Assert.That(edgesIntoSetUp, Is.SupersetOf(WorkItemsEdgesIntoSetUpThatBecomeLegal),
                $"Work Items' Application no longer reaches set-up along {string.Join("; ", WorkItemsEdgesIntoSetUpThatBecomeLegal.Except(edgesIntoSetUp))} " +
                $"in the compiled code; it reaches {string.Join("; ", edgesIntoSetUp)}. These edges are what proves the allowance: with them still " +
                "compiled, an emptied list passes only when the rule admits set-up. Keep the services' calls into set-up as they are; the move " +
                "changes no behaviour.");

        private static void ThenEverySetUpNamespaceIsReadAsAnotherContextByTrackerIntegration(List<string> setUpNamespacesReadAsForeign)
            => Assert.That(setUpNamespacesReadAsForeign, Is.EquivalentTo(SetUpNamespacesOpenToDownstreamModules),
                $"Tracker integration's rule OtherContexts reads only {string.Join(", ", setUpNamespacesReadAsForeign)} of set-up's namespaces " +
                "as another context. Tracker integration is a leaf that translates what the trackers send; it does not build on set-up, so a " +
                "new Tracker integration type reaching set-up must be named. Keep set-up in Tracker integration's other contexts.");

        private static void ThenTheMadeUpTrackerIntegrationTypeIsNamed(List<string> unlistedEdges)
            => Assert.That(unlistedEdges, Is.EqualTo(new[] { $"{typeof(AConnectorAskingTheSettings).FullName} -> {typeof(global::Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService).FullName}" }),
                $"Tracker integration's rule named {string.Join("; ", unlistedEdges)} for a made-up Tracker integration type that asks the settings. " +
                "Tracker integration reaches set-up only along its one listed edge, so any other type of it reaching set-up must be named. Keep " +
                "set-up among Tracker integration's other contexts and keep its list to the cadence edge.");

        private static void ThenTheConstructionsOutsideSetUpAreExactlyTheList(List<string> constructionsOutsideSetUp)
            => Assert.That(constructionsOutsideSetUp, Is.EqualTo(SetUpEventsConstructedOutsideSetUpsApplication),
                $"Set-up's events are constructed outside set-up's Application at {string.Join("; ", constructionsOutsideSetUp)}; the list names " +
                $"{string.Join("; ", SetUpEventsConstructedOutsideSetUpsApplication)}. Set-up publishes its own events, so a second publisher " +
                "could announce a change set-up never made. Publish the event from set-up's Application instead; take an entry off when its " +
                "publisher leaves, and never add one.");

        private static void ThenTheMadeUpControllerIsNamed(List<string> constructionsOutsideSetUp)
            => Assert.That(constructionsOutsideSetUp, Is.EqualTo(new[] { $"{typeof(ABlackoutController).FullName} -> {TheEvents}.BlackoutConfigurationChanged" }),
                $"The check of where set-up's events are constructed named {string.Join("; ", constructionsOutsideSetUp)} for a made-up controller " +
                "that publishes a blackout change. A publisher outside set-up's Application must be named, or a controller could announce a " +
                "blackout change unseen. Keep reading every construction outside set-up's Application, including those inside lambdas and " +
                "async methods.");

        private static void ThenTrackerIntegrationListsExactlyTheCadenceEdge(string[] entries, List<string> entriesIntoSetUp)
            => Assert.That(entriesIntoSetUp, Is.EqualTo(new[] { TheCadenceEdgeIntoTheSettings }),
                $"Tracker integration's list EdgesOfTheModuleIntoOtherContexts names {entriesIntoSetUp.Count} edges into set-up " +
                $"({string.Join("; ", entriesIntoSetUp)}) out of its {entries.Length} entries; it must name exactly {TheCadenceEdgeIntoTheSettings}. " +
                "The connection-health cadence reads its interval from the settings, and that is the one edge Tracker integration may take " +
                "into set-up. Remove any other set-up edge from the code, or bring a new one to the design first.");
    }
}

namespace Lighthouse.Backend.Modules.OrganisationSetupFixtures.OpenHost.TrackerIntegration
{
    internal sealed class AConnectorAskingTheSettings(global::Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService settings)
    {
        public global::Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService Settings { get; } = settings;
    }
}

namespace Lighthouse.Backend.Modules.OrganisationSetupFixtures.OpenHost.Api
{
    internal static class ABlackoutController
    {
        public static global::Lighthouse.Backend.Models.Events.IDomainEvent Publish() => new global::Lighthouse.Backend.Models.Events.BlackoutConfigurationChanged();
    }
}
