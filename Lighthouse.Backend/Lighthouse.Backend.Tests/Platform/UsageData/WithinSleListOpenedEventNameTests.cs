using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Platform.UsageData;

namespace Lighthouse.Backend.Tests.Platform.UsageData
{
    /// <summary>
    /// The name is the contract. The event is appended after the one that came before it, so nothing already
    /// travelling changes its number, and it carries nothing beside its name.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class WithinSleListOpenedEventNameTests
    {
        [Test]
        public void The_event_keeps_the_number_31_it_travels_under()
        {
            Assert.That((int)UsageDataEventName.WithinSleListOpened, Is.EqualTo(31));
        }

        [Test]
        public void The_event_carries_nothing_beside_its_name()
        {
            var plain = new UsageDataEventReported(
                UsageDataEventName.WithinSleListOpened,
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
