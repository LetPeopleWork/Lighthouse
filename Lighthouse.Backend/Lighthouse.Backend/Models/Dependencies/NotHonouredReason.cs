namespace Lighthouse.Backend.Models.Dependencies
{
    /// <summary>
    /// Why Lighthouse will not act on a dependency. There are three reasons and there is no fourth: an open
    /// set is how "probably fine" quietly becomes a reason, so widening this has to be a decision somebody
    /// makes on purpose rather than a value added to get one screen to read better. A Feature positioned
    /// below the one waiting on it is deliberately not in here - the order stays the user's, and an order
    /// that reads oddly is still an order Lighthouse can work with.
    /// </summary>
    public enum NotHonouredReason
    {
        OutsideThisPortfolio,

        InALoop,

        BlockerCannotBeForecast,
    }
}
