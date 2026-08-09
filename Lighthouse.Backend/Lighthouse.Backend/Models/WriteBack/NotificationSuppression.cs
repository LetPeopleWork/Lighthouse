namespace Lighthouse.Backend.Models.WriteBack
{
    /// <summary>
    /// Whether a write-back reached the tracker without emailing the item's watchers (ADR-142 §3).
    /// <see cref="Unknown"/> is not "we did not look" — it is the question arising and going unanswered,
    /// which is what a 403 surviving the unsuppressed retry means. That distinction is load-bearing:
    /// only <see cref="NotSuppressed"/> may be reported as a permission problem.
    /// </summary>
    public enum NotificationSuppression
    {
        /// <summary>The connector never asks for silence, so the question does not arise.</summary>
        NotApplicable,

        /// <summary>The write landed and the tracker was asked — and permitted — to stay quiet.</summary>
        Suppressed,

        /// <summary>The write landed only after dropping the suppression request. Watchers were emailed.</summary>
        NotSuppressed,

        /// <summary>The write was refused through the retry, so whether it could have been quiet is unknowable.</summary>
        Unknown,
    }
}
