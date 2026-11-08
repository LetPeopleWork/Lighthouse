using System.Text.RegularExpressions;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The rules that keep the application core in its place, each defined once so the real code and a
    /// made-up set of types are checked by the same rule. The types a rule is about are parameters, so a
    /// test can hand it a made-up type in their place.
    /// </summary>
    /// <remarks>
    /// The rules read the compiled IL rather than ArchUnitNET's dependency graph, because in a Release build,
    /// which is what CI tests, that graph drops what an async method uses.
    /// </remarks>
    internal static class ApplicationCoreRules
    {
        /// <summary>The application core: Services, and the modules its types move into one at a time.</summary>
        public const string ApplicationCore = @"^Lighthouse\.Backend\.(Services|Modules)(\.|$)";

        public const string TheApi = @"^Lighthouse\.Backend\.API(\.|$)";

        public const string TheData = @"^Lighthouse\.Backend\.Data(\.|$)";

        public const string TheForecastModels = @"^Lighthouse\.Backend\.Models\.Forecast(\.|$)";

        public const string FeatureEntity = "Lighthouse.Backend.Models.Feature";

        public const string DeliveryEntity = "Lighthouse.Backend.Models.Delivery";

        public const string BlackoutPeriodEntity = "Lighthouse.Backend.Models.BlackoutPeriod";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly string[] RecalculationEntities = [FeatureEntity, DeliveryEntity, BlackoutPeriodEntity];

        private static readonly string[] RepositorySaveMethods = ["Save", "SaveChangesAsync"];

        private const string ServiceLocatorMethod = "GetRequiredService";

        private const string AsyncMethodBuilderTypeFragment = "AsyncTaskMethodBuilder";

        /// <summary>No type in the application core reaches a type in the API.</summary>
        public static KernelRuleVerdict CoreDoesNotDependOnApi(ModuleDefinition module, string core = ApplicationCore)
            => new(
                IlDependencies.TypesIn(module, core),
                IlDependencies.DependentsIn(module, core, type => IlDependencies.ResidesInNamespaceMatching(type, TheApi)),
                $"Types in namespaces matching \"{core}\" should not depend on any type in namespaces matching \"{TheApi}\", because " +
                "the application core is driven by the API, so it must not depend back on it; the DTOs the core returns " +
                "live in the Models, not in API.DTO.");

        /// <summary>The types that decide under which parent a record hangs reach neither the application core nor the data layer.</summary>
        public static KernelRuleVerdict ParentPlacementKernelDependsOnNoCoreOrData(ModuleDefinition module, IReadOnlyCollection<string> kernelTypes, string core = ApplicationCore)
            => VerdictAmong(
                module,
                kernelTypes,
                type => IlDependencies.ResidesInNamespaceMatching(type, core) || IlDependencies.ResidesInNamespaceMatching(type, TheData),
                $"namespaces matching \"{core}\" or \"{TheData}\"",
                "deciding under which issue a record hangs must be checkable from what it is handed alone. If the pick " +
                "could reach a repository, the database context, a connector or a logging service, its answer could " +
                "change with what it happened to read, and a wrongly placed parent looks exactly like correct data.");

        /// <summary>The types that show a retired Delivery reach nothing its numbers could be recalculated from.</summary>
        public static KernelRuleVerdict ArchivedReadPathReachesNoRecalculation(ModuleDefinition module, IReadOnlyCollection<string> projectionTypes, string core = ApplicationCore)
            => VerdictAmong(
                module,
                projectionTypes,
                type => RecalculationEntities.Contains(type.FullName, StringComparer.Ordinal)
                    || IlDependencies.ResidesInNamespaceMatching(type, TheForecastModels)
                    || IlDependencies.ResidesInNamespaceMatching(type, core)
                    || IlDependencies.ResidesInNamespaceMatching(type, TheData),
                $"{string.Join(", ", RecalculationEntities)} or namespaces matching \"{TheForecastModels}\", \"{core}\" or \"{TheData}\"",
                "a retired Delivery's numbers were written down once and must read the same every time. The only way to " +
                "be sure of that is for the code that builds the view to have nothing it could recalculate from: no " +
                "Feature, no Delivery, no calendar of non-working days, no forecast and no service. Handing it any of " +
                "those makes the guarantee a matter of nobody choosing to use them.");

        /// <summary>No domain-event handler, nor the dispatcher, resolves a collaborator through the service locator.</summary>
        public static KernelRuleVerdict HandlersUseNoServiceLocator(ModuleDefinition module)
            => HandlersUseNoServiceLocator(module, HandlerSelection.DomainEventHandlersAndTheDomainEventsNamespace);

        public static KernelRuleVerdict HandlersUseNoServiceLocator(ModuleDefinition module, HandlerSelection handlers)
        {
            var selected = handlers.In(module);

            var offenders = selected
                .SelectMany(type => type.Methods.Where(method => method.HasBody).Select(method => (Handler: DeclaringTypeOf(type), Method: method)))
                .SelectMany(entry => CalledMethods(entry.Method)
                    .Where(called => called.Name == ServiceLocatorMethod)
                    .Select(called => (entry.Handler, Target: $"{called.DeclaringType.FullName}.{called.Name}")))
                .GroupBy(edge => edge.Handler, StringComparer.Ordinal)
                .Select(group => new IlDependent(group.Key, [.. group.Select(edge => edge.Target).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]))
                .ToList();

            return new KernelRuleVerdict(
                Examined(selected),
                offenders,
                $"{handlers.Description} should not resolve a collaborator through IServiceProvider.GetRequiredService, because " +
                "the dispatcher and its handlers take their collaborators as typed IEnumerable<IDomainEventHandler<TEvent>> " +
                "(IServiceProvider.GetServices); calling GetRequiredService brings back the service locator the dispatcher removes.",
                "resolves through");
        }

        /// <summary>A domain-event handler method that saves through a repository awaits the save.</summary>
        public static KernelRuleVerdict HandlersAwaitRepositorySaves(ModuleDefinition module)
            => HandlersAwaitRepositorySaves(module, HandlerSelection.DomainEventHandlersAndTheDomainEventsNamespace);

        public static KernelRuleVerdict HandlersAwaitRepositorySaves(ModuleDefinition module, HandlerSelection handlers)
        {
            var selected = handlers.In(module);

            // An awaited save moves into the compiler-made state machine and leaves the method itself calling the
            // async method builder; a save left in the method's own body, with no builder call, is not awaited.
            var offenders = selected
                .Where(type => !IsCompilerMade(type))
                .SelectMany(type => type.Methods.Where(method => method.HasBody && CallsRepositorySave(method) && !IsAsync(method))
                    .Select(method => (Handler: DeclaringTypeOf(type), Method: method.Name)))
                .GroupBy(edge => edge.Handler, StringComparer.Ordinal)
                .Select(group => new IlDependent(group.Key, [.. group.Select(edge => edge.Method).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]))
                .ToList();

            return new KernelRuleVerdict(
                Examined(selected),
                offenders,
                $"{handlers.Description} that persist through a repository Save() or SaveChangesAsync() must await it, because " +
                "a save that is not awaited returns before SaveChangesAsync finishes, so the dispatcher's scope disposes the " +
                "database context mid-write on the shared Npgsql connection and desyncs the wire protocol (Postgres only; " +
                "SQLite and the in-memory database tolerate it).",
                "calls a repository save without awaiting it in");
        }

        private static KernelRuleVerdict VerdictAmong(
            ModuleDefinition module, IReadOnlyCollection<string> subjectTypes, Func<TypeReference, bool> isForbidden, string forbidden, string because)
        {
            var subjects = subjectTypes.ToHashSet(StringComparer.Ordinal);
            var namespaces = $"^({string.Join("|", subjectTypes.Select(NamespaceOfFullName).Distinct(StringComparer.Ordinal).Select(Regex.Escape))})$";

            List<string> examined = [.. IlDependencies.TypesIn(module, namespaces).Where(subjects.Contains)];

            return new(
                examined,
                [.. IlDependencies.DependentsIn(module, namespaces, isForbidden).Where(dependent => subjects.Contains(dependent.TypeName))],
                $"The types {string.Join(", ", subjectTypes)} should not depend on {forbidden}, because {because}")
            {
                MissingSubjects = [.. subjectTypes.Except(examined, StringComparer.Ordinal)],
            };
        }

        private static string NamespaceOfFullName(string typeFullName)
            => typeFullName[..Math.Max(typeFullName.LastIndexOf('.'), 0)];

        private static List<string> Examined(IEnumerable<TypeDefinition> selected)
            => [.. selected.Select(DeclaringTypeOf).Distinct(StringComparer.Ordinal)];

        private static string DeclaringTypeOf(TypeDefinition type)
            => IlDependencies.DeclaringTypeOf(type.FullName.Replace('/', '+'));

        private static bool IsCompilerMade(TypeDefinition type)
            => type.Name.Contains('<');

        private static IEnumerable<MethodReference> CalledMethods(MethodDefinition method)
            => method.Body.Instructions
                .Where(instruction => instruction.OpCode.FlowControl == FlowControl.Call)
                .Select(instruction => instruction.Operand)
                .OfType<MethodReference>();

        private static bool CallsRepositorySave(MethodDefinition method)
            => CalledMethods(method).Any(called => RepositorySaveMethods.Contains(called.Name, StringComparer.Ordinal));

        private static bool IsAsync(MethodDefinition method)
            => CalledMethods(method).Any(called => called.DeclaringType.Name.Contains(AsyncMethodBuilderTypeFragment, StringComparison.Ordinal));
    }

    /// <summary>Which types the handler rules look at: the domain-event handlers, picked out of a module.</summary>
    internal sealed class HandlerSelection
    {
        private static readonly string DomainEventHandlerInterface = typeof(IDomainEventHandler<>).FullName!;

        /// <summary>Every type whose full name carries the domain-events implementation namespace, with the types nested in it.</summary>
        public static readonly HandlerSelection ByDomainEventsNamespace = new(
            type => type.FullName.Contains(".Services.Implementation.DomainEvents", StringComparison.Ordinal),
            "Types in the .Services.Implementation.DomainEvents namespace");

        /// <summary>
        /// Every concrete domain-event handler, wherever its namespace, together with every type the domain-events
        /// namespace holds, so a handler that moves out of that namespace stays checked, and so does the dispatcher.
        /// </summary>
        public static readonly HandlerSelection DomainEventHandlersAndTheDomainEventsNamespace = new(
            type => ByDomainEventsNamespace.selects(type) || IsDomainEventHandler(type),
            "Domain-event handlers, wherever they live, and the types in the .Services.Implementation.DomainEvents namespace");

        private readonly Func<TypeDefinition, bool> selects;

        private HandlerSelection(Func<TypeDefinition, bool> selects, string description)
        {
            this.selects = selects;
            Description = description;
        }

        public string Description { get; }

        /// <summary>The selected types and every type nested in them, the compiler-made ones included.</summary>
        public List<TypeDefinition> In(ModuleDefinition module)
            => [.. module.Types
                .SelectMany(TypeAndItsNestedTypes)
                .Where(selects)
                .SelectMany(TypeAndItsNestedTypes)
                .DistinctBy(type => type.FullName, StringComparer.Ordinal)];

        private static bool IsDomainEventHandler(TypeDefinition type)
            => type is { IsAbstract: false, IsInterface: false }
                && type.Interfaces.Any(implemented => implemented.InterfaceType is GenericInstanceType generic
                    && generic.ElementType.FullName == DomainEventHandlerInterface);

        private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];
    }
}
