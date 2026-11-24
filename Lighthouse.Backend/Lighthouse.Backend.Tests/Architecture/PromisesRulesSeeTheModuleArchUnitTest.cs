using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;
using InTheModule = Lighthouse.Backend.Modules.Promises.Application.PromisesFixtures;
using InTheOldPlace = Lighthouse.Backend.Services.Implementation.DeliverySources.PromisesFixtures;
using JustOutsideTheModule = Lighthouse.Backend.Modules.PromisesFixtures.RulesSeeTheModule;
using TrackerConnectors = Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.PromisesFixtures;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Promises (the Delivery services) is about to move from Services into Modules/Promises. Every architecture rule that
    /// finds Promises by its namespace must already see the new place, or the move quietly switches it off: a rule that
    /// looks only at the old place passes because it no longer finds anything there. The real code holds no type in the new
    /// place yet, so each rule's own Promises key is read from the rule and run over made-up Delivery services placed below
    /// Lighthouse.Backend.Modules.Promises. A made-up service there that reaches a repository, Forecasting's adapters or the
    /// update code must be named exactly as its twin in the old place is; a service that reads values only, and a namesake
    /// just outside Modules.Promises, must not be.
    /// </summary>
    [TestFixture]
    public class PromisesRulesSeeTheModuleArchUnitTest
    {
        public enum Reach
        {
            ARepository,
            TheForecastingAdapters,
            TheUpdateCode,
        }

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PromisesRulesSeeTheModuleArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (TestModule.IsValueCreated)
            {
                TestModule.Value.Dispose();
            }
        }

        [TestCase(Reach.ARepository)]
        [TestCase(Reach.TheForecastingAdapters)]
        [TestCase(Reach.TheUpdateCode)]
        public void ADeliveryServiceUnderTheModuleReachingWhatPromisesMayNot_IsNamedByTheModuleMapsPromisesNamespaces_AsItsTwinInTheOldPlaceIs(Reach reach)
        {
            var promises = GivenThePromisesKeyOf(typeof(ModuleBoundariesArchUnitTest), "PromisesPattern");

            var named = WhenTheRuleRunsOver(
                [ServiceInTheModule(reach), ServiceInTheOldPlace(reach), NamesakeJustOutsideTheModule(reach), typeof(InTheModule.ADeliveryServiceReadingValuesOnly)],
                inSubject: typeNamespace => Matches(typeNamespace, promises),
                forbidden: WhatPromisesMayNotReach(reach));

            ThenTheRuleNamesTheOffendersAndNotTheirTwins(
                named,
                [ServiceInTheModule(reach), ServiceInTheOldPlace(reach)],
                [NamesakeJustOutsideTheModule(reach), typeof(InTheModule.ADeliveryServiceReadingValuesOnly)],
                "The module map's Promises namespaces",
                "once the Delivery services move to Modules/Promises, the module map must still tell them apart from every other module, or a Promises rule built on it would quietly stop seeing them; a namespace that only shares the prefix, Modules.PromisesFixtures, is not Promises.",
                @"add PromisesPattern to the module map: ^Lighthouse\.Backend\.Modules\.Promises(\.|$) together with the old ^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.DeliverySources(\.|$), and leave PortfolioDeliveryPattern, which names the update code, as it is.");
        }

        [TestCase(Reach.ARepository)]
        [TestCase(Reach.TheForecastingAdapters)]
        [TestCase(Reach.TheUpdateCode)]
        public void ADeliveryServiceUnderTheModuleReachingWhatPromisesMayNot_IsNamedByTheSharedModuleRulesPromisesNamespace_WhileANamesakeIsNot(Reach reach)
        {
            var promisesModule = GivenThePromisesKeyOf(typeof(ModuleRules), "PromisesModule");

            var named = WhenTheRuleRunsOver(
                [ServiceInTheModule(reach), NamesakeJustOutsideTheModule(reach), typeof(InTheModule.ADeliveryServiceReadingValuesOnly)],
                inSubject: typeNamespace => Matches(typeNamespace, promisesModule),
                forbidden: WhatPromisesMayNotReach(reach));

            ThenTheRuleNamesTheOffendersAndNotTheirTwins(
                named,
                [ServiceInTheModule(reach)],
                [NamesakeJustOutsideTheModule(reach), typeof(InTheModule.ADeliveryServiceReadingValuesOnly)],
                "The shared module rules' Promises namespace",
                "the module's own rules are written with the shared module rules and their namespace constants, as Flow metrics and Forecasting are; without a Promises constant that sees Modules/Promises, those rules would have nothing to point at, and one without its end anchor would take in Modules.PromisesFixtures.",
                @"add PromisesModule = ^Lighthouse\.Backend\.Modules\.Promises(\.|$) to ModuleRules, beside FlowMetricsModule and ForecastingModule.");
        }

        [Test]
        public void AConnectorReachingADeliveryServiceUnderTheModule_IsNamedByTheTrackerIntegrationRule_AsOneReachingTheOldPlaceIs()
        {
            var promisesAndTheUpdateCode = GivenThePromisesKeyOf(typeof(TrackerIntegrationModuleBoundariesArchUnitTest), "PromisesAndTheUpdateCode");
            var trackerIntegration = GivenThePromisesKeyOf(typeof(ModuleBoundariesArchUnitTest), "TrackerIntegrationPattern");

            var named = WhenTheRuleRunsOver(
                [typeof(TrackerConnectors.AConnectorReachingTheModule), typeof(TrackerConnectors.AConnectorReachingTheOldPlace), typeof(TrackerConnectors.AConnectorReachingANamesake)],
                inSubject: typeNamespace => Matches(typeNamespace, trackerIntegration),
                forbidden: ResidesIn(promisesAndTheUpdateCode));

            ThenTheRuleNamesTheOffendersAndNotTheirTwins(
                named,
                [typeof(TrackerConnectors.AConnectorReachingTheModule), typeof(TrackerConnectors.AConnectorReachingTheOldPlace)],
                [typeof(TrackerConnectors.AConnectorReachingANamesake)],
                "Tracker integration's key for Promises and the update code",
                "Tracker integration is a leaf that translates what trackers send; once the Delivery services live in Modules/Promises, a connector that reached one would pass the rule on its edges into other contexts unseen. The Jira connector's listed edge to the forecast block renderer stays listed.",
                @"add |Modules\.Promises to PromisesAndTheUpdateCode, for example ^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.(DeliverySources|BackgroundServices|Update)|Modules\.Promises)(\.|$), and keep every listed edge as it is.");
        }

        private static string GivenThePromisesKeyOf(Type rule, string member)
        {
            var field = rule.GetField(member, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var key = (field is { IsLiteral: true } ? field.GetRawConstantValue() : field?.GetValue(null)) as string;

            Assert.That(
                key,
                Is.Not.Null.And.Not.Empty,
                $"{rule.Name} has no {member} that says where Promises lives. Why it matters: this check reads the rule's own key and " +
                "runs it over a made-up Delivery service in Modules/Promises; without the key there is nothing to show the rule sees the " +
                $"new place, and the move would switch the rule off unseen. How to fix: keep the rule's Promises namespace in a constant " +
                $"or static field named {member} and let the rule use it.");

            return key!;
        }

        private static List<string> WhenTheRuleRunsOver(IReadOnlyCollection<Type> madeUpTypes, Func<string, bool> inSubject, Func<TypeReference, bool> forbidden)
            => [.. madeUpTypes
                .Where(madeUpType => inSubject(madeUpType.Namespace!))
                .Where(madeUpType => IlDependencies.DependentsIn(TestModule.Value, $"^{Regex.Escape(madeUpType.Namespace!)}$", forbidden)
                    .Any(dependent => dependent.TypeName == madeUpType.FullName))
                .Select(madeUpType => madeUpType.FullName!)];

        private static void ThenTheRuleNamesTheOffendersAndNotTheirTwins(List<string> named, Type[] offenders, Type[] twins, string key, string why, string how)
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

                foreach (var twin in twins)
                {
                    Assert.That(
                        named,
                        Does.Not.Contain(twin.FullName),
                        $"{key} caught {twin.FullName}, which reads values only or sits outside Modules.Promises. Why it matters: a key " +
                        $"that catches everything proves nothing, and a rule that fails on compliant code gets switched off. How to fix: {how}");
                }
            }
        }

        private static Func<TypeReference, bool> WhatPromisesMayNotReach(Reach reach)
            => reach switch
            {
                Reach.ARepository => RepositoryRecognition.IsRepository,
                Reach.TheForecastingAdapters => ResidesIn(GivenThePromisesKeyOf(typeof(ForecastingModuleBoundariesArchUnitTest), "ForecastingAdapters")),
                Reach.TheUpdateCode => ResidesIn(GivenThePromisesKeyOf(typeof(ModuleBoundariesArchUnitTest), "PortfolioDeliveryPattern")),
                _ => throw new ArgumentOutOfRangeException(nameof(reach), reach, null),
            };

        private static Type ServiceInTheModule(Reach reach)
            => reach switch
            {
                Reach.ARepository => typeof(InTheModule.ADeliveryServiceReachingARepository),
                Reach.TheForecastingAdapters => typeof(InTheModule.ADeliveryServiceReachingTheForecastingAdapters),
                Reach.TheUpdateCode => typeof(InTheModule.ADeliveryServiceReachingTheUpdateCode),
                _ => throw new ArgumentOutOfRangeException(nameof(reach), reach, null),
            };

        private static Type ServiceInTheOldPlace(Reach reach)
            => reach switch
            {
                Reach.ARepository => typeof(InTheOldPlace.ADeliveryServiceReachingARepository),
                Reach.TheForecastingAdapters => typeof(InTheOldPlace.ADeliveryServiceReachingTheForecastingAdapters),
                Reach.TheUpdateCode => typeof(InTheOldPlace.ADeliveryServiceReachingTheUpdateCode),
                _ => throw new ArgumentOutOfRangeException(nameof(reach), reach, null),
            };

        private static Type NamesakeJustOutsideTheModule(Reach reach)
            => reach switch
            {
                Reach.ARepository => typeof(JustOutsideTheModule.ADeliveryServiceReachingARepository),
                Reach.TheForecastingAdapters => typeof(JustOutsideTheModule.ADeliveryServiceReachingTheForecastingAdapters),
                Reach.TheUpdateCode => typeof(JustOutsideTheModule.ADeliveryServiceReachingTheUpdateCode),
                _ => throw new ArgumentOutOfRangeException(nameof(reach), reach, null),
            };

        private static Func<TypeReference, bool> ResidesIn(string namespacePattern)
            => type => Matches(IlDependencies.NamespaceOf(type), namespacePattern);

        private static bool Matches(string typeNamespace, string pattern)
            => Regex.IsMatch(typeNamespace, pattern, RegexOptions.None, PatternTimeout);
    }
}

