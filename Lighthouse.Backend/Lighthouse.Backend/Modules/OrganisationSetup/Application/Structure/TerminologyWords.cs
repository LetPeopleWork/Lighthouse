using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure
{
    public sealed record TerminologyWords(TermPair Team, TermPair WorkItem, TermPair Portfolio, TermPair Feature)
    {
        public static TerminologyWords From(IEnumerable<TerminologyEntry> entries)
            => throw new NotImplementedException($"Reading the level words from {entries} is not built yet.");
    }
}
