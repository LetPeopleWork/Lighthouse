using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ITerminologyService
    {
        IEnumerable<TerminologyEntry> GetAll();

        Task UpdateTerminology(IEnumerable<TerminologyEntry> terminology);
    }
}
