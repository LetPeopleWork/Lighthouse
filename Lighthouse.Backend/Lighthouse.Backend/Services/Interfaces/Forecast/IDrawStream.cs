namespace Lighthouse.Backend.Services.Interfaces.Forecast
{
    /// <summary>
    /// Where the feature forecast gets its random numbers. A draw is decided by the place it is asked for
    /// rather than by how many draws came before it, so one Team's numbers do not change when another
    /// Team's draws are taken in between, and two runs of the same forecast can be compared number for
    /// number instead of only distribution against distribution.
    ///
    /// Nothing here is stored between calls, which is also what lets simulated runs be carried out side by
    /// side: there is no shared position for them to race over.
    /// </summary>
    public interface IDrawStream
    {
        /// <summary>
        /// The number this run's draws all come from. Reported when a run has to be given up on, so that the
        /// exact run can be set going again on its own rather than hunted for.
        /// </summary>
        long StartingNumber { get; }

        /// <param name="trial">Which simulated run.</param>
        /// <param name="team">Which Team within that run.</param>
        /// <param name="day">Which simulated day of that Team's work.</param>
        /// <param name="ordinal">
        /// Which draw within that Team's day. The draw for how much the Team delivers and the draws that
        /// pick which Feature received each item have to hold different ordinals, or a high-delivery day
        /// would correlate with which Feature the work went to and nothing in the output would look wrong.
        /// </param>
        /// <param name="maxExclusive">The number of possibilities. Zero and one both draw zero.</param>
        int Draw(int trial, int team, int day, int ordinal, int maxExclusive);
    }
}
