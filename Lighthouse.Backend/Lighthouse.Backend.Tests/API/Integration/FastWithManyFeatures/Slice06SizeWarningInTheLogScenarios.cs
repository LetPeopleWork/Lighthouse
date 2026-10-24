using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API.Integration.TaskManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures
{
    /// <summary>
    /// The warning is a nudge, never a block: the update that triggers it stores its Features exactly as it would
    /// without it.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-06")]
    [Category("us-06")]
    [Category("story-6410")]
    public class Slice06SizeWarningInTheLogScenarios : TaskManagerAcceptanceTest
    {
        private const string ThePortfolio = "Northwind Platform";

        private const string WhatTheWarningSaysOfEveryOversizedPortfolio = "Features after its update, more than 100.";

        private const string ProblemsRoute = "/api/latest/logs/problems";

        /// <summary>Recent Problems is fed by the host's own logger, so the captured sink has to sit beside it, not replace it.</summary>
        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            services.RemoveAll<ILoggerFactory>();
            services.AddSingleton<ILoggerFactory>(serviceProvider => new SerilogLoggerFactory(
                new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Logger(serviceProvider.GetRequiredService<Serilog.ILogger>())
                    .WriteTo.Sink(CapturedLogs)
                    .CreateLogger(),
                dispose: true));
        }

        [Test]
        [Ignore(PendingEpic6400.Slice06)]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task An_update_that_stores_101_Features_leaves_one_warning_naming_the_Portfolio_and_still_stores_them_all()
        {
            var portfolioId = SeedPortfolio(SeedConnection(), ThePortfolio);
            GivenTheTrackerReturnsFeatures(101);

            await ThePortfolioRefreshRuns(portfolioId);

            var theWarning = $"Portfolio '{ThePortfolio}' ({portfolioId}) holds 101 Features after its update, more than 100. "
                + "Its pages and forecasts get slower; narrowing its query keeps them fast.";
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CapturedLogs.WarningsAsWritten.Count(line => line.Contains(theWarning, StringComparison.Ordinal)), Is.EqualTo(1),
                    "The update should leave exactly this warning once. Warnings: " + string.Join(" | ", CapturedLogs.WarningsAsWritten));
                Assert.That(TheSizeWarningsAt(LogEventLevel.Warning), Is.EqualTo(1), "The line is a Warning, not Information.");
                Assert.That(TheSizeWarningsAt(LogEventLevel.Error) + TheSizeWarningsAt(LogEventLevel.Fatal), Is.Zero,
                    "A Portfolio that grew is not an error.");
                Assert.That(TheFeaturesStoredFor(portfolioId), Is.EqualTo(101), "The warning must never keep the update from storing its Features.");
            }
        }

        [Test]
        [Ignore(PendingEpic6400.Slice06)]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task The_size_warning_is_listed_once_in_Recent_Problems()
        {
            var portfolioId = SeedPortfolio(SeedConnection(), ThePortfolio);
            GivenTheTrackerReturnsFeatures(101);

            await ThePortfolioRefreshRuns(portfolioId);

            var problems = await TheRecentProblems();
            Assert.That(problems.Count(problem => (Text(problem, "message") ?? string.Empty).Contains(WhatTheWarningSaysOfEveryOversizedPortfolio, StringComparison.Ordinal)),
                Is.EqualTo(1), "Recent problems held: " + Describe(problems));
        }

        [Test]
        [Ignore(PendingEpic6400.Slice06)]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task An_update_that_fails_leaves_no_size_warning_even_for_a_Portfolio_that_held_101_Features()
        {
            var portfolioId = SeedPortfolio(SeedConnection(), ThePortfolio);
            GivenTheTrackerReturnsFeatures(101);
            await ThePortfolioRefreshRuns(portfolioId);
            Assert.That(TheSizeWarningsAt(LogEventLevel.Warning), Is.EqualTo(1),
                "The successful update before the failure warns, or the failure below proves nothing.");
            TheTrackerIsUnreachable(new InvalidOperationException("The tracker is down."));

            await ThePortfolioRefreshRuns(portfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheRecordedRefreshFor(RefreshType.Portfolio, portfolioId)?.Success, Is.False, "The second update was to fail.");
                Assert.That(TheSizeWarningsAt(LogEventLevel.Warning), Is.EqualTo(1),
                    "An update that stored nothing says nothing about the Portfolio's size. Warnings: " + string.Join(" | ", CapturedLogs.WarningsAsWritten));
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("edge")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task An_update_that_stores_exactly_100_Features_leaves_no_size_warning()
        {
            var portfolioId = SeedPortfolio(SeedConnection(), ThePortfolio);
            GivenTheTrackerReturnsFeatures(100);

            await ThePortfolioRefreshRuns(portfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheFeaturesStoredFor(portfolioId), Is.EqualTo(100), "The update stored its Features.");
                Assert.That(CapturedLogs.WarningsAsWritten, Has.None.Contains(WhatTheWarningSaysOfEveryOversizedPortfolio));
            }
        }

        private void GivenTheTrackerReturnsFeatures(int count)
        {
            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([.. Enumerable.Range(1, count).Select(index => new Feature(new WorkItemBase
                {
                    ReferenceId = $"NW-{index}",
                    Name = $"Feature {index}",
                    Type = "Epic",
                    State = "In Progress",
                    StateCategory = StateCategories.Doing,
                    Order = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ParentReferenceId = string.Empty,
                }))]);
        }

        private int TheSizeWarningsAt(LogEventLevel level)
            => CapturedLogs.At(level).Count(line => line.Contains(WhatTheWarningSaysOfEveryOversizedPortfolio, StringComparison.Ordinal));

        private async Task<List<JsonElement>> TheRecentProblems()
        {
            using var client = Factory.CreateClient();
            using var response = await client.GetAsync(new Uri(ProblemsRoute, UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{ProblemsRoute} answered {(int)response.StatusCode}: {body}");

            using var document = JsonDocument.Parse(body);
            return [.. document.RootElement.EnumerateArray().Select(element => element.Clone())];
        }

        private int TheFeaturesStoredFor(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)!.Features.Count;
        }
    }
}
