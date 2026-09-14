using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public abstract class UpdateServiceTestBase
    {
        private readonly Mock<IUpdateQueueService> updateQueueServiceMock;

        private readonly Mock<IServiceProvider> serviceProviderMock;

        protected UpdateServiceTestBase()
        {
            updateQueueServiceMock = new Mock<IUpdateQueueService>();
            serviceProviderMock = new Mock<IServiceProvider>();
            
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            ServiceScopeFactory = serviceScopeFactoryMock.Object;
            
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

            // The real queue runs the task and catches whatever comes out of it, recording the refresh as
            // failed rather than letting it escape to the caller that triggered it. The double does the
            // same, so a test whose refresh throws sees what production sees instead of an exception
            // surfacing at the trigger, which is a place it never reaches in the running application.
            updateQueueServiceMock
                .Setup(x => x.EnqueueUpdate(It.IsAny<UpdateType>(), It.IsAny<int>(), It.IsAny<Func<IServiceProvider, Task>>()))
                .Callback((UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask) =>
                {
                    try
                    {
                        updateTask(serviceProviderMock.Object).Wait();
                    }
                    catch (AggregateException exception)
                    {
                        WhatTheRefreshThrew = exception.InnerException ?? exception;
                    }
                });

            // Every enqueued update now ends in a write-back flush (ADR-144 §4). Registering the
            // collector here means an updater test exercises that terminal call instead of silently
            // hitting the resolution failure the flush's own catch would swallow.
            WriteBackCollectorMock = new Mock<IWriteBackCollector>();
            WriteBackCollectorMock.Setup(c => c.FlushAsync()).ReturnsAsync([]);
            SetupServiceProviderMock(WriteBackCollectorMock.Object);
        }

        /// <summary>
        /// What the last refresh threw, or null if it did not throw. The queue is where a failing refresh
        /// stops in production, so this is the only place a test driving an updater can see that it failed.
        /// </summary>
        protected Exception? WhatTheRefreshThrew { get; private set; }

        protected Mock<IWriteBackCollector> WriteBackCollectorMock { get; }

        protected IServiceScopeFactory ServiceScopeFactory { get; }

        protected IUpdateQueueService UpdateQueueService => updateQueueServiceMock.Object;

        protected void SetupServiceProviderMock<T>(T @object) where T : class
        {
            serviceProviderMock.Setup(x => x.GetService(typeof(T))).Returns(@object);
        }

        protected static async Task WaitUntilVerified(Action verification)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (true)
            {
                try
                {
                    verification();
                    return;
                }
                catch (MockException)
                {
                    if (DateTime.UtcNow >= deadline)
                    {
                        throw;
                    }

                    await Task.Delay(10);
                }
            }
        }
    }
}
