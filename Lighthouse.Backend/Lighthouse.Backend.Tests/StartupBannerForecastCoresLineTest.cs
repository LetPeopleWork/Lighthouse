using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Startup;
using Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Lighthouse.Backend.Tests
{
    /// <summary>
    /// The startup banner says how many cores a forecast may use, so an operator reading the log of a
    /// container capped to four CPUs can see that forecasts will take two of them. The numbers are the ones
    /// the instance itself resolved, which on this machine are its own core count and half of it.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-01")]
    [Category("us-01")]
    [Category("ac-01.2")]
    public class StartupBannerForecastCoresLineTest
    {
        [Test]
        [Ignore(PendingEpic6311.Slice01)]
        [Category("contract-shape:pure-function")]
        public void The_banner_says_how_many_of_the_cores_a_forecast_may_use()
        {
            var cores = Environment.ProcessorCount;
            var forecastsMayUse = Math.Max(1, cores / 2);

            var lines = StartupBanner.BuildInfoLines(new StartupBannerFacts(
                "1.2.3.4",
                ["http://localhost:8080"],
                "sqlite",
                "/app/logs",
                new ConfigurationBuilder().Build(),
                new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, new EncryptionKey("k-2026-10-18-01", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength))),
                new KeyStoreLocation("/app/Data/keys", KeyStoreCase.ExplicitKeyStorePath),
                false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines, Is.Not.Empty);
                Assert.That(lines, Has.Some.Contains($"Forecasts use up to {forecastsMayUse} of {cores} cores"),
                    "The banner does not say how many cores a forecast may use. It said: " + string.Join(" | ", lines));
            }
        }
    }
}
