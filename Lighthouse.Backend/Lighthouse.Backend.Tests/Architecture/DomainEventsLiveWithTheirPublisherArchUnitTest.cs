using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures;
using Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures.Contexts.Beta.Contracts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A domain event belongs to the context that publishes it: it is declared in that context's Contracts, so a context that
    /// reacts to it names the publisher's published surface and nothing deeper. The publisher is the context whose code
    /// constructs the event; an event only code outside every context constructs belongs to the owner the context map names.
    /// The kernel keeps the event base type and nothing else. The rule reads the compiled IL, and a made-up set of types
    /// proves that a misfiled event is named with its place, its owner and the way out.
    /// </summary>
    [TestFixture]
    public class DomainEventsLiveWithTheirPublisherArchUnitTest
    {
        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(DomainEventsLiveWithTheirPublisherArchUnitTest).Assembly));

        private static string TheKernelEventsNamespace => typeof(IDomainEvent).Namespace!;

        private static string TheEventBase => typeof(IDomainEvent).FullName!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryDomainEvent_LivesInTheContractsOfTheContextThatPublishesIt()
        {
            var misfiled = WhenTheEventsAreCheckedAgainstTheirPublishers(
                ProductionModule.Value,
                ContextContractsMembership.TheModulesRoot,
                ContextContractsMembership.TheContexts,
                OwnerByTheContextMap(ContextContractsMembership.Entries),
                _ => true);

            ThenNoEventIsMisfiled(misfiled);
        }

        [TestCaseSource(nameof(TheEventsTheContextMapOwns))]
        public void TheEvent_ResolvesToExactlyOneTypeInTheContractsOfItsOwner(string eventName, string owner, string ownersContracts)
        {
            var found = WhenTheEventIsLookedUpByName(ProductionModule.Value, eventName);

            ThenItIsFoundOnlyInTheContractsOfItsOwner(eventName, owner, ownersContracts, found);
        }

        [Test]
        public void TheKernelEventsNamespace_HoldsNothingButTheEventBase()
        {
            var inTheKernel = WhenTheKernelEventsNamespaceIsRead(ProductionModule.Value);

            ThenOnlyTheEventBaseIsLeft(inTheKernel);
        }

        [Test]
        public void TheContextMap_NamesExactlyTheEventsTheCompiledCodeDeclares()
        {
            var declared = WhenTheDeclaredEventsAreRead(ProductionModule.Value);

            ThenTheMapAndTheCompiledCodeNameTheSameEvents(declared, ContextContractsMembership.Entries);
        }

        [Test]
        public void AnEventFiledInAnotherContextsContracts_IsNamedWithItsPlaceItsOwnerAndTheWayOut()
        {
            var misfiled = WhenTheEventsAreCheckedAgainstTheirPublishers(
                TestModule.Value,
                MadeUpEventContexts.TheRoot,
                MadeUpEventContexts.TheContexts,
                MadeUpEventContexts.OwnerOfAnEventOnlyOutsideCodePublishes,
                type => type.Namespace.StartsWith(typeof(MadeUpEventContexts).Namespace!, StringComparison.Ordinal));

            ThenOnlyTheMisfiledMadeUpEventsAreNamedWithTheirPlaceOwnerAndWayOut(misfiled);
        }

        private static IEnumerable<TestCaseData> TheEventsTheContextMapOwns()
            => ContextContractsMembership.Entries
                .Select(entry => entry.Type)
                .Where(type => type.OldNamespace == typeof(IDomainEvent).Namespace)
                .Select(type => new TestCaseData(type.Name, type.Area, type.NewNamespace).SetArgDisplayNames(type.Name));

        private static List<MisfiledEvent> WhenTheEventsAreCheckedAgainstTheirPublishers(ModuleDefinition module, string root, IReadOnlyList<string> contexts, Func<string, string?> ownerOfAnEventOnlyOutsideCodePublishes, Func<TypeDefinition, bool> isChecked)
        {
            var contextRoot = new Regex($@"^{Regex.Escape(root)}\.(?<context>[A-Za-z0-9_]+)(\.|$)", RegexOptions.None, TimeSpan.FromSeconds(1));
            string? ContextOf(string ns) => contextRoot.Match(ns) is { Success: true } found && contexts.Contains(found.Groups["context"].Value) ? found.Groups["context"].Value : null;

            var events = module.Types.Where(type => IsAnEvent(type) && isChecked(type)).ToList();
            var constructedBy = ContextsThatConstruct(module, events, ContextOf);
            var misfiled = new List<MisfiledEvent>();

            foreach (var domainEvent in events)
            {
                var publishers = constructedBy[domainEvent.FullName];
                var owner = publishers.Count == 1 ? publishers.Single() : publishers.Count == 0 ? ownerOfAnEventOnlyOutsideCodePublishes(domainEvent.Name) : null;
                var source = publishers.Count == 0 ? "constructed only outside the contexts, so the owner is the one the context map names" : $"constructed by the context code of {string.Join(" and ", publishers)}";

                if (owner is null)
                {
                    misfiled.Add(new MisfiledEvent(domainEvent.Name, domainEvent.Namespace, source, null, null));
                }
                else if (domainEvent.Namespace != ContractsNamespaceOf(root, owner))
                {
                    misfiled.Add(new MisfiledEvent(domainEvent.Name, domainEvent.Namespace, source, owner, ContractsNamespaceOf(root, owner)));
                }
            }

            return misfiled;
        }

        private static Dictionary<string, SortedSet<string>> ContextsThatConstruct(ModuleDefinition module, List<TypeDefinition> events, Func<string, string?> contextOf)
        {
            var constructedBy = events.ToDictionary(domainEvent => domainEvent.FullName, _ => new SortedSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);

            foreach (var constructing in module.Types.SelectMany(AllTypesIn))
            {
                var topLevel = constructing;
                while (topLevel.DeclaringType is not null)
                {
                    topLevel = topLevel.DeclaringType;
                }

                var context = contextOf(topLevel.Namespace);
                if (context is null)
                {
                    continue;
                }

                var constructed = constructing.Methods
                    .Where(method => method.HasBody)
                    .SelectMany(method => method.Body.Instructions)
                    .Where(instruction => instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference)
                    .Select(instruction => ((MethodReference)instruction.Operand).DeclaringType.FullName)
                    .Where(fullName => constructedBy.ContainsKey(fullName) && fullName != topLevel.FullName);

                foreach (var fullName in constructed)
                {
                    constructedBy[fullName].Add(context);
                }
            }

            return constructedBy;
        }

        private static IEnumerable<TypeDefinition> AllTypesIn(TypeDefinition type)
            => [type, .. type.NestedTypes.SelectMany(AllTypesIn)];

        private static bool IsAnEvent(TypeDefinition type)
            => !type.IsInterface && type.Interfaces.Any(implemented => implemented.InterfaceType.FullName == TheEventBase);

        private static string ContractsNamespaceOf(string root, string context) => string.Join('.', root, context, "Contracts");

        private static Func<string, string?> OwnerByTheContextMap(IReadOnlyList<(MovingType Type, ContractAdmission Admission)> entries)
            => name => entries.Where(entry => entry.Type.Name == name).Select(entry => entry.Type.Area).FirstOrDefault();

        private static List<string> WhenTheEventIsLookedUpByName(ModuleDefinition module, string name)
            => [.. module.Types.Where(type => type.Name == name).Select(type => type.FullName)];

        private static List<string> WhenTheKernelEventsNamespaceIsRead(ModuleDefinition module)
            => [.. module.Types.Where(type => type.Namespace == TheKernelEventsNamespace).Select(type => type.Name).Order(StringComparer.Ordinal)];

        private static List<string> WhenTheDeclaredEventsAreRead(ModuleDefinition module)
            => [.. module.Types.Where(IsAnEvent).Select(type => type.Name).Order(StringComparer.Ordinal)];

        private static void ThenNoEventIsMisfiled(List<MisfiledEvent> misfiled)
            => Assert.That(
                misfiled,
                Is.Empty,
                MessageFor(misfiled));

        private static void ThenItIsFoundOnlyInTheContractsOfItsOwner(string eventName, string owner, string ownersContracts, List<string> found)
            => Assert.That(
                found,
                Is.EqualTo(new[] { $"{ownersContracts}.{eventName}" }),
                $"WHAT: the event {eventName} should resolve to exactly one type, {ownersContracts}.{eventName}, in the Contracts of {owner}; it resolves to [{string.Join(", ", found)}]." + Environment.NewLine +
                "WHY: " + TheReason + Environment.NewLine +
                $"HOW: {TheWayOut}");

        private static void ThenOnlyTheEventBaseIsLeft(List<string> inTheKernel)
            => Assert.That(
                inTheKernel,
                Is.EqualTo(new[] { nameof(IDomainEvent) }),
                $"WHAT: the kernel namespace {TheKernelEventsNamespace} should hold {nameof(IDomainEvent)} only; it holds [{string.Join(", ", inTheKernel)}]." + Environment.NewLine +
                "WHY: " + TheReason + Environment.NewLine +
                "HOW: " + TheWayOut);

        private static void ThenTheMapAndTheCompiledCodeNameTheSameEvents(List<string> declared, IReadOnlyList<(MovingType Type, ContractAdmission Admission)> entries)
        {
            var mapped = entries.Select(entry => entry.Type).Where(type => type.OldNamespace == TheKernelEventsNamespace).Select(type => type.Name).Order(StringComparer.Ordinal).ToList();

            Assert.That(
                declared,
                Is.EqualTo(mapped),
                $"WHAT: the {mapped.Count} events of the context map and the {declared.Count} events the compiled code declares should be the same names. " +
                "WHY: an event the map does not own has no declared owner, so the placement rule cannot say where it belongs. " +
                "HOW: add the new event to the context map with its owner, or remove the entry of an event that no longer exists.");
        }

        private static void ThenOnlyTheMisfiledMadeUpEventsAreNamedWithTheirPlaceOwnerAndWayOut(List<MisfiledEvent> misfiled)
        {
            var named = misfiled.Select(entry => entry.Event).Order(StringComparer.Ordinal).ToList();
            var message = MessageFor(misfiled);

            using var scope = Assert.EnterMultipleScope();
            Assert.That(named, Is.EqualTo(new[] { nameof(BetaFiledEvent), nameof(OutsideMisfiledEvent) }), "WHAT: exactly the two misfiled made-up events are named, and the rightly filed ones are not. WHY: a rule that names a correct event or misses a wrong one cannot be trusted. HOW: file each event in the Contracts of its publisher.");
            Assert.That(message, Does.Contain(MadeUpEventContexts.ContractsOf(MadeUpEventContexts.Beta)), "WHAT: the message names the place the event is declared in. WHY: the reader must see where it sits. HOW: print the declared namespace.");
            Assert.That(message, Does.Contain(MadeUpEventContexts.ContractsOf(MadeUpEventContexts.Alpha)), "WHAT: the message names the Contracts of the owner. WHY: the reader must see where it belongs. HOW: print the owner's Contracts namespace.");
            Assert.That(message, Does.Contain("constructed by the context code of Alpha"), "WHAT: the message names the source that makes Alpha the publisher. WHY: the reader must see why the owner is Alpha. HOW: print how the publisher was found.");
            Assert.That(message, Does.Contain("Move the event"), "WHAT: the message names the way out. WHY: the reader must know what to do. HOW: print the move to make.");
        }

        private static string MessageFor(List<MisfiledEvent> misfiled)
            => "WHAT: " + string.Join(Environment.NewLine, misfiled.Select(Describe)) + Environment.NewLine +
                "WHY: " + TheReason + Environment.NewLine +
                "HOW: " + TheWayOut;

        private static string Describe(MisfiledEvent entry)
            => entry.Owner is null
                ? $"The event {entry.Event} in {entry.DeclaredIn} has no single publisher: {entry.Source}."
                : $"The event {entry.Event} is declared in {entry.DeclaredIn}, but its publisher is {entry.Owner} ({entry.Source}), so it belongs in {entry.ExpectedNamespace}.";

        private static string TheReason
            => "a domain event is promised to the contexts that react to it, so it is published by the context that raises it; one left in the kernel or filed under another context hides who owns it and ties the reacting contexts to the wrong place.";

        private static string TheWayOut
            => $"Move the event with git mv into the Contracts folder of the context that publishes it, change its namespace line to that Contracts namespace, and keep only {nameof(IDomainEvent)} in the kernel.";

        private sealed record MisfiledEvent(string Event, string DeclaredIn, string Source, string? Owner, string? ExpectedNamespace);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures
{
    using Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures.Contexts.Alpha.Contracts;
    using Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures.Contexts.Beta.Contracts;

