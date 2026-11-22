using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;
using AccessInTheModule = Lighthouse.Backend.Modules.Access.Application.AccessAndLicensingFixtures;
using AccessNamesake = Lighthouse.Backend.Modules.AccessFixtures.RulesSeeTheModules;
using ForecastingCallers = Lighthouse.Backend.Modules.Forecasting.Application.AccessAndLicensingFixtures;
using LicensingInTheModule = Lighthouse.Backend.Modules.Licensing.Application.AccessAndLicensingFixtures;
using LicensingNamesake = Lighthouse.Backend.Modules.LicensingFixtures.RulesSeeTheModules;
using PromisesCallers = Lighthouse.Backend.Modules.Promises.Application.AccessAndLicensingFixtures;
using TrackerConnectors = Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.AccessAndLicensingFixtures;
using WorkItemsCallers = Lighthouse.Backend.Modules.WorkItems.Application.AccessAndLicensingFixtures;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Access (who may see and do what) and Licensing (what the licence allows) are about to move from Services into
    /// Modules/Access and Modules/Licensing. Every architecture rule that finds them by namespace must already see the new
    /// places, or the move quietly switches the rule off: a rule that looks only at the old place passes because it no
    /// longer finds anything there. The real code holds no type in the new places yet, so each rule's own key is read from
    /// the rule and run over made-up types: one under the module, its twin in the old place, and a namesake just outside
    /// the module (Modules.AccessFixtures, Modules.LicensingFixtures). The made-up type under the module must be named
    /// exactly as its twin is; the namesake never.
    /// </summary>
    [TestFixture]
    public class AccessAndLicensingRulesSeeTheModulesArchUnitTest
    {
        public enum Side
        {
            Access,
            Licensing,
        }

        public enum Context
        {
            WorkItems,
            TrackerIntegration,
            Promises,
        }

        private enum Place
        {
            TheModule,
            TheOldPlace,
            JustOutsideTheModule,
        }

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(AccessAndLicensingRulesSeeTheModulesArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (TestModule.IsValueCreated)
            {
                TestModule.Value.Dispose();
            }
        }

        [TestCase(Side.Access)]
        [TestCase(Side.Licensing)]
        public void AServiceUnderTheModuleReachingARepository_IsNamedByTheSharedModuleRulesNamespaceOfItsModule_WhileANamesakeAndTheOtherModuleAreNot(Side side)
        {
            var module = GivenTheKeyOf(typeof(ModuleRules), ModuleRulesConstantOf(side));

            var named = WhenTheRuleRunsOver(
                [ReachingARepository(side, Place.TheModule), ReachingARepository(side, Place.JustOutsideTheModule), ReachingARepository(TheOther(side), Place.TheModule), ReadingValuesOnlyInTheModule(side)],
                inSubject: typeNamespace => Matches(typeNamespace, module),
                forbidden: RepositoryRecognition.IsRepository);

            ThenTheRuleNamesTheOffendersAndNotTheOthers(
                named,
                [ReachingARepository(side, Place.TheModule)],
                [ReachingARepository(side, Place.JustOutsideTheModule), ReachingARepository(TheOther(side), Place.TheModule), ReadingValuesOnlyInTheModule(side)],
                $"The shared module rules' {side} namespace",
                $"the module's own rules are written with the shared module rules and their namespace constants, as Flow metrics, Forecasting and Promises are; without a {side} constant that sees Modules/{side} alone, those rules would have nothing to point at, one without its end anchor would take in Modules.{side}Fixtures, and one that also took in the other module would merge two contexts.",
                @"add AccessModule = ^Lighthouse\.Backend\.Modules\.Access(\.|$) and LicensingModule = ^Lighthouse\.Backend\.Modules\.Licensing(\.|$) to ModuleRules, beside PromisesModule.");
        }

        [Test]
        public void AServiceOfAnotherContextReachingAnAccessOrLicensingServiceUnderTheModule_IsNamedByThatContextsRule_AsOneReachingTheOldPlaceIs(
            [Values] Context context,
            [Values] Side side)
        {
            var subject = SubjectOf(context);
            var accessAndLicensing = GivenTheKeyOf(RuleOf(context), AccessAndLicensingKeyOf(context));

            var named = WhenTheRuleRunsOver(
                [Caller(context, side, Place.TheModule), Caller(context, side, Place.TheOldPlace), Caller(context, side, Place.JustOutsideTheModule)],
                inSubject: typeNamespace => Matches(typeNamespace, subject),
                forbidden: ResidesIn(accessAndLicensing));

            ThenTheRuleNamesTheOffendersAndNotTheOthers(
                named,
                [Caller(context, side, Place.TheModule), Caller(context, side, Place.TheOldPlace)],
                [Caller(context, side, Place.JustOutsideTheModule)],
                $"The {context} rule's key for Access and Licensing, {AccessAndLicensingKeyOf(context)}",
                $"{context} may reach Access and Licensing only through its listed edges; once {side} lives in Modules/{side}, a {context} type that reached it would pass the rule unseen. Every edge the rule lists today stays listed.",
                $@"add |Modules\.(Access|Licensing) to {AccessAndLicensingKeyOf(context)}, for example ^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.(Auth|Authorization|Licensing)|Modules\.(Access|Licensing))(\.|$), keeping the old alternatives and every other name the key holds.");
        }

        [Test]
        public void AForecastingApplicationTypeReachingALicensingServiceUnderTheModule_IsNamedByForecastingsLicenceSelector_AsOneReachingTheOldPlaceIs()
        {
            var forecastingApplication = GivenTheKeyOf(typeof(ForecastingModuleBoundariesArchUnitTest), "ForecastingApplication");
            var theLicence = GivenTheKeyOf(typeof(ForecastingModuleBoundariesArchUnitTest), "TheLicence");

            var named = WhenTheRuleRunsOver(
                [
                    typeof(ForecastingCallers.AForecastServiceReachingTheLicensingModule),
                    typeof(ForecastingCallers.AForecastServiceReachingTheOldLicensingPlace),
                    typeof(ForecastingCallers.AForecastServiceReachingALicensingNamesake),
                    typeof(ForecastingCallers.AForecastServiceReachingTheAccessModule),
                    typeof(ForecastingCallers.AForecastServiceReachingTheOldAccessPlace),
                ],
                inSubject: typeNamespace => Matches(typeNamespace, forecastingApplication),
                forbidden: ResidesIn(theLicence));

            ThenTheRuleNamesTheOffendersAndNotTheOthers(
                named,
                [typeof(ForecastingCallers.AForecastServiceReachingTheLicensingModule), typeof(ForecastingCallers.AForecastServiceReachingTheOldLicensingPlace)],
                [
                    typeof(ForecastingCallers.AForecastServiceReachingALicensingNamesake),
                    typeof(ForecastingCallers.AForecastServiceReachingTheAccessModule),
                    typeof(ForecastingCallers.AForecastServiceReachingTheOldAccessPlace),
                ],
                "Forecasting's licence selector, TheLicence",
                "Forecasting's Application may ask the licence only through its listed edge (the forecast filter rule service); once Licensing lives in Modules/Licensing, a Forecasting type that asked it would pass unseen. The selector is about the licence alone: Access, old or new, is not the licence.",
                @"add |Modules\.Licensing to TheLicence, for example ^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.Licensing|Modules\.Licensing)(\.|$), add no Access alternative, and keep the listed ForecastFilterRuleService edge.");
        }

        private static string GivenTheKeyOf(Type rule, string member)
        {
            var field = rule.GetField(member, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var key = (field is { IsLiteral: true } ? field.GetRawConstantValue() : field?.GetValue(null)) as string;

            Assert.That(
                key,
                Is.Not.Null.And.Not.Empty,
                $"{rule.Name} has no {member} that says where its namespaces are. Why it matters: this check reads the rule's own key and " +
                "runs it over made-up types in Modules/Access and Modules/Licensing; without the key there is nothing to show the rule sees the " +
                $"new places, and the move would switch the rule off unseen. How to fix: keep the namespace in a constant or static field named " +
                $"{member} and let the rule use it.");

            return key!;
        }

        private static List<string> WhenTheRuleRunsOver(IReadOnlyCollection<Type> madeUpTypes, Func<string, bool> inSubject, Func<TypeReference, bool> forbidden)
            => [.. madeUpTypes
                .Where(madeUpType => inSubject(madeUpType.Namespace!))
                .Where(madeUpType => IlDependencies.DependentsIn(TestModule.Value, $"^{Regex.Escape(madeUpType.Namespace!)}$", forbidden)
                    .Any(dependent => dependent.TypeName == madeUpType.FullName))
                .Select(madeUpType => madeUpType.FullName!)];

        private static void ThenTheRuleNamesTheOffendersAndNotTheOthers(List<string> named, Type[] offenders, Type[] others, string key, string why, string how)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var offender in offenders)
                {
                    Assert.That(
                        named,
                        Does.Contain(offender.FullName),
                        $"{key} did not catch {offender.FullName}. Named: [{string.Join(", ", named)}]. Why it matters: {why} How to fix: {how}");
                }

                foreach (var other in others)
                {
                    Assert.That(
                        named,
                        Does.Not.Contain(other.FullName),
                        $"{key} caught {other.FullName}, which reads values only, sits outside the module or is not what the key names. Why it " +
                        $"matters: a key that catches everything proves nothing, and a rule that fails on compliant code gets switched off. How to fix: {how}");
                }
            }
        }

        private static Side TheOther(Side side) => side == Side.Access ? Side.Licensing : Side.Access;

        private static string ModuleRulesConstantOf(Side side)
            => side switch
            {
                Side.Access => "AccessModule",
                Side.Licensing => "LicensingModule",
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
            };

        private static Type ReachingARepository(Side side, Place place)
            => (side, place) switch
            {
                (Side.Access, Place.TheModule) => typeof(AccessInTheModule.AnAccessServiceReachingARepository),
                (Side.Access, Place.JustOutsideTheModule) => typeof(AccessNamesake.AnAccessServiceReachingARepository),
                (Side.Licensing, Place.TheModule) => typeof(LicensingInTheModule.ALicensingServiceReachingARepository),
                (Side.Licensing, Place.JustOutsideTheModule) => typeof(LicensingNamesake.ALicensingServiceReachingARepository),
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
            };

        private static Type ReadingValuesOnlyInTheModule(Side side)
            => side switch
            {
                Side.Access => typeof(AccessInTheModule.AnAccessService),
                Side.Licensing => typeof(LicensingInTheModule.ALicensingService),
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
            };

        private static Type RuleOf(Context context)
            => context switch
            {
                Context.WorkItems => typeof(WorkItemsModuleBoundariesArchUnitTest),
                Context.TrackerIntegration => typeof(TrackerIntegrationModuleBoundariesArchUnitTest),
                Context.Promises => typeof(PromisesModuleBoundariesArchUnitTest),
                _ => throw new ArgumentOutOfRangeException(nameof(context), context, null),
            };

        private static string AccessAndLicensingKeyOf(Context context)
            => context switch
            {
                Context.TrackerIntegration => "RefinementReportsAccessAndLicensing",
                _ => "AccessAndLicensing",
            };

        private static string SubjectOf(Context context)
            => context switch
            {
                Context.WorkItems => GivenTheKeyOf(typeof(WorkItemsModuleBoundariesArchUnitTest), "WorkItemsApplication"),
                Context.TrackerIntegration => GivenTheKeyOf(typeof(ModuleBoundariesArchUnitTest), "TrackerIntegrationPattern"),
                Context.Promises => GivenTheKeyOf(typeof(ModuleRules), "PromisesModule"),
                _ => throw new ArgumentOutOfRangeException(nameof(context), context, null),
            };

        private static Type Caller(Context context, Side side, Place place)
            => (context, side, place) switch
            {
                (Context.WorkItems, Side.Access, Place.TheModule) => typeof(WorkItemsCallers.AWorkItemsServiceReachingTheAccessModule),
                (Context.WorkItems, Side.Access, Place.TheOldPlace) => typeof(WorkItemsCallers.AWorkItemsServiceReachingTheOldAccessPlace),
                (Context.WorkItems, Side.Access, Place.JustOutsideTheModule) => typeof(WorkItemsCallers.AWorkItemsServiceReachingAnAccessNamesake),
                (Context.WorkItems, Side.Licensing, Place.TheModule) => typeof(WorkItemsCallers.AWorkItemsServiceReachingTheLicensingModule),
                (Context.WorkItems, Side.Licensing, Place.TheOldPlace) => typeof(WorkItemsCallers.AWorkItemsServiceReachingTheOldLicensingPlace),
                (Context.WorkItems, Side.Licensing, Place.JustOutsideTheModule) => typeof(WorkItemsCallers.AWorkItemsServiceReachingALicensingNamesake),
                (Context.TrackerIntegration, Side.Access, Place.TheModule) => typeof(TrackerConnectors.AConnectorReachingTheAccessModule),
                (Context.TrackerIntegration, Side.Access, Place.TheOldPlace) => typeof(TrackerConnectors.AConnectorReachingTheOldAccessPlace),
                (Context.TrackerIntegration, Side.Access, Place.JustOutsideTheModule) => typeof(TrackerConnectors.AConnectorReachingAnAccessNamesake),
                (Context.TrackerIntegration, Side.Licensing, Place.TheModule) => typeof(TrackerConnectors.AConnectorReachingTheLicensingModule),
                (Context.TrackerIntegration, Side.Licensing, Place.TheOldPlace) => typeof(TrackerConnectors.AConnectorReachingTheOldLicensingPlace),
                (Context.TrackerIntegration, Side.Licensing, Place.JustOutsideTheModule) => typeof(TrackerConnectors.AConnectorReachingALicensingNamesake),
                (Context.Promises, Side.Access, Place.TheModule) => typeof(PromisesCallers.ADeliveryServiceReachingTheAccessModule),
                (Context.Promises, Side.Access, Place.TheOldPlace) => typeof(PromisesCallers.ADeliveryServiceReachingTheOldAccessPlace),
                (Context.Promises, Side.Access, Place.JustOutsideTheModule) => typeof(PromisesCallers.ADeliveryServiceReachingAnAccessNamesake),
                (Context.Promises, Side.Licensing, Place.TheModule) => typeof(PromisesCallers.ADeliveryServiceReachingTheLicensingModule),
                (Context.Promises, Side.Licensing, Place.TheOldPlace) => typeof(PromisesCallers.ADeliveryServiceReachingTheOldLicensingPlace),
                (Context.Promises, Side.Licensing, Place.JustOutsideTheModule) => typeof(PromisesCallers.ADeliveryServiceReachingALicensingNamesake),
                _ => throw new ArgumentOutOfRangeException(nameof(context), context, null),
            };

        private static Func<TypeReference, bool> ResidesIn(string namespacePattern)
            => type => Matches(IlDependencies.NamespaceOf(type), namespacePattern);

        private static bool Matches(string typeNamespace, string pattern)
            => Regex.IsMatch(typeNamespace, pattern, RegexOptions.None, PatternTimeout);
    }
}

