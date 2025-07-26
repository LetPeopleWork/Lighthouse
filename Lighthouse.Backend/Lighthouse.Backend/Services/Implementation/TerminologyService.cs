using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class TerminologyService : ITerminologyService
    {
        public TerminologyDto GetTerminology()
        {
            // NOTE: As specified in the story, no DB/Repo implementation yet
            // For now, return default terminology that can be easily extended later
            return new TerminologyDto
            {
                WorkItem = "Work Item",
                WorkItems = "Work Items",
            };
        }
    }
}
