using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.CallingAPromisesHelper.TrackerIntegration.Adapters;
using Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.CallingAPromisesHelper.Promises.Application;
using Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.HoldingTheRenderer.TrackerIntegration.Adapters;
using Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.HoldingTheRenderer.Promises.Application;
using Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.NestedHelperHoldingTheRenderer.TrackerIntegration.Adapters;
using Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.NestedHelperHoldingTheRenderer.Promises.Application;
using Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.ThroughItsPort.TrackerIntegration.Adapters;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Promises builds on Tracker integration: it publishes the Delivery forecast through the connectors. When the Jira
    /// connector writes that forecast into a description, it merges the block through a port Tracker integration owns in
    /// its Application, and the Promises renderer implements it, so Tracker integration depends on no Promises type and
    /// the two modules form no cycle. Read from the compiled production assembly: no type of Tracker integration, in any
    /// layer and with its nested types, reaches a Promises type, with no list of allowed edges; and the merge port lives in
    /// Tracker integration's Application with the merge method the renderer has always had, the renderer implements it,
    /// the Jira connector takes it, and the renderer's own port no longer offers the merge. Types are named as strings, so
    /// this file compiles before and after the port exists. The text the merge returns is pinned by the renderer's own
    /// merge tests, not here. The same rules run on made-up modules: a Tracker integration type that holds, calls or
    /// nests a Promises type fails and is named with what it reaches; one that merges only through its own port passes;
    /// a port declared in Promises, or a renderer port that still offers the merge, fails and is named.
    /// </summary>
    /// <remarks>
    /// The IL reader does not see types used only in a catch clause, an event or a named attribute argument.
    /// </remarks>
    [TestFixture]
    public class TrackerIntegrationReachesNoPromisesArchUnitTest
    {
        private const string MadeUpModules = "Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach";

        private const string MergeMethod = "String MergeInto(String existingDescription, String blockText)";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(TrackerIntegrationReachesNoPromisesArchUnitTest).Assembly));

        private static TheModules TheRealModules => new(ModuleBoundariesArchUnitTest.TrackerIntegrationPattern, ModuleRules.PromisesModule);

        private static TheMergeNames TheRealMergeNames => new(
            Port: "Lighthouse.Backend.Modules.TrackerIntegration.Contracts.IDescriptionBlockMerger",
            Renderer: "Lighthouse.Backend.Modules.Promises.Application.DeliveryForecastBlockRenderer",
            RendererPort: "Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer",
            Connector: "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Jira.JiraWorkTrackingConnector",
            Scope: "Lighthouse.Backend");

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TrackerIntegration_ReachesNoPromisesType()
        {
            var verdict = TrackerIntegrationReachesNoPromisesType(ProductionModule.Value, TheRealModules);

            verdict.Check();
        }

        [Test]
        public void TheJiraConnector_MergesTheForecastBlockThroughTrackerIntegrationsOwnPort_ThatThePromisesRendererImplements()
        {
            var verdict = TheConnectorMergesThroughTrackerIntegrationsPort(ProductionModule.Value, TheRealMergeNames);

            verdict.Check();
        }

        [Test]
        public void ATrackerIntegrationTypeMergingOnlyThroughItsOwnPort_PassesBothRules()
        {
            var madeUpCase = GivenTheMadeUpCaseOf(typeof(AConnectorMergingThroughThePort));

            var reach = TrackerIntegrationReachesNoPromisesType(TestModule.Value, ModulesOf(madeUpCase));
            var port = TheConnectorMergesThroughTrackerIntegrationsPort(TestModule.Value, MergeNamesOf(madeUpCase, typeof(AConnectorMergingThroughThePort)));

            ThenTheReachRuleLookedAtAndPasses(reach, typeof(AConnectorMergingThroughThePort));
            ThenThePortRulePasses(port);
        }

        [TestCaseSource(nameof(TrackerIntegrationTypesReachingPromises))]
        public void ATrackerIntegrationTypeReachingPromises_FailsAndIsNamedWithWhatItReaches(Type offender, Type reached)
        {
            var madeUpCase = GivenTheMadeUpCaseOf(offender);

            var reach = TrackerIntegrationReachesNoPromisesType(TestModule.Value, ModulesOf(madeUpCase));

            ThenTheReachRuleFailsNamingOnly(reach, $"{offender.FullName} -> {reached.FullName}");
        }

        [Test]
        public void ATrackerIntegrationPatternThatMatchesNoType_FailsInsteadOfPassing()
        {
            var nowhere = $"{MadeUpModules}.NoSuchCase";

            var reach = TrackerIntegrationReachesNoPromisesType(TestModule.Value, ModulesOf(nowhere));

            ThenTheReachRuleFailsSaying(reach, "so the rule checked nothing");
        }

        [Test]
        public void AMergePortDeclaredInPromisesInsteadOfTrackerIntegration_FailsAndIsNamedWithWhereItLives()
        {
            var madeUpCase = $"{MadeUpModules}.PortInsidePromises";

            var port = TheConnectorMergesThroughTrackerIntegrationsPort(TestModule.Value, MergeNamesOf(madeUpCase, connectorAdapter: "AConnector"));

            ThenThePortRuleFailsNaming(
                port,
                $"{madeUpCase}.TrackerIntegration.Application.IDescriptionMerger is not an interface there; a type of that name lives at {madeUpCase}.Promises.Application.IDescriptionMerger.");
        }

        [Test]
        public void ARendererPortThatStillOffersTheMerge_FailsAndIsNamed()
        {
            var madeUpCase = $"{MadeUpModules}.MergeLeftOnTheRendererPort";

            var port = TheConnectorMergesThroughTrackerIntegrationsPort(TestModule.Value, MergeNamesOf(madeUpCase, connectorAdapter: "AConnector"));

            ThenThePortRuleFailsNaming(
                port,
                $"{madeUpCase}.Promises.Application.IABlockRenderer still declares MergeInto; the merge belongs to {madeUpCase}.TrackerIntegration.Application.IDescriptionMerger only.");
        }

        private static IEnumerable<TestCaseData> TrackerIntegrationTypesReachingPromises()
        {
            yield return new TestCaseData(typeof(AConnectorHoldingTheRenderer), typeof(IABlockRenderer))
                .SetName("ATrackerIntegrationTypeTakingAPromisesRendererInItsConstructor_FailsAndIsNamedWithWhatItReaches");
            yield return new TestCaseData(typeof(AConnectorCallingTheBlockMarkers), typeof(ABlockMarkers))
                .SetName("ATrackerIntegrationTypeCallingAStaticPromisesHelper_FailsAndIsNamedWithWhatItReaches");
            yield return new TestCaseData(typeof(AConnectorWithANestedWriter.Writer), typeof(IANestedBlockRenderer))
                .SetName("ATypeNestedInATrackerIntegrationTypeHoldingAPromisesRenderer_FailsAndIsNamedWithWhatItReaches");
        }

        /// <summary>
        /// Every edge from a Tracker integration type, in any layer, to a Promises type is an offender: there is no list,
        /// because Tracker integration asks Promises nothing except through ports it owns. A module that holds no type
        /// fails too.
        /// </summary>
        private static OutboundVerdict TrackerIntegrationReachesNoPromisesType(ModuleDefinition module, TheModules modules)
            => ModuleRules.OutboundEdges(
                module,
                modules.TrackerIntegration,
                modules.Promises,
                [],
                $"Types in namespaces matching \"{modules.TrackerIntegration}\" should depend on no type in namespaces matching \"{modules.Promises}\", " +
                "because Promises publishes the Delivery forecast through Tracker integration's connectors, and one edge back from " +
                "Tracker integration makes the two modules one cycle that cannot change, be tested or move apart. Let the connector " +
                "take a port Tracker integration owns in its Contracts (the description block merger), let the Promises type " +
                "that owns the rule implement it and the composition root register it; there is no list of allowed edges.");

        /// <summary>
        /// The merge port is an interface in Tracker integration's Application with the one merge method, the renderer
        /// implements it, the connector takes it in its constructor, and the renderer's own port keeps only rendering.
        /// </summary>
        private static PortVerdict TheConnectorMergesThroughTrackerIntegrationsPort(ModuleDefinition module, TheMergeNames names)
            => new(
                [.. ProblemsOfThePort(module, names), .. ProblemsOfTheRenderer(module, names), .. ProblemsOfTheRendererPort(module, names), .. ProblemsOfTheConnector(module, names)],
                $"The connector should merge the forecast block through {names.Port}, an interface Tracker integration owns that " +
                $"declares only \"{MergeMethod}\", which {names.Renderer} implements; {names.Connector} should take that port, and " +
                $"{names.RendererPort} should keep rendering only. Why: a connector that holds the Promises renderer makes Tracker " +
                "integration depend on Promises, and a merge left on the renderer's port lets that edge come back. How: declare the " +
                "port in Tracker integration's Application with the renderer's merge signature unchanged, implement it on the " +
                "renderer, change the connector's constructor parameter to the port, and remove the merge from the renderer's port.");

        private static IEnumerable<string> ProblemsOfThePort(ModuleDefinition module, TheMergeNames names)
        {
            var port = module.GetType(names.Port);

            if (port is null || !port.IsInterface)
            {
                var simpleName = names.Port[(names.Port.LastIndexOf('.') + 1)..];
                var elsewhere = module.Types
                    .Where(type => type.Name == simpleName && type.FullName != names.Port && type.FullName.StartsWith($"{names.Scope}.", StringComparison.Ordinal))
                    .Select(type => type.FullName)
                    .Order(StringComparer.Ordinal)
                    .ToList();

                return [elsewhere.Count > 0
                    ? $"{names.Port} is not an interface there; a type of that name lives at {string.Join(", ", elsewhere)}."
                    : $"{names.Port} is not an interface there, and no type of that name exists."];
            }

            var declared = port.Methods.Select(Described).ToList();

            return declared.SequenceEqual([MergeMethod], StringComparer.Ordinal)
                ? []
                : [$"{names.Port} should declare only \"{MergeMethod}\"; it declares [{string.Join(", ", declared)}]."];
        }

        private static IEnumerable<string> ProblemsOfTheRenderer(ModuleDefinition module, TheMergeNames names)
        {
            var renderer = module.GetType(names.Renderer);

            if (renderer is null)
            {
                return [$"{names.Renderer} does not exist, so nothing in Promises implements the merge port."];
            }

            return renderer.Interfaces.Any(implemented => implemented.InterfaceType.FullName == names.Port)
                ? []
                : [$"{names.Renderer} does not implement {names.Port}; it implements [{string.Join(", ", renderer.Interfaces.Select(implemented => implemented.InterfaceType.FullName).Order(StringComparer.Ordinal))}]."];
        }

        private static IEnumerable<string> ProblemsOfTheRendererPort(ModuleDefinition module, TheMergeNames names)
        {
            var rendererPort = module.GetType(names.RendererPort);

            if (rendererPort is null)
            {
                return [$"{names.RendererPort} does not exist, but Promises' publishing still renders the block through it."];
            }

            return rendererPort.Methods.Any(method => method.Name == "MergeInto")
                ? [$"{names.RendererPort} still declares MergeInto; the merge belongs to {names.Port} only."]
                : [];
        }

        private static IEnumerable<string> ProblemsOfTheConnector(ModuleDefinition module, TheMergeNames names)
        {
            var connector = module.GetType(names.Connector);

            if (connector is null)
            {
                return [$"{names.Connector} does not exist, so no connector takes the merge port."];
            }

            var parameterTypes = connector.Methods
                .Where(method => method.IsConstructor && !method.IsStatic)
                .SelectMany(constructor => constructor.Parameters.Select(parameter => parameter.ParameterType.FullName))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return parameterTypes.Contains(names.Port, StringComparer.Ordinal)
                ? []
                : [$"{names.Connector} takes no {names.Port} in its constructor; it takes [{string.Join(", ", parameterTypes)}]."];
        }

        private static string Described(MethodDefinition method)
            => $"{method.ReturnType.Name} {method.Name}({string.Join(", ", method.Parameters.Select(parameter => $"{parameter.ParameterType.Name} {parameter.Name}"))})";

        // Each made-up case lives under a namespace of its own with a TrackerIntegration and a Promises part, so a rule
        // handed one case sees only that case.
        private static string GivenTheMadeUpCaseOf(Type madeUpType)
        {
            var typeNamespace = madeUpType.Namespace!;

            return typeNamespace[..typeNamespace.IndexOf('.', MadeUpModules.Length + 1)];
        }

        private static TheModules ModulesOf(string madeUpCase)
            => new($@"^{Regex.Escape(madeUpCase)}\.TrackerIntegration(\.|$)", $@"^{Regex.Escape(madeUpCase)}\.Promises(\.|$)");

        private static TheMergeNames MergeNamesOf(string madeUpCase, Type connector)
            => MergeNamesOf(madeUpCase, connector.Name);

        private static TheMergeNames MergeNamesOf(string madeUpCase, string connectorAdapter)
            => new(
                Port: $"{madeUpCase}.TrackerIntegration.Application.IDescriptionMerger",
                Renderer: $"{madeUpCase}.Promises.Application.ABlockRenderer",
                RendererPort: $"{madeUpCase}.Promises.Application.IABlockRenderer",
                Connector: $"{madeUpCase}.TrackerIntegration.Adapters.{connectorAdapter}",
                Scope: madeUpCase);

        private static void ThenTheReachRuleLookedAtAndPasses(OutboundVerdict verdict, Type connector)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    verdict.Examined,
                    Does.Contain(connector.FullName),
                    $"Expected the rule to look at {connector.FullName}. Why it matters: a rule that does not see the Tracker " +
                    "integration types passes without checking anything. How to fix: examine every type in the Tracker integration namespaces.");
                Assert.That(
                    verdict.Edges,
                    Is.Empty,
                    $"Expected no edge into Promises, found [{string.Join(", ", verdict.Edges)}]. Why it matters: a connector that " +
                    "merges through its own port, which a Promises type implements, does not depend on Promises. How to fix: report " +
                    "only edges whose target lives in the Promises namespaces, not types that implement a Tracker integration port.");
            }

            Assert.DoesNotThrow(
                verdict.Check,
                "The rule failed although the connector merged only through its own port. Why it matters: a rule that fails on " +
                "compliant code gets switched off. How to fix: compare what was found with an empty list, nothing more.");
        }

        private static void ThenThePortRulePasses(PortVerdict verdict)
        {
            Assert.DoesNotThrow(
                verdict.Check,
                $"The port rule failed with [{string.Join(" ", verdict.Problems)}] although the port is an interface in Tracker " +
                "integration's Application with the merge method, the renderer implements it and the connector takes it. Why it " +
                "matters: a rule that fails on compliant code gets switched off. How to fix: accept exactly that shape.");
        }

        private static void ThenTheReachRuleFailsNamingOnly(OutboundVerdict verdict, string edge)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The rule passed although {edge} exists. Why it matters: Tracker integration would depend on Promises again, " +
                "unseen, and the two modules would form a cycle. How to fix: report every edge from a Tracker integration type, " +
                "nested types included, into Promises, against an empty list.");

            Assert.That(
                failure!.Message,
                Does.Contain($"Not on the list, and offending: {edge}. "),
                $"The failure must name {edge}, and only it, so the reader knows which Tracker integration type to change and " +
                "what it must stop reaching.");
        }

        private static void ThenTheReachRuleFailsSaying(OutboundVerdict verdict, string words)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                "The rule passed although its Tracker integration pattern matches no type. Why it matters: a renamed or emptied " +
                "namespace would leave the rule green while it guards nothing. How to fix: fail when the rule examined no type.");

            Assert.That(
                failure!.Message,
                Does.Contain(words),
                $"The failure must say \"{words}\", so the reader knows the rule looked at nothing rather than found nothing.");
        }

        private static void ThenThePortRuleFailsNaming(PortVerdict verdict, string problem)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The port rule passed although {problem} Why it matters: the connector would again reach Promises, or could, " +
                "through a merge that Tracker integration does not own. How to fix: fail for a port outside Tracker integration's " +
                "Application, and for a renderer port that still offers the merge.");

            Assert.That(
                failure!.Message,
                Does.Contain(problem),
                $"The failure must say \"{problem}\", so the reader knows which port to move or which merge to remove.");
        }

        private sealed record TheModules(string TrackerIntegration, string Promises);

        private sealed record TheMergeNames(string Port, string Renderer, string RendererPort, string Connector, string Scope);

        private sealed record PortVerdict(IReadOnlyList<string> Problems, string Rule)
        {
            public void Check()
            {
                if (Problems.Count > 0)
                {
                    Assert.Fail($"{string.Join(Environment.NewLine, Problems)}{Environment.NewLine}{Rule}");
                }
            }
        }
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.ThroughItsPort.TrackerIntegration.Application
{
    internal interface IDescriptionMerger
    {
        string MergeInto(string? existingDescription, string blockText);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.ThroughItsPort.TrackerIntegration.Adapters
{
    internal sealed class AConnectorMergingThroughThePort(global::Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.ThroughItsPort.TrackerIntegration.Application.IDescriptionMerger merger)
    {
        public string Publish(string? description, string block) => merger.MergeInto(description, block);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.ThroughItsPort.Promises.Application
{
    internal interface IABlockRenderer
    {
        string Render(string forecast);
    }

    internal sealed class ABlockRenderer : IABlockRenderer, global::Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.ThroughItsPort.TrackerIntegration.Application.IDescriptionMerger
    {
        public string Render(string forecast) => $"[forecast]{forecast}[/forecast]";

        public string MergeInto(string? existingDescription, string blockText) => $"{existingDescription}{blockText}";
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.HoldingTheRenderer.TrackerIntegration.Adapters
{
    internal sealed class AConnectorHoldingTheRenderer(global::Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.HoldingTheRenderer.Promises.Application.IABlockRenderer renderer)
    {
        public string Publish(string? description, string block) => renderer.MergeInto(description, block);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.HoldingTheRenderer.Promises.Application
{
    internal interface IABlockRenderer
    {
        string MergeInto(string? existingDescription, string blockText);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.CallingAPromisesHelper.TrackerIntegration.Adapters
{
    internal static class AConnectorCallingTheBlockMarkers
    {
        public static string Publish(string? description, string block)
            => description + global::Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.CallingAPromisesHelper.Promises.Application.ABlockMarkers.Wrap(block);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.CallingAPromisesHelper.Promises.Application
{
    internal static class ABlockMarkers
    {
        public static string Wrap(string block) => $"[forecast]{block}[/forecast]";
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.NestedHelperHoldingTheRenderer.TrackerIntegration.Adapters
{
    internal static class AConnectorWithANestedWriter
    {
        internal sealed class Writer(global::Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.NestedHelperHoldingTheRenderer.Promises.Application.IANestedBlockRenderer renderer)
        {
            public string Publish(string? description, string block) => renderer.MergeInto(description, block);
        }
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.NestedHelperHoldingTheRenderer.Promises.Application
{
    internal interface IANestedBlockRenderer
    {
        string MergeInto(string? existingDescription, string blockText);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.PortInsidePromises.TrackerIntegration.Adapters
{
    internal sealed class AConnector(global::Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.PortInsidePromises.Promises.Application.IDescriptionMerger merger)
    {
        public string Publish(string? description, string block) => merger.MergeInto(description, block);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.PortInsidePromises.Promises.Application
{
    internal interface IDescriptionMerger
    {
        string MergeInto(string? existingDescription, string blockText);
    }

    internal interface IABlockRenderer
    {
        string Render(string forecast);
    }

    internal sealed class ABlockRenderer : IABlockRenderer, IDescriptionMerger
    {
        public string Render(string forecast) => $"[forecast]{forecast}[/forecast]";

        public string MergeInto(string? existingDescription, string blockText) => $"{existingDescription}{blockText}";
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.MergeLeftOnTheRendererPort.TrackerIntegration.Application
{
    internal interface IDescriptionMerger
    {
        string MergeInto(string? existingDescription, string blockText);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.MergeLeftOnTheRendererPort.TrackerIntegration.Adapters
{
    internal sealed class AConnector(global::Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.MergeLeftOnTheRendererPort.TrackerIntegration.Application.IDescriptionMerger merger)
    {
        public string Publish(string? description, string block) => merger.MergeInto(description, block);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.MergeLeftOnTheRendererPort.Promises.Application
{
    internal interface IABlockRenderer
    {
        string Render(string forecast);

        string MergeInto(string? existingDescription, string blockText);
    }

    internal sealed class ABlockRenderer : IABlockRenderer, global::Lighthouse.Backend.Modules.PromisesFixtures.TrackerIntegrationReach.MergeLeftOnTheRendererPort.TrackerIntegration.Application.IDescriptionMerger
    {
        public string Render(string forecast) => $"[forecast]{forecast}[/forecast]";

        public string MergeInto(string? existingDescription, string blockText) => $"{existingDescription}{blockText}";
    }
}
