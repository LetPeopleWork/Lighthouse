using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class ForecastFilterTeamForecastIntegrationTest
    {
        private const int TeamId = 9001;
        private const string BugExclusionWarning = "Filter excluded all throughput; showing unfiltered forecast";

        private Mock<IForecastService> forecastServiceMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IBlackoutPeriodService> blackoutPeriodServiceMock;
        private Mock<IForecastUpdater> forecastUpdaterMock;
        private Team team;
        private ForecastController subject;

        [SetUp]
        public void Setup()
        {
            forecastServiceMock = new Mock<IForecastService>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            blackoutPeriodServiceMock = new Mock<IBlackoutPeriodService>();
            blackoutPeriodServiceMock.Setup(s => s.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([]);
            forecastUpdaterMock = new Mock<IForecastUpdater>();

            team = new Team { Id = TeamId, Name = "Premium-Team" };
            teamRepositoryMock.Setup(r => r.GetById(TeamId)).Returns(team);

            forecastServiceMock
                .Setup(s => s.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()))
                .Returns(new HowManyForecast(new Dictionary<int, int> { { 10, 1 } }, 7));

            forecastServiceMock
                .Setup(s => s.When(It.IsAny<Team>(), It.IsAny<int>(), It.IsAny<ThroughputFilterMode>()))
                .ReturnsAsync((Team _, int items, ThroughputFilterMode mode) =>
                {
                    var status = teamMetricsServiceMock.Object.GetForecastThroughputStatus(team, mode);
                    return new WhenForecast
                    {
                        NumberOfItems = items,
                        FilterApplied = status.FilterApplied,
                        ExcludedSummary = status.ExcludedSummary,
                    };
                });

            subject = new ForecastController(
                forecastUpdaterMock.Object,
                forecastServiceMock.Object,
                teamRepositoryMock.Object,
                teamMetricsServiceMock.Object,
                blackoutPeriodServiceMock.Object);
        }

        [Test]
        public async Task HowMany_PremiumTenantTeamWithFilterApplyOverrideFalse_ReturnsUnfilteredForecast()
        {
            StubForecastStatus(ThroughputFilterMode.SkipFilter, filterApplied: false, excludedSummary: null);

            await RunManualForecast(applyFilterOverride: false, targetDate: DateTime.Today.AddDays(14));

            teamMetricsServiceMock.Verify(s => s.GetForecastThroughputStatus(team, ThroughputFilterMode.SkipFilter), Times.Once);
        }

        [Test]
        public async Task HowMany_PremiumTenantTeamWithFilterApplyOverrideTrue_ReturnsFilteredForecast()
        {
            StubForecastStatus(ThroughputFilterMode.ApplyFilter, filterApplied: true, excludedSummary: "Excluded 3 work items via team forecast filter");

            await RunManualForecast(applyFilterOverride: true, targetDate: DateTime.Today.AddDays(14));

            teamMetricsServiceMock.Verify(s => s.GetForecastThroughputStatus(team, ThroughputFilterMode.ApplyFilter), Times.Once);
        }

        [Test]
        public async Task HowMany_PremiumTenantTeamWithFilterApplyOverrideOmitted_DefaultsToFilteredViaTeamSetting()
        {
            StubForecastStatus(ThroughputFilterMode.RespectTeamSetting, filterApplied: true, excludedSummary: "Excluded 2 work items via team forecast filter");

            await RunManualForecast(applyFilterOverride: null, targetDate: DateTime.Today.AddDays(14));

            teamMetricsServiceMock.Verify(s => s.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting), Times.Once);
        }

        [Test]
        public async Task HowMany_PremiumTenantTeamWithoutFilter_IgnoresOverrideAndReturnsUnfilteredForecast()
        {
            StubForecastStatus(ThroughputFilterMode.ApplyFilter, filterApplied: false, excludedSummary: null);

            var result = await RunManualForecast(applyFilterOverride: true, targetDate: DateTime.Today.AddDays(14));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FilterApplied, Is.False);
                Assert.That(result.ExcludedSummary, Is.Null);
            }
        }

        [Test]
        public async Task HowMany_ResponsePayload_IncludesFilterAppliedAndExcludedSummary()
        {
            const string summary = "Excluded 5 work items via team forecast filter";
            StubForecastStatus(ThroughputFilterMode.ApplyFilter, filterApplied: true, excludedSummary: summary);

            var result = await RunManualForecast(applyFilterOverride: true, targetDate: DateTime.Today.AddDays(14));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FilterApplied, Is.True);
                Assert.That(result.ExcludedSummary, Is.EqualTo(summary));
            }
        }

        [Test]
        public async Task When_PremiumTenantTeamWithFilterApplyOverrideFalse_ReturnsUnfilteredForecast()
        {
            StubForecastStatus(ThroughputFilterMode.SkipFilter, filterApplied: false, excludedSummary: null);

            await RunManualForecast(applyFilterOverride: false, remainingItems: 12, targetDate: null);

            forecastServiceMock.Verify(s => s.When(team, 12, ThroughputFilterMode.SkipFilter), Times.Once);
        }

        private void StubForecastStatus(ThroughputFilterMode mode, bool filterApplied, string? excludedSummary)
        {
            var runChart = new RunChartData(new Dictionary<int, List<WorkItemBase>>());
            var status = new ForecastThroughputStatus(runChart, filterApplied, excludedSummary);
            teamMetricsServiceMock.Setup(s => s.GetForecastThroughputStatus(team, mode)).Returns(status);
        }

        private async Task<ManualForecastDto> RunManualForecast(bool? applyFilterOverride, int? remainingItems = null, DateTime? targetDate = null)
        {
            var input = new ForecastController.ManualForecastInputDto
            {
                RemainingItems = remainingItems,
                TargetDate = targetDate,
                ApplyFilterOverride = applyFilterOverride,
            };

            var response = await subject.RunManualForecastAsync(TeamId, input);

            Assert.That(response.Result, Is.InstanceOf<OkObjectResult>(), "Controller did not return Ok");
            var okResult = (OkObjectResult)response.Result!;
            return (ManualForecastDto)okResult.Value!;
        }
    }
}
