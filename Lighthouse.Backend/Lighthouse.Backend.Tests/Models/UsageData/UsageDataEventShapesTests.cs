using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.Tests.Models.UsageData
{
    /// <summary>
    /// When a vote was cast is the one thing a sizing event says, and nothing else may say it. Checked
    /// over every name on the list, each otherwise in the shape it is sent in, so a name appended later
    /// is covered without anybody remembering to add it here.
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
