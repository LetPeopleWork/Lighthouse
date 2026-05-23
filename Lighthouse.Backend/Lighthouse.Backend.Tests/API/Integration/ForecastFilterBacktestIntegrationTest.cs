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
    public class ForecastFilterBacktestIntegrationTest
    {
        private const int TeamId = 9101;

        private Mock<IForecastService> forecastServiceMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IForecastUpdater> forecastUpdaterMock;
        private Team team;
        private ForecastController subject;

        [SetUp]
        public void Setup()
        {
            forecastServiceMock = new Mock<IForecastService>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            forecastUpdaterMock = new Mock<IForecastUpdater>();

            team = new Team { Id = TeamId, Name = "Premium-Backtest-Team" };
            teamRepositoryMock.Setup(r => r.GetById(TeamId)).Returns(team);

            forecastServiceMock
                .Setup(s => s.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()))
                .Returns(new HowManyForecast(new Dictionary<int, int> { { 10, 1 } }, 14));

            teamMetricsServiceMock
                .Setup(s => s.GetThroughputForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new RunChartData(new Dictionary<int, List<WorkItemBase>>()));

            subject = new ForecastController(
                forecastUpdaterMock.Object,
                forecastServiceMock.Object,
                teamRepositoryMock.Object,
                teamMetricsServiceMock.Object);
        }

        [Test]
        public void Backtest_PremiumTenantTeamWithFilterApplyOverrideFalse_RunsAgainstUnfilteredHistoricalThroughput()
        {
            StubBlackoutAwareThroughput(ThroughputFilterMode.SkipFilter);
            StubForecastStatus(ThroughputFilterMode.SkipFilter, filterApplied: false, excludedSummary: null);

            RunBacktest(applyFilterOverride: false);

            teamMetricsServiceMock.Verify(
                s => s.GetBlackoutAwareThroughputForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>(), ThroughputFilterMode.SkipFilter),
                Times.Once);
        }

        [Test]
        public void Backtest_PremiumTenantTeamWithFilterApplyOverrideTrue_RunsAgainstFilteredHistoricalThroughput()
        {
            StubBlackoutAwareThroughput(ThroughputFilterMode.ApplyFilter);
            StubForecastStatus(ThroughputFilterMode.ApplyFilter, filterApplied: true, excludedSummary: "Excluded 3 work items via team forecast filter");

            RunBacktest(applyFilterOverride: true);

            teamMetricsServiceMock.Verify(
                s => s.GetBlackoutAwareThroughputForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>(), ThroughputFilterMode.ApplyFilter),
                Times.Once);
        }

        [Test]
        public void Backtest_PremiumTenantTeamWithFilterApplyOverrideOmitted_DefaultsToFilteredViaTeamSetting()
        {
            StubBlackoutAwareThroughput(ThroughputFilterMode.RespectTeamSetting);
            StubForecastStatus(ThroughputFilterMode.RespectTeamSetting, filterApplied: true, excludedSummary: "Excluded 2 work items via team forecast filter");

            RunBacktest(applyFilterOverride: null);

            teamMetricsServiceMock.Verify(
                s => s.GetBlackoutAwareThroughputForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>(), ThroughputFilterMode.RespectTeamSetting),
                Times.Once);
        }

        [Test]
        public void Backtest_ResultDto_IncludesFilterAppliedAndExcludedSummaryForChip()
        {
            const string summary = "Excluded 5 work items via team forecast filter";
            StubBlackoutAwareThroughput(ThroughputFilterMode.ApplyFilter);
            StubForecastStatus(ThroughputFilterMode.ApplyFilter, filterApplied: true, excludedSummary: summary);

            var result = RunBacktest(applyFilterOverride: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FilterApplied, Is.True);
                Assert.That(result.ExcludedSummary, Is.EqualTo(summary));
            }
        }

        [Test]
        public void Backtest_PremiumTenantTeamWithoutFilter_IgnoresOverrideAndReturnsUnfilteredResult()
        {
            StubBlackoutAwareThroughput(ThroughputFilterMode.ApplyFilter);
            StubForecastStatus(ThroughputFilterMode.ApplyFilter, filterApplied: false, excludedSummary: null);

            var result = RunBacktest(applyFilterOverride: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FilterApplied, Is.False);
                Assert.That(result.ExcludedSummary, Is.Null);
            }
        }

        private void StubBlackoutAwareThroughput(ThroughputFilterMode mode)
        {
            teamMetricsServiceMock
                .Setup(s => s.GetBlackoutAwareThroughputForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>(), mode))
                .Returns(new RunChartData(new Dictionary<int, List<WorkItemBase>>()));
        }

        private void StubForecastStatus(ThroughputFilterMode mode, bool filterApplied, string? excludedSummary)
        {
            var runChart = new RunChartData(new Dictionary<int, List<WorkItemBase>>());
            var status = new ForecastThroughputStatus(runChart, filterApplied, excludedSummary);
            teamMetricsServiceMock.Setup(s => s.GetForecastThroughputStatus(team, mode)).Returns(status);
        }

        private BacktestResultDto RunBacktest(bool? applyFilterOverride)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var input = new BacktestInputDto
            {
                StartDate = today.AddDays(-60),
                EndDate = today.AddDays(-30),
                HistoricalStartDate = today.AddDays(-120),
                HistoricalEndDate = today.AddDays(-60),
                ApplyFilterOverride = applyFilterOverride,
            };

            var response = subject.RunBacktest(TeamId, input);

            Assert.That(response.Result, Is.InstanceOf<OkObjectResult>(), "Controller did not return Ok");
            var okResult = (OkObjectResult)response.Result!;
            return (BacktestResultDto)okResult.Value!;
        }
    }
}
