using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.ForecastingRulesFixtures.Connectors;
using Lighthouse.Backend.ForecastingRulesFixtures.RealityCheck;
using Lighthouse.Backend.ForecastingRulesFixtures.Refinement;
using Lighthouse.Backend.ForecastingRulesFixtures.Staleness;
using Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastingFixtures;
using Lighthouse.Backend.Modules.Forecasting.Domain.ForecastingFixtures;
using Lighthouse.Backend.Modules.ForecastingFixtures.RulesSeeTheModule;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Forecasting is about to move from Services into Modules/Forecasting. Every architecture rule that finds Forecasting
    /// by its namespace, folder or file must already see the new place, or the move quietly switches it off: a rule that
    /// looks only at the old place passes because it no longer finds anything there. The real code holds no type in the new
    /// place yet, so each rule's own Forecasting key is read from the rule and run over made-up types placed below
    /// Lighthouse.Backend.Modules.Forecasting. The made-up offender must be named; a compliant twin beside it, and a
    /// namesake just outside Modules.Forecasting, must not be.
    /// </summary>
    [TestFixture]
    public class ForecastingRulesSeeTheModuleArchUnitTest
    {
        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(ForecastingRulesSeeTheModuleArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (TestModule.IsValueCreated)
            {
                TestModule.Value.Dispose();
            }
        }

        [Test]
        public void ATypeReachingForecastingUnderTheModule_IsCaughtAsReachingForecastingByTheModuleMap()
        {
            var forecasting = GivenTheForecastingKeyOf(typeof(ModuleBoundariesArchUnitTest), "ForecastingPattern");

            var named = WhenTheRuleRunsOver(
                [typeof(AConnectorReachingTheForecastModule), typeof(AConnectorReachingANamesake)],
                inSubject: _ => true,
                forbidden: ResidesIn(forecasting));

            ThenTheRuleNamesTheOffenderAndNotItsTwins(
                named,
                typeof(AConnectorReachingTheForecastModule),
                [typeof(AConnectorReachingANamesake)],
                "The module map's Forecasting namespaces",
                "the module map, and the Flow metrics rule that reads it, would let anything reach Forecasting once it lives in Modules/Forecasting, and would treat Modules.ForecastingFixtures as Forecasting if the pattern lost its end anchor.",
                @"add |^Lighthouse\.Backend\.Modules\.Forecasting(\.|$) to ForecastingPattern on the module map.");
        }

        [Test]
        public void ARunsWorkingStateHeldUnderTheModule_IsInsideTheForecast_WhileAnOutsiderHoldingItIsNamed()
        {
            var forecastComponents = GivenTheForecastingKeyOf(typeof(SimulatedRunIsolationArchUnitTest), "TheForecastComponents");

            var named = WhenTheRuleRunsOver(
                [typeof(AnOutsiderHoldingARun), typeof(AMadeUpSimulatedRunStep)],
                inSubject: typeNamespace => !Matches(typeNamespace, forecastComponents),
                forbidden: IsOneOf(typeof(TrialState), typeof(TrialRecordings)));

            ThenTheRuleNamesTheOffenderAndNotItsTwins(
                named,
                typeof(AnOutsiderHoldingARun),
                [typeof(AMadeUpSimulatedRunStep)],
                "The simulated-run isolation rule's forecast components",
                "once the simulation moves to Modules/Forecasting it would look like an outsider holding one run's working state, so the rule would fail on the move or be switched off.",
                @"make TheForecastComponents a namespace pattern of the old place or the module, for example ^Lighthouse\.Backend\.Services\.Implementation\.Forecast$|^Lighthouse\.Backend\.Modules\.Forecasting(\.|$), and match it with DoNotResideInNamespaceMatching.");
        }

        [Test]
        public void AServiceUnderTheModule_StaysOutOfReachOfTheRealityCheckVerdictRules()
        {
            var forbiddenServices = GivenTheForecastingKeyOf(typeof(RealityCheckReadOnlyArchUnitTest), "ImplementationPattern");

            var named = WhenTheRuleRunsOver(
                [typeof(AVerdictRuleReachingAForecastService), typeof(AVerdictRuleReadingValuesOnly)],
                inSubject: _ => true,
                forbidden: ResidesIn(forbiddenServices));

            ThenTheRuleNamesTheOffenderAndNotItsTwins(
                named,
                typeof(AVerdictRuleReachingAForecastService),
                [typeof(AVerdictRuleReadingValuesOnly)],
                "The reality check's services the verdict rules may not reach",
                "the verdict is decided from values alone; once the forecast services live in Modules/Forecasting, a verdict that reached one would pass unseen.",
                "set ImplementationPattern to ApplicationCoreRules.ApplicationCore, and exempt the sufficiency and verdict policies by typeof rather than by their old full names.");
        }

        [Test]
        public void ABackgroundComponentUnderTheModuleReachingForTheForecastService_IsNamed()
        {
            var updateComponents = GivenTheForecastingKeyOf(typeof(DependencyAwareForecastSeamArchUnitTest), "TheUpdateComponents");

            var named = WhenTheRuleRunsOver(
                [typeof(AMadeUpForecastRoundJob), typeof(AMadeUpQueueJob), typeof(AnOutsiderAskingForAForecast)],
                inSubject: typeNamespace => Matches(typeNamespace, updateComponents),
                forbidden: IsOneOf(typeof(IForecastService)));

            ThenTheRuleNamesTheOffenderAndNotItsTwins(
                named,
                typeof(AMadeUpForecastRoundJob),
                [typeof(AMadeUpQueueJob), typeof(AnOutsiderAskingForAForecast)],
                "The update components that may not start a forecast",
                "once the forecast updater moves to Modules/Forecasting/Adapters/Background, a second component there could forecast behind the admission check, and one refresh round would leave a portfolio with two delivery dates.",
                @"make TheUpdateComponents a namespace pattern of the update components or ^Lighthouse\.Backend\.Modules\.Forecasting\.Adapters\.Background(\.|$), match it with ResideInNamespaceMatching, and exempt the updater by typeof(ForecastUpdater).");
        }

        [Test]
        public void TheForecastRoundFiles_AreFoundByTheirNameWhereverTheyLive()
        {
            var forecastService = GivenTheForecastingKeyOf(typeof(FastWithManyFeaturesArchUnitTest), "TheForecastService");
            var forecastUpdater = GivenTheForecastingKeyOf(typeof(FastWithManyFeaturesArchUnitTest), "TheForecastUpdater");

            var productionFileNames = WhenTheProductionSourceIsListedByFileName();

            ThenEachKeyFindsExactlyOneFileByItsNameAlone(productionFileNames, forecastService, forecastUpdater);
        }

        [Test]
        public void ABackgroundUpdateUnderTheModule_StaysOutOfReachOfRefinement_WhileTheForecastAnswersStayOpen()
        {
            var backgroundUpdates = GivenTheForecastingKeyOf(typeof(RefinementModuleArchUnitTest), "BackgroundUpdatesPattern");

            var named = WhenTheRuleRunsOver(
                [typeof(ARefinementReadStartingAForecastRound), typeof(ARefinementReadAskingForAForecast)],
                inSubject: _ => true,
                forbidden: ResidesIn(backgroundUpdates));

            ThenTheRuleNamesTheOffenderAndNotItsTwins(
                named,
                typeof(ARefinementReadStartingAForecastRound),
                [typeof(ARefinementReadAskingForAForecast)],
                "The background updates Refinement may not reach",
                "refinement reads what Lighthouse already holds; once the forecast updater leaves BackgroundServices for Modules/Forecasting/Adapters, Refinement could start a round unseen. Reading a forecast through Forecasting's Application stays allowed.",
                @"give RefinementModuleArchUnitTest a BackgroundUpdatesPattern of the module map's PortfolioDeliveryPattern or ^Lighthouse\.Backend\.Modules\.Forecasting\.Adapters(\.|$), and forbid it in the rule that Refinement reaches no background update.");
        }

        [Test]
        public void AForecastingTypeUnderTheModule_StaysOutOfReachOfTheStalenessRule()
        {
            var services = GivenTheForecastingKeyOf(typeof(StalenessPolicyPurityArchUnitTest), "ServicesPattern");

            var named = WhenTheRuleRunsOver(
                [typeof(AStalenessRuleReachingAForecastCalculation), typeof(AStalenessRuleReadingValuesOnly)],
                inSubject: _ => true,
                forbidden: ResidesIn(services));

            ThenTheRuleNamesTheOffenderAndNotItsTwins(
                named,
                typeof(AStalenessRuleReachingAForecastCalculation),
                [typeof(AStalenessRuleReadingValuesOnly)],
                "The services the staleness rule may not reach",
                "the staleness rule is plain arithmetic over values; today it may reach no Forecasting type because Forecasting sits in Services, and the move would quietly lift that.",
                "set ServicesPattern to ApplicationCoreRules.ApplicationCore, so the rule forbids Services and Modules alike.");
        }

        private static string GivenTheForecastingKeyOf(Type rule, string member)
        {
            var field = rule.GetField(member, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var key = (field is { IsLiteral: true } ? field.GetRawConstantValue() : field?.GetValue(null)) as string;

            Assert.That(
                key,
                Is.Not.Null.And.Not.Empty,
                $"{rule.Name} has no {member} that says where Forecasting lives. Why it matters: this check reads the rule's own key " +
                "and runs it over a made-up type in Modules/Forecasting; without the key there is nothing to show the rule sees the new " +
                $"place. How to fix: keep the rule's Forecasting namespace, folder or file in a constant or static field named {member} " +
                "and let the rule use it.");

            return key!;
        }

        private static List<string> WhenTheRuleRunsOver(IReadOnlyCollection<Type> madeUpTypes, Func<string, bool> inSubject, Func<TypeReference, bool> forbidden)
            => [.. madeUpTypes
                .Where(madeUpType => inSubject(madeUpType.Namespace!))
                .Where(madeUpType => IlDependencies.DependentsIn(TestModule.Value, $"^{Regex.Escape(madeUpType.Namespace!)}$", forbidden)
                    .Any(dependent => dependent.TypeName == madeUpType.FullName))
                .Select(madeUpType => madeUpType.FullName!)];

        private static List<string> WhenTheProductionSourceIsListedByFileName()
            => [.. ProductionSource.Files().Select(file => Path.GetFileName(file.RelativePath))];

        private static void ThenTheRuleNamesTheOffenderAndNotItsTwins(List<string> named, Type offender, Type[] twins, string key, string why, string how)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    named,
                    Does.Contain(offender.FullName),
                    $"{key} did not catch {offender.FullName}. Named: [{string.Join(", ", named)}]. Why it matters: {why} How to fix: {how}");

                foreach (var twin in twins)
                {
                    Assert.That(
                        named,
                        Does.Not.Contain(twin.FullName),
                        $"{key} caught {twin.FullName}, which does only what the rule allows or sits outside Modules.Forecasting. Why it " +
                        $"matters: a key that catches everything proves nothing, and a rule that fails on compliant code gets switched off. How to fix: {how}");
                }
            }
        }

        private static void ThenEachKeyFindsExactlyOneFileByItsNameAlone(List<string> productionFileNames, params string[] keys)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var key in keys)
                {
                    Assert.That(
                        key,
                        Is.EqualTo(Path.GetFileName(key)),
                        $"The fast-with-many-Features rules find {key} by its folder. Why it matters: when the file moves to " +
                        "Modules/Forecasting the round rules stop reading it. How to fix: keep only the file name, as the rules already " +
                        "do for ForecastLane.cs, and find the file by that name under the production source.");
                    Assert.That(
                        productionFileNames.Count(fileName => fileName == Path.GetFileName(key)),
                        Is.EqualTo(1),
                        $"The production source should hold exactly one file named {Path.GetFileName(key)}. Why it matters: a rule that " +
                        "finds its file by name alone would read the wrong one, or none. How to fix: give the forecast round's file a " +
                        "name no other production file has.");
                }
            }
        }

        private static Func<TypeReference, bool> ResidesIn(string namespacePattern)
            => type => Matches(IlDependencies.NamespaceOf(type), namespacePattern);

        private static Func<TypeReference, bool> IsOneOf(params Type[] types)
            => type => types.Any(candidate => candidate.FullName == type.FullName.Replace('/', '+'));

        private static bool Matches(string typeNamespace, string pattern)
            => Regex.IsMatch(typeNamespace, pattern, RegexOptions.None, PatternTimeout);
    }
}

