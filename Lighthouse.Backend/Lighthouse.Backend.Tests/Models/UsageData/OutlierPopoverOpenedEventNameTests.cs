using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.Tests.Models.UsageData
{
    /// <summary>
    /// The name is the contract. The event is appended after the one that came before it, so nothing already
    /// travelling changes its number, and it carries nothing beside its name.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class OutlierPopoverOpenedEventNameTests
    {
        [Test]
        public void The_event_is_appended_after_the_last_one_named_before_it()
        {
            Assert.That((int)UsageDataEventName.OutlierPopoverOpened, Is.EqualTo((int)UsageDataEventName.SystemWideAccessGranted + 1));
        }

        [Test]
        public void The_event_carries_nothing_beside_its_name()
        {
            var plain = new UsageDataEventReported(
                UsageDataEventName.OutlierPopoverOpened,
                Route: null,
                WorkTrackingSystem: null,
                OptionalFeature: null,
                Enabled: null,
                SizingMoment: null,
                OffsetMs: 0,
                Sequence: 0);

            Assert.That(UsageDataEventShapes.Fits(plain), Is.True);
        }
    }
}
