using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests
{
    [TestFixture]
    public class RecurringBlackoutRulePersistenceTest
    {
        private Mock<ICryptoService> cryptoService;
        private Mock<ILogger<LighthouseAppContext>> logger;
        private DbContextOptions<LighthouseAppContext> options;

        [SetUp]
        public void Setup()
        {
            cryptoService = new Mock<ICryptoService>();
            logger = new Mock<ILogger<LighthouseAppContext>>();

            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object);
            context.Database.EnsureCreated();
        }

        [Test]
        public async Task RecurringBlackoutRule_RoundTripsThroughContext_WithItsWeekdaySet()
        {
            var start = new DateOnly(2026, 1, 5);
            var end = new DateOnly(2026, 12, 31);
            int ruleId;

            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var rule = new RecurringBlackoutRule
                {
                    Weekdays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
                    IntervalWeeks = 2,
                    Start = start,
                    End = end,
                    Description = "Sprint planning blackout",
                };
                context.Set<RecurringBlackoutRule>().Add(rule);
                await context.SaveChangesAsync();
                ruleId = rule.Id;
            }

            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var saved = await context.Set<RecurringBlackoutRule>().SingleAsync(r => r.Id == ruleId);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(saved.Weekdays, Is.EqualTo(new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }));
                    Assert.That(saved.IntervalWeeks, Is.EqualTo(2));
                    Assert.That(saved.Start, Is.EqualTo(start));
                    Assert.That(saved.End, Is.EqualTo(end));
                    Assert.That(saved.Description, Is.EqualTo("Sprint planning blackout"));
                }
            }
        }

        [Test]
        public async Task InPlaceWeekdayMutation_IsTracked_OnSaveAndReload()
        {
            int ruleId;

            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var rule = new RecurringBlackoutRule
                {
                    Weekdays = [DayOfWeek.Tuesday],
                    Start = new DateOnly(2026, 2, 3),
                    Description = "Initial",
                };
                context.Set<RecurringBlackoutRule>().Add(rule);
                await context.SaveChangesAsync();
                ruleId = rule.Id;
            }

            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var tracked = await context.Set<RecurringBlackoutRule>().SingleAsync(r => r.Id == ruleId);
                tracked.Weekdays.Add(DayOfWeek.Thursday);
                await context.SaveChangesAsync();
            }

            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var reloaded = await context.Set<RecurringBlackoutRule>().SingleAsync(r => r.Id == ruleId);

                Assert.That(reloaded.Weekdays, Is.EqualTo(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }));
            }
        }
    }
}
