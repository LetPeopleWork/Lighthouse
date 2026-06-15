using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    [Category("recurring-blackout-events")]
    public class RecurringBlackoutRulesDownstreamParityIntegrationTest : RecurringBlackoutRulesTestBase
    {
        [SetUp]
        public void Init()
        {
            StartApplicationWithDeterministicForecast();
        }

        [TearDown]
        public void Cleanup()
        {
            StopApplication();
        }

        [Test]
        public async Task WhenForecast_RecurringRuleDay_ProducesSamePercentileDateAsAnEquivalentOneOffPeriod()
        {
            var firstBlackoutDay = DateOnly.FromDateTime(Today.AddDays(3));
            var secondBlackoutDay = DateOnly.FromDateTime(Today.AddDays(4));

            var unshiftedDate = Today.AddDays(WorkingDaysAtAllPercentiles);
            var dateShiftedByTwoBlackoutDays = Today.AddDays(WorkingDaysAtAllPercentiles + 2);

            ConfigureOneOffBlackoutPeriod(firstBlackoutDay, secondBlackoutDay);
            var oneOffDate = await PercentileDate(85, remainingItems: 5);

            RemoveAllOneOffPeriods();
            await ConfigureRecurringRuleCovering(firstBlackoutDay, secondBlackoutDay);
            var recurringDate = await PercentileDate(85, remainingItems: 5);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(oneOffDate, Is.EqualTo(dateShiftedByTwoBlackoutDays),
                    "Two one-off blackout days inside the forecast window must push the percentile date out by exactly two calendar days.");
                Assert.That(oneOffDate, Is.Not.EqualTo(unshiftedDate),
                    "Guards parity against vacuity: the one-off run must genuinely differ from the unshifted baseline.");
                Assert.That(recurringDate, Is.EqualTo(oneOffDate),
                    "A recurring-rule day must shift the percentile date identically to a one-off blackout period (D4 unified evaluation).");
            }
        }

        [Test]
        public async Task WhenForecast_NoRecurringRulesAndNoOneOffPeriods_PercentileDateIsUnshifted()
        {
            var expectedDate = await PercentileDate(85, remainingItems: 5);

            Assert.That(expectedDate, Is.EqualTo(Today.AddDays(WorkingDaysAtAllPercentiles)),
                "With no rules and no periods every surface is byte-identical to pre-feature (inherits #4974 D6).");
        }

        private async Task ConfigureRecurringRuleCovering(DateOnly firstDay, DateOnly secondDay)
        {
            Client.AsSystemAdmin();
            var rule = new
            {
                weekdays = new[] { firstDay.DayOfWeek.ToString(), secondDay.DayOfWeek.ToString() },
                intervalWeeks = 1,
                start = firstDay.ToString("yyyy-MM-dd"),
                end = secondDay.ToString("yyyy-MM-dd"),
                description = "Equivalent recurring window",
            };

            var response = await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                await response.Content.ReadAsStringAsync());
        }

        private void RemoveAllOneOffPeriods()
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider
                .GetRequiredService<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Lighthouse.Backend.Models.BlackoutPeriod>>();
            foreach (var period in repository.GetAll().ToList())
            {
                repository.Remove(period);
            }
            repository.Save().GetAwaiter().GetResult();
        }

        private async Task<DateTime> PercentileDate(int probability, int remainingItems)
        {
            Client.AsTeamViewer(SeededTeamId);

            var input = new { RemainingItems = remainingItems };
            var response = await Client.PostAsJsonAsync($"/api/latest/forecast/manual/{SeededTeamId}", input);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("whenForecasts")
                .EnumerateArray()
                .Single(p => p.GetProperty("probability").GetInt32() == probability)
                .GetProperty("expectedDate").GetDateTime();
        }
    }
}
