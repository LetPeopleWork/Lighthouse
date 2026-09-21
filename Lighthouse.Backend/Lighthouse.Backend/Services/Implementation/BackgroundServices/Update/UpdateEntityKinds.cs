namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Which kind of thing a piece of work is about. There are five kinds of work and only two kinds of
    /// thing: a Team is refreshed and deleted, and everything else happens to a Portfolio.
    /// </summary>
    public enum UpdateEntityKind
    {
        Team,

        Portfolio,
    }

    /// <summary>
    /// The single place that says which kind of thing an update type is about.
    ///
    /// It is named so that there is one answer rather than a copy of the answer wherever it is needed -
    /// two places deciding this for themselves is how they come to disagree. It matters because an
    /// update type and an id only identify a row together: team 4 and portfolio 4 are different work,
    /// so anything comparing two rows has to ask what each one is about before comparing their ids.
    /// </summary>
    public static class UpdateEntityKinds
    {
        /// <summary>
        /// Every update type is written out and there is no catch-all arm, so a sixth kind of work
        /// cannot be added without someone saying here what it is about. That is the point of the shape:
        /// a catch-all was silently answering for three of the five, which is how a portfolio refresh and
        /// the forecast it triggers came to be described as the same thing.
        /// </summary>
        // The compiler additionally wants an arm for a value outside the enum, which any int cast to
        // UpdateType would be. Writing one would be the catch-all this is built to avoid, and it would
        // swallow the missing-member error along with it, so only the out-of-range complaint is silenced.
#pragma warning disable CS8524
        public static UpdateEntityKind Of(UpdateType updateType) => updateType switch
        {
            UpdateType.Team => UpdateEntityKind.Team,
            UpdateType.TeamDelete => UpdateEntityKind.Team,
            UpdateType.Features => UpdateEntityKind.Portfolio,
            UpdateType.Forecasts => UpdateEntityKind.Portfolio,
            UpdateType.PortfolioDelete => UpdateEntityKind.Portfolio,
        };
#pragma warning restore CS8524
    }
}
