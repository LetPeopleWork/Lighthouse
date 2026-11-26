using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.Forecasting.Application;
using Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.Forecasting.Application.WriteBack;
using Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.Forecasting.Adapters.Background;
using Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.Promises.Application;
using Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.Services.Interfaces;
using Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.TrackerIntegration;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The write-back round, five classes and three ports in Forecasting's Application under WriteBack, has rules of its own,
    /// read from the compiled code. Each of its types lives there and none is left in the loose Services namespaces; inside
    /// Forecasting only the write-back service reaches Tracker integration, along its listed edges; Promises, Access and
    /// Licensing reach no round type; and inside Forecasting only the forecast updater and the round itself reach the round.
    /// Every list is compared both ways. The same rules run on made-up types, one per way a rule can be broken, so a rule that
    /// names nothing cannot pass.
    /// </summary>
    [TestFixture]
    public class WriteBackRoundBoundariesArchUnitTest
    {
        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(WriteBackRoundBoundariesArchUnitTest).Assembly));

        private static string TrackerIntegration => @"^Lighthouse\.Backend\.Modules\.TrackerIntegration(\.|$)";

        private static string TheContextsThatMayNotJoinTheRound => @"^Lighthouse\.Backend\.Modules\.(Promises|Access|Licensing)(\.|$)";

        private static string TheLooseServices => @"^Lighthouse\.Backend\.Services(\.|$)";

        private static string TheRoundsNamespace => $"^{Regex.Escape(WriteBackMembership.TheNewNamespace)}(\\.|$)";

        private static string ForecastingOutsideTheRound => @"^Lighthouse\.Backend\.Modules\.Forecasting(?!\.Application\.WriteBack(\.|$))(\.|$)";

        private static string[] TheEdgesOfTheRoundIntoTrackerIntegration =>
        [
            $"{WriteBackMembership.TheNewNamespace}.WriteBackService -> Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingConnector",
            $"{WriteBackMembership.TheNewNamespace}.WriteBackService -> Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingConnectorFactory",
            $"{WriteBackMembership.TheNewNamespace}.WriteBackService -> Lighthouse.Backend.Modules.TrackerIntegration.Application.IWriteBackTarget",
        ];

        private static string[] TheForecastingTypesReachingTheRound =>
        [
            "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater",
        ];

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryRoundTypeLivesInForecastingsWriteBackNamespace_AndNoWriteBackTypeIsLeftInTheLooseServices()
        {
            var names = WriteBackMembership.MovingTypes.Select(type => type.Name).ToList();

            var problems = WhenThePlacementIsJudged(ProductionModule.Value, TheLooseServices, WriteBackMembership.TheNewNamespace, names);

            ThenThePlacementIsSound(problems);
        }

        [Test]
        public void ThePlacementRule_NamesAWriteBackNewcomerLeftInTheLooseServices_AndARoundTypeThatIsNotInTheNamespace()
        {
            var home = Fixture("Services");
            var round = typeof(IAPlantedRoundPort).Namespace!;

            var problems = WhenThePlacementIsJudged(TestModule.Value, home, round, [nameof(IAPlantedRoundPort), "AMissingRoundType"]);

            ThenOnlyTheNewcomerAndTheMissingTypeAreNamed(problems);
        }

        [Test]
        public void InsideForecastingOnlyTheWriteBackServiceReachesTrackerIntegration_AlongTheListedEdges()
        {
            var verdict = ModuleRules.OutboundEdges(ProductionModule.Value, ModuleRules.ForecastingModule, TrackerIntegration, TheEdgesOfTheRoundIntoTrackerIntegration, "The write-back service alone reaches Tracker integration");

            var (unlisted, gone) = WhenTheEdgesAreComparedWithTheList(verdict.Edges, TheEdgesOfTheRoundIntoTrackerIntegration);

            ThenTheEdgesAreExactlyTheListed(unlisted, gone);
        }

        [Test]
        public void TheTrackerEdgeRule_NamesASecondForecastingTypeReachingTrackerIntegration_AndAListedEdgeNoTypeMakesAnyMore()
        {
            var listed = new[] { $"{typeof(APlantedRoundService).FullName} -> {typeof(IAPlantedTracker).FullName}", "The.Gone.Type -> The.Gone.Target" };
            var verdict = ModuleRules.OutboundEdges(TestModule.Value, Fixture("Forecasting"), Fixture("TrackerIntegration"), listed, "made-up");

            var (unlisted, gone) = WhenTheEdgesAreComparedWithTheList(verdict.Edges, listed);

            ThenOnlyTheSecondReacherAndTheGoneEdgeAreNamed(unlisted, gone);
        }

        [Test]
        public void PromisesAccessAndLicensingReachNoWriteBackRoundType()
        {
            var verdict = ModuleRules.OutboundEdges(ProductionModule.Value, TheContextsThatMayNotJoinTheRound, "(?!)", [], "No other context joins the round", ModuleRules.TheWriteBackRound);

            ThenNoEdgeIntoTheRoundIsFound(verdict);
        }

        [Test]
        public void TheContextRule_NamesAPromisesTypeJoiningTheRound_AndPassesItsTwinAskingForecastingForAForecast()
        {
            var roundPattern = $"^{Regex.Escape(typeof(IAPlantedRoundPort).Namespace!)}\\.";
            var verdict = ModuleRules.OutboundEdges(TestModule.Value, Fixture("Promises"), "(?!)", [], "made-up", roundPattern);

            ThenOnlyThePromisesTypeJoiningTheRoundIsNamed(verdict);
        }

        [Test]
        public void InsideForecastingOnlyTheForecastUpdaterAndTheRoundItselfReachTheRound()
        {
            var reaching = WhenTheForecastingTypesOutsideTheRoundReachTheRound(ProductionModule.Value, ForecastingOutsideTheRound, TheRoundsNamespace);

            var (unlisted, gone) = WhenTheEdgesAreComparedWithTheList(reaching, TheForecastingTypesReachingTheRound);

            ThenTheReachersAreExactlyTheListed(unlisted, gone);
        }

        [Test]
        public void TheRoundReachRule_NamesASecondForecastingTypeReachingTheRound_AndAListedReacherThatNoLongerDoes()
        {
            var listed = new[] { typeof(TheListedReacher).FullName!, "The.Gone.Reacher" };

            var reaching = WhenTheForecastingTypesOutsideTheRoundReachTheRound(TestModule.Value, ForecastingFixtureOutsideTheRound, $"^{Regex.Escape(typeof(IAPlantedRoundPort).Namespace!)}(\\.|$)");
            var (unlisted, gone) = WhenTheEdgesAreComparedWithTheList(reaching, listed);

            ThenOnlyTheSecondReacherAndTheGoneReacherAreNamed(unlisted, gone);
        }

        private static string ForecastingFixtureOutsideTheRound
            => $@"^{Regex.Escape(typeof(IAForecastPort).Namespace![..^".Application".Length])}(?!\.Application\.WriteBack(\.|$))(\.|$)";

        private static string Fixture(string area)
            => $@"^{Regex.Escape(typeof(WriteBackRoundBoundariesArchUnitTest).Namespace!)}\.WriteBackRoundBoundariesFixtures\.{area}(\.|$)";

        private static List<string> WhenThePlacementIsJudged(ModuleDefinition module, string looseServices, string roundNamespace, IReadOnlyCollection<string> names)
        {
            var missing = names
                .Where(name => module.GetType($"{roundNamespace}.{name}") is null)
                .Select(name => $"{name} is not in {roundNamespace}");
            var leftBehind = IlDependencies.TypesIn(module, looseServices)
                .Select(fullName => fullName[(fullName.LastIndexOf('.') + 1)..])
                .Where(name => name.StartsWith("WriteBack", StringComparison.Ordinal) || name.StartsWith("IWriteBack", StringComparison.Ordinal))
                .Select(name => $"{name} is left in the loose Services namespaces");

            return [.. missing, .. leftBehind];
        }

        private static (List<string> Unlisted, List<string> Gone) WhenTheEdgesAreComparedWithTheList(IEnumerable<string> found, IReadOnlyCollection<string> listed)
            => ([.. found.Except(listed, StringComparer.Ordinal).Order(StringComparer.Ordinal)], [.. listed.Except(found, StringComparer.Ordinal).Order(StringComparer.Ordinal)]);

        private static List<string> WhenTheForecastingTypesOutsideTheRoundReachTheRound(ModuleDefinition module, string forecastingOutsideTheRound, string roundNamespace)
            => [.. IlDependencies.DependentsIn(module, forecastingOutsideTheRound, type => !(type is GenericParameter) && Regex.IsMatch(IlDependencies.NamespaceOf(type), roundNamespace, RegexOptions.None, TimeSpan.FromSeconds(1)))
                .Select(dependent => dependent.TypeName)];

        private static void ThenThePlacementIsSound(List<string> problems)
            => Assert.That(
                problems,
                Is.Empty,
                $"The write-back round is not placed as designed: {string.Join("; ", problems)}. " +
                "Why it matters: the round is one namespace of Forecasting's Application, so a round type elsewhere, or a write-back type back in the loose Services, splits it between two owners. " +
                "How to fix: put each round type in Lighthouse.Backend.Modules.Forecasting.Application.WriteBack and keep no WriteBack or IWriteBack type under Services.");

        private static void ThenOnlyTheNewcomerAndTheMissingTypeAreNamed(List<string> problems)
            => Assert.That(
                problems.Order(StringComparer.Ordinal).ToList(),
                Is.EqualTo(new[] { "AMissingRoundType is not in " + typeof(IAPlantedRoundPort).Namespace, $"{nameof(IWriteBackMadeUp)} is left in the loose Services namespaces" }),
                $"The placement rule named [{string.Join(" | ", problems)}]. Expected the planted IWriteBackMadeUp left in Services and the round type that is nowhere, and not the planted round port that is in place. " +
                "Why it matters: a rule that names nothing passes a round that has lost a type or taken in a newcomer. How to fix: report each missing round type and each write-back type left under Services.");

        private static void ThenTheEdgesAreExactlyTheListed(List<string> unlisted, List<string> gone)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(unlisted, Is.Empty, $"These edges from Forecasting into Tracker integration are not listed: {string.Join(", ", unlisted)}. Why it matters: only the write-back service may reach the trackers, because translating a write into a tracker's fields is their job. How to fix: reach Tracker integration through the write-back service, or list the edge with its owner.");
                Assert.That(gone, Is.Empty, $"These listed edges are no longer made: {string.Join(", ", gone)}. Why it matters: a list that keeps a gone edge hides a later one. How to fix: delete it from the list.");
            }
        }

        private static void ThenOnlyTheSecondReacherAndTheGoneEdgeAreNamed(List<string> unlisted, List<string> gone)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(unlisted, Is.EqualTo(new[] { $"{typeof(ASecondTrackerReacher).FullName} -> {typeof(IAPlantedTracker).FullName}" }), $"The rule named [{string.Join(" | ", unlisted)}] as unlisted. Expected exactly the second Forecasting type reaching the made-up tracker. Why it matters: a rule that misses a second reacher lets the trackers be reached from anywhere. How to fix: compare every Forecasting edge into Tracker integration with the list.");
                Assert.That(gone, Is.EqualTo(new List<string> { "The.Gone.Type -> The.Gone.Target" }), $"The rule named [{string.Join(" | ", gone)}] as gone. Expected exactly the listed edge nothing makes. Why it matters: a list that is only compared one way keeps dead entries. How to fix: compare the list both ways.");
            }
        }

        private static void ThenNoEdgeIntoTheRoundIsFound(OutboundVerdict verdict)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Examined, Is.Not.Empty, "No Promises, Access or Licensing type was examined, so the rule would pass without looking. How to fix: point the rule at the namespaces of those contexts.");
                Assert.That(verdict.Edges, Is.Empty, $"These types of Promises, Access or Licensing reach the write-back round: {string.Join(", ", verdict.Edges)}. Why it matters: the round belongs to Forecasting and only its forecast updater, update code and update queue may take part in it. How to fix: let the type react to a domain event instead of calling the round.");
            }
        }

        private static void ThenOnlyThePromisesTypeJoiningTheRoundIsNamed(OutboundVerdict verdict)
            => Assert.That(
                verdict.Edges,
                Is.EqualTo(new[] { $"{typeof(APromisesServiceJoiningTheRound).FullName} -> {typeof(IAPlantedRoundPort).FullName}" }),
                $"The rule named [{string.Join(" | ", verdict.Edges)}]. Expected exactly the Promises type reaching the made-up round port, and not its twin asking Forecasting for a forecast. " +
                "Why it matters: Promises may call Forecasting by right, so refusing all of Forecasting is wrong and refusing nothing lets a type join the round unseen. How to fix: refuse by the round pattern only.");

        private static void ThenTheReachersAreExactlyTheListed(List<string> unlisted, List<string> gone)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(unlisted, Is.Empty, $"These Forecasting types outside the round reach it and are not listed: {string.Join(", ", unlisted)}. Why it matters: only the forecast updater triggers the round, so another reacher ties the round to more of Forecasting. How to fix: trigger the round through the forecast updater, or list the type with its owner.");
                Assert.That(gone, Is.Empty, $"These listed reachers no longer reach the round: {string.Join(", ", gone)}. How to fix: delete them from the list.");
            }
        }

        private static void ThenOnlyTheSecondReacherAndTheGoneReacherAreNamed(List<string> unlisted, List<string> gone)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(unlisted, Is.EqualTo(new[] { typeof(ASecondRoundReacher).FullName }), $"The rule named [{string.Join(" | ", unlisted)}] as unlisted. Expected exactly the second Forecasting type reaching the made-up round, not the listed one and not the twin that does not reach it. How to fix: compare every Forecasting type outside the round that reaches it with the list.");
                Assert.That(gone, Is.EqualTo(new List<string> { "The.Gone.Reacher" }), $"The rule named [{string.Join(" | ", gone)}] as gone. Expected exactly the listed type that reaches nothing. How to fix: compare the list both ways.");
            }
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.Services.Interfaces
{
    /// <summary>A made-up write-back newcomer left in a made-up loose Services namespace.</summary>
    internal interface IWriteBackMadeUp
    {
        int Stage(int fields);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.TrackerIntegration
{
    internal interface IAPlantedTracker
    {
        int Write(int fields);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.Forecasting.Application.WriteBack
{
    internal interface IAPlantedRoundPort
    {
        int Stage(int fields);
    }

    internal sealed class APlantedRoundService(TrackerIntegration.IAPlantedTracker tracker, IAPlantedRoundPort port)
    {
        public int Run(int fields) => tracker.Write(port.Stage(fields));
    }
}

namespace Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.Forecasting.Application
{
    internal interface IAForecastPort
    {
        int Forecast(int days);
    }

    internal sealed class ASecondTrackerReacher(TrackerIntegration.IAPlantedTracker tracker)
    {
        public int Write(int fields) => tracker.Write(fields);
    }

    internal sealed class ASecondRoundReacher(WriteBack.IAPlantedRoundPort port)
    {
        public int Stage(int fields) => port.Stage(fields);
    }

    internal sealed class ATwinThatDoesNotReachTheRound(IAForecastPort forecast)
    {
        public int Ask(int days) => forecast.Forecast(days);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.Forecasting.Adapters.Background
{
    internal sealed class TheListedReacher(Application.WriteBack.IAPlantedRoundPort port)
    {
        public int Stage(int fields) => port.Stage(fields);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.WriteBackRoundBoundariesFixtures.Promises.Application
{
    internal sealed class APromisesServiceJoiningTheRound(Forecasting.Application.WriteBack.IAPlantedRoundPort round)
    {
        public int Promise(int fields) => round.Stage(fields);
    }

    internal sealed class APromisesServiceAskingAForecast(Forecasting.Application.IAForecastPort forecast)
    {
        public int Promise(int days) => forecast.Forecast(days);
    }
}
