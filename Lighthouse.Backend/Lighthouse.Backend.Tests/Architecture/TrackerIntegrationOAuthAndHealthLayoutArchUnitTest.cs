using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// OAuth (the service, its registry, the state-token issuer, the refresh options, the seven exceptions, the four
    /// interfaces and the four Models value types the flow hands around) lives in the Tracker integration module's
    /// Application.OAuth, connection health in Application.ConnectionHealth, the three OAuth providers in Adapters.OAuth and
    /// the health prober in Adapters.Background. Once these have moved, no type of the module's move list is left under
    /// Services or Factories, and the Models types that must not move are still in Models. The types are named as strings
    /// and read from the compiled production assembly, so a type that is missing, renamed or left behind is a failure that
    /// names it. Only top-level types are named; nested ones live where their outer type lives. Every rule also runs on
    /// made-up types: an offender must be reported and its compliant twin must not be. That the persistence-platform
    /// recognition already sees the OAuth namespaces is held by RepositoryRecognition.PlatformPersistencePattern, which names
    /// Modules.TrackerIntegration.(Application|Adapters).OAuth.
    /// </summary>
    [TestFixture]
    public class TrackerIntegrationOAuthAndHealthLayoutArchUnitTest
    {
        private const string ModuleNamespace = "Lighthouse.Backend.Modules.TrackerIntegration";

        private const string WhereTheTypesLivedBefore = @"^Lighthouse\.Backend\.(Services|Factories)(\.|$)";

        private const string WhereTheValueTypesLivedBefore = @"^Lighthouse\.Backend\.(Services|Factories|Models)(\.|$)";

        private const string TheModels = @"^Lighthouse\.Backend\.Models(\.|$)";

        private const string FixtureNamespace = "Lighthouse.Backend.Modules.TrackerIntegrationFixtures.OAuthAndHealthLayout";

        private const string FixtureLayerNamespace = FixtureNamespace + ".Application";

        private const string FixtureFormerHome = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.OAuthAndHealthLayout\.Services(\.|$)";

        private const string FixtureModels = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.OAuthAndHealthLayout\.Models(\.|$)";

        private static readonly string[] OAuthApplicationTypes =
        [
            "OAuthService",
            "OAuthProviderRegistry",
            "OAuthStateTokenIssuer",
            "OAuthRefreshOptions",
            "OAuthCredentialNotValidException",
            "OAuthProviderNotFoundException",
            "OAuthProviderResponseException",
            "OAuthRefreshFailedException",
            "OAuthRefreshTimeoutException",
            "OAuthStateTokenExpiredException",
            "OAuthStateTokenInvalidException",
            "IOAuthProvider",
            "IOAuthProviderRegistry",
            "IOAuthService",
            "IOAuthStateTokenIssuer",
        ];

        private static readonly string[] OAuthValueTypes =
        [
            "OAuthFlowContext",
            "OAuthRefreshContext",
            "OAuthStateClaims",
            "OAuthTokens",
        ];

        private static readonly string[] ConnectionHealthTypes =
        [
            "ConnectionHealthService",
            "ConnectionHealthCadence",
            "IConnectionHealthService",
        ];

        private static readonly string[] OAuthProviderTypes =
        [
            "AdoOAuthProvider",
            "JiraOAuthProvider",
            "StubOAuthProvider",
        ];

        private static readonly string[] ProberTypes =
        [
            "ConnectionHealthProber",
        ];

        // What the OAuth flow and the write-back round share with code that stays: they are read by a controller, by the
        // event bus or by the platform, so they stay in Models.
        private static readonly string[] ModelsTypesThatStay =
        [
            "OAuthCompleteResult",
            "WriteBackAppliesTo",
            "WriteBackTargetValueType",
            "WriteBackFieldUpdate",
            "WriteBackResult",
            "WriteBackMappingDefinition",
            "NotificationSuppression",
            "WriteBackItemResult",
            "WriteBackValueSource",
            "WriteBackValueSources",
        ];

        private static readonly string[] TheOnlyLeftBehindFixture = ["ALeftBehindOAuthType"];

        private static readonly string[] TheOnlyMovedFixture = ["AModelsTypeThatWasMoved"];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(TrackerIntegrationOAuthAndHealthLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheOAuthServiceItsRegistryTokensExceptionsAndPorts_LiveInTheOAuthApplicationFolder_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ModuleNamespace}.Application.OAuth", OAuthApplicationTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheFourOAuthValueTypes_LiveInTheOAuthApplicationFolder_AndNoneIsLeftInModels()
        {
            Assert.That(
                TrackerIntegrationMembership.ModelsTypesThatMove,
                Is.EquivalentTo(OAuthValueTypes),
                "The Models types the module may take with it are not the four OAuth value types this rule names. Why it matters: a rule over a list that drifted from the move list checks the wrong types. How to fix: keep the two lists the same.");

            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ModuleNamespace}.Application.OAuth", OAuthValueTypes, WhereTheValueTypesLivedBefore).Check();
        }

        [Test]
        public void TheConnectionHealthServiceItsCadenceAndItsPort_LiveInTheConnectionHealthApplicationFolder_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ModuleNamespace}.Application.ConnectionHealth", ConnectionHealthTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheOAuthProviders_LiveInTheOAuthAdaptersFolder_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ModuleNamespace}.Adapters.OAuth", OAuthProviderTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheConnectionHealthProber_LivesInTheBackgroundAdaptersFolder_AndIsNotLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ModuleNamespace}.Adapters.Background", ProberTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void NoTypeOfTheModuleMoveList_IsLeftUnderServicesOrFactories()
        {
            var leftBehind = TopLevelTypesLeftUnder(ProductionModule.Value, WhereTheTypesLivedBefore, TrackerIntegrationMembership.Moves);

            Assert.That(
                leftBehind,
                Is.Empty,
                $"These types of the Tracker integration move list are still under Services or Factories: [{string.Join(", ", leftBehind)}]. " +
                "Why it matters: a type left behind keeps its callers on the old home, so the module does not own all of Tracker integration. " +
                "How to fix: move each file into the module's folder for its layer and change its namespace line, nothing else.");
        }

        [Test]
        public void TheModelsTypesThatDoNotMove_AreStillInModels()
        {
            var missing = TopLevelTypesMissingFrom(ProductionModule.Value, TheModels, ModelsTypesThatStay);

            Assert.That(
                missing,
                Is.Empty,
                $"These Models types should still be in Models but are not found there: [{string.Join(", ", missing)}]. " +
                "Why it matters: a controller, the event bus or the write-back round reads them, so moving them would pull Models into the module or break the wire. " +
                "How to fix: keep these types in Models; only the four OAuth value types of the flow move.");
        }

        [Test]
        public void ATypeLeftUnderItsFormerHome_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twinPlaced = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureLayerNamespace, ["AWellPlacedOAuthType"], FixtureFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureLayerNamespace, ["ALeftBehindOAuthType"], FixtureFormerHome);

            Assert.DoesNotThrow(twinPlaced.Check, "A type that lives in its layer and has no copy in its former home was reported. Why it matters: a rule that flags a correct move hides the real offenders. How to fix: report only a type missing from the layer or still present in the former home.");

            var failure = Assert.Throws<AssertionException>(leftBehind.Check, "A type left in its former home passed the layout rule.");
            Assert.That(failure!.Message, Does.Contain("ALeftBehindOAuthType"), "The failure must name the type that was left behind.");
        }

        [Test]
        public void ATypeThatIsMissingFromItsLayer_IsNamed()
        {
            var missing = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureLayerNamespace, ["AnOAuthTypeThatWasRenamed"], FixtureFormerHome);

            var failure = Assert.Throws<AssertionException>(missing.Check, "A type that does not exist passed the layout rule.");
            Assert.That(failure!.Message, Does.Contain("AnOAuthTypeThatWasRenamed"), "The failure must name the type that cannot be found. Why it matters: a rule that looks at nothing passes unseen. How to fix: report every named type the compiled assembly does not hold in the layer.");
        }

        [Test]
        public void AMoveListTypeLeftUnderTheFormerHome_IsReported_AndAMovedTwinIsNot()
        {
            var leftBehind = TopLevelTypesLeftUnder(FixtureModule.Value, FixtureFormerHome, ["ALeftBehindOAuthType", "AWellPlacedOAuthType"]);

            Assert.That(
                leftBehind.Select(SimpleNameOf).ToList(),
                Is.EqualTo(TheOnlyLeftBehindFixture),
                "The leftover check must report the made-up type still under the former home and must not report its twin that sits in the layer. Why it matters: a check that misses the offender, or flags the twin, says nothing about the real move list. How to fix: report exactly the listed top-level types found in the former home.");
        }

        [Test]
        public void AModelsTypeThatLeftModels_IsReported_AndATypeThatStayedIsNot()
        {
            var missing = TopLevelTypesMissingFrom(FixtureModule.Value, FixtureModels, ["AModelsTypeThatStayed", "AModelsTypeThatWasMoved"]);

            Assert.That(
                missing,
                Is.EqualTo(TheOnlyMovedFixture),
                "The stay check must report the made-up Models type that is no longer in Models and must not report the one that stayed. Why it matters: a check that passes when a type is gone from Models would let a wire type move unseen. How to fix: report every named type not found in Models.");
        }

        // A nested type lives where its outer type lives, so only top-level types are compared by name.
        private static List<string> TopLevelTypesIn(ModuleDefinition module, string namespacePattern)
            => IlDependencies.TypesIn(module, namespacePattern).Where(fullName => !fullName.Contains('+')).ToList();

        private static List<string> TopLevelTypesLeftUnder(ModuleDefinition module, string formerHome, IReadOnlyCollection<string> names)
            => [.. TopLevelTypesIn(module, formerHome).Where(fullName => names.Contains(SimpleNameOf(fullName), StringComparer.Ordinal)).Order(StringComparer.Ordinal)];

        private static List<string> TopLevelTypesMissingFrom(ModuleDefinition module, string namespacePattern, IReadOnlyCollection<string> names)
        {
            var present = TopLevelTypesIn(module, namespacePattern).Select(SimpleNameOf).ToHashSet(StringComparer.Ordinal);

            return [.. names.Where(name => !present.Contains(name))];
        }

        private static string SimpleNameOf(string fullName) => fullName[(fullName.LastIndexOf('.') + 1)..];
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.OAuthAndHealthLayout.Application
{
    internal sealed class AWellPlacedOAuthType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.OAuthAndHealthLayout.Services
{
    internal sealed class ALeftBehindOAuthType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.OAuthAndHealthLayout.Models
{
    internal sealed class AModelsTypeThatStayed
    {
        public int Answer => 42;
    }
}
