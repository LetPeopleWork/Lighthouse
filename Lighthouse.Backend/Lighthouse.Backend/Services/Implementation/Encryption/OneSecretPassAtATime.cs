namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Two rotations running at once is the one way this feature can destroy a credential outright. Each
    // would take its own snapshot of the ring, mint a key named after the same day, and write its own file
    // over the other's - so the second one to finish leaves a key store that has never heard of the key the
    // first one already moved a thousand secrets onto. Nothing on disk or in memory could read them again.
    //
    // One at a time removes that. It is a lock inside one process and nothing more, which is enough here
    // because minting only happens where Lighthouse keeps its own key - a standalone install or a single
    // container - and never in the deployments that run more than one replica. Where several replicas do
    // run, the key was handed to them, only the moving is offered, and two of those are already safe: every
    // write names the value it observed, so the second one finds the row already at its destination.
    public sealed class OneSecretPassAtATime : IDisposable
    {
        private readonly SemaphoreSlim gate = new(1, 1);

        public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> pass, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(pass);

            await gate.WaitAsync(cancellationToken);

            try
            {
                return await pass(cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }

        public void Dispose()
        {
            gate.Dispose();
        }
    }
}
