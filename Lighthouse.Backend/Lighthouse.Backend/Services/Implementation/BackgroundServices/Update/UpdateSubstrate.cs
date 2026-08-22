using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    // The three ports that answer differently depending on whether Lighthouse runs as one process or
    // several. They are swapped together - all in-process, or all Redis-backed - because a deployment
    // that recorded work in flight across replicas while locking only within one would let two replicas
    // run the same update at once. Injecting them as one thing is what keeps them swapped together.
    public sealed class UpdateSubstrate(
        IUpdateStatusStore statusStore,
        IUpdateExecutionLock executionLock,
        IUpdateCompletionNotifier completionNotifier)
    {
        public IUpdateStatusStore StatusStore => statusStore;

        public IUpdateExecutionLock ExecutionLock => executionLock;

        public IUpdateCompletionNotifier CompletionNotifier => completionNotifier;
    }
}
