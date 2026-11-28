using System.Reflection;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The guards, markers, filters and authentication handlers of the Access context live in the module's Http adapters,
    /// and none is left under the Auth or Authorization folders of Services. Every type is named as a string and read from the
    /// compiled assembly, so a type that is missing, renamed or left behind is a failure that names it. The move changes
    /// names and nothing else, so four things are pinned as they were: what the RBAC guard offers its callers, how each of
    /// the four attributes may be applied, how many controller actions carry a guard or a marker, and the development log
    /// level that follows the disabled-authentication handler by its full type name.
    /// </summary>
    [TestFixture]
    public class AccessHttpAdaptersLayoutArchUnitTest
    {
        private const string HttpAdaptersNamespace = "Lighthouse.Backend.Modules.Access.Adapters.Http";

        private const string WhereTheyLivedBefore = @"^Lighthouse\.Backend\.Services\.Implementation\.(Auth|Authorization)(\.|$)";

        private const string TheWholeAssembly = @"^Lighthouse\.Backend\.";

        private const string FixtureFormerHome = @"^Lighthouse\.Backend\.Modules\.AccessHttpFixtures\.Services(\.|$)";

        private const string FixtureHttpNamespace = "Lighthouse.Backend.Modules.AccessHttpFixtures.Http";

        private const int TheMeasuredNumberOfGuardedActions = 256;

        private static readonly string[] TheFourAttributeNames = ["RbacGuardAttribute", "AccessMarkerAttribute", "SignedInOnlyAttribute", "CheckedInHandlerAttribute"];

        private static readonly string[] TheAccessAttributeNames = ["RbacGuardAttribute", "SignedInOnlyAttribute", "CheckedInHandlerAttribute"];

        private static readonly IReadOnlyList<string> TheGuardSurface =
        [
            "Boolean Check { get; set; }",
            "Boolean RequiresScope()",
            "RbacGuardRequirement Requirement { get; set; }",
            "String ScopeIdRouteKey { get; set; }",
            "Task OnAuthorizationAsync(AuthorizationFilterContext)",
            "new()",
            "new(RbacGuardRequirement)",
        ];

        private static readonly IReadOnlyList<string> TheAttributeUsages =
        [
            "RbacGuardAttribute: Class, Method, AllowMultiple=False, Inherited=True",
            "AccessMarkerAttribute: All, AllowMultiple=False, Inherited=True",
            "SignedInOnlyAttribute: Method, AllowMultiple=False, Inherited=True",
            "CheckedInHandlerAttribute: Method, AllowMultiple=False, Inherited=True",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(AccessHttpAdaptersLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheGuardsMarkersFiltersAndHandlers_LiveInTheAccessHttpAdapters_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, HttpAdaptersNamespace, AccessAndLicensingMembership.AccessAdaptersHttp, WhereTheyLivedBefore).Check();
        }

        [Test]
        public void ANameThatNoTypeMatches_FailsTheLayoutRuleAsMissing_InsteadOfPassingUnseen()
        {
            const string anInventedName = "RbacGuardAttributeThatDoesNotExist";

            var verdict = ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, HttpAdaptersNamespace, [anInventedName], WhereTheyLivedBefore);

            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                "A list name that no type in the assembly matches passed the layout rule. Why it matters: a list that has drifted from the code " +
                "stays green forever and guards nothing. How to fix: the rule must fail for a name whose type is not in the layer.");

            Assert.That(failure!.Message, Does.Contain($"{HttpAdaptersNamespace}.{anInventedName}"), "The failure must name the missing type, so the reader knows which list entry to correct.");
        }

        [Test]
        public void ATypeLeftUnderItsFormerHome_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twinPlaced = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, FixtureHttpNamespace, ["AWellPlacedGuard"], FixtureFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, FixtureHttpNamespace, ["ALeftBehindGuard"], FixtureFormerHome);

            Assert.DoesNotThrow(twinPlaced.Check, "A type that lives in the Http adapters and has no copy in its former home was reported. Why it matters: a rule that flags a correct move hides the real offenders. How to fix: report only a type missing from the layer or still present in the former home.");

            var failure = Assert.Throws<AssertionException>(leftBehind.Check, "A type left in its former home passed the layout rule.");
            Assert.That(failure!.Message, Does.Contain("ALeftBehindGuard"), "The failure must name the type that was left behind.");
        }

        [Test]
        public void EveryNameInTheList_ResolvesToExactlyOneTypeInTheAssembly()
        {
            var inTheAssembly = IlDependencies.TypesIn(ProductionModule.Value, TheWholeAssembly)
                .Select(fullName => fullName.Split('+')[0][(fullName.Split('+')[0].LastIndexOf('.') + 1)..])
                .ToList();

            var notOne = AccessAndLicensingMembership.AccessAdaptersHttp
                .Where(name => inTheAssembly.Count(found => found == name) != 1)
                .ToList();

            Assert.That(
                notOne,
                Is.Empty,
                "These listed names match no type, or more than one, in the compiled assembly. Why it matters: a name that matches nothing makes its layout check look at nothing, " +
                "and a name that matches two makes the pins below read the wrong type. How to fix: correct the name in the list, or restore the type it names.");
        }

        [Test]
        public void TheRbacGuard_OffersItsCallersTheSameConstructorsAndPublicMembers()
        {
            var surface = PublicSurfaceOf(TheAdapter("RbacGuardAttribute"));

            Assert.That(
                surface,
                Is.EqualTo(TheGuardSurface),
                "The RBAC guard's constructors and public members differ from what the controllers were written against. Why it matters: every controller action " +
                "declares its access through this attribute, so a changed constructor or member changes what they can say. How to fix: the move changes the namespace " +
                $"line and nothing else; restore the members. Found: [{string.Join("; ", surface)}].");
        }

        [Test]
        public void TheFourAttributes_AreAppliedWhereTheyWereBeforeTheMove()
        {
            var usages = TheFourAttributeNames
                .Select(name => UsageOf(TheAdapter(name)))
                .ToList();

            Assert.That(
                usages,
                Is.EqualTo(TheAttributeUsages),
                "The way an attribute may be applied changed. Why it matters: a marker that may suddenly sit on a class, or a guard that may be stacked, " +
                "changes what the access rule over every controller can see. How to fix: restore the AttributeUsage the type had before the move. " +
                $"Found: [{string.Join("; ", usages)}].");
        }

        [Test]
        public void TheNumberOfControllerActionsThatCarryAGuardOrAMarker_IsTheNumberMeasuredBeforeTheMove()
        {
            var accessAttributes = TheAccessAttributeNames.Select(TheAdapter).ToList();

            var controllers = typeof(Feature).Assembly.GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
                .ToList();
            var guarded = controllers
                .SelectMany(controller => ActionsOf(controller).Where(action => DeclaresAccessWith(controller, action, accessAttributes)))
                .Count();

            Assert.That(
                guarded,
                Is.EqualTo(TheMeasuredNumberOfGuardedActions),
                $"{guarded} controller actions carry a guard or a marker, not {TheMeasuredNumberOfGuardedActions}. Why it matters: an action that lost its guard in the move is open to callers it was closed to, and " +
                "a count that matches only because a guard is now looked up by a stale name hides it. How to fix: find the action that lost or gained its attribute and restore it.");
        }

        [Test]
        public void TheDevelopmentLogLevel_NamesTheDisabledAuthenticationHandlerByItsFullTypeName()
        {
            var handler = TheAdapter("DisabledAuthenticationHandler");
            var overrides = DevelopmentLogLevelOverrides();

            Assert.That(
                overrides.GetValueOrDefault(handler.FullName!),
                Is.EqualTo("Warning"),
                $"The development log levels hold no Warning entry named {handler.FullName}. Why it matters: Serilog matches a level to a logger by its full type name, so an entry that still names the old " +
                "namespace silences nothing and the handler warning appears on every request in development. How to fix: rename the key in appsettings.Development.json to the handler full name and keep its value Warning. " +
                $"Found keys: [{string.Join(", ", overrides.Keys)}].");
        }

        private static Type TheAdapter(string name)
            => typeof(Feature).Assembly.GetTypes().Single(type => type.Name == name && !type.IsNested);

        private static string UsageOf(Type attribute)
        {
            var usage = attribute.GetCustomAttribute<AttributeUsageAttribute>(inherit: true)
                ?? throw new InvalidOperationException($"{attribute.Name} carries no AttributeUsage.");
            return $"{attribute.Name}: {usage.ValidOn}, AllowMultiple={usage.AllowMultiple}, Inherited={usage.Inherited}";
        }

        private static List<string> PublicSurfaceOf(Type type)
        {
            const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            return type.GetMembers(Declared)
                .Select(member => member switch
                {
                    ConstructorInfo ctor => $"new({string.Join(", ", ctor.GetParameters().Select(p => p.ParameterType.Name))})",
                    MethodInfo { IsSpecialName: true } => string.Empty,
                    MethodInfo method => $"{method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})",
                    PropertyInfo property => $"{property.PropertyType.Name} {property.Name} {{ {(property.GetMethod?.IsPublic == true ? "get; " : string.Empty)}{(property.SetMethod?.IsPublic == true ? "set; " : string.Empty)}}}",
                    _ => $"{member.MemberType} {member.Name}",
                })
                .Where(line => line.Length > 0)
                .Order(StringComparer.Ordinal)
                .ToList();
        }

        private static IEnumerable<MethodInfo> ActionsOf(Type controller)
            => controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => !method.IsDefined(typeof(NonActionAttribute), inherit: true))
                .Where(method => !method.IsSpecialName)
                .Where(method => method.GetBaseDefinition().DeclaringType is not { } owner || (owner != typeof(object) && owner != typeof(ControllerBase) && owner != typeof(Controller)));

        private static bool DeclaresAccessWith(Type controller, MethodInfo action, IReadOnlyCollection<Type> accessAttributes)
            => controller.GetCustomAttributes(inherit: true).Concat(action.GetCustomAttributes(inherit: true))
                .Any(attribute => accessAttributes.Any(access => access.IsInstanceOfType(attribute)));

        private static Dictionary<string, string> DevelopmentLogLevelOverrides()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the development settings. How to fix: run the tests from inside the repository checkout.");

            var path = Path.Combine(directory!.FullName, "Lighthouse.Backend", "appsettings.Development.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.GetProperty("Serilog").GetProperty("MinimumLevel").GetProperty("Override")
                .EnumerateObject()
                .ToDictionary(entry => entry.Name, entry => entry.Value.GetString() ?? string.Empty, StringComparer.Ordinal);
        }
    }
}

namespace Lighthouse.Backend.Modules.AccessHttpFixtures.Http
{
    internal sealed class AWellPlacedGuard
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.AccessHttpFixtures.Services
{
    internal sealed class ALeftBehindGuard
    {
        public int Answer => 42;
    }
}