namespace Lighthouse.Backend.Modules.Access.Application.AccessAndLicensingFixtures
{
    internal static class AnAccessServiceReachingARepository
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<global::Lighthouse.Backend.Models.Delivery> deliveries) => deliveries is not null;
    }

    internal static class AnAccessService
    {
        public static bool Allows(int level) => level > 0;
    }
}

namespace Lighthouse.Backend.Modules.Licensing.Application.AccessAndLicensingFixtures
{
    internal static class ALicensingServiceReachingARepository
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<global::Lighthouse.Backend.Models.Delivery> deliveries) => deliveries is not null;
    }

    internal static class ALicensingService
    {
        public static bool Allows(int level) => level > 0;
    }
}

namespace Lighthouse.Backend.Services.Implementation.Authorization.AccessAndLicensingFixtures
{
    internal static class AnAccessService
    {
        public static bool Allows(int level) => level > 0;
    }
}

namespace Lighthouse.Backend.Services.Implementation.Licensing.AccessAndLicensingFixtures
{
    internal static class ALicensingService
    {
        public static bool Allows(int level) => level > 0;
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.RulesSeeTheModules
{
    internal static class AnAccessServiceReachingARepository
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<global::Lighthouse.Backend.Models.Delivery> deliveries) => deliveries is not null;
    }

