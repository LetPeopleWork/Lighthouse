using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure
{
    /// <param name="Types">In the mapping's spelling, in the order the types first appear.</param>
    public sealed record PlannedMapping(int ConnectionId, LevelId Level, IReadOnlyList<string> Types);
}
