using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static Lighthouse.Backend.Tests.Architecture.TestHostScanning;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A database context taken straight from a test host's root provider is one object shared by everything
    /// that runs on the host, and it is not safe to use from two threads at once. Tests take theirs from a
    /// scope they create and dispose. This guard reads the compiled test assembly and fails for every method
    /// that asks a host's root provider for a database context in one expression, such as
    /// factory.Services.GetRequiredService of a context type. It is an approximation: a root provider first stored in a
    /// variable or passed in as an argument is not seen.
    /// </summary>
    [TestFixture]
    public class DbContextComesFromAScopeGuardTest
    {
        private static readonly string PlantedPrefix = typeof(PlantedMethods).FullName!.Replace('+', '/');

        private static readonly string[] ProviderLookups = ["GetRequiredService", "GetService"];

        // Keys are "Type.Method". Each entry carries the reason the method may take a context from the root provider.
        private static readonly IReadOnlyDictionary<string, string> MethodsAllowedToTakeAContextFromTheRoot = new Dictionary<string, string>();

        private ModuleDefinition module = null!;
        private IReadOnlyList<string> methodsFoundTakingAContextFromTheRoot = [];

        [OneTimeSetUp]
        public void ReadTheTestAssemblyOnce()
        {
            module = IlDependencies.ReadModule(typeof(DbContextComesFromAScopeGuardTest).Assembly);
            methodsFoundTakingAContextFromTheRoot = MethodsTakingAContextFromTheRoot(module)
                .Where(name => !name.StartsWith(PlantedPrefix, StringComparison.Ordinal))
                .ToList();
        }

        [OneTimeTearDown]
        public void ReleaseTheTestAssembly() => module.Dispose();

        [Test]
        public void NoTestCodeTakesADatabaseContextFromAHostsRootProviderWithoutAScope()
        {
            var offenders = methodsFoundTakingAContextFromTheRoot.Except(MethodsAllowedToTakeAContextFromTheRoot.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

            Assert.That(
                offenders,
                Is.Empty,
                "These methods ask a host's root provider for a database context: " + string.Join(", ", offenders) +
                ". The root provider hands every caller the same context, which is not safe to share between threads. " +
                "Create a scope with host.Services.CreateScope() (dispose it when done) and take the context from scope.ServiceProvider.");
        }

        [Test]
        public void EveryMethodOnTheAllowListStillTakesAContextFromTheRoot()
        {
            var stale = MethodsAllowedToTakeAContextFromTheRoot.Keys.Except(methodsFoundTakingAContextFromTheRoot, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

            Assert.That(
                stale,
                Is.Empty,
                "These methods are on the allow-list but no longer take a context from the root provider: " + string.Join(", ", stale) +
                ". Remove them from the list so it cannot hide a new offender.");
        }

        [TestCase(nameof(PlantedMethods.TakesTheContextFromTheRootProvider), true)]
        [TestCase(nameof(PlantedMethods.TakesTheContextFromTheRootProviderWithGetService), true)]
        [TestCase(nameof(PlantedMethods.TakesTheContextFromTheRootProviderInALambda), true)]
        [TestCase(nameof(PlantedMethods.TakesTheContextFromTheRootProviderAfterAnAwait), true)]
        [TestCase(nameof(PlantedMethods.TakesTheContextFromAScope), false)]
        [TestCase(nameof(PlantedMethods.TakesTheContextFromAnAsyncScope), false)]
        [TestCase(nameof(PlantedMethods.TakesANonContextServiceFromTheRootProvider), false)]
        [TestCase(nameof(PlantedMethods.TakesTheContextFromAScopeWhileAskingTheRootForAnotherService), false)]
        public void TheScanSeesADatabaseContextTakenFromTheRootProviderHoweverItIsAsked(string plantedMethod, bool shouldBeFound)
        {
            var found = MethodsTakingAContextFromTheRoot(module)
                .Where(name => name.StartsWith(PlantedPrefix, StringComparison.Ordinal))
                .ToHashSet(StringComparer.Ordinal);

            Assert.That(found.Contains(PlantedPrefix + "." + plantedMethod), Is.EqualTo(shouldBeFound));
        }

        private static IEnumerable<string> MethodsTakingAContextFromTheRoot(ModuleDefinition module)
        {
            var allTypes = module.Types.SelectMany(TypeAndItsNestedTypes).ToList();
            var factoryTypeNames = FactoryTypesIn(allTypes);

            return allTypes
                .Where(type => !type.Name.StartsWith('<'))
                .SelectMany(type => type.Methods.Where(method => method.HasBody && !IsCompilerMade(method.DeclaringType))
                    .Select(method => (Name: type.FullName + "." + method.Name, Bodies: BodiesOf(method))))
                .Where(method => TakesAContextFromTheRoot(method.Bodies, factoryTypeNames))
                .Select(method => method.Name)
                .Distinct(StringComparer.Ordinal);
        }

        // The compiler moves lambdas and async code into nested types named with a leading '<'; those bodies
        // count as part of the method that wrote them.
        private static List<MethodDefinition> BodiesOf(MethodDefinition method)
            => [method, .. method.DeclaringType.NestedTypes
                .Where(nested => nested.Name.StartsWith('<'))
                .SelectMany(nested => nested.Methods)
                .Where(candidate => candidate.HasBody && MadeFor(candidate, method))];

        private static bool MadeFor(MethodDefinition compilerMade, MethodDefinition writtenMethod)
            => compilerMade.DeclaringType.Name.Contains("<" + writtenMethod.Name + ">", StringComparison.Ordinal)
                || compilerMade.Name.Contains("<" + writtenMethod.Name + ">", StringComparison.Ordinal);

        private static bool IsCompilerMade(TypeDefinition type) => type.Name.StartsWith('<');

        private static bool TakesAContextFromTheRoot(List<MethodDefinition> bodies, HashSet<string> factoryTypeNames)
            => bodies.Any(body => AsksTheRootProviderForAContext(body.Body.Instructions, factoryTypeNames));

        // The lookup takes the provider as its only argument, so the instruction right before the call is the one
        // that produced it.
        private static bool AsksTheRootProviderForAContext(IList<Instruction> instructions, HashSet<string> factoryTypeNames)
            => instructions.Skip(1)
                .Where(IsAProviderLookupOfADatabaseContext)
                .Any(lookup => IsAHostsRootProvider(lookup.Previous, factoryTypeNames));

        private static bool IsAHostsRootProvider(Instruction? instruction, HashSet<string> factoryTypeNames)
            => instruction?.Operand is MethodReference { Name: "get_Services" } called
                && (NamesAFactory(called.DeclaringType) || factoryTypeNames.Contains(called.DeclaringType.GetElementType().FullName));

        private static bool IsAProviderLookupOfADatabaseContext(Instruction instruction)
            => instruction.Operand is GenericInstanceMethod { ElementMethod.DeclaringType.Name: nameof(ServiceProviderServiceExtensions) } lookup
                && ProviderLookups.Contains(lookup.ElementMethod.Name)
                && lookup.GenericArguments.Any(IsADatabaseContext);

        private static bool IsADatabaseContext(TypeReference type)
        {
            for (var current = TryResolve(type); current is not null; current = TryResolve(current.BaseType))
            {
                if (current.FullName == "Microsoft.EntityFrameworkCore.DbContext")
                {
                    return true;
                }
            }

            return type.Name.EndsWith("Context", StringComparison.Ordinal) && type.Name.Contains("App", StringComparison.Ordinal);
        }

        private static TypeDefinition? TryResolve(TypeReference? type)
        {
            try
            {
                return type?.Resolve();
            }
            catch (AssemblyResolutionException)
            {
                return null;
            }
        }

        // Methods that exist only so the scan can be pointed at something. They are never called.
        internal static class PlantedMethods
        {
            internal static LighthouseAppContext TakesTheContextFromTheRootProvider(TestWebApplicationFactory<Program> factory)
                => factory.Services.GetRequiredService<LighthouseAppContext>();

            internal static LighthouseAppContext? TakesTheContextFromTheRootProviderWithGetService(TestWebApplicationFactory<Program> factory)
                => factory.Services.GetService<LighthouseAppContext>();

            internal static LighthouseAppContext TakesTheContextFromTheRootProviderInALambda(TestWebApplicationFactory<Program> factory)
            {
                Func<LighthouseAppContext> take = () => factory.Services.GetRequiredService<LighthouseAppContext>();
                return take();
            }

            internal static async Task<LighthouseAppContext> TakesTheContextFromTheRootProviderAfterAnAwait(TestWebApplicationFactory<Program> factory)
            {
                await Task.Yield();
                return factory.Services.GetRequiredService<LighthouseAppContext>();
            }

            internal static int TakesTheContextFromAScope(TestWebApplicationFactory<Program> factory)
            {
                using var scope = factory.Services.CreateScope();
                return scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().GetHashCode();
            }

            internal static async Task<int> TakesTheContextFromAnAsyncScope(TestWebApplicationFactory<Program> factory)
            {
                await using var scope = factory.Services.CreateAsyncScope();
                return scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().GetHashCode();
            }

            internal static int TakesTheContextFromAScopeWhileAskingTheRootForAnotherService(TestWebApplicationFactory<Program> factory, IServiceScope scope)
                => factory.Services.GetRequiredService<IServiceScopeFactory>().GetHashCode() + scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().GetHashCode();

            internal static IServiceScopeFactory TakesANonContextServiceFromTheRootProvider(TestWebApplicationFactory<Program> factory)
                => factory.Services.GetRequiredService<IServiceScopeFactory>();
        }
    }
}
