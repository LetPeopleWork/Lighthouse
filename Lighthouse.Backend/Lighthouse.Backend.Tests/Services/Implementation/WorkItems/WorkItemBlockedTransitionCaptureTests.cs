using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class WorkItemBlockedTransitionCaptureTests
    {
        private Mock<IWorkItemBlockedTransitionRepository> transitionRepositoryMock = null!;
        private Mock<ILogger<WorkItemBlockedTransitionCaptureHandler>> captureLoggerMock = null!;
        private Mock<ILogger<WorkItemBlockedTransitionCloseHandler>> closeLoggerMock = null!;

        [SetUp]
        public void SetUp()
        {
            transitionRepositoryMock = new Mock<IWorkItemBlockedTransitionRepository>();
            captureLoggerMock = new Mock<ILogger<WorkItemBlockedTransitionCaptureHandler>>();
            closeLoggerMock = new Mock<ILogger<WorkItemBlockedTransitionCloseHandler>>();
        }

        [Test]
        public async Task WorkItemBlockedCaptureHandler_CreatesOpenTransition()
        {
            var handler = new WorkItemBlockedTransitionCaptureHandler(
                transitionRepositoryMock.Object,
                captureLoggerMock.Object);

            var blockedEvent = new WorkItemBlocked(42, "Blocked");

            transitionRepositoryMock
                .Setup(r => r.GetByPredicate(It.IsAny<Func<WorkItemBlockedTransition, bool>>()))
                .Returns((WorkItemBlockedTransition?)null);

            await handler.HandleAsync(blockedEvent, CancellationToken.None);

            transitionRepositoryMock.Verify(
                r => r.Add(It.Is<WorkItemBlockedTransition>(t =>
                    t.WorkItemId == blockedEvent.WorkItemId &&
                    t.LeftAt == null &&
                    t.EnteredAt != default)),
                Times.Once);
            transitionRepositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task WorkItemBlockedCaptureHandler_AlreadyOpen_IsIdempotent()
        {
            var handler = new WorkItemBlockedTransitionCaptureHandler(
                transitionRepositoryMock.Object,
                captureLoggerMock.Object);

            var blockedEvent = new WorkItemBlocked(42, "Blocked");
            var existingOpenTransition = new WorkItemBlockedTransition
            {
                WorkItemId = 42,
                EnteredAt = DateTime.UtcNow,
                LeftAt = null,
            };

            transitionRepositoryMock
                .Setup(r => r.GetByPredicate(It.IsAny<Func<WorkItemBlockedTransition, bool>>()))
                .Returns(existingOpenTransition);

            await handler.HandleAsync(blockedEvent, CancellationToken.None);

            transitionRepositoryMock.Verify(r => r.Add(It.IsAny<WorkItemBlockedTransition>()), Times.Never);
            transitionRepositoryMock.Verify(r => r.Save(), Times.Never);
            VerifyLoggerCalled(captureLoggerMock, LogLevel.Information, "skipping capture");
        }

        [Test]
        public async Task WorkItemUnblockedCaptureHandler_ClosesOpenTransition()
        {
            var closeHandler = new WorkItemBlockedTransitionCloseHandler(
                transitionRepositoryMock.Object,
                closeLoggerMock.Object);

            var unblockedEvent = new WorkItemUnblocked(42);
            var openTransition = new WorkItemBlockedTransition
            {
                Id = 1,
                WorkItemId = 42,
                EnteredAt = DateTime.UtcNow.AddHours(-3),
                LeftAt = null,
            };

            transitionRepositoryMock
                .Setup(r => r.GetByPredicate(It.IsAny<Func<WorkItemBlockedTransition, bool>>()))
                .Returns(openTransition);

            await closeHandler.HandleAsync(unblockedEvent, CancellationToken.None);

            Assert.That(openTransition.LeftAt, Is.Not.Null,
                "the close handler must set LeftAt on the open transition");
            transitionRepositoryMock.Verify(r => r.Update(openTransition), Times.Once);
            transitionRepositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task WorkItemUnblockedCaptureHandler_NoOpenTransition_DoesNothing()
        {
            var closeHandler = new WorkItemBlockedTransitionCloseHandler(
                transitionRepositoryMock.Object,
                closeLoggerMock.Object);

            var unblockedEvent = new WorkItemUnblocked(42);

            transitionRepositoryMock
                .Setup(r => r.GetByPredicate(It.IsAny<Func<WorkItemBlockedTransition, bool>>()))
                .Returns((WorkItemBlockedTransition?)null);

            await closeHandler.HandleAsync(unblockedEvent, CancellationToken.None);

            transitionRepositoryMock.Verify(r => r.Update(It.IsAny<WorkItemBlockedTransition>()), Times.Never);
            transitionRepositoryMock.Verify(r => r.Save(), Times.Never);
            VerifyLoggerCalled(closeLoggerMock, LogLevel.Information, "skipping close");
        }

        private static void VerifyLoggerCalled<T>(Mock<ILogger<T>> loggerMock, LogLevel level, string contains)
        {
            loggerMock.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(contains)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}
