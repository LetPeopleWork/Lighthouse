namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// INV-A3 - fills the places nobody holds yet, appending, leaving every existing one alone. One rule
    /// serves handing the order over (AC-2.1), a Feature arriving afterwards (AC-2.6) and taking the
    /// order over again (AC-5.3).
    /// </summary>
    public interface IFeatureRankSeeder
    {
        Task SeedMissingRanks();
    }
}
