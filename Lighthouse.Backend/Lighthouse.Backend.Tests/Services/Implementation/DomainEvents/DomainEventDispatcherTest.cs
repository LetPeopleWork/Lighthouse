using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    public class DomainEventDispatcherTest
    {
        [Test]
        public async Task PublishAsync_RunsEveryRegisteredHandlerExactlyOnceWithTheEvent()
        {
            var firstHandler = new RecordingHandler();
            var secondHandler = new RecordingHandler();
            var subject = CreateSubject(firstHandler, secondHandler);

            var domainEvent = new PortfolioFeaturesRefreshed(42);
            await subject.PublishAsync(domainEvent);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstHandler.HandledEvents, Is.EqualTo(new[] { domainEvent }));
                Assert.That(secondHandler.HandledEvents, Is.EqualTo(new[] { domainEvent }));
            }
        }

        [Test]
        public async Task PublishAsync_OneHandlerThrows_StillRunsTheOthersAndDoesNotPropagate()
        {
            var throwingHandler = new ThrowingHandler();
            var survivingHandler = new RecordingHandler();
            var subject = CreateSubject(throwingHandler, survivingHandler);

            await subject.PublishAsync(new PortfolioFeaturesRefreshed(7));

            Assert.That(survivingHandler.HandledEvents, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task PublishAsync_OneHandlerThrows_LogsTheFailure()
        {
            var loggerMock = new Mock<ILogger<DomainEventDispatcher>>();
            var subject = CreateSubject(loggerMock.Object, new ThrowingHandler());

            await subject.PublishAsync(new PortfolioFeaturesRefreshed(7));

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static DomainEventDispatcher CreateSubject(params IDomainEventHandler<PortfolioFeaturesRefreshed>[] handlers)
        {
            return CreateSubject(Mock.Of<ILogger<DomainEventDispatcher>>(), handlers);
        }

        private static DomainEventDispatcher CreateSubject(ILogger<DomainEventDispatcher> logger, params IDomainEventHandler<PortfolioFeaturesRefreshed>[] handlers)
        {
            var services = new ServiceCollection();
            foreach (var handler in handlers)
            {
                services.AddSingleton(handler);
            }

            var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
            return new DomainEventDispatcher(scopeFactory, logger);
        }

        private sealed class RecordingHandler : IDomainEventHandler<PortfolioFeaturesRefreshed>
        {
            private readonly List<PortfolioFeaturesRefreshed> handledEvents = [];

            public IReadOnlyList<PortfolioFeaturesRefreshed> HandledEvents => handledEvents;

            public Task HandleAsync(PortfolioFeaturesRefreshed domainEvent, CancellationToken cancellationToken)
            {
                handledEvents.Add(domainEvent);
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingHandler : IDomainEventHandler<PortfolioFeaturesRefreshed>
        {
            public Task HandleAsync(PortfolioFeaturesRefreshed domainEvent, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("handler boom");
            }
        }
    }
}
