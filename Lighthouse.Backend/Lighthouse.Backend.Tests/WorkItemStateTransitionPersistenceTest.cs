using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests
{
    [TestFixture]
    public class WorkItemStateTransitionPersistenceTest
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
        public async Task PersistsWorkItemStateTransition_AndCurrentStateEnteredAt_RoundTripsThroughContext()
        {
            var enteredAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);
            var transitionedAt = new DateTime(2026, 1, 20, 8, 30, 0, DateTimeKind.Utc);
            int workItemId;

            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var team = new Team { Name = "Test Team" };
                context.Teams.Add(team);
                await context.SaveChangesAsync();

                var workItem = new WorkItem
                {
                    Name = "Test Work Item",
                    Order = "1",
                    State = "In Progress",
                    CurrentStateEnteredAt = enteredAt,
                    TeamId = team.Id,
                };
                context.WorkItems.Add(workItem);
                await context.SaveChangesAsync();
                workItemId = workItem.Id;

                context.WorkItemStateTransitions.Add(new WorkItemStateTransition
                {
                    WorkItemId = workItemId,
                    FromState = "To Do",
                    ToState = "In Progress",
                    TransitionedAt = transitionedAt,
                });
                await context.SaveChangesAsync();
            }

            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var savedWorkItem = await context.WorkItems.SingleAsync(wi => wi.Id == workItemId);
                var savedTransition = await context.WorkItemStateTransitions.SingleAsync(t => t.WorkItemId == workItemId);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(savedWorkItem.CurrentStateEnteredAt, Is.EqualTo(enteredAt));
                    Assert.That(savedTransition.FromState, Is.EqualTo("To Do"));
                    Assert.That(savedTransition.ToState, Is.EqualTo("In Progress"));
                    Assert.That(savedTransition.TransitionedAt, Is.EqualTo(transitionedAt));
                }
            }
        }

        [Test]
        public async Task SyncedTransitions_IsTransient_AndNotPersisted()
        {
            int workItemId;

            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var team = new Team { Name = "Transient Team" };
                context.Teams.Add(team);
                await context.SaveChangesAsync();

                var workItem = new WorkItem
                {
                    Name = "Transient Work Item",
                    Order = "1",
                    State = "In Progress",
                    TeamId = team.Id,
                    SyncedTransitions =
                    [
                        new WorkItemStateTransition
                        {
                            FromState = "To Do",
                            ToState = "In Progress",
                            TransitionedAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc),
                        },
                    ],
                };
                context.WorkItems.Add(workItem);
                await context.SaveChangesAsync();
                workItemId = workItem.Id;
            }

            var workItemEntityType = new LighthouseAppContext(options, cryptoService.Object, logger.Object)
                .Model.FindEntityType(typeof(WorkItem));

            using (var context = new LighthouseAppContext(options, cryptoService.Object, logger.Object))
            {
                var savedWorkItem = await context.WorkItems.SingleAsync(wi => wi.Id == workItemId);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(workItemEntityType!.FindProperty(nameof(WorkItem.SyncedTransitions)), Is.Null);
                    Assert.That(savedWorkItem.SyncedTransitions, Is.Empty);
                }
            }
        }
    }
}
