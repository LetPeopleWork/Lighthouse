namespace Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Persistence
{
    public class ConnectionLevelMappingRecord
    {
        public int ConnectionId { get; set; }

        /// <summary>The stored level key, such as Team or Portfolio.</summary>
        public string LevelKey { get; set; } = string.Empty;

        /// <summary>In the mapping's spelling and order.</summary>
        public List<string> ItemTypes { get; set; } = [];
    }
}
