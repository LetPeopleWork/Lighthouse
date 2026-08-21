namespace Lighthouse.Backend.Models.Dependencies
{
    /// <summary>
    /// Why Lighthouse will not act on a dependency. The set is closed: an open one is how "probably fine"
    /// quietly becomes a reason, so widening it has to be a decision somebody makes on purpose rather than a
    /// value added to get one screen to read better. A Feature positioned below the one waiting on it is
    /// deliberately not in here - the order stays the user's, and an order that reads oddly is still an
    /// order Lighthouse can work with.
    ///
    /// Three of these say something is wrong with the dependency. The fourth says nothing is wrong with it
    /// and somebody asked to set it aside anyway, which is why it is the only one that raises no warning.
    /// </summary>
    public enum NotHonouredReason
    {
        OutsideThisPortfolio,

        InALoop,

        BlockerCannotBeForecast,

        IgnoredByPortfolio,
    }
}