    internal static class AnAccessService
    {
        public static bool Allows(int level) => level > 0;
    }
}

namespace Lighthouse.Backend.Modules.LicensingFixtures.RulesSeeTheModules
{
    internal static class ALicensingServiceReachingARepository
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<global::Lighthouse.Backend.Models.Delivery> deliveries) => deliveries is not null;
    }

    internal static class ALicensingService
    {
        public static bool Allows(int level) => level > 0;
    }
}

namespace Lighthouse.Backend.Modules.WorkItems.Application.AccessAndLicensingFixtures
{
    internal static class AWorkItemsServiceReachingTheAccessModule
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.Access.Application.AccessAndLicensingFixtures.AnAccessService.Allows(2);
    }

    internal static class AWorkItemsServiceReachingTheOldAccessPlace
    {
        public static bool Reaches() => global::Lighthouse.Backend.Services.Implementation.Authorization.AccessAndLicensingFixtures.AnAccessService.Allows(2);
    }

    internal static class AWorkItemsServiceReachingAnAccessNamesake
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.AccessFixtures.RulesSeeTheModules.AnAccessService.Allows(2);
    }

    internal static class AWorkItemsServiceReachingTheLicensingModule
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.Licensing.Application.AccessAndLicensingFixtures.ALicensingService.Allows(2);
    }

    internal static class AWorkItemsServiceReachingTheOldLicensingPlace
    {
        public static bool Reaches() => global::Lighthouse.Backend.Services.Implementation.Licensing.AccessAndLicensingFixtures.ALicensingService.Allows(2);
    }

    internal static class AWorkItemsServiceReachingALicensingNamesake
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.LicensingFixtures.RulesSeeTheModules.ALicensingService.Allows(2);
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.AccessAndLicensingFixtures
{
    internal static class AConnectorReachingTheAccessModule
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.Access.Application.AccessAndLicensingFixtures.AnAccessService.Allows(2);
    }

    internal static class AConnectorReachingTheOldAccessPlace
    {
        public static bool Reaches() => global::Lighthouse.Backend.Services.Implementation.Authorization.AccessAndLicensingFixtures.AnAccessService.Allows(2);
    }

    internal static class AConnectorReachingAnAccessNamesake
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.AccessFixtures.RulesSeeTheModules.AnAccessService.Allows(2);
    }

    internal static class AConnectorReachingTheLicensingModule
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.Licensing.Application.AccessAndLicensingFixtures.ALicensingService.Allows(2);
    }

    internal static class AConnectorReachingTheOldLicensingPlace
    {
        public static bool Reaches() => global::Lighthouse.Backend.Services.Implementation.Licensing.AccessAndLicensingFixtures.ALicensingService.Allows(2);
    }

    internal static class AConnectorReachingALicensingNamesake
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.LicensingFixtures.RulesSeeTheModules.ALicensingService.Allows(2);
    }
}

