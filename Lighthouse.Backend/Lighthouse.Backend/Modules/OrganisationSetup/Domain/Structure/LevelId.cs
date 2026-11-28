using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure
{
    public readonly record struct LevelId(string Key)
    {
        public static LevelId Team { get; } = new("Team");

        public static LevelId Portfolio { get; } = new("Portfolio");

        /// <summary>Teams sit on the level where the work is done, Portfolios on the one above it.</summary>
        public static LevelId Of(WorkTrackingSystemOptionsOwner unit) => unit is Models.Team ? Team : Portfolio;
    }
}
