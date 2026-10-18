namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    public enum HowTheRunEnded
    {
        EverythingFinished,

        /// <summary>
        /// Every row with work left was waiting on one that had not finished. Nothing a later day brings can
        /// change that, so the run has ended, and what it left unfinished counts as beyond the horizon. It
        /// can only happen when Features are waiting on each other in a circle, which the decision that
        /// produces the waits is supposed to have left out.
        /// </summary>
        NothingLeftCouldBeStarted,

        /// <summary>
        /// The run passed the most days a single run may cover, so what it left unfinished lies beyond the
        /// horizon. Work that would take longer than that is enough to end a run here.
        /// </summary>
        RanOutOfDays,
    }
}
