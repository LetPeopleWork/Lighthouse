using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Modules.FlowMetrics.Adapters.Persistence;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Acceptance.PercentilesOverTime
{
    /// <summary>
    /// Two copies of the application serving one database can both work out the same missing day. Each
    /// copy is a scope of its own, on a real SQLite file with the real unique day, with no web host. The
    /// in-memory provider the unit tests use does not enforce that uniqueness, so only a real database
    /// shows that the day stays one point when the copies collide.
    /// </summary>
    [TestFixture]
    public class TwoCopiesFillingTheSameDayAtThePortTest
    {
        private const int TeamId = 7;

        private static readonly DateOnly Day = new(2026, 9, 15);

        private static readonly int[] EveryCycleTimeHorizon = [30, 60, 90];

        private ServicePortHarness harness = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            harness = new ServicePortHarness(Register);
        }

        [TearDown]
        public void RemoveTheScenariosDatabase()
        {
            harness.Dispose();
        }

        /// <summary>What this fixture adds to the harness, read without building or resolving any of it.</summary>
        internal static IServiceCollection WhatItRegisters
        {
            get
            {
                var services = new ServiceCollection();
                Register(services);
                return services;
            }
        }

        [Test]
        public async Task Two_copies_of_the_application_filling_the_same_day_leave_one_point_not_two()
        {
            using var firstCopy = harness.NewScope();
            using var secondCopy = harness.NewScope();
            var firstWriter = firstCopy.ServiceProvider.GetRequiredService<IPercentileSnapshotWriter>();
            var secondWriter = secondCopy.ServiceProvider.GetRequiredService<IPercentileSnapshotWriter>();
            firstWriter.FillDayIfAbsent(TeamId, OwnerType.Team, MetricType.CycleTime, Day, (_, _) => Readings(1, 2, 3, 4));
            secondWriter.FillDayIfAbsent(TeamId, OwnerType.Team, MetricType.CycleTime, Day, (_, _) => Readings(5, 6, 7, 8));

            await firstWriter.SaveFilledDay();
            await Assert.DoesNotThrowAsync(secondWriter.SaveFilledDay,
                "The copy that lost the day was refused with an error. A day another copy has already stored is not a failure of the pass that lost it.");

            var held = WhatTheDatabaseHolds();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(held.Select(row => row.Horizon), Is.EqualTo(EveryCycleTimeHorizon),
                    "Two copies filling one day must leave exactly one point per horizon on the chart, not two stacked on the same date.");
                Assert.That(held.TrueForAll(row => row.P50 == 1 && row.P95 == 4), Is.True,
                    "The copy that stored the day first keeps it; the other copy's reading must not replace it.");
            }
        }

        private List<PercentilesOverTimeSnapshot> WhatTheDatabaseHolds()
        {
            using var scope = harness.NewScope();

            return [.. scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().PercentilesOverTimeSnapshots
                .AsNoTracking()
                .Where(row => row.OwnerId == TeamId && row.RecordedAt == Day)
                .OrderBy(row => row.Horizon)];
        }

        private static List<PercentileValue> Readings(int p50, int p70, int p85, int p95)
            => [new(50, p50), new(70, p70), new(85, p85), new(95, p95)];

        private static void Register(IServiceCollection services)
        {
            services.AddScoped<IPercentilesOverTimeSnapshotRepository, PercentilesOverTimeSnapshotRepository>();
            services.AddScoped<IPercentileSnapshotWriter, PercentileSnapshotWriter>();
        }
    }
}
