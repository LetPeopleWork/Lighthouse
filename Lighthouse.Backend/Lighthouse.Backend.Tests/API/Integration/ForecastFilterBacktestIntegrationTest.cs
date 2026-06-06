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
using System.Runtime.CompilerServices;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class ForecastFilterBacktestIntegrationTest
    {
        private const int TeamId = 9101;

        private Mock<IForecastService> forecastServiceMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<BlackoutPeriod>> blackoutPeriodRepositoryMock;
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
            blackoutPeriodRepositoryMock = new Mock<IRepository<BlackoutPeriod>>();
            blackoutPeriodRepositoryMock.Setup(r => r.GetAll()).Returns([]);
            blackoutPeriodServiceMock = new Mock<IBlackoutPeriodService>();
            blackoutPeriodServiceMock.Setup(s => s.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([]);
            forecastUpdaterMock = new Mock<IForecastUpdater>();

            team = new Team { Id = TeamId, Name = "Premium-Backtest-Team" };
            teamRepositoryMock.Setup(r => r.GetById(TeamId)).Returns(team);

            forecastServiceMock
                .Setup(s => s.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()))
                .Returns(new HowManyForecast(new Dictionary<int, int> { { 10, 1 } }, 14));

            teamMetricsServiceMock
                .Setup(s => s.GetThroughputForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new RunChartData(new Dictionary<int, List<WorkItemBase>>()));

            teamMetricsServiceMock
                .Setup(s => s.GetThroughputForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<ThroughputFilterMode>()))
                .Returns(new RunChartData(new Dictionary<int, List<WorkItemBase>>()));

            subject = new ForecastController(
                forecastUpdaterMock.Object,
                forecastServiceMock.Object,
                teamRepositoryMock.Object,
                teamMetricsServiceMock.Object,
                blackoutPeriodRepositoryMock.Object,
                blackoutPeriodServiceMock.Object);
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

        [Test]
        public void Backtest_WindowContainsBlackoutDays_ForecastHorizonExcludesThem()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var windowStart = today.AddDays(-58);
            var windowEnd = windowStart.AddDays(28);
            var blackoutStart = windowStart.AddDays(10);
            blackoutPeriodRepositoryMock
                .Setup(r => r.GetAll())
                .Returns(BlackoutPeriods((blackoutStart, blackoutStart.AddDays(2))));
            var capturedHorizon = CaptureForecastHorizon();
            StubBlackoutAwareThroughput(ThroughputFilterMode.RespectTeamSetting);
            StubForecastStatus(ThroughputFilterMode.RespectTeamSetting, filterApplied: false, excludedSummary: null);

            RunBacktest(applyFilterOverride: null, windowStart, windowEnd);

            Assert.That(capturedHorizon.Value, Is.EqualTo(25));
        }

        [Test]
        public void Backtest_WindowWithoutBlackoutDays_ForecastHorizonIsCalendarSpan()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var windowStart = today.AddDays(-58);
            var windowEnd = windowStart.AddDays(28);
            var capturedHorizon = CaptureForecastHorizon();
            StubBlackoutAwareThroughput(ThroughputFilterMode.RespectTeamSetting);
            StubForecastStatus(ThroughputFilterMode.RespectTeamSetting, filterApplied: false, excludedSummary: null);

            RunBacktest(applyFilterOverride: null, windowStart, windowEnd);

            Assert.That(capturedHorizon.Value, Is.EqualTo(28));
        }

        private StrongBox<int> CaptureForecastHorizon()
        {
            var capturedHorizon = new StrongBox<int>(-1);
            forecastServiceMock
                .Setup(s => s.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()))
                .Callback<RunChartData, int>((_, days) => capturedHorizon.Value = days)
                .Returns(new HowManyForecast(new Dictionary<int, int> { { 10, 1 } }, 14));
            return capturedHorizon;
        }

        private static List<BlackoutPeriod> BlackoutPeriods(params (DateOnly Start, DateOnly End)[] periods)
        {
            return periods
                .Select(p => new BlackoutPeriod { Start = p.Start, End = p.End })
                .ToList();
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
            return RunBacktest(applyFilterOverride, today.AddDays(-60), today.AddDays(-30));
        }

        private BacktestResultDto RunBacktest(bool? applyFilterOverride, DateOnly windowStart, DateOnly windowEnd)
        {
            var input = new BacktestInputDto
            {
                StartDate = windowStart,
                EndDate = windowEnd,
                HistoricalStartDate = windowStart.AddDays(-60),
                HistoricalEndDate = windowStart,
                ApplyFilterOverride = applyFilterOverride,
            };

            var response = subject.RunBacktest(TeamId, input);

            Assert.That(response.Result, Is.InstanceOf<OkObjectResult>(), "Controller did not return Ok");
            var okResult = (OkObjectResult)response.Result!;
            return (BacktestResultDto)okResult.Value!;
        }
    }
}
