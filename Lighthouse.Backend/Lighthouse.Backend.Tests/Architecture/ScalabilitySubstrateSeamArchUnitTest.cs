using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ScalabilitySubstrateSeamArchUnitTest
    {
        private static readonly Type[] JustACancellationToken = [typeof(CancellationToken)];

        [Test]
        public void IUpdateQueueService_StillExposes_EnqueueUpdate_WithUnchangedSignature()
        {
            var method = typeof(IUpdateQueueService).GetMethod(nameof(IUpdateQueueService.EnqueueUpdate));

            Assert.That(method, Is.Not.Null,
                "Horizontal-scalability slice must not change the IUpdateQueueService caller contract: EnqueueUpdate is the fire-and-forget entry point every updater depends on. The substrate swap (in-process vs Redis-gated) lives behind IUpdateExecutionLock / IUpdateCompletionNotifier, never on this interface.");

            var parameterTypes = method!.GetParameters().Select(p => p.ParameterType).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
                Assert.That(parameterTypes, Is.EqualTo(new[]
                {
                    typeof(UpdateType),
                    typeof(int),
                    typeof(Func<IServiceProvider, Task>),
                }));
            }
        }

        /// <summary>
        /// Shutdown's entry point. The lanes moved out of the queue service and there are three of them
        /// now, so how a host asks the queue to finish what it started is the one thing about that move
        /// a caller could have noticed - and the host is the one caller that cannot be given a second
        /// chance to get it right.
        /// </summary>
        [Test]
        public void IUpdateQueueService_StillExposes_DrainAsync_WithUnchangedSignature()
        {
            var method = typeof(IUpdateQueueService).GetMethod(nameof(IUpdateQueueService.DrainAsync));

            Assert.That(method, Is.Not.Null,
                "DrainAsync is how shutdown waits for work already under way. Splitting the single queue into a lane per kind of update changed how many things it waits for, never what the host calls.");

            var parameterTypes = method!.GetParameters().Select(p => p.ParameterType).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(method.ReturnType, Is.EqualTo(typeof(Task)));
                Assert.That(parameterTypes, Is.EqualTo(JustACancellationToken));
            }
        }

        [Test]
        public void IUpdateQueueService_StillExposes_EnqueueAndAwaitAsync_WithUnchangedSignature()
        {
            var method = typeof(IUpdateQueueService).GetMethod(nameof(IUpdateQueueService.EnqueueAndAwaitAsync));

            Assert.That(method, Is.Not.Null,
                "Horizontal-scalability slice must not change the IUpdateQueueService caller contract: EnqueueAndAwaitAsync is the await-completion entry point (e.g. portfolio delete). Cross-pod awaiting is implemented behind IUpdateCompletionNotifier, not by reshaping this signature.");

            var parameterTypes = method!.GetParameters().Select(p => p.ParameterType).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(method.ReturnType, Is.EqualTo(typeof(Task)));
                Assert.That(parameterTypes, Is.EqualTo(new[]
                {
                    typeof(UpdateType),
                    typeof(int),
                    typeof(Func<IServiceProvider, Task>),
                    typeof(CancellationToken),
                }));
            }
        }
    }
}
