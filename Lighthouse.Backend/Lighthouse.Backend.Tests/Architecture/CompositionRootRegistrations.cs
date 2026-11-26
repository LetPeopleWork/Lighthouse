using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// What a composition root hands to the container and what it asks the container for, read from the compiled IL of the
    /// type and of every type nested in it, lambdas and their closures included. The two are kept apart, because a service the
    /// host looks up to set itself up is not thereby allowed to be registered there.
    /// </summary>
    /// <remarks>
    /// Registered: every generic argument of a service-collection extension named Add..., TryAdd... or Configure... (keyed,
    /// hosted-service and typed-HTTP-client forms included), of any generic extension of an HTTP client builder (message
    /// handlers, typed clients), and of a generic service-descriptor factory; and every type the root takes with typeof, which
    /// is how the non-generic overloads and a service descriptor built by hand receive a type. Looked up: every generic argument
    /// of GetService, GetRequiredService, GetServices and their keyed forms, and of the activator utilities that build a type
    /// from the container. A generic argument that is itself generic counts with each type it is built from.
    /// </remarks>
    internal static class CompositionRootRegistrations
    {
        private const string TheServiceCollection = "Microsoft.Extensions.DependencyInjection.IServiceCollection";

        private const string TheHttpClientBuilder = "Microsoft.Extensions.DependencyInjection.IHttpClientBuilder";

        private const string TheServiceDescriptor = "Microsoft.Extensions.DependencyInjection.ServiceDescriptor";

        private const string TheServiceProvider = "System.IServiceProvider";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly string[] RegistrationPrefixes = ["Add", "TryAdd", "Configure"];

        private static readonly string[] LookupNames =
        [
            "GetService", "GetRequiredService", "GetServices",
            "GetKeyedService", "GetRequiredKeyedService", "GetKeyedServices",
            "CreateInstance", "GetServiceOrCreateInstance",
        ];

        /// <summary>
        /// The full names of the types in the namespaces matching the pattern that the named composition root registers, and
        /// those it looks up, once each and in order. A nested type is named with '+' between it and the type it is nested in.
        /// </summary>
        public static CompositionRootUse Of(ModuleDefinition module, string compositionRoot, string namespacePattern)
        {
            ArgumentNullException.ThrowIfNull(module);

            var root = module.Types.SingleOrDefault(type => type.FullName == compositionRoot)
                ?? throw new InvalidOperationException(
                    $"No top-level type {compositionRoot} is in {module.Name}, so its registrations cannot be read. " +
                    "Name the type that composes the application.");

            var instructions = TypeAndItsNestedTypes(root)
                .SelectMany(type => type.Methods)
                .Where(method => method.HasBody)
                .SelectMany(method => method.Body.Instructions)
                .ToList();

            var calls = instructions.Select(instruction => instruction.Operand).OfType<GenericInstanceMethod>().ToList();
            var takenWithTypeof = instructions
                .Where(instruction => instruction.OpCode == OpCodes.Ldtoken)
                .Select(instruction => instruction.Operand)
                .OfType<TypeReference>();

            var registered = calls.Where(IsARegistration).SelectMany(call => call.GenericArguments).Concat(takenWithTypeof);
            var lookedUp = calls.Where(IsALookup).SelectMany(call => call.GenericArguments);

            return new CompositionRootUse(Matching(registered, namespacePattern), Matching(lookedUp, namespacePattern));
        }

        private static List<string> Matching(IEnumerable<TypeReference> types, string namespacePattern)
            => [.. types
                .SelectMany(Unwrapped)
                .Where(type => Regex.IsMatch(IlDependencies.NamespaceOf(type), namespacePattern, RegexOptions.None, PatternTimeout))
                .Select(type => type.FullName.Replace('/', '+'))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];

        private static bool IsARegistration(GenericInstanceMethod call)
        {
            var method = call.ElementMethod;
            var firstParameter = FirstParameterOf(method);

            return (firstParameter == TheServiceCollection && RegistrationPrefixes.Any(prefix => method.Name.StartsWith(prefix, StringComparison.Ordinal)))
                || firstParameter == TheHttpClientBuilder
                || method.DeclaringType.FullName == TheServiceDescriptor;
        }

        private static bool IsALookup(GenericInstanceMethod call)
        {
            var method = call.ElementMethod;

            return FirstParameterOf(method) == TheServiceProvider && LookupNames.Contains(method.Name, StringComparer.Ordinal);
        }

        private static string? FirstParameterOf(MethodReference method)
            => method.Parameters.Count > 0 ? method.Parameters[0].ParameterType.FullName : null;

        private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];

        private static IEnumerable<TypeReference> Unwrapped(TypeReference type)
            => type switch
            {
                GenericInstanceType generic => [generic.ElementType, .. generic.GenericArguments.SelectMany(Unwrapped)],
                TypeSpecification wrapper => Unwrapped(wrapper.ElementType),
                _ => [type],
            };
    }

    /// <summary>The context types a composition root registers, and those it looks up, each by full name, once each, in order.</summary>
    internal sealed record CompositionRootUse(IReadOnlyList<string> Registered, IReadOnlyList<string> LookedUp);
}
