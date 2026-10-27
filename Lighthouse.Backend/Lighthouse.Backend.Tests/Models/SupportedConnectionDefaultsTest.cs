using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class SupportedConnectionDefaultsTest
    {
        private static readonly ConnectionDefaultSetting[] OfferedOnCsv =
        [
            ConnectionDefaultSetting.EstimationForTeams,
            ConnectionDefaultSetting.FeatureOwnerFieldForPortfolios,
            ConnectionDefaultSetting.EstimationForPortfolios,
        ];

        private static IEnumerable<TestCaseData> EveryTrackerAndSetting()
        {
            foreach (var tracker in Enum.GetValues<WorkTrackingSystems>())
            {
                foreach (var setting in Enum.GetValues<ConnectionDefaultSetting>())
                {
                    yield return new TestCaseData(tracker, setting, ExpectedHonoured(tracker, setting))
                        .SetName($"{tracker} × {setting}");
                }
            }
        }

        private static bool ExpectedHonoured(WorkTrackingSystems tracker, ConnectionDefaultSetting setting) => tracker switch
        {
            WorkTrackingSystems.Jira or WorkTrackingSystems.AzureDevOps => true,
            WorkTrackingSystems.Csv => OfferedOnCsv.Contains(setting),
            WorkTrackingSystems.Linear or WorkTrackingSystems.ServiceNow => false,
            _ => throw new InvalidOperationException($"Decide what a {tracker} connection offers as defaults."),
        };

        [TestCaseSource(nameof(EveryTrackerAndSetting))]
        public void A_tracker_honours_exactly_the_defaults_it_offers(WorkTrackingSystems tracker, ConnectionDefaultSetting setting, bool honoured)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(SupportedConnectionDefaults.Honours(tracker, setting), Is.EqualTo(honoured));
                Assert.That(SupportedConnectionDefaults.For(tracker).Contains(setting), Is.EqualTo(honoured));
            }
        }

        [TestCaseSource(nameof(EveryTracker))]
        public void A_tracker_offers_each_default_once(WorkTrackingSystems tracker)
        {
            Assert.That(SupportedConnectionDefaults.For(tracker), Is.Unique);
        }

        private static WorkTrackingSystems[] EveryTracker() => Enum.GetValues<WorkTrackingSystems>();
    }
}
