using System.Text.RegularExpressions;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The types a composition root hands to the container, read from the compiled IL: every generic argument of a call to a
    /// service-collection registration extension (a method named Add..., TryAdd... or Configure... whose first parameter is the
    /// service collection) or of a service lookup (GetService or GetRequiredService), in the type and in every type nested in it,
    /// lambdas and their closures included. A generic argument that is itself generic counts with each type it is built from.
    /// </summary>
    /// <remarks>
    /// A type handed over as a typeof argument to a non-generic overload is not a generic argument and is not read.
    /// </remarks>
    internal static class CompositionRootRegistrations
    {
        private const string TheServiceCollection = "Microsoft.Extensions.DependencyInjection.IServiceCollection";

        private const string TheServiceProvider = "System.IServiceProvider";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly string[] RegistrationPrefixes = ["Add", "TryAdd", "Configure"];

        private static readonly string[] LookupNames = ["GetService", "GetRequiredService"];

        /// <summary>
        /// The full names of the types in the namespaces matching the pattern that the named composition root registers or looks
        /// up, once each and in order. A nested type is named with '+' between it and the type it is nested in.
        /// </summary>
        public static IReadOnlyList<string> Of(ModuleDefinition module, string compositionRoot, string namespacePattern)
        {
            ArgumentNullException.ThrowIfNull(module);

            var root = module.Types.SingleOrDefault(type => type.FullName == compositionRoot)
                ?? throw new InvalidOperationException(
                    $"No top-level type {compositionRoot} is in {module.Name}, so its registrations cannot be read. " +
                    "Name the type that composes the application.");

            return [.. TypeAndItsNestedTypes(root)
                .SelectMany(type => type.Methods)
                .Where(method => method.HasBody)
                .SelectMany(method => method.Body.Instructions)
                .Select(instruction => instruction.Operand)
                .OfType<GenericInstanceMethod>()
                .Where(IsARegistrationOrALookup)
                .SelectMany(call => call.GenericArguments)
                .SelectMany(Unwrapped)
                .Where(type => Regex.IsMatch(IlDependencies.NamespaceOf(type), namespacePattern, RegexOptions.None, PatternTimeout))
                .Select(type => type.FullName.Replace('/', '+'))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];
        }

        private static bool IsARegistrationOrALookup(GenericInstanceMethod call)
        {
            var method = call.ElementMethod;
            var firstParameter = method.Parameters.Count > 0 ? method.Parameters[0].ParameterType.FullName : null;

            return (firstParameter == TheServiceCollection && RegistrationPrefixes.Any(prefix => method.Name.StartsWith(prefix, StringComparison.Ordinal)))
                || (firstParameter == TheServiceProvider && LookupNames.Contains(method.Name, StringComparer.Ordinal));
        }

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
}
