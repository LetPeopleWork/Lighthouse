namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Which setting under Settings → System was switched. Deliberately usage data's own list rather
    /// than the keys the product stores its settings under: a setting reaches the census only when
    /// somebody adds it here on purpose, so a new switch appearing in the product never starts being
    /// reported just because it exists.
    ///
    /// The switch that stops usage data altogether is not on this list. Reporting it would mean
    /// telling the collector about the one decision that says to tell it nothing.
    /// </summary>
    public enum UsageDataOptionalFeature
    {
        // Zero is a real answer here, not a stand-in for "none given". Anything reading this has to
        // establish that a value was actually sent before trusting it.
        FeatureOrder = 0,
    }
}
