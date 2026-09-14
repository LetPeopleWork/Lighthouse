namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Whether the work running right now has been asked to stop, readable from anywhere inside an update
    /// execution without being threaded through every signature. Sibling of WriteBackRoundContext, and the
    /// update queue is its only writer.
    ///
    /// It exists because nearly all of a refresh's wall-clock is inside one connector call: a checkpoint
    /// between phases would, on the ordinary full-fetch path, be no checkpoint at all - that path makes a
    /// single call. The paging loops read this, so cancel bites between pages, which is where the time is.
    ///
    /// An AsyncLocal does not escape the async method that set it, which is what confines the token to the
    /// one execution the queue set it for. A coalesced follow-up is started after that call returns and
    /// gets its own: it is new work, and stopping the run before it should not stop it.
    /// </summary>
    public sealed class UpdateCancellationContext
    {
        private readonly AsyncLocal<CancellationToken?> current = new();

        public CancellationToken Current
        {
            get => current.Value ?? CancellationToken.None;
            set => current.Value = value;
        }
    }
}
