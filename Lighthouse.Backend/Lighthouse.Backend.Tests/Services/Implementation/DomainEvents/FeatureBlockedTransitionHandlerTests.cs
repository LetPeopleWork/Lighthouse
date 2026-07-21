using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    [TestFixture]
    [Category("portfolio-blocked-history")]
    public class FeatureBlockedTransitionHandlerTests
    {
        private Mock<IFeatureBlockedTransitionRepository> transitionRepositoryMock = null!;
        private Mock<ILogger<FeatureBlockedTransitionCaptureHandler>> captureLoggerMock = null!;
        private Mock<ILogger<FeatureBlockedTransitionCloseHandler>> closeLoggerMock = null!;

        [SetUp]
        public void SetUp()
        {
            transitionRepositoryMock = new Mock<IFeatureBlockedTransitionRepository>();
            captureLoggerMock = new Mock<ILogger<FeatureBlockedTransitionCaptureHandler>>();
            closeLoggerMock = new Mock<ILogger<FeatureBlockedTransitionCloseHandler>>();
        }

        [Test]
        public async Task CaptureHandler_NoOpenSpell_OpensOneSpellForFeatureAndPortfolio()
        {
            var handler = new FeatureBlockedTransitionCaptureHandler(
                transitionRepositoryMock.Object,
                captureLoggerMock.Object);

            var blockedEvent = new FeatureBlocked(42, 7, "Blocked");

            transitionRepositoryMock
                .Setup(r => r.GetOpenSpell(blockedEvent.PortfolioId, blockedEvent.FeatureId))
                .Returns((FeatureBlockedTransition?)null);

            await handler.HandleAsync(blockedEvent, CancellationToken.None);

            transitionRepositoryMock.Verify(
                r => r.Add(It.Is<FeatureBlockedTransition>(t =>
                    t.FeatureId == blockedEvent.FeatureId &&
                    t.PortfolioId == blockedEvent.PortfolioId &&
                    t.LeftAt == null &&
                    t.EnteredAt != default)),
                Times.Once);
            transitionRepositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task CaptureHandler_OpenSpellAlreadyExists_IsIdempotent()
        {
            var handler = new FeatureBlockedTransitionCaptureHandler(
                transitionRepositoryMock.Object,
                captureLoggerMock.Object);

            var blockedEvent = new FeatureBlocked(42, 7, "Blocked");
            var existingOpenSpell = new FeatureBlockedTransition
            {
                FeatureId = 42,
                PortfolioId = 7,
                EnteredAt = DateTime.UtcNow,
                LeftAt = null,
            };

            transitionRepositoryMock
                .Setup(r => r.GetOpenSpell(blockedEvent.PortfolioId, blockedEvent.FeatureId))
                .Returns(existingOpenSpell);

            await handler.HandleAsync(blockedEvent, CancellationToken.None);

            transitionRepositoryMock.Verify(r => r.Add(It.IsAny<FeatureBlockedTransition>()), Times.Never);
            transitionRepositoryMock.Verify(r => r.Save(), Times.Never);
            VerifyLoggerCalled(captureLoggerMock, LogLevel.Information, "skipping capture");
        }

        [Test]
        public async Task CloseHandler_OpenSpell_ClosesItBySettingLeftAt()
        {
            var closeHandler = new FeatureBlockedTransitionCloseHandler(
                transitionRepositoryMock.Object,
                closeLoggerMock.Object);

            var unblockedEvent = new FeatureUnblocked(42, 7);
            var openSpell = new FeatureBlockedTransition
            {
                Id = 1,
                FeatureId = 42,
                PortfolioId = 7,
                EnteredAt = DateTime.UtcNow.AddHours(-3),
                LeftAt = null,
            };

            transitionRepositoryMock
                .Setup(r => r.GetOpenSpell(unblockedEvent.PortfolioId, unblockedEvent.FeatureId))
                .Returns(openSpell);

            await closeHandler.HandleAsync(unblockedEvent, CancellationToken.None);

            Assert.That(openSpell.LeftAt, Is.Not.Null,
                "the close handler must set LeftAt on the open spell");
            transitionRepositoryMock.Verify(r => r.Update(openSpell), Times.Once);
            transitionRepositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task CloseHandler_NoOpenSpell_DoesNothing()
        {
            var closeHandler = new FeatureBlockedTransitionCloseHandler(
                transitionRepositoryMock.Object,
                closeLoggerMock.Object);

            var unblockedEvent = new FeatureUnblocked(42, 7);

            transitionRepositoryMock
                .Setup(r => r.GetOpenSpell(unblockedEvent.PortfolioId, unblockedEvent.FeatureId))
                .Returns((FeatureBlockedTransition?)null);

            await closeHandler.HandleAsync(unblockedEvent, CancellationToken.None);

            transitionRepositoryMock.Verify(r => r.Update(It.IsAny<FeatureBlockedTransition>()), Times.Never);
            transitionRepositoryMock.Verify(r => r.Save(), Times.Never);
            VerifyLoggerCalled(closeLoggerMock, LogLevel.Information, "skipping close");
        }

        [Test]
        public async Task ReBlockAfterClose_OpensANewSpellNotAReopen()
        {
            var captureHandler = new FeatureBlockedTransitionCaptureHandler(
                transitionRepositoryMock.Object,
                captureLoggerMock.Object);
            var closeHandler = new FeatureBlockedTransitionCloseHandler(
                transitionRepositoryMock.Object,
                closeLoggerMock.Object);

            var blockedEvent = new FeatureBlocked(42, 7, "Blocked");
            var unblockedEvent = new FeatureUnblocked(42, 7);

            // First block: no open spell yet.
            transitionRepositoryMock
                .Setup(r => r.GetOpenSpell(7, 42))
                .Returns((FeatureBlockedTransition?)null);
            await captureHandler.HandleAsync(blockedEvent, CancellationToken.None);

            // Close: an open spell now exists and gets closed.
            var firstSpell = new FeatureBlockedTransition
            {
                Id = 1,
                FeatureId = 42,
                PortfolioId = 7,
                EnteredAt = DateTime.UtcNow.AddHours(-2),
                LeftAt = null,
            };
            transitionRepositoryMock
                .Setup(r => r.GetOpenSpell(7, 42))
                .Returns(firstSpell);
            await closeHandler.HandleAsync(unblockedEvent, CancellationToken.None);

            // Re-block: the previous spell is closed, so no open spell → a NEW spell opens.
            transitionRepositoryMock
                .Setup(r => r.GetOpenSpell(7, 42))
                .Returns((FeatureBlockedTransition?)null);
            await captureHandler.HandleAsync(blockedEvent, CancellationToken.None);

            transitionRepositoryMock.Verify(
                r => r.Add(It.IsAny<FeatureBlockedTransition>()), Times.Exactly(2));
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
