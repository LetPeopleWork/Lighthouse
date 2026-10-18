using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Startup;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace Lighthouse.Backend.Tests
{
    /// <summary>
    /// The startup banner says how many cores a forecast may use, so an operator reading the log of a
    /// container capped to four CPUs can see that forecasts will take two of them. The numbers are the ones
    /// the instance itself resolved: the core count this machine reports, and the share the forecast limits
    /// the application registers allow. How that share follows from the cores is pinned, core count by core
    /// count, next to the forecast itself.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-01")]
    [Category("us-01")]
    [Category("ac-01.2")]
    public class StartupBannerForecastCoresLineTest
    {
        [Test]
        [Category("contract-shape:pure-function")]
        public void The_banner_says_how_many_of_the_cores_a_forecast_may_use()
        {
            var cores = Environment.ProcessorCount;
            var forecastsMayUse = ForecastSimulationLimits.Default.MostRunsAtOnce;

            var lines = TheBanner();

            Assert.That(lines, Has.Some.Contains($"Forecasts use up to {forecastsMayUse} of {cores} cores"),
                "The banner does not say how many cores a forecast may use. It said: " + string.Join(" | ", lines));
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void The_cores_line_sits_in_the_banner_under_its_own_label_like_every_other_line()
        {
            var lines = TheBanner();

            Assert.That(lines, Has.Some.StartsWith($"🧮  {"Cores",-13} : Forecasts use up to "),
                "The cores line is not labelled like the others. The banner said: " + string.Join(" | ", lines));
        }

        private static List<string> TheBanner()
        {
            return StartupBanner.BuildInfoLines(new StartupBannerFacts(
                "1.2.3.4",
                ["http://localhost:8080"],
                "sqlite",
                "/app/logs",
                new ConfigurationBuilder().Build(),
                new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, new EncryptionKey("k-2026-10-18-01", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength))),
                new KeyStoreLocation("/app/Data/keys", KeyStoreCase.ExplicitKeyStorePath),
                false)).ToList();
        }

        /// <summary>
        /// The banner reads the shared default limits rather than asking the container, so it is only truthful
        /// while the application registers that very instance for its forecasts.
        /// </summary>
        [Test]
        public void The_limits_the_banner_reads_are_the_ones_the_application_forecasts_with()
        {
            using var factory = new TestWebApplicationFactory<Program>();

            var registered = factory.Services.GetRequiredService<ForecastSimulationLimits>();

            Assert.That(registered, Is.SameAs(ForecastSimulationLimits.Default),
                "The application forecasts with other limits than the ones the startup banner reports.");
        }
    }
}
