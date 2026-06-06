using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    [NonParallelizable]
    [Category("recurring-blackout-events")]
    public class RecurringBlackoutRulesByDateForecastIntegrationTest : RecurringBlackoutRulesTestBase
    {
        private const int CalendarDaysToTarget = 14;

        [SetUp]
        public void Init()
        {
            StartApplicationWithDeterministicForecast();
            UseLikelihoodSensitiveToWorkingDays();
        }

        [TearDown]
        public void Cleanup()
        {
            StopApplication();
        }

        [Test]
        public async Task ByDateLikelihood_RecurringRuleDay_ShiftsIdenticallyToOneOffPeriodAndUnchangedWithoutRules()
        {
            var firstBlackoutDay = DateOnly.FromDateTime(Today.AddDays(3));
            var secondBlackoutDay = DateOnly.FromDateTime(Today.AddDays(4));

            var noRuleLikelihood = await ByDateLikelihood();

            ConfigureOneOffBlackoutPeriod(firstBlackoutDay, secondBlackoutDay);
            var oneOffLikelihood = await ByDateLikelihood();

            RemoveAllOneOffPeriods();
            await ConfigureRecurringRuleCovering(firstBlackoutDay, secondBlackoutDay);
            var recurringLikelihood = await ByDateLikelihood();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(oneOffLikelihood, Is.LessThan(noRuleLikelihood),
                    "Two blackout days between today and the target remove two working days, so the by-date likelihood at a fixed distribution must drop below the no-rule baseline.");
                Assert.That(recurringLikelihood, Is.EqualTo(oneOffLikelihood),
                    "A recurring-rule day must shift the by-date likelihood identically to an equivalent one-off period (D4 unified evaluation through GetEffectiveBlackoutDays).");
            }
        }

        private void UseLikelihoodSensitiveToWorkingDays()
        {
            ForecastServiceMock
                .Setup(s => s.When(It.IsAny<Team>(), It.IsAny<int>(), It.IsAny<ThroughputFilterMode>()))
                .ReturnsAsync(ForecastWithUniformWorkingDayDistribution());

            ForecastServiceMock
                .Setup(s => s.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()))
                .Returns(new HowManyForecast());
        }

        private static WhenForecast ForecastWithUniformWorkingDayDistribution()
        {
            var simulation = new SimulationResult();
            for (var workingDays = 1; workingDays <= 30; workingDays++)
            {
                simulation.SimulationResults[workingDays] = 1;
            }

            return new WhenForecast(simulation) { HasSufficientData = true };
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
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<BlackoutPeriod>>();
            foreach (var period in repository.GetAll().ToList())
            {
                repository.Remove(period);
            }
            repository.Save().GetAwaiter().GetResult();
        }

        private async Task<double> ByDateLikelihood()
        {
            Client.AsTeamViewer(SeededTeamId);

            var input = new
            {
                RemainingItems = 5,
                TargetDate = Today.AddDays(CalendarDaysToTarget),
            };
            var response = await Client.PostAsJsonAsync($"/api/latest/forecast/manual/{SeededTeamId}", input);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("likelihood").GetDouble();
        }
    }
}
