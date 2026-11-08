using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// What the guards that read the compiled test assembly share: which types are test-host factories, which are
    /// classes NUnit runs, how to walk a type with the types nested in it or up to the type it is nested in, and how
    /// a call is matched to the method it runs.
    /// </summary>
    internal static class TestHostScanning
    {
        private static readonly HashSet<string> FactoryTypeNames = new(StringComparer.Ordinal)
        {
            typeof(WebApplicationFactory<>).FullName!,
            typeof(TestWebApplicationFactory<>).FullName!,
        };

        private static readonly string TestServerTypeName = typeof(TestServer).FullName!;

        // Combinatorial, Pairwise and Sequential make a method a test on their own, without [Test].
        public static readonly IReadOnlySet<string> TestAttributes = new HashSet<string>(StringComparer.Ordinal)
        {
            "NUnit.Framework.TestAttribute",
            "NUnit.Framework.TestCaseAttribute",
            "NUnit.Framework.TestCaseSourceAttribute",
            "NUnit.Framework.TheoryAttribute",
            "NUnit.Framework.CombinatorialAttribute",
            "NUnit.Framework.PairwiseAttribute",
            "NUnit.Framework.SequentialAttribute",
        };

        public static readonly IReadOnlySet<string> SetUpAndTearDownAttributes = new HashSet<string>(StringComparer.Ordinal)
        {
            "NUnit.Framework.OneTimeSetUpAttribute",
            "NUnit.Framework.OneTimeTearDownAttribute",
            "NUnit.Framework.SetUpAttribute",
            "NUnit.Framework.TearDownAttribute",
        };

        /// <summary>
        /// Whether NUnit runs the method when it runs a fixture: a constructor, a test, or a set-up or tear-down,
        /// one-time or per test. Anything such a method starts counts as the fixture's own.
        /// </summary>
        public static bool RunsWhenAFixtureRuns(MethodDefinition method)
            => method.IsConstructor || HasAnAttribute(method, TestAttributes) || HasAnAttribute(method, SetUpAndTearDownAttributes);

        /// <summary>Whether NUnit runs the type once around the tests of its namespace rather than as a fixture of its own.</summary>
        public static bool IsASetUpFixture(TypeDefinition type)
            => type.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == "NUnit.Framework.SetUpFixtureAttribute");

        public static bool HasAnAttribute(MethodDefinition method, IReadOnlySet<string> attributeNames)
            => method.CustomAttributes.Any(attribute => attributeNames.Contains(attribute.AttributeType.FullName));

        public static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];

        /// <summary>The outermost type the type is nested in, or the type itself when it is not nested.</summary>
        public static TypeDefinition TopLevelTypeOf(TypeDefinition type)
            => type.DeclaringType is null ? type : TopLevelTypeOf(type.DeclaringType);

        /// <summary>Whether NUnit could make an instance of the type: a class that is not abstract and that the compiler did not make.</summary>
        public static bool IsAConcreteClass(TypeDefinition type)
            => type is { IsClass: true, IsAbstract: false } && !type.Name.StartsWith('<');

        /// <summary>Whether the type is one of the two host factories, whatever its type argument.</summary>
        public static bool NamesAFactory(TypeReference type)
            => FactoryTypeNames.Contains(type.GetElementType().FullName);

        /// <summary>The full names of the types that are a factory or derive from one, however deep.</summary>
        public static HashSet<string> FactoryTypesIn(List<TypeDefinition> allTypes)
        {
            var factories = allTypes.Where(type => type.BaseType is not null && NamesAFactory(type.BaseType))
                .Select(type => type.FullName)
                .ToHashSet(StringComparer.Ordinal);

            bool grew;

            do
            {
                grew = false;

                foreach (var type in allTypes.Where(type => type.BaseType is not null && factories.Contains(type.BaseType.GetElementType().FullName)))
                {
                    grew |= factories.Add(type.FullName);
                }
            }
            while (grew);

            return factories;
        }

        /// <summary>Whether the instruction builds a host: a new factory, a host derived from one, or a new test server.</summary>
        public static bool IsAHostStart(Instruction instruction, HashSet<string> factoryTypeNames)
            => instruction.Operand is MethodReference called
                && (IsAFactoryType(called.DeclaringType, factoryTypeNames)
                        && (instruction.OpCode.Code == Code.Newobj
                            || instruction.OpCode.Code is Code.Call or Code.Callvirt && called.Name is "WithWebHostBuilder" or "WithTestAuthentication")
                    || instruction.OpCode.Code == Code.Newobj && called.DeclaringType.FullName == TestServerTypeName);

        private static bool IsAFactoryType(TypeReference type, HashSet<string> factoryTypeNames)
            => NamesAFactory(type) || factoryTypeNames.Contains(type.GetElementType().FullName);

        /// <summary>What a list of type names says against the names a scan found: the ones missing from the list, and the ones on it that were not found.</summary>
        public static ListComparison Compare(IEnumerable<string> listed, IEnumerable<string> found)
        {
            var listedNames = listed.ToHashSet(StringComparer.Ordinal);
            var foundNames = found.ToHashSet(StringComparer.Ordinal);

            return new ListComparison(
                [.. foundNames.Except(listedNames).Order(StringComparer.Ordinal)],
                [.. listedNames.Except(foundNames).Order(StringComparer.Ordinal)]);
        }

        public sealed record ListComparison(IReadOnlyList<string> Newcomers, IReadOnlyList<string> Stale);

        public static string KeyOf(MethodDefinition method)
            => method.DeclaringType.FullName + "::" + method.Name + "/" + method.Parameters.Count;

        public static string KeyOf(MethodReference method)
        {
            var declared = method is GenericInstanceMethod generic ? generic.ElementMethod : method;
            return declared.DeclaringType.GetElementType().FullName + "::" + declared.Name + "/" + declared.Parameters.Count;
        }

        /// <summary>
        /// Walks the call tree below every root method in an assembly and answers which of them can reach the
        /// start of an application host. Each guard says which methods are roots, because the guards ask
        /// different questions. Calls are followed into methods and constructors that are written in the same
        /// assembly, into the bodies the compiler makes for lambdas, local functions and async methods, into
        /// overrides of a virtual method that is called, and into the static constructor of a type written here
        /// whose static field is read or written or whose static method is called.
        /// </summary>
        public sealed class HostBuildingScan
        {
            private readonly Func<MethodDefinition, bool> isInTheRootSet;
            private readonly List<MethodDefinition> allMethods;
            private readonly Dictionary<string, List<MethodDefinition>> methodsByKey;
            private readonly Dictionary<string, TypeDefinition> typesByName;
            private readonly HashSet<string> factoryTypeNames;
            private readonly HashSet<MethodDefinition> knownToStartNoHost = [];

            public HostBuildingScan(ModuleDefinition module, Func<MethodDefinition, bool> isInTheRootSet)
            {
                this.isInTheRootSet = isInTheRootSet;

                var allTypes = module.Types.SelectMany(TypeAndItsNestedTypes).ToList();
                allMethods = [.. allTypes.SelectMany(type => type.Methods)];
                methodsByKey = allMethods.GroupBy(KeyOf).ToDictionary(group => group.Key, group => group.ToList());
                typesByName = allTypes.ToDictionary(type => type.FullName, type => type);
                factoryTypeNames = FactoryTypesIn(allTypes);
            }

            /// <summary>The root methods, among those the filter picks, from which a host start can be reached.</summary>
            public IReadOnlyList<MethodDefinition> OffendingRoots(Func<MethodDefinition, bool> pickRoots)
                => [.. allMethods.Where(IsARoot).Where(pickRoots).Where(ReachesAHostStart)];

            /// <summary>Whether a root on the type, or on a type it derives from, can reach a host start.</summary>
            public bool FixtureReachesAHost(TypeDefinition fixture)
                => TypeAndItsBases(fixture).SelectMany(type => type.Methods).Where(IsARoot).Any(ReachesAHostStart);

            /// <summary>Whether the type, or a type it derives from, has a test method.</summary>
            public bool HasTests(TypeDefinition type)
                => TypeAndItsBases(type).SelectMany(each => each.Methods).Any(method => HasAnAttribute(method, TestAttributes));

            /// <summary>The type and the types above it, as far as they are written in this assembly.</summary>
            public IEnumerable<TypeDefinition> TypeAndItsBases(TypeDefinition type)
            {
                for (var current = type; current is not null; current = BaseWrittenHere(current))
                {
                    yield return current;
                }
            }

            private TypeDefinition? BaseWrittenHere(TypeDefinition type)
                => type.BaseType is null ? null : typesByName.GetValueOrDefault(type.BaseType.GetElementType().FullName);

            private bool IsARoot(MethodDefinition method)
                => !method.DeclaringType.Name.StartsWith('<') && isInTheRootSet(method);

            private bool ReachesAHostStart(MethodDefinition root)
            {
                var visited = new HashSet<MethodDefinition>();
                var pending = new Stack<MethodDefinition>([root]);

                while (pending.Count > 0)
                {
                    var method = pending.Pop();

                    if (!visited.Add(method) || knownToStartNoHost.Contains(method))
                    {
                        continue;
                    }

                    if (method.HasBody && method.Body.Instructions.Any(instruction => IsAHostStart(instruction, factoryTypeNames)))
                    {
                        return true;
                    }

                    foreach (var next in MethodsRunBy(method))
                    {
                        pending.Push(next);
                    }
                }

                knownToStartNoHost.UnionWith(visited);
                return false;
            }

            private IEnumerable<MethodDefinition> MethodsRunBy(MethodDefinition method)
                => CompilerMadeBodiesOf(method).Concat(
                    method.HasBody
                        ? method.Body.Instructions.SelectMany(instruction => MethodsStartedBy(instruction).Concat(StaticConstructorsRunBy(instruction)))
                        : []);

            private IEnumerable<MethodDefinition> StaticConstructorsRunBy(Instruction instruction)
            {
                var touched = instruction.Operand switch
                {
                    FieldReference field when instruction.OpCode.Code is Code.Ldsfld or Code.Stsfld or Code.Ldsflda => field.DeclaringType,
                    MethodReference { HasThis: false } called when instruction.OpCode.Code == Code.Call => called.DeclaringType,
                    _ => null,
                };

                return touched is null || IsAFactoryType(touched, factoryTypeNames)
                    ? []
                    : typesByName.GetValueOrDefault(touched.GetElementType().FullName)?.Methods.Where(candidate => candidate is { IsConstructor: true, IsStatic: true }) ?? [];
            }

            // An async method's own body lives in the MoveNext of a type the compiler names "<Method>d__N".
            private static IEnumerable<MethodDefinition> CompilerMadeBodiesOf(MethodDefinition method)
                => method.DeclaringType.NestedTypes
                    .Where(nested => nested.Name.StartsWith("<" + method.Name + ">", StringComparison.Ordinal))
                    .SelectMany(nested => nested.Methods);

            private IEnumerable<MethodDefinition> MethodsStartedBy(Instruction instruction)
            {
                if (instruction.Operand is not MethodReference called
                    || instruction.OpCode.Code is not (Code.Call or Code.Callvirt or Code.Newobj or Code.Ldftn or Code.Ldvirtftn)
                    || NamesAFactory(called.DeclaringType)
                    || factoryTypeNames.Contains(called.DeclaringType.GetElementType().FullName))
                {
                    return [];
                }

                var declared = methodsByKey.GetValueOrDefault(KeyOf(called)) ?? [];

                var calledOnATypeWrittenHere = typesByName.ContainsKey(called.DeclaringType.GetElementType().FullName);

                return instruction.OpCode.Code is Code.Callvirt or Code.Ldvirtftn && calledOnATypeWrittenHere
                    ? declared.Concat(OverridesOf(called))
                    : declared;
            }

            private IEnumerable<MethodDefinition> OverridesOf(MethodReference called)
                => allMethods.Where(candidate => candidate.IsVirtual
                    && candidate.Name == called.Name
                    && candidate.Parameters.Count == called.Parameters.Count
                    && DerivesFrom(candidate.DeclaringType, called.DeclaringType.GetElementType().FullName));

            public bool DerivesFrom(TypeDefinition type, string baseTypeName)
            {
                var current = type.BaseType;

                while (current is not null)
                {
                    var name = current.GetElementType().FullName;

                    if (name == baseTypeName)
                    {
                        return true;
                    }

                    current = typesByName.GetValueOrDefault(name)?.BaseType;
                }

                return false;
            }
        }
    }
}
