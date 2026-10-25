using System.Security.Cryptography;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Startup;
using Microsoft.Extensions.Configuration;

namespace Lighthouse.Backend.Tests
{
    /// <summary>
    /// The banner line is how an operator reading a shared log can tell that forecast rounds ran on their own
    /// threads, so its numbers are worked out here from the cores rather than read back from the code under test.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-01")]
    [Category("us-01")]
    [Category("story-6404")]
    [Category("kpi-OUT-6400-no-stall-during-rounds")]
    public class StartupBannerForecastLaneLineTest
    {
        [Test]
        [Category("contract-shape:pure-function")]
        public void The_banner_says_how_many_threads_forecast_rounds_run_on_and_how_many_cores_each_forecast_may_use()
        {
            var cores = Environment.ProcessorCount;
            var threads = Math.Max(1, cores / 2);

            var lines = TheBanner();

            Assert.That(lines, Has.Some.Contains($"Forecast rounds run on {TheThreads(threads)}; each forecast uses up to {threads} of {cores} cores"),
                "The banner does not say how many threads forecast rounds run on and how many cores each forecast may use. It said: " + string.Join(" | ", lines));
        }

        [Test]
        [Category("contract-shape:pure-function")]
        public void The_forecast_line_sits_in_the_banner_under_its_own_label_like_every_other_line()
        {
            var lines = TheBanner();

            Assert.That(lines, Has.Some.StartsWith($"🧮  {"Cores",-13} : Forecast rounds run on "),
                "The forecast line is not labelled like the others. The banner said: " + string.Join(" | ", lines));
        }

        [TestCase(1, "1 dedicated thread")]
        [TestCase(2, "2 dedicated threads")]
        [TestCase(7, "7 dedicated threads")]
        [Category("contract-shape:pure-function")]
        public void The_banner_names_one_thread_in_the_singular_and_several_in_the_plural(int threads, string said)
        {
            var lines = TheBanner(threads);

            Assert.That(lines, Has.Some.Contains($"Forecast rounds run on {said}; each forecast uses up to "),
                "The banner does not name the forecast threads it was handed. It said: " + string.Join(" | ", lines));
        }

        // One thread is named in the singular, as a one- or two-core host has it; which form a run checks follows
        // from the cores of the machine it runs on.
        private static string TheThreads(int threads) => threads == 1 ? "1 dedicated thread" : $"{threads} dedicated threads";

        private static List<string> TheBanner() => TheBanner(null);

        private static List<string> TheBanner(int? forecastThreads)
        {
            var facts = new StartupBannerFacts(
                "1.2.3.4",
                ["http://localhost:8080"],
                "sqlite",
                "/app/logs",
                new ConfigurationBuilder().Build(),
                new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, new EncryptionKey("k-2026-10-24-01", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength))),
                new KeyStoreLocation("/app/Data/keys", KeyStoreCase.ExplicitKeyStorePath),
                false);

            return StartupBanner.BuildInfoLines(forecastThreads is { } threads ? facts with { ForecastThreads = threads } : facts).ToList();
        }
    }
}
