namespace Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure
{
    public abstract record RenameOutcome
    {
        private RenameOutcome()
        {
        }

        public sealed record Renamed(LevelId Level) : RenameOutcome;

        public sealed record LevelNotFound(LevelId Level) : RenameOutcome;

        /// <param name="Field">Which of the four names is empty.</param>
        public sealed record NameMissing(LevelId Level, string Field) : RenameOutcome;
    }
}
