using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Answers "which types in these namespaces reference a type like that one" from the compiled IL.
    /// Lambdas and async methods compile to nested types the compiler names; those are walked too, and
    /// what they reference is reported against the type that wrote them.
    /// </summary>
    internal static class IlDependencies
    {
        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Reads an assembly into memory. A module is not safe to read from two threads at once, so each
        /// test fixture reads its own and disposes it when the fixture ends.
        /// </summary>
        public static ModuleDefinition ReadModule(Assembly assembly)
            => ModuleDefinition.ReadModule(assembly.Location, new ReaderParameters { InMemory = true });

        /// <summary>The types the code's authors wrote in the namespaces matching the pattern.</summary>
        public static List<string> TypesIn(ModuleDefinition module, string namespacePattern)
            => TypesWrittenIn(module, namespacePattern)
                .SelectMany(TypeAndItsNestedTypes)
                .Select(type => DeclaringTypeOf(NameOf(type)))
                .Distinct(StringComparer.Ordinal)
                .ToList();

        /// <summary>
        /// The types in the namespaces matching the pattern that reference at least one type the predicate
        /// picks, each with the names of the types it was caught referencing.
        /// </summary>
        public static List<IlDependent> DependentsIn(ModuleDefinition module, string namespacePattern, Func<TypeReference, bool> isTarget)
            => TypesWrittenIn(module, namespacePattern)
                .SelectMany(TypeAndItsNestedTypes)
                .SelectMany(type => ReferencedTypes(type).Where(isTarget).Select(target => (Dependent: DeclaringTypeOf(NameOf(type)), Target: NameOf(target))))
                .GroupBy(edge => edge.Dependent, StringComparer.Ordinal)
                .Select(group => new IlDependent(group.Key, [.. group.Select(edge => edge.Target).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]))
                .ToList();

        public static bool ResidesInNamespaceMatching(TypeReference type, string namespacePattern)
            => Regex.IsMatch(NamespaceOf(type), namespacePattern, RegexOptions.None, PatternTimeout);

        /// <summary>A nested type has no namespace of its own; it lives where its outermost type lives.</summary>
        public static string NamespaceOf(TypeReference type)
            => type.DeclaringType is null ? type.Namespace : NamespaceOf(type.DeclaringType);

        // The compiler names the nested types it makes (lambda closures, async state machines) starting
        // with '<', which no hand-written type can, so the name is cut where the first such level begins.
        public static string DeclaringTypeOf(string typeFullName)
        {
            var firstCompilerMadeLevel = typeFullName.IndexOf("+<", StringComparison.Ordinal);

            return firstCompilerMadeLevel < 0 ? typeFullName : typeFullName[..firstCompilerMadeLevel];
        }

        private static IEnumerable<TypeDefinition> TypesWrittenIn(ModuleDefinition module, string namespacePattern)
            => module.Types.Where(type => Regex.IsMatch(type.Namespace, namespacePattern, RegexOptions.None, PatternTimeout));

        private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];

        private static string NameOf(TypeReference type) => type.FullName.Replace('/', '+');

        private static IEnumerable<TypeReference> ReferencedTypes(TypeDefinition type)
            => type.Interfaces.Select(implemented => implemented.InterfaceType)
                .Prepend(type.BaseType)
                .OfType<TypeReference>()
                .Concat(type.GenericParameters.SelectMany(TypesInConstraints))
                .Concat(TypesInAttributes(type))
                .Concat(type.Fields.SelectMany(field => TypesInAttributes(field).Prepend(field.FieldType)))
                .Concat(type.Properties.SelectMany(property => TypesInAttributes(property).Prepend(property.PropertyType)))
                .Concat(type.Methods.SelectMany(TypesInMethod))
                .SelectMany(Unwrapped);

        private static IEnumerable<TypeReference> TypesInMethod(MethodDefinition method)
        {
            var declared = TypesInSignature(method)
                .Concat(TypesInAttributes(method))
                .Concat(method.Parameters.SelectMany(TypesInAttributes))
                .Concat(method.GenericParameters.SelectMany(TypesInConstraints));

            return method.HasBody
                ? declared
                    .Concat(method.Body.Variables.Select(variable => variable.VariableType))
                    .Concat(method.Body.Instructions.SelectMany(TypesInOperand))
                : declared;
        }

        private static IEnumerable<TypeReference> TypesInSignature(MethodReference method)
            => [method.ReturnType, .. method.Parameters.Select(parameter => parameter.ParameterType)];

        private static IEnumerable<TypeReference> TypesInConstraints(GenericParameter parameter)
            => parameter.Constraints.Select(constraint => constraint.ConstraintType);

        private static IEnumerable<TypeReference> TypesInAttributes(Mono.Cecil.ICustomAttributeProvider owner)
            => owner.CustomAttributes.SelectMany(attribute => attribute.ConstructorArguments
                .Select(argument => argument.Value)
                .OfType<TypeReference>()
                .Prepend(attribute.AttributeType));

        private static IEnumerable<TypeReference> TypesInOperand(Instruction instruction)
            => instruction.Operand switch
            {
                TypeReference type => [type],
                GenericInstanceMethod generic => [generic.DeclaringType, .. generic.GenericArguments, .. TypesInSignature(generic)],
                MethodReference method => [method.DeclaringType, .. TypesInSignature(method)],
                FieldReference field => [field.DeclaringType, field.FieldType],
                _ => [],
            };

        private static IEnumerable<TypeReference> Unwrapped(TypeReference type)
            => type switch
            {
                GenericInstanceType generic => [generic.ElementType, .. generic.GenericArguments.SelectMany(Unwrapped)],
                TypeSpecification wrapper => Unwrapped(wrapper.ElementType),
                _ => [type],
            };
    }

    /// <summary>A type caught referencing what a rule forbids, with the names of what it referenced.</summary>
    internal sealed record IlDependent(string TypeName, IReadOnlyList<string> Targets);
}
