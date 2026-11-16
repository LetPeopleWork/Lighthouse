using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Factories.TrackerIntegrationFixtures;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.TrackerIntegrationFixtures;
using Lighthouse.Backend.Modules.TrackerIntegrationFixtures.RulesSeeTheModule;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.FactoriesFixtures.RulesSeeTheModule;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.TrackerIntegrationFixtures;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Tracker integration is about to move from Services/Implementation/WorkTrackingConnectors, Factories and
    /// Services/Factories into Modules/TrackerIntegration. The rules that keep a connector a leaf (it reaches neither Work
    /// Items, Forecasting nor the update code) and keep it away from every repository find the connectors by namespace, so
    /// they must already see the new place, or the move quietly switches them off. The real code holds no type in the new
    /// place yet, so each rule's own key is read from the rule and run over made-up connectors placed below
    /// Lighthouse.Backend.Modules.TrackerIntegration. A made-up offender must be named, as the same offender is named in the
    /// old place today; a compliant twin beside it, and a namesake just outside Modules.TrackerIntegration, must not be.
    /// </summary>
    [TestFixture]
    public class TrackerIntegrationRulesSeeTheModuleArchUnitTest
    {
        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(TrackerIntegrationRulesSeeTheModuleArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (TestModule.IsValueCreated)
            {
                TestModule.Value.Dispose();
            }
        }

        [Test]
        public void AConnectorUnderTheModuleReachingWorkItemsForecastingOrTheUpdateCode_IsNamedByTheLeafRule()
        {
            var leafRule = GivenTheLeafRule();

            var named = WhenTheRuleRunsOver(
                [
                    typeof(AModuleConnectorReachingWorkItems),
                    typeof(AModuleConnectorReachingForecasting),
                    typeof(AModuleConnectorReachingTheUpdateCode),
                    typeof(AnOldPlaceConnectorReachingWorkItems),
                    typeof(AModuleConnectorReadingAFeature),
                    typeof(AModuleConnectorReachingADeliverySource),
                    typeof(ANamesakeConnectorReachingWorkItems),
                ],
                leafRule);

            ThenTheRuleNamesTheOffendersAndNotTheirTwins(
                named,
                [
                    typeof(AModuleConnectorReachingWorkItems),
                    typeof(AModuleConnectorReachingForecasting),
                    typeof(AModuleConnectorReachingTheUpdateCode),
                    typeof(AnOldPlaceConnectorReachingWorkItems),
                ],
                [typeof(AModuleConnectorReadingAFeature), typeof(AModuleConnectorReachingADeliverySource), typeof(ANamesakeConnectorReachingWorkItems)],
                "The leaf rule for Tracker integration",
                "once the connectors live in Modules/TrackerIntegration, a connector that reached Work Items, Forecasting or the update code " +
                "would pass unseen, and the context would stop being a leaf the other contexts can rely on. Reaching a Feature of the shared " +
                "kernel is allowed, and the delivery sources stay outside the rule until their one known edge is listed.",
                @"add |^Lighthouse\.Backend\.Modules\.TrackerIntegration(\.|$) to WorkTrackingIntegrationPattern on the module map, keep the old " +
                "alternatives, and do not add the delivery sources to the Portfolio/Delivery side yet.");
        }

        [Test]
        public void AFactoryInTheServicesFactoriesNamespaceReachingWorkItems_IsNamedByTheLeafRule()
        {
            var leafRule = GivenTheLeafRule();

            var named = WhenTheRuleRunsOver(
                [typeof(AServicesFactoryReachingWorkItems), typeof(AnOldPlaceFactoryReachingWorkItems), typeof(ANamesakeFactoryReachingWorkItems)],
                leafRule);

            ThenTheRuleNamesTheOffendersAndNotTheirTwins(
                named,
                [typeof(AServicesFactoryReachingWorkItems), typeof(AnOldPlaceFactoryReachingWorkItems)],
                [typeof(ANamesakeFactoryReachingWorkItems)],
                "The leaf rule for Tracker integration",
                "the connector factory sits partly in Factories and partly in Services.Factories; the half the rule does not see could reach " +
                "Work Items unseen, before and after the move.",
                @"add |^Lighthouse\.Backend\.Services\.Factories$ to WorkTrackingIntegrationPattern on the module map.");
        }

        [Test]
        public void AConnectorUnderTheModuleReachingTheDatabase_IsNamedByTheNoRepositoryRule_WhileItsOAuthTokensStayAllowed()
        {
            var noRepositoryRule = GivenTheNoRepositoryRule();

            var named = WhenTheRuleRunsOver(
                [
                    typeof(AModuleConnectorReachingTheDatabase),
                    typeof(AnOldPlaceConnectorReachingTheDatabase),
                    typeof(AModuleConnectorReadingItsOAuthToken),
                    typeof(ANamesakeConnectorReachingTheDatabase),
                ],
                noRepositoryRule);

            ThenTheRuleNamesTheOffendersAndNotTheirTwins(
                named,
                [typeof(AModuleConnectorReachingTheDatabase), typeof(AnOldPlaceConnectorReachingTheDatabase)],
                [typeof(AModuleConnectorReadingItsOAuthToken), typeof(ANamesakeConnectorReachingTheDatabase)],
                "The rule that keeps connectors away from every repository",
                "a connector only translates what the tracker sent; once it lives in Modules/TrackerIntegration it could read stored records " +
                "unseen. Reading the OAuth token through the token service stays allowed.",
                @"add |^Lighthouse\.Backend\.Modules\.TrackerIntegration(\.|$) to WorkTrackingIntegrationPattern, which this rule reads too.");
        }

        [Test]
        public void TheOAuthTokenPlaceUnderTheModule_CountsAsThePersistencePlatform_WhileANamesakeDoesNot()
        {
            var tokenPlaces = new[]
            {
                "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth",
                "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.OAuth",
                "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.OAuth.Providers",
                "Lighthouse.Backend.Services.Implementation.OAuth",
                "Lighthouse.Backend.Services.Interfaces.OAuth",
            };
            var namesakes = new[]
            {
                "Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Application.OAuth",
                "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuthFixtures",
            };

            var recognised = WhenThePersistencePlatformIsRecognisedAmong(tokenPlaces.Concat(namesakes));

            ThenExactlyTheTokenPlacesArePersistencePlatform(recognised, tokenPlaces, namesakes);
        }

        private static (string Subject, string Forbidden) GivenTheLeafRule()
        {
            var forbidden = string.Join(
                "|",
                GivenTheKeyOf(typeof(ModuleBoundariesArchUnitTest), "ForecastingPattern"),
                GivenTheKeyOf(typeof(ModuleBoundariesArchUnitTest), "PortfolioDeliveryPattern"),
                GivenTheKeyOf(typeof(ModuleBoundariesArchUnitTest), "WorkItemsSyncPattern"));

            return (GivenTheTrackerIntegrationNamespaces(), forbidden);
        }

        private static (string Subject, string Forbidden) GivenTheNoRepositoryRule()
            => (GivenTheTrackerIntegrationNamespaces(), RepositoryRecognition.StoredRecordsPattern);

        private static string GivenTheTrackerIntegrationNamespaces()
            => GivenTheKeyOf(typeof(ModuleBoundariesArchUnitTest), "WorkTrackingIntegrationPattern");

        private static string GivenTheKeyOf(Type rule, string member)
        {
            var field = rule.GetField(member, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var key = (field is { IsLiteral: true } ? field.GetRawConstantValue() : field?.GetValue(null)) as string;

            Assert.That(
                key,
                Is.Not.Null.And.Not.Empty,
                $"{rule.Name} has no {member}. Why it matters: this check reads the rule's own namespace key and runs it over made-up " +
                "connectors in Modules/TrackerIntegration; without the key there is nothing to show the rule sees the new place. How to " +
                $"fix: keep the rule's namespace pattern in a constant or static field named {member} and let the rule use it.");

            return key!;
        }

        private static List<string> WhenTheRuleRunsOver(IReadOnlyCollection<Type> madeUpTypes, (string Subject, string Forbidden) rule)
            => [.. madeUpTypes
                .Where(madeUpType => Matches(madeUpType.Namespace!, rule.Subject))
                .Where(madeUpType => IlDependencies.DependentsIn(TestModule.Value, $"^{Regex.Escape(madeUpType.Namespace!)}$", ResidesIn(rule.Forbidden))
                    .Any(dependent => dependent.TypeName == madeUpType.FullName))
                .Select(madeUpType => madeUpType.FullName!)];

        private static List<string> WhenThePersistencePlatformIsRecognisedAmong(IEnumerable<string> namespaceNames)
            => [.. namespaceNames.Where(RepositoryRecognition.IsPlatformPersistence)];

        private static void ThenTheRuleNamesTheOffendersAndNotTheirTwins(List<string> named, Type[] offenders, Type[] twins, string rule, string why, string how)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var offender in offenders)
                {
                    Assert.That(
                        named,
                        Does.Contain(offender.FullName),
                        $"{rule} did not catch {offender.FullName}. Named: [{string.Join(", ", named)}]. Why it matters: {why} How to fix: {how}");
                }

                foreach (var twin in twins)
                {
                    Assert.That(
                        named,
                        Does.Not.Contain(twin.FullName),
                        $"{rule} caught {twin.FullName}, which does only what the rule allows or sits outside Tracker integration. Why it " +
                        $"matters: a key that catches everything proves nothing, and a rule that fails on compliant code gets switched off. How to fix: {how}");
                }
            }
        }

        private static void ThenExactlyTheTokenPlacesArePersistencePlatform(List<string> recognised, string[] tokenPlaces, string[] namesakes)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var tokenPlace in tokenPlaces)
                {
                    Assert.That(
                        recognised,
                        Does.Contain(tokenPlace),
                        $"{tokenPlace} is not recognised as the persistence platform. Why it matters: the OAuth token service keeps tokens " +
                        "for the connectors and moves into Modules/TrackerIntegration; if it is no longer seen as the platform, the platform " +
                        "map loses it and the rules that tell a repository from a platform service read it as neither. How to fix: add " +
                        @"Modules\.TrackerIntegration\.(Application|Adapters)\.OAuth to PlatformPersistencePattern in RepositoryRecognition.");
                }

                foreach (var namesake in namesakes)
                {
                    Assert.That(
                        recognised,
                        Does.Not.Contain(namesake),
                        $"{namesake} is recognised as the persistence platform, but only shares a prefix with the OAuth token place. Why " +
                        "it matters: a pattern without its end anchor makes anything that starts alike part of the platform. How to fix: " +
                        @"end the OAuth alternative with ($|\..*), as the other alternatives do.");
                }
            }
        }

        private static Func<TypeReference, bool> ResidesIn(string namespacePattern)
            => type => Matches(IlDependencies.NamespaceOf(type), namespacePattern);

        private static bool Matches(string typeNamespace, string pattern)
            => Regex.IsMatch(typeNamespace, pattern, RegexOptions.None, PatternTimeout);
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.TrackerIntegrationFixtures
{
    internal static class AModuleConnectorReachingWorkItems
    {
        public static bool Fetch(global::Lighthouse.Backend.Modules.WorkItems.Application.IBlockedItemService blockedItems) => blockedItems is not null;
    }

    internal static class AModuleConnectorReachingForecasting
    {
        public static bool Fetch(global::Lighthouse.Backend.Modules.Forecasting.Application.IForecastService forecasts) => forecasts is not null;
    }

    internal static class AModuleConnectorReachingTheUpdateCode
    {
        public static bool Fetch(global::Lighthouse.Backend.Services.Interfaces.Update.IUpdateQueueService updates) => updates is not null;
    }

    internal static class AModuleConnectorReachingTheDatabase
    {
        public static bool Fetch(global::Lighthouse.Backend.Data.LighthouseAppContext context) => context is not null;
    }

    internal static class AModuleConnectorReadingAFeature
    {
        public static bool Fetch(global::Lighthouse.Backend.Models.Feature feature) => feature is not null;
    }

    internal static class AModuleConnectorReachingADeliverySource
    {
        public static bool Fetch(global::Lighthouse.Backend.Services.Interfaces.DeliverySources.IDeliveryForecastBlockRenderer renderer) => renderer is not null;
    }

    internal static class AModuleConnectorReadingItsOAuthToken
    {
        public static int Fetch(int attempt) => global::Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.TrackerIntegrationFixtures.AMadeUpTokenReader.Read(attempt);
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.TrackerIntegrationFixtures
{
    internal static class AMadeUpTokenReader
    {
        public static int Read(int attempt) => attempt + 1;
    }
}

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.TrackerIntegrationFixtures
{
    internal static class AnOldPlaceConnectorReachingWorkItems
    {
        public static bool Fetch(global::Lighthouse.Backend.Modules.WorkItems.Application.IBlockedItemService blockedItems) => blockedItems is not null;
    }

    internal static class AnOldPlaceConnectorReachingTheDatabase
    {
        public static bool Fetch(global::Lighthouse.Backend.Data.LighthouseAppContext context) => context is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.RulesSeeTheModule
{
    internal static class ANamesakeConnectorReachingWorkItems
    {
        public static bool Fetch(global::Lighthouse.Backend.Modules.WorkItems.Application.IBlockedItemService blockedItems) => blockedItems is not null;
    }

    internal static class ANamesakeConnectorReachingTheDatabase
    {
        public static bool Fetch(global::Lighthouse.Backend.Data.LighthouseAppContext context) => context is not null;
    }
}

namespace Lighthouse.Backend.Services.Factories
{
    internal static class AServicesFactoryReachingWorkItems
    {
        public static bool Create(global::Lighthouse.Backend.Modules.WorkItems.Application.IBlockedItemService blockedItems) => blockedItems is not null;
    }
}

namespace Lighthouse.Backend.Factories.TrackerIntegrationFixtures
{
    internal static class AnOldPlaceFactoryReachingWorkItems
    {
        public static bool Create(global::Lighthouse.Backend.Modules.WorkItems.Application.IBlockedItemService blockedItems) => blockedItems is not null;
    }
}

namespace Lighthouse.Backend.Services.FactoriesFixtures.RulesSeeTheModule
{
    internal static class ANamesakeFactoryReachingWorkItems
    {
        public static bool Create(global::Lighthouse.Backend.Modules.WorkItems.Application.IBlockedItemService blockedItems) => blockedItems is not null;
    }
}
