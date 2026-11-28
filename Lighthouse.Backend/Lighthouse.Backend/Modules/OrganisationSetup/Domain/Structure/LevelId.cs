namespace Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure
{
    public readonly record struct LevelId(string Key)
    {
        public static LevelId Team { get; } = new("Team");

        public static LevelId Portfolio { get; } = new("Portfolio");
    }
}
