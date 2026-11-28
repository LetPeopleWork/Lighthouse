namespace Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Persistence
{
    public class ConnectionMappingRecord
    {
        public int ConnectionId { get; set; }

        public List<ConnectionLevelMappingRecord> Levels { get; set; } = [];
    }
}
