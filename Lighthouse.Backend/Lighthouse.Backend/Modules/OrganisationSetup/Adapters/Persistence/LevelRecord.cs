namespace Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Persistence
{
    public class LevelRecord
    {
        /// <summary>The stored level key, such as Team or Portfolio.</summary>
        public string Key { get; set; } = string.Empty;

        public int OrganisationStructureId { get; set; }

        public int Ordinal { get; set; }

        public string UnitSingular { get; set; } = string.Empty;

        public string UnitPlural { get; set; } = string.Empty;

        public string ItemSingular { get; set; } = string.Empty;

        public string ItemPlural { get; set; } = string.Empty;
    }
}