namespace Lighthouse.Backend.Modules.Promises.Application.AccessAndLicensingFixtures
{
    internal static class ADeliveryServiceReachingTheAccessModule
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.Access.Application.AccessAndLicensingFixtures.AnAccessService.Allows(2);
    }

    internal static class ADeliveryServiceReachingTheOldAccessPlace
    {
        public static bool Reaches() => global::Lighthouse.Backend.Services.Implementation.Authorization.AccessAndLicensingFixtures.AnAccessService.Allows(2);
    }

    internal static class ADeliveryServiceReachingAnAccessNamesake
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.AccessFixtures.RulesSeeTheModules.AnAccessService.Allows(2);
    }

    internal static class ADeliveryServiceReachingTheLicensingModule
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.Licensing.Application.AccessAndLicensingFixtures.ALicensingService.Allows(2);
    }

    internal static class ADeliveryServiceReachingTheOldLicensingPlace
    {
        public static bool Reaches() => global::Lighthouse.Backend.Services.Implementation.Licensing.AccessAndLicensingFixtures.ALicensingService.Allows(2);
    }

    internal static class ADeliveryServiceReachingALicensingNamesake
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.LicensingFixtures.RulesSeeTheModules.ALicensingService.Allows(2);
    }
}

namespace Lighthouse.Backend.Modules.Forecasting.Application.AccessAndLicensingFixtures
{
    internal static class AForecastServiceReachingTheLicensingModule
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.Licensing.Application.AccessAndLicensingFixtures.ALicensingService.Allows(2);
    }

    internal static class AForecastServiceReachingTheOldLicensingPlace
    {
        public static bool Reaches() => global::Lighthouse.Backend.Services.Implementation.Licensing.AccessAndLicensingFixtures.ALicensingService.Allows(2);
    }

    internal static class AForecastServiceReachingALicensingNamesake
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.LicensingFixtures.RulesSeeTheModules.ALicensingService.Allows(2);
    }

    internal static class AForecastServiceReachingTheAccessModule
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.Access.Application.AccessAndLicensingFixtures.AnAccessService.Allows(2);
    }

    internal static class AForecastServiceReachingTheOldAccessPlace
    {
        public static bool Reaches() => global::Lighthouse.Backend.Services.Implementation.Authorization.AccessAndLicensingFixtures.AnAccessService.Allows(2);
    }
}
