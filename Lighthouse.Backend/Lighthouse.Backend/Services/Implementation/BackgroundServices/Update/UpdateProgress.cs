namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    // Append new members only, and never reorder. Advance compares ordinals, so a member's position is
    // its meaning: inserting one above another relabels every value already persisted in the status hash
    // and already in flight to a browser, and makes a key appear to move backwards.
    public enum UpdateProgress
    {
        Queued,

        InProgress,

        Completed,

        Failed,

        Cancelled,
    }
}
