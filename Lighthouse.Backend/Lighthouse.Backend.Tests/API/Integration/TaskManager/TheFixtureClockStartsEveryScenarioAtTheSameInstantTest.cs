using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// The host and its clock outlive a scenario, so a scenario that moves the clock must not hand the next
    /// one a later "now". Both tests expect the starting instant and then move the clock, so whichever runs
    /// second fails if the first one's move is still visible.
    /// </summary>
    [TestFixture]
    public class TheFixtureClockStartsEveryScenarioAtTheSameInstantTest : TaskManagerHostPerFixtureAcceptanceTest
    {
        [Test]
        public void A_scenario_finds_the_clock_at_the_starting_instant_and_moves_it_forward()
        {
            Assert.That(RootFactory.Clock.GetUtcNow(), Is.EqualTo(TestToday.FrozenNow));

            RootFactory.Clock.Advance(TimeSpan.FromDays(3));
        }

        [Test]
        public void The_next_scenario_finds_the_clock_at_the_starting_instant_too()
        {
            Assert.That(RootFactory.Clock.GetUtcNow(), Is.EqualTo(TestToday.FrozenNow));

            RootFactory.Clock.Advance(TimeSpan.FromHours(5));
        }
    }
}
