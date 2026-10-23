using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    /// <summary>
    /// What a Portfolio's recorded forecast inputs and its Features' confirmation stamps do on a real
    /// relational database. These writes are single statements whose answer is the number of rows they
    /// touched, which the in-memory provider cannot run, and the order they race in is the whole point:
    /// a person who asks for a fresh forecast while one is running has to win, and a Portfolio saved whole
    /// from an old copy must never put back a value that was already replaced.
    ///
    /// Every step runs on its own context, as each request and each background round does in the app.
    /// </summary>
    [TestFixture]
    [Category("story-6354-skip-unchanged-forecasts")]
    public class ForecastConfirmationRepositoryRelationalTest
    {
        private const string WhatTheTenOClockRunRead = "3f1c0a9e5b7d4c2e8a6f1b0d9c7e5a31";

        private const string WhatTheTenFifteenRunRead = "9b2e7d5c3a1f4e6b8d0c2a4f6e8b0d13";

        private static readonly DateTime ComputedAtSix = new(2026, 10, 23, 6, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime ConfirmedAtNineFortyFive = new(2026, 10, 23, 9, 45, 0, DateTimeKind.Utc);

        private SqliteConnection connection = null!;
        private DbContextOptions<LighthouseAppContext> options = null!;
        private Mock<ICryptoService> crypto = null!;

        [SetUp]
        public void SetUp()
        {
            crypto = new Mock<ICryptoService>();
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            options = new DbContextOptionsBuilder<LighthouseAppContext>().UseSqlite(connection).Options;

            using var context = AContext();
            context.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            connection.Dispose();
        }

        [Test]
        [Category("slice-01")]
        [Category("contract-shape:bounded-change")]
        public async Task A_first_record_succeeds_and_the_next_one_has_to_start_from_it()
        {
            var portfolioId = await APortfolio("Payments Platform");

            var first = await Record(portfolioId, null, WhatTheTenOClockRunRead);
            var fromAValueNoLongerStored = await Record(portfolioId, null, WhatTheTenFifteenRunRead);
            var fromTheValueStored = await Record(portfolioId, WhatTheTenOClockRunRead, WhatTheTenFifteenRunRead);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.True, "Nothing was recorded yet, so the first computed forecast had to be able to record what it read.");
                Assert.That(fromAValueNoLongerStored, Is.False, "A run that read nothing recorded overwrote the value another run had recorded since.");
                Assert.That(fromTheValueStored, Is.True, "A run that read the stored value could not record its own.");
            }
        }

        [Test]
        [Category("slice-01")]
        [Category("contract-shape:bounded-change")]
        public async Task A_forget_that_lands_while_a_forecast_runs_wins_over_that_forecast()
        {
            var portfolioId = await APortfolio("Payments Platform");
            await Record(portfolioId, null, WhatTheTenOClockRunRead);

            await Forget(portfolioId);
            var theRunningForecastRecords = await Record(portfolioId, WhatTheTenOClockRunRead, WhatTheTenFifteenRunRead);
            var whatTheNextForecastReads = await WhatIsRecordedFor(portfolioId);
            var theNextForecastRecords = await Record(portfolioId, whatTheNextForecastReads, WhatTheTenFifteenRunRead);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(theRunningForecastRecords, Is.False, "A forecast that started before the press recorded its inputs over the forget.");
                Assert.That(whatTheNextForecastReads, Is.Not.EqualTo(WhatTheTenOClockRunRead), "The forget left the inputs it was meant to forget.");
                Assert.That(theNextForecastRecords, Is.True, "The forecast after the forget could not record what it computed from.");
            }
        }

        /// <summary>
        /// A forecast that started after one press and before the next read whatever the first press left.
        /// If a forget only emptied the value, that forecast would read nothing, the second press would leave
        /// nothing again, and the forecast would record over the second press as though nobody had pressed.
        /// </summary>
        [Test]
        [Category("slice-01")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_forecast_that_read_what_an_earlier_forget_left_loses_to_the_next_forget()
        {
            var portfolioId = await APortfolio("Payments Platform");

            await Forget(portfolioId);
            var whatTheForecastAfterTheFirstPressRead = await WhatIsRecordedFor(portfolioId);
            await Forget(portfolioId);

            var thatForecastRecords = await Record(portfolioId, whatTheForecastAfterTheFirstPressRead, WhatTheTenOClockRunRead);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whatTheForecastAfterTheFirstPressRead, Is.Not.Null,
                    "A forget left the Portfolio with nothing recorded, which is also what a Portfolio nobody forecast yet has.");
                Assert.That(thatForecastRecords, Is.False, "A forecast that started before the second press recorded its inputs over it.");
            }
        }

        /// <summary>An instance upgraded into this release has nothing recorded, and a press during its first forecast still has to win.</summary>
        [Test]
        [Category("slice-01")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_forecast_that_read_nothing_recorded_loses_to_a_forget()
        {
            var portfolioId = await APortfolio("Payments Platform");

            await Forget(portfolioId);

            Assert.That(await Record(portfolioId, null, WhatTheTenOClockRunRead), Is.False,
                "A forecast that started before the press, on a Portfolio with nothing recorded, recorded its inputs over the press.");
        }

        [Test]
        [Category("slice-01")]
        [Category("contract-shape:bounded-change")]
        public async Task A_forget_clears_exactly_the_Portfolios_it_names()
        {
            var paymentsPlatform = await APortfolio("Payments Platform");
            var onboarding = await APortfolio("Onboarding");
            await Record(paymentsPlatform, null, WhatTheTenOClockRunRead);
            await Record(onboarding, null, WhatTheTenOClockRunRead);

            await Forget(paymentsPlatform);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Record(paymentsPlatform, WhatTheTenOClockRunRead, WhatTheTenFifteenRunRead), Is.False,
                    "The Portfolio that was named still had its inputs recorded.");
                Assert.That(await Record(onboarding, WhatTheTenOClockRunRead, WhatTheTenFifteenRunRead), Is.True,
                    "The forget also cleared a Portfolio it did not name.");
            }
        }

        /// <summary>
        /// Another build may have run in between, a rollback for instance, and computed forecasts without
        /// recording what it computed them from. So nothing recorded before the app started may let the first
        /// round after it skip.
        /// </summary>
        [Test]
        [Category("slice-01")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task Starting_the_app_forgets_what_every_Portfolio_recorded()
        {
            var paymentsPlatform = await APortfolio("Payments Platform");
            var onboarding = await APortfolio("Onboarding");
            await Record(paymentsPlatform, null, WhatTheTenOClockRunRead);

            using (var context = AContext())
            {
                await new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance).ForgetAllForecastInputs(CancellationToken.None);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Record(paymentsPlatform, WhatTheTenOClockRunRead, WhatTheTenFifteenRunRead), Is.False,
                    "What Payments Platform recorded before the start still counts after it.");
                Assert.That(await Record(onboarding, null, WhatTheTenFifteenRunRead), Is.False,
                    "A forecast that started before the app did, on a Portfolio with nothing recorded, recorded over the start.");
            }
        }

        /// <summary>
        /// A settings save or a refresh loads the whole Portfolio and saves it whole, marking every column as
        /// changed. If that copy was read before a forecast recorded its inputs, saving it would put the old
        /// value back, and the next round would compare against inputs nobody computed from.
        /// </summary>
        [Test]
        [Category("slice-01")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_Portfolio_saved_whole_from_an_old_copy_leaves_the_recorded_inputs_alone()
        {
            var portfolioId = await APortfolio("Payments Platform");

            using var staleContext = AContext();
            var staleRepository = new PortfolioRepository(staleContext, NullLogger<PortfolioRepository>.Instance);
            var staleCopy = staleRepository.GetById(portfolioId)!;

            await Record(portfolioId, null, WhatTheTenOClockRunRead);

            staleCopy.DefaultAmountOfWorkItemsPerFeature = 30;
            staleRepository.Update(staleCopy);
            await staleRepository.Save();

            Assert.That(await Record(portfolioId, WhatTheTenOClockRunRead, WhatTheTenFifteenRunRead), Is.True,
                "Saving an old copy of the Portfolio wrote the inputs it was read with back over the ones recorded since.");
        }

        [Test]
        [Category("slice-01")]
        [Category("edge")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Recording_for_a_Portfolio_that_is_gone_records_nothing()
        {
            Assert.That(await Record(4242, null, WhatTheTenOClockRunRead), Is.False);
        }

        [Test]
        [Category("slice-02")]
        [Category("contract-shape:bounded-change")]
        public async Task Confirming_stamps_every_forecast_of_the_named_Features_and_changes_no_value()
        {
            var (cardVault, refunds) = await TwoForecastFeatures();
            var before = TheForecastsOf(cardVault, refunds);

            using (var context = AContext())
            {
                await new FeatureRepository(context, Mock.Of<IFeatureOrdering>(), NullLogger<FeatureRepository>.Instance)
                    .ConfirmForecastsAsOf([cardVault], ConfirmedAtNineFortyFive, CancellationToken.None);
            }

            var after = TheForecastsOf(cardVault, refunds);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(after.Where(row => row.FeatureId == cardVault).Select(row => row.CreationTime),
                    Is.All.EqualTo(ConfirmedAtNineFortyFive), "A forecast of the confirmed Feature still reads the time it was computed.");
                Assert.That(after.Where(row => row.FeatureId == cardVault), Has.Exactly(2).Items,
                    "Both the completion and the start forecast belong to the confirmation.");
                Assert.That(after.Select(row => row.Values), Is.EqualTo(before.Select(row => row.Values)),
                    "Confirming a forecast changed what it says.");
                Assert.That(after.Where(row => row.FeatureId == refunds), Is.EqualTo(before.Where(row => row.FeatureId == refunds)),
                    "A Feature that was not named was stamped too.");
            }
        }

        // --- Each step on its own context, as each request and each round has its own ---

        private LighthouseAppContext AContext() => new(options, crypto.Object, Mock.Of<ILogger<LighthouseAppContext>>());

        private async Task<bool> Record(int portfolioId, string? readAtGather, string fresh)
        {
            using var context = AContext();

            return await new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance)
                .TryRecordForecastInputs(portfolioId, readAtGather, fresh, CancellationToken.None);
        }

        private async Task Forget(int portfolioId)
        {
            using var context = AContext();

            await new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance)
                .ForgetForecastInputs([portfolioId], CancellationToken.None);
        }

        /// <summary>
        /// What the next forecast of the Portfolio would read. Read as a column, because the Portfolio model
        /// gains the property only with the migration that adds the column. Qualified with the table, because
        /// SQLite reads an unknown double-quoted name on its own as a string.
        /// </summary>
        private async Task<string?> WhatIsRecordedFor(int portfolioId)
        {
            using var context = AContext();

            return await context.Database
                .SqlQuery<string?>($"SELECT portfolio.\"ForecastInputFingerprint\" AS \"Value\" FROM \"Portfolios\" AS portfolio WHERE portfolio.\"Id\" = {portfolioId}")
                .SingleAsync();
        }

        // --- Seeding ---

        private async Task<int> APortfolio(string name)
        {
            using var context = AContext();
            var portfolio = new Portfolio { Name = name, WorkTrackingSystemConnection = AConnection(name) };
            context.Portfolios.Add(portfolio);
            await context.SaveChangesAsync();

            return portfolio.Id;
        }

        private async Task<(int CardVault, int Refunds)> TwoForecastFeatures()
        {
            using var context = AContext();
            var team = new Team { Name = "FSS", WorkTrackingSystemConnection = AConnection("FSS") };
            context.Teams.Add(team);

            var cardVault = AForecastFeature(team, "PAY-88", 9);
            var refunds = AForecastFeature(team, "PAY-90", 14);
            context.Features.AddRange(cardVault, refunds);
            await context.SaveChangesAsync();

            return (cardVault.Id, refunds.Id);
        }

        private static Feature AForecastFeature(Team team, string referenceId, int days)
        {
            var feature = new Feature(team, 3) { Name = referenceId, ReferenceId = referenceId, Order = "1" };

            var completion = new WhenForecast { Team = team, TotalTrials = 200, CreationTime = ComputedAtSix };
            completion.SimulationResults.Add(new IndividualSimulationResult { Key = days, Value = 200 });
            feature.Forecasts.Add(completion);

            var start = new StartForecast(new Dictionary<int, int> { [days - 3] = 200 }, team, 200) { CreationTime = ComputedAtSix };
            feature.StartForecasts.Add(start);

            return feature;
        }

        private static WorkTrackingSystemConnection AConnection(string owner)
            => new() { Name = $"{owner} Connection", WorkTrackingSystem = WorkTrackingSystems.Jira, AuthenticationMethodKey = "pat" };

        private sealed record StoredForecast(int FeatureId, string Kind, DateTime CreationTime, string Values);

        private List<StoredForecast> TheForecastsOf(params int[] featureIds)
        {
            using var context = AContext();

            return [.. context.Features
                .Include(feature => feature.Forecasts).ThenInclude(forecast => forecast.SimulationResults)
                .Include(feature => feature.StartForecasts).ThenInclude(forecast => forecast.SimulationResults)
                .Where(feature => featureIds.Contains(feature.Id))
                .AsEnumerable()
                .SelectMany(feature => feature.Forecasts.Cast<ForecastBase>().Select(forecast => AsStored(feature.Id, "completion", forecast))
                    .Concat(feature.StartForecasts.Select(forecast => AsStored(feature.Id, "start", forecast))))
                .OrderBy(row => row.FeatureId).ThenBy(row => row.Kind)];
        }

        private static StoredForecast AsStored(int featureId, string kind, ForecastBase forecast)
            => new(featureId, kind, DateTime.SpecifyKind(forecast.CreationTime, DateTimeKind.Utc),
                $"{forecast.TotalTrials} runs [{string.Join(", ", forecast.SimulationResults.OrderBy(day => day.Key).Select(day => $"{day.Key}:{day.Value}"))}]");
    }
}