namespace Lighthouse.Backend.Modules.Forecasting.Domain.ForecastingFixtures
{
    internal static class AMadeUpForecastCalculation
    {
        public static int Percentile(int days) => days * 2;
    }

    internal static class AMadeUpSimulatedRunStep
    {
        public static bool Continues(global::Lighthouse.Backend.Services.Implementation.Forecast.TrialState run) => run is not null;
    }
}

namespace Lighthouse.Backend.Modules.Forecasting.Application.ForecastingFixtures
{
    internal static class AMadeUpForecastAnswerService
    {
        public static int Answer(int days) => global::Lighthouse.Backend.Modules.Forecasting.Domain.ForecastingFixtures.AMadeUpForecastCalculation.Percentile(days);
    }
}

namespace Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastingFixtures
{
    internal static class AMadeUpForecastRoundJob
    {
        public static bool CanForecast(global::Lighthouse.Backend.Services.Interfaces.Forecast.IForecastService forecasts) => forecasts is not null;

        public static int Start(int round) => round + 1;
    }

    internal static class AMadeUpQueueJob
    {
        public static int Run(int queued) => queued + 1;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.RulesSeeTheModule
{
    internal static class ANamesakeCalculation
    {
        public static int Percentile(int days) => days * 3;
    }

    internal static class AnOutsiderHoldingARun
    {
        public static bool Continues(global::Lighthouse.Backend.Services.Implementation.Forecast.TrialState run) => run is not null;
    }

    internal static class AnOutsiderAskingForAForecast
    {
        public static bool CanForecast(global::Lighthouse.Backend.Services.Interfaces.Forecast.IForecastService forecasts) => forecasts is not null;
    }
}

namespace Lighthouse.Backend.ForecastingRulesFixtures.Values
{
    internal static class AValue
    {
        public static int Of(int days) => days;
    }
}

namespace Lighthouse.Backend.ForecastingRulesFixtures.Connectors
{
    internal static class AConnectorReachingTheForecastModule
    {
        public static int Fetch(int days) => global::Lighthouse.Backend.Modules.Forecasting.Domain.ForecastingFixtures.AMadeUpForecastCalculation.Percentile(days);
    }

