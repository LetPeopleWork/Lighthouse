using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application
{
    public interface ITerminologyService
    {
        IEnumerable<TerminologyEntry> GetAll();

        Task UpdateTerminology(IEnumerable<TerminologyEntry> terminology);
    }
}
