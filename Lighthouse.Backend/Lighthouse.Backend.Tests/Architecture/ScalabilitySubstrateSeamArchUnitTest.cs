using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ScalabilitySubstrateSeamArchUnitTest
    {
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