    internal static class AConnectorReachingANamesake
    {
        public static int Fetch(int days) => global::Lighthouse.Backend.Modules.ForecastingFixtures.RulesSeeTheModule.ANamesakeCalculation.Percentile(days);
    }
}

namespace Lighthouse.Backend.ForecastingRulesFixtures.RealityCheck
{
    internal static class AVerdictRuleReachingAForecastService
    {
        public static int Decide(int days) => global::Lighthouse.Backend.Modules.Forecasting.Application.ForecastingFixtures.AMadeUpForecastAnswerService.Answer(days);
    }

    internal static class AVerdictRuleReadingValuesOnly
    {
        public static int Decide(int days) => global::Lighthouse.Backend.ForecastingRulesFixtures.Values.AValue.Of(days);
    }
}

namespace Lighthouse.Backend.ForecastingRulesFixtures.Refinement
{
    internal static class ARefinementReadStartingAForecastRound
    {
        public static int Read(int round) => global::Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastingFixtures.AMadeUpForecastRoundJob.Start(round);
    }

    internal static class ARefinementReadAskingForAForecast
    {
        public static int Read(int days) => global::Lighthouse.Backend.Modules.Forecasting.Application.ForecastingFixtures.AMadeUpForecastAnswerService.Answer(days);
    }
}

namespace Lighthouse.Backend.ForecastingRulesFixtures.Staleness
{
    internal static class AStalenessRuleReachingAForecastCalculation
    {
        public static int DaysStale(int days) => global::Lighthouse.Backend.Modules.Forecasting.Domain.ForecastingFixtures.AMadeUpForecastCalculation.Percentile(days);
    }

    internal static class AStalenessRuleReadingValuesOnly
    {
        public static int DaysStale(int days) => global::Lighthouse.Backend.ForecastingRulesFixtures.Values.AValue.Of(days);
    }
}
