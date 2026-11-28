using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Persistence
{
    /// <summary>
    /// The one stored structure. Its key is fixed, so a second instance starting at the same time fails to insert
    /// its own and re-reads the winner's.
    /// </summary>
    public class OrganisationStructureRecord : IConcurrencyTokenEntity
    {
        public const int TheOnlyKey = 1;

        public int Id { get; set; } = TheOnlyKey;

        public Guid ConcurrencyToken { get; set; }

        public List<LevelRecord> Levels { get; set; } = [];
    }
}
