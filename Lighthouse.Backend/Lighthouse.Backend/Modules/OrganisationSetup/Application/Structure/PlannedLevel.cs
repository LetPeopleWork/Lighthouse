using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure
{
    public sealed record PlannedLevel(LevelId Id, int Ordinal, TermPair UnitName, TermPair ItemName);
}
