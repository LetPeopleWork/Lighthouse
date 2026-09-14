namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class UpdateStatus
    {
        public UpdateType UpdateType { get; set; }

        public int Id { get; set; }

        public UpdateProgress Status { get; set; } = UpdateProgress.Queued;

        /// <summary>
        /// Best-effort, and absent is a legitimate state: during a rolling upgrade a replica running the
        /// older build admits work without recording anything, so every reader has to render a row with no
        /// duration rather than a nonsense one. ADR-182.
        /// </summary>
        public DateTimeOffset? QueuedAt { get; set; }

        /// <summary><see cref="QueuedAt"/>'s counterpart for the moment the work actually started running.</summary>
        public DateTimeOffset? StartedAt { get; set; }
    }
}
