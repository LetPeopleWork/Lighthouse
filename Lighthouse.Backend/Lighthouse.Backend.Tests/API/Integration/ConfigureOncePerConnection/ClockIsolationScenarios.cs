namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// The host outlives the test, and so does the clock it reads. A scenario that lets a minute pass
    /// moves that clock; the next scenario must still begin at the instant every scenario begins at.
    /// Whichever of these two runs second would find the first one's minute still on the clock if it
    /// were not put back between tests.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    public class ClockIsolationScenarios : NamedDependencyLinkAcceptanceTest
    {
        [Test]
        public void A_scenario_that_lets_a_minute_pass_begins_at_the_instant_every_scenario_begins_at()
        {
            Assert.That(TheHostClockNow, Is.EqualTo(TestToday.FrozenNow), "A previous scenario's minutes were still on the clock.");

            AMinutePasses();

            Assert.That(TheHostClockNow, Is.EqualTo(TestToday.FrozenNow.AddMinutes(1)));
        }

        [Test]
        public void Another_scenario_that_lets_a_minute_pass_begins_at_the_same_instant()
        {
            Assert.That(TheHostClockNow, Is.EqualTo(TestToday.FrozenNow), "A previous scenario's minutes were still on the clock.");

            AMinutePasses();

            Assert.That(TheHostClockNow, Is.EqualTo(TestToday.FrozenNow.AddMinutes(1)));
        }
    }
}
