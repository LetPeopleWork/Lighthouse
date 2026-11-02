using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize
{
    /// <summary>
    /// The host outlives the test, and so does the counter of database commands it carries. Each test
    /// asks the same thing of it: nothing is counted when I begin, and asking costs commands. Whichever
    /// runs second would find the first one's commands still there if the counter were not emptied
    /// between tests.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    public class CommandCountIsolationTest : TeamFeatureSizeAcceptanceTest
    {
        [Test]
        public async Task A_test_that_asks_for_the_Feature_sizes_starts_with_no_commands_counted()
        {
            Assert.That(CommandsSentSoFar, Is.Zero, "A previous test's commands were still counted.");

            var gravity = TeamNamed(Gravity);
            Client.AsSystemAdmin();
            await Ask(TeamMetric(gravity, AllFeaturesForSizeChart, WindowStart, WindowEnd));

            Assert.That(CommandsSentSoFar, Is.GreaterThan(0), "Asking should have reached the database.");
        }

        [Test]
        public async Task A_test_that_asks_for_the_size_percentiles_starts_with_no_commands_counted()
        {
            Assert.That(CommandsSentSoFar, Is.Zero, "A previous test's commands were still counted.");

            var gravity = TeamNamed(Gravity);
            Client.AsSystemAdmin();
            await Ask(TeamMetric(gravity, SizePercentiles, WindowStart, WindowEnd));

            Assert.That(CommandsSentSoFar, Is.GreaterThan(0), "Asking should have reached the database.");
        }
    }
}
