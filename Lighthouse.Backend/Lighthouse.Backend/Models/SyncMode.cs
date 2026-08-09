namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// How much an update had to fetch. Epic #5687: the field exists before delta does, so later slices
    /// change the data rather than the shape of what is reported.
    /// </summary>
    public enum SyncMode
    {
        Full,
        Delta
    }
}
