using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Only the access service reads who holds which role: the stored permissions of people and the rules of SSO groups.
    /// A second reader would decide a read or write from exact unit rows and miss a rule that reaches every unit, so a
    /// person reading through a rule would be refused there. Read from the compiled code of every production type,
    /// including any added later, lambdas and async methods counted against the type that wrote them.
    /// </summary>
    [TestFixture]
    public class AccessTablesSingleReaderArchUnitTest
    {
        private static readonly string[] AccessTables = ["RbacGroupMappings", "UserPermissions"];

        private static readonly string[] AccessTableRows = ["Lighthouse.Backend.Models.Authorization.RbacGroupMapping", "Lighthouse.Backend.Models.Authorization.UserPermission"];

        private static readonly string TheAccessService = typeof(RbacAdministrationService).FullName!;

        private static readonly string TheDatabaseContext = typeof(LighthouseAppContext).FullName!;

        private ModuleDefinition production = null!;

        [OneTimeSetUp]
        public void ReadTheProductionCode() => production = IlDependencies.ReadModule(typeof(RbacAdministrationService).Assembly);

        [OneTimeTearDown]
        public void DisposeModule() => production.Dispose();

        [Test]
        public void Only_the_access_service_reads_the_permission_and_group_rule_tables()
        {
            var readers = ReadersOfTheAccessTables();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readers, Does.Contain(TheAccessService), "WHAT: the scan found no reader at all, not even the access service. WHY: an empty answer must mean nobody else reads, not that the scan saw nothing. HOW: fix the scan before trusting it.");
                Assert.That(
                    readers.Where(reader => reader != TheAccessService && reader != TheDatabaseContext),
                    Is.Empty,
                    "WHAT: these production types read the permission or group rule tables themselves. WHY: they would decide access from exact unit rows and refuse a person who reaches the unit through a rule. HOW: ask the access service instead.");
            }
        }

        private List<string> ReadersOfTheAccessTables()
            => production.Types
                .SelectMany(TypeAndNested)
                .Where(type => type.Methods.Any(ReadsAnAccessTable))
                .Select(type => IlDependencies.DeclaringTypeOf(type.FullName.Replace('/', '+')))
                .Distinct(StringComparer.Ordinal)
                .ToList();

        private static IEnumerable<TypeDefinition> TypeAndNested(TypeDefinition type) => [type, .. type.NestedTypes.SelectMany(TypeAndNested)];

        private static bool ReadsAnAccessTable(MethodDefinition method)
            => method.HasBody && method.Body.Instructions.Any(instruction => instruction.Operand is MethodReference called && IsAnAccessTableRead(called));

        private static bool IsAnAccessTableRead(MethodReference called)
            => (called.DeclaringType.FullName == TheDatabaseContext && AccessTables.Any(table => called.Name == $"get_{table}"))
               || (called is GenericInstanceMethod { Name: "Set" } set && set.GenericArguments.Any(argument => AccessTableRows.Contains(argument.FullName)));
    }
}
