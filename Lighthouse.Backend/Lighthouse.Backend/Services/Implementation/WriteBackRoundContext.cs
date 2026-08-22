namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// The refresh round the code running right now belongs to, readable from anywhere inside an update
    /// execution without threading it through every call. The update queue is the only writer; it opens a
    /// round when work starts and lets the work an execution asks for join the round that asked. Code
    /// running outside an update execution - an HTTP request, say - sees no round and works on its own.
    /// </summary>
    public sealed class WriteBackRoundContext
    {
        private readonly AsyncLocal<WriteBackRound?> current = new();

        public WriteBackRound? Current
        {
            get => current.Value;
            set => current.Value = value;
        }
    }
}
