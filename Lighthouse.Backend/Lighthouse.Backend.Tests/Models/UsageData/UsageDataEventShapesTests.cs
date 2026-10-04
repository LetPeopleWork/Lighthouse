using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.Tests.Models.UsageData
{
    /// <summary>
    /// When a vote was cast is the one thing a sizing event says, and nothing else may say it. Checked
    /// over every name on the list, each otherwise in the shape it is sent in, so a name appended later
    /// is covered without anybody remembering to add it here. The other parts are judged alongside it,
    /// so an event short of any one of them must still not fit.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("epic-5510-5881-refinement")]
    public class UsageDataEventShapesTests
    {
        private static readonly UsageDataEventName[] EveryName = Enum.GetValues<UsageDataEventName>();

        [TestCaseSource(nameof(EveryName))]
        public void A_sizing_moment_is_carried_exactly_by_a_sizing_event(UsageDataEventName name)
        {
            var isASizingEvent = name is UsageDataEventName.TeamSizingVoteCast or UsageDataEventName.TeamSizingReadinessReached;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(UsageDataEventShapes.Fits(InItsOwnShape(name, UsageDataSizingMoment.NoCadence)), Is.EqualTo(isASizingEvent),
                    $"{name} carrying a sizing moment");
                Assert.That(UsageDataEventShapes.Fits(InItsOwnShape(name, sizingMoment: null)), Is.EqualTo(!isASizingEvent),
                    $"{name} without a sizing moment");
            }
        }

        private static IEnumerable<TestCaseData> EventsMissingOrMisplacingAPart()
        {
            yield return new TestCaseData(InItsOwnShape(UsageDataEventName.TeamTabOpened, null) with { Route = UsageDataRouteKey.PortfolioDetail_Metrics })
                .SetName("A Team tab naming a Portfolio page");
            yield return new TestCaseData(InItsOwnShape(UsageDataEventName.PortfolioTabOpened, null) with { Route = UsageDataRouteKey.TeamDetail_Metrics })
                .SetName("A Portfolio tab naming a Team page");
            yield return new TestCaseData(InItsOwnShape(UsageDataEventName.TeamTabOpened, null) with { Route = null })
                .SetName("A Team tab naming no page");
            yield return new TestCaseData(InItsOwnShape(UsageDataEventName.WorkTrackingSystemConnected, null) with { WorkTrackingSystem = null })
                .SetName("A connection naming no kind of system");
            yield return new TestCaseData(InItsOwnShape(UsageDataEventName.OptionalFeatureToggled, null) with { OptionalFeature = null })
                .SetName("A switch naming its direction but not the setting");
            yield return new TestCaseData(InItsOwnShape(UsageDataEventName.OptionalFeatureToggled, null) with { Enabled = null })
                .SetName("A switch naming the setting but not its direction");
        }

        [TestCaseSource(nameof(EventsMissingOrMisplacingAPart))]
        public void An_event_missing_or_misplacing_one_of_its_other_parts_does_not_fit(UsageDataEventReported reported)
        {
            Assert.That(UsageDataEventShapes.Fits(reported), Is.False);
        }

        private static UsageDataEventReported InItsOwnShape(UsageDataEventName name, UsageDataSizingMoment? sizingMoment)
        {
            var nameOnly = new UsageDataEventReported(
                name,
                Route: null,
                WorkTrackingSystem: null,
                OptionalFeature: null,
                Enabled: null,
                sizingMoment,
                OffsetMs: 0,
                Sequence: 0);

            return name switch
            {
                UsageDataEventName.TeamTabOpened => nameOnly with { Route = UsageDataRouteKey.TeamDetail_Metrics },
                UsageDataEventName.PortfolioTabOpened => nameOnly with { Route = UsageDataRouteKey.PortfolioDetail_Metrics },
                UsageDataEventName.WorkTrackingSystemConnected => nameOnly with { WorkTrackingSystem = UsageDataWorkTrackingSystem.Jira },
                UsageDataEventName.OptionalFeatureToggled => nameOnly with { OptionalFeature = UsageDataOptionalFeature.FeatureOrder, Enabled = true },
                _ => nameOnly,
            };
        }
    }
}
