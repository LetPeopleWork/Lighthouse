using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Startup
{
    /// <summary>
    /// The configuration boundary in front of the recent-problems buffer.
    ///
    /// The sink itself refuses to be built with room for no problems, and
    /// <c>RecentProblemsSinkTest</c> pins that. What this pins is the other half: the number arrives from
    /// a settings file nobody validated, and a typo there must not be the reason the whole instance will
    /// not start — least of all for the sake of a buffer whose only job is to make trouble easier to
    /// notice. A misconfigured instance starts, and it still notices trouble.
    ///
    /// The instance is really booted, because the boundary being tested is only reached that way: the
    /// buffer's size is read while the logger is built, on the way in to <c>builder.Build()</c>. That is
    /// also why the override is <c>UseSetting</c> rather than <c>ConfigureAppConfiguration</c> — sources
    /// added the other way are not applied until the build itself, so they arrive after the sink already
    /// exists and the override silently does nothing.
    ///
    /// Both the logger and the buffer are taken from the running host's own container rather than from
    /// Serilog's static <c>Log.Logger</c>, which every other booting fixture in this assembly overwrites.
    /// </summary>
    [Category("epic-5511-task-manager")]
    [Category("slice-06")]
    public class RecentProblemsCapacityConfigurationTest
    {
        private const string TheCapacitySetting = "RecentProblems:Capacity";

        private const string TheFirstThingThatWentWrong = "the first thing that went wrong";

        private const string TheSecondThingThatWentWrong = "the second thing that went wrong";

        // Two values a settings file plausibly ends up holding, either of which the sink would refuse.
        // Both are retained rather than one, so that falling back to room for a single problem — which
        // would start just as happily — cannot pass this.
        [TestCase("0")]
        [TestCase("-5")]
        public void ConfigureLogging_ACapacityNoInstanceCouldUse_StartsAnywayAndStillRetainsProblems(string theTypo)
        {
            using var lighthouse = new TestWebApplicationFactory<Backend.Program>();

            var misconfigured = lighthouse.WithWebHostBuilder(
                builder => builder.UseSetting(TheCapacitySetting, theTypo));

            var logger = misconfigured.Services.GetRequiredService<Serilog.ILogger>();
            logger.Warning(TheFirstThingThatWentWrong);
            logger.Warning(TheSecondThingThatWentWrong);

            var retained = misconfigured.Services
                .GetRequiredService<IRecentProblems>()
                .MostRecentFirst()
                .Select(problem => problem.Message)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(retained, Does.Contain(TheFirstThingThatWentWrong));
                Assert.That(retained, Does.Contain(TheSecondThingThatWentWrong));
            }
        }
    }
}
