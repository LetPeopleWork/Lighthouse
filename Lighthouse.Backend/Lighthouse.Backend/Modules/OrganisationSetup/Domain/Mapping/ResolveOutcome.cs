using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Domain.Mapping
{
    public abstract record ResolveOutcome
    {
        private ResolveOutcome()
        {
        }

        public sealed record MappedSubset(IReadOnlyList<string> Types) : ResolveOutcome;

        public sealed record TypeNotMapped(string Type, LevelId Level, int ConnectionId) : ResolveOutcome;
    }
}
