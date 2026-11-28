namespace Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure
{
    public sealed class Level
    {
        internal Level(LevelId id, int ordinal, TermPair unitName, TermPair itemName)
        {
            Id = id;
            Ordinal = ordinal;
            UnitName = unitName;
            ItemName = itemName;
        }

        public LevelId Id { get; }

        public int Ordinal { get; }

        public TermPair UnitName { get; }

        public TermPair ItemName { get; }
    }
}
