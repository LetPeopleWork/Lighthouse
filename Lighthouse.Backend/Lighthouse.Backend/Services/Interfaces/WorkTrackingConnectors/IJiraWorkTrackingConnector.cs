using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    public interface IJiraWorkTrackingConnector : IWorkTrackingConnector
    {
        Task<IEnumerable<JiraBoard>> GetBoards(WorkTrackingSystemConnection workTrackingSystemConnection);
    }
}