namespace Lighthouse.Backend.Models.Dependencies
{
    /// <summary>
    /// Why Lighthouse will not act on a dependency. The set is closed: an open one is how "probably fine"
    /// quietly becomes a reason, so widening it has to be a decision somebody makes on purpose rather than a
    /// value added to get one screen to read better. A Feature positioned below the one waiting on it is
    /// deliberately not in here - the order stays the user's, and an order that reads oddly is still an
    /// order Lighthouse can work with.
    ///
    /// Most of these say something is wrong with the dependency. One says nothing is wrong with it and
    /// somebody asked to set it aside anyway, which is why it is the only one that raises no warning. The
    /// last one says nothing is wrong with it either and Lighthouse cannot act on it yet.
    /// </summary>
    public enum NotHonouredReason
    {
        OutsideThisPortfolio,

        InALoop,

        BlockerCannotBeForecast,

        IgnoredByPortfolio,

        /// <summary>
        /// The two ends are not one Team's work. Each Team is forecast on its own clock, so one Team's
        /// simulated run has no moment at which it can say another Team has finished something - and a
        /// guess about a moment it cannot see would be a date presented as if it were measured.
        /// </summary>
        CrossesATeam,

        /// <summary>
        /// Letting a dependency change a date is paid behaviour and this instance has not paid for it. It is
        /// given only where nothing else stands against the dependency: a reader told that a licence is what
        /// is missing, about a wait a licensed instance would leave out anyway, has been sold a date that
        /// would not move.
        /// </summary>
        NotLicensed,
    }
}
