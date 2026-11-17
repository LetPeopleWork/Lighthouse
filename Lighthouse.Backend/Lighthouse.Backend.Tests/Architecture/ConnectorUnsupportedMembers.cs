using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Answers "which connector methods create a NotSupportedException" from the compiled IL. A connector is any type
    /// implementing an interface of the connector port. Lambdas, local functions and async bodies compile to methods
    /// and nested types the compiler names; their code is read as part of the method that wrote them, so a throw moved
    /// into one of them is still reported against that method. A hand-written private helper is reported by its own name.
    /// </summary>
    internal static class ConnectorUnsupportedMembers
    {
        private const string NotSupported = "System.NotSupportedException";

        // The two factories sit beside the connector port but build connectors; they are not connectors themselves.
        private static readonly HashSet<string> FactoryInterfaces =
        [
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingConnectorFactory",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingSystemFactory",
        ];

        /// <summary>
        /// The connector methods that still create a NotSupportedException, each with the reason it is allowed for now.
        /// The list may only shrink: each entry goes once its connector stops claiming what it cannot do.
        /// </summary>
        public static IReadOnlyDictionary<string, string> Listed { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["JiraWorkTrackingConnector.SweepIdentities(IWorkItemQueryOwner, IEnumerable`1, String, CancellationToken)"] =
                "A private helper, no interface member: a sweep cannot start before the connection has learned whether it talks " +
                "to Jira Cloud or Data Center, because the two page their search results differently.",
            ["AzureDevOpsWorkTrackingConnector.ConvertAuthorizationHeaderToVssCredentials(AuthenticationHeaderValue)"] =
                "A private helper, no interface member: an authentication strategy produced a scheme the Azure DevOps client " +
                "cannot turn into credentials, which is a configuration error rather than a missing capability.",
        };

        /// <summary>
        /// The methods, as Type.Method(ParameterTypes), of every type written in the namespaces matching
        /// <paramref name="typeNamespacePattern"/> that implements an interface of the namespaces matching
        /// <paramref name="portNamespacePattern"/>, whose code creates a NotSupportedException or an exception derived from it.
        /// </summary>
        public static IReadOnlyList<string> FindIn(ModuleDefinition module, string typeNamespacePattern, string portNamespacePattern)
            => module.Types
                .Where(type => IlDependencies.ResidesInNamespaceMatching(type, typeNamespacePattern))
                .SelectMany(HandWrittenTypeAndItsNestedTypes)
                .Where(type => ImplementsAnInterfaceIn(type, portNamespacePattern))
                .SelectMany(type => type.Methods.Where(IsHandWritten))
                .Where(CreatesANotSupportedException)
                .Select(EntryOf)
                .Distinct(StringComparer.Ordinal)
                .ToList();

        private static IEnumerable<TypeDefinition> HandWrittenTypeAndItsNestedTypes(TypeDefinition type)
            => [type, .. type.NestedTypes.Where(IsHandWritten).SelectMany(HandWrittenTypeAndItsNestedTypes)];

        // An interface may be declared on a base class the connector inherits from, so the base classes count too.
        private static bool ImplementsAnInterfaceIn(TypeDefinition type, string portNamespacePattern)
            => type.Interfaces.Any(implemented => IlDependencies.ResidesInNamespaceMatching(implemented.InterfaceType, portNamespacePattern)
                && !FactoryInterfaces.Contains(implemented.InterfaceType.FullName))
                || (BaseTypeInTheSameModule(type) is { } baseType && ImplementsAnInterfaceIn(baseType, portNamespacePattern));

        private static TypeDefinition? BaseTypeInTheSameModule(TypeDefinition type)
            => type.BaseType?.Scope == type.Module ? type.BaseType.Resolve() : null;

        // The compiler names what it makes (lambda bodies, local functions, closures, state machines) starting with '<',
        // which no hand-written method or type can.
        private static bool IsHandWritten(IMemberDefinition member) => !member.Name.StartsWith('<');

        private static bool CreatesANotSupportedException(MethodDefinition method)
            => CodeWrittenBy(method).Any(instruction =>
                instruction.OpCode == OpCodes.Newobj
                && instruction.Operand is MethodReference constructor
                && IsANotSupportedException(constructor.DeclaringType));

        /// <summary>The method's own instructions and those of every compiler-made method and type it brings in.</summary>
        private static IEnumerable<Instruction> CodeWrittenBy(MethodDefinition method)
        {
            var visited = new HashSet<MethodDefinition>();
            var pending = new Stack<MethodDefinition>([method]);

            while (pending.TryPop(out var current))
            {
                if (!visited.Add(current))
                {
                    continue;
                }

                foreach (var compilerMade in CompilerMadeMethodsBroughtInBy(current))
                {
                    pending.Push(compilerMade);
                }
            }

            return visited.Where(visitedMethod => visitedMethod.HasBody).SelectMany(visitedMethod => visitedMethod.Body.Instructions);
        }

        private static IEnumerable<MethodDefinition> CompilerMadeMethodsBroughtInBy(MethodDefinition method)
        {
            var stateMachineMethods = method.CustomAttributes
                .Where(attribute => attribute.AttributeType.Namespace == "System.Runtime.CompilerServices"
                    && attribute.AttributeType.Name.EndsWith("StateMachineAttribute", StringComparison.Ordinal))
                .SelectMany(attribute => attribute.ConstructorArguments)
                .Select(argument => argument.Value)
                .OfType<TypeReference>()
                .Select(stateMachine => stateMachine.Resolve())
                .OfType<TypeDefinition>()
                .SelectMany(stateMachine => stateMachine.Methods.Where(IsTheWrittenBody));

            var referencedMethods = method.HasBody
                ? method.Body.Instructions
                    .Select(instruction => instruction.Operand)
                    .OfType<MethodReference>()
                    .Where(referenced => referenced.DeclaringType.Scope == method.Module)
                    .Select(referenced => referenced.Resolve())
                    .OfType<MethodDefinition>()
                    .Where(IsCompilerMade)
                : [];

            return stateMachineMethods.Concat(referencedMethods);
        }

        // A state machine holds the written body in MoveNext. Its other methods are the compiler's own plumbing, and an
        // iterator's Reset always throws a NotSupportedException nobody wrote, so they are not read.
        private static bool IsTheWrittenBody(MethodDefinition stateMachineMethod) => stateMachineMethod.Name == "MoveNext";

        private static bool IsCompilerMade(MethodDefinition method)
            => !IsHandWritten(method) || (method.DeclaringType.IsNested && !IsHandWritten(method.DeclaringType));

        private static bool IsANotSupportedException(TypeReference constructed)
        {
            var type = constructed.GetElementType();

            if (type.FullName == NotSupported)
            {
                return true;
            }

            return type.Scope switch
            {
                ModuleDefinition => type.Resolve()?.BaseType is { } baseType && IsANotSupportedException(baseType),
                AssemblyNameReference assembly => RuntimeTypeDerivesFromNotSupported(type, assembly),
                _ => false,
            };
        }

        // A framework or library type lives in an assembly the test already runs against, so the runtime can answer
        // whether it derives from NotSupportedException without Cecil resolving that assembly from disk.
        private static bool RuntimeTypeDerivesFromNotSupported(TypeReference type, AssemblyNameReference assembly)
        {
            var runtimeType = Type.GetType($"{type.FullName.Replace('/', '+')}, {assembly.FullName}", throwOnError: false);

            return runtimeType is not null && typeof(NotSupportedException).IsAssignableFrom(runtimeType);
        }

        private static string EntryOf(MethodDefinition method)
            => $"{method.DeclaringType.Name}.{method.Name}({string.Join(", ", method.Parameters.Select(parameter => parameter.ParameterType.Name))})";
    }
}