    internal static class MadeUpEventContexts
    {
        public const string Alpha = "Alpha";

        public const string Beta = "Beta";

        public static string TheRoot => string.Join('.', typeof(MadeUpEventContexts).Namespace, "Contexts");

        public static IReadOnlyList<string> TheContexts { get; } = [Alpha, Beta];

        public static string ContractsOf(string context) => string.Join('.', TheRoot, context, "Contracts");

        public static string? OwnerOfAnEventOnlyOutsideCodePublishes(string eventName)
            => eventName is nameof(OutsideMisfiledEvent) or nameof(OutsideFiledEvent) ? Alpha : null;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures.Contexts.Alpha.Contracts
{
    using Lighthouse.Backend.Models.Events;

    public sealed record AlphaFiledEvent(int Id) : IDomainEvent;

    public sealed record OutsideFiledEvent(int Id) : IDomainEvent;
}

namespace Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures.Contexts.Beta.Contracts
{
    using Lighthouse.Backend.Models.Events;

    public sealed record BetaFiledEvent(int Id) : IDomainEvent;

    public sealed record OutsideMisfiledEvent(int Id) : IDomainEvent;
}

namespace Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures.Contexts.Alpha.Application
{
    using Lighthouse.Backend.Models.Events;
    using Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures.Contexts.Alpha.Contracts;
    using Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures.Contexts.Beta.Contracts;

    public sealed class AlphaPublisher
    {
        public IDomainEvent[] Publish() => [new AlphaFiledEvent(1), new BetaFiledEvent(2)];
    }
}

namespace Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures.Outside
{
    using Lighthouse.Backend.Models.Events;
    using Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures.Contexts.Alpha.Contracts;
    using Lighthouse.Backend.Tests.Architecture.DomainEventPublisherFixtures.Contexts.Beta.Contracts;

    public sealed class OutsidePublisher
    {
        public IDomainEvent[] Publish() => [new OutsideFiledEvent(3), new OutsideMisfiledEvent(4)];
    }
}
