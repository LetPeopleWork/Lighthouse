namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    public enum HowTheRunEnded
    {
        EverythingFinished,

        /// <summary>
        /// Every row with work left was waiting on one that had not finished. Nothing a later day brings can
        /// change that, so the run has ended. It can only happen when Features are waiting on each other in
        /// a circle, which the decision that produces the waits is supposed to have left out.
        /// </summary>
        NothingLeftCouldBeStarted,

        /// <summary>
        /// The run passed the most days a single run may cover. This is not how a run is meant to end and no
        /// data can cause it; it is here so that a mistake in how runs end shows up in minutes rather than
        /// tying up a background thread for good with nothing anywhere saying why.
        /// </summary>
        RanOutOfDays,
    }
}