namespace Lighthouse.Backend.Modules.Promises.Application.PromisesFixtures
{
    internal static class ADeliveryServiceReachingARepository
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<global::Lighthouse.Backend.Models.Delivery> deliveries) => deliveries is not null;
    }

    internal static class ADeliveryServiceReachingTheForecastingAdapters
    {
        public static bool Reaches(global::Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater updater) => updater is not null;
    }

    internal static class ADeliveryServiceReachingTheUpdateCode
    {
        public static bool Reaches(global::Lighthouse.Backend.Platform.UpdateQueue.UpdateKey key) => key is not null;
    }

    internal static class ADeliveryServiceReadingValuesOnly
    {
        public static int DaysLeft(int days) => days - 1;
    }
}

namespace Lighthouse.Backend.Services.Implementation.DeliverySources.PromisesFixtures
{
    internal static class ADeliveryServiceReachingARepository
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<global::Lighthouse.Backend.Models.Delivery> deliveries) => deliveries is not null;
    }

    internal static class ADeliveryServiceReachingTheForecastingAdapters
    {
        public static bool Reaches(global::Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater updater) => updater is not null;
    }

    internal static class ADeliveryServiceReachingTheUpdateCode
    {
        public static bool Reaches(global::Lighthouse.Backend.Platform.UpdateQueue.UpdateKey key) => key is not null;
    }

    internal static class ADeliveryServiceReadingValuesOnly
    {
        public static int DaysLeft(int days) => days - 1;
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.RulesSeeTheModule
{
    internal static class ADeliveryServiceReachingARepository
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<global::Lighthouse.Backend.Models.Delivery> deliveries) => deliveries is not null;
    }

    internal static class ADeliveryServiceReachingTheForecastingAdapters
    {
        public static bool Reaches(global::Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater updater) => updater is not null;
    }

    internal static class ADeliveryServiceReachingTheUpdateCode
    {
        public static bool Reaches(global::Lighthouse.Backend.Platform.UpdateQueue.UpdateKey key) => key is not null;
    }

    internal static class ADeliveryServiceNamesake
    {
        public static int DaysLeft(int days) => days - 1;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.PromisesFixtures
{
    internal static class AConnectorReachingTheModule
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.Promises.Application.PromisesFixtures.ADeliveryServiceReadingValuesOnly.DaysLeft(2) > 0;
    }

    internal static class AConnectorReachingTheOldPlace
    {
        public static bool Reaches() => global::Lighthouse.Backend.Services.Implementation.DeliverySources.PromisesFixtures.ADeliveryServiceReadingValuesOnly.DaysLeft(2) > 0;
    }

    internal static class AConnectorReachingANamesake
    {
        public static bool Reaches() => global::Lighthouse.Backend.Modules.PromisesFixtures.RulesSeeTheModule.ADeliveryServiceNamesake.DaysLeft(2) > 0;
    }
}
