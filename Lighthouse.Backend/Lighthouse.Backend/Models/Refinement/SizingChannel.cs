namespace Lighthouse.Backend.Models.Refinement
{
    /// <summary>Where an entry was cast from, as the caller declares it. The numbers are stored, so they never change.</summary>
    public enum SizingChannel
    {
        Web = 0,
        LiveSession = 1,
        Cli = 2,
        Assistant = 3,
    }
}
