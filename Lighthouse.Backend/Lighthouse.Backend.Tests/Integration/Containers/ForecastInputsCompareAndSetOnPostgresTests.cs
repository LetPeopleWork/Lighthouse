using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// The recorded forecast inputs are only ever replaced by a compare-and-set, and a first record compares
    /// against nothing at all. SQL compares a null with "IS NULL", never with "=", so what holds on SQLite has
    /// to be seen holding on Postgres too: a first record succeeds once and only once, and a run's mark can be
    /// replaced only by that run. Every step runs on a context of its own, as each round does.
    /// </summary>
    [TestFixture]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-01")]
    public class ForecastInputsCompareAndSetOnPostgresTests
    {
        private const string WhatTheTenOClockRunRead = "3f1c0a9e5b7d4c2e8a6f1b0d9c7e5a31";

        private const string WhatTheTenFifteenRunRead = "9b2e7d5c3a1f4e6b8d0c2a4f6e8b0d13";

        private SharedPostgresContainer? postgres;

        [OneTimeTearDown]
        public async Task StopPostgres()
        {
            if (postgres is not null)
            {
                await postgres.DisposeAsync();
            }
        }

        [Test]
        public async Task A_first_record_from_nothing_succeeds_once_and_a_second_from_nothing_fails()
        {
            await using var provider = await AFreshDatabase();
            var portfolioId = await APortfolio(provider);

            var first = await Record(provider, portfolioId, null, WhatTheTenOClockRunRead);
            var second = await Record(provider, portfolioId, null, WhatTheTenFifteenRunRead);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.True, "Nothing was recorded yet, so the first forecast had to be able to record.");
                Assert.That(second, Is.False, "A run that read nothing recorded over the value another run had recorded since.");
            }
        }

        [Test]
        public async Task A_run_marks_from_what_it_read_and_only_its_mark_can_be_replaced()
        {
            await using var provider = await AFreshDatabase();
            var portfolioId = await APortfolio(provider);
            await Record(provider, portfolioId, null, WhatTheTenOClockRunRead);

            var fromAStaleRead = await Mark(provider, portfolioId, WhatTheTenFifteenRunRead);
            var mark = await Mark(provider, portfolioId, WhatTheTenOClockRunRead);
            var fromWhatTheRunRead = await Record(provider, portfolioId, WhatTheTenOClockRunRead, WhatTheTenFifteenRunRead);
            var fromTheMark = await Record(provider, portfolioId, mark, WhatTheTenFifteenRunRead);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fromAStaleRead, Is.Null, "A run marked from a value that was no longer stored.");
                Assert.That(mark, Is.Not.Null, "A run could not mark the value it read.");
                Assert.That(fromWhatTheRunRead, Is.False, "The value the mark replaced could still be recorded over it.");
                Assert.That(fromTheMark, Is.True, "The run could not replace its own mark.");
            }
        }

        private async Task<ServiceProvider> AFreshDatabase()
        {
            postgres ??= await SharedPostgresContainer.StartAsync();

            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.MigrateAsync();

            return provider;
        }

        private static async Task<int> APortfolio(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var portfolio = new Portfolio
            {
                Name = "Payments Platform",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Payments Platform Connection", WorkTrackingSystem = WorkTrackingSystems.Jira, AuthenticationMethodKey = "pat" },
            };
            context.Portfolios.Add(portfolio);
            await context.SaveChangesAsync();

            return portfolio.Id;
        }

        private static async Task<bool> Record(ServiceProvider provider, int portfolioId, string? readAtGather, string fresh)
        {
            using var scope = provider.CreateScope();

            return await ARepository(scope).TryRecordForecastInputs(portfolioId, readAtGather, fresh, CancellationToken.None);
        }

        private static async Task<string?> Mark(ServiceProvider provider, int portfolioId, string? readAtGather)
        {
            using var scope = provider.CreateScope();

            return await ARepository(scope).TryMarkForecastRunning(portfolioId, readAtGather, CancellationToken.None);
        }

        private static PortfolioRepository ARepository(IServiceScope scope)
            => new(scope.ServiceProvider.GetRequiredService<LighthouseAppContext>(), NullLogger<PortfolioRepository>.Instance);
    }
}
