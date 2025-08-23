using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv
{
    // Minimal CSV connector implementation - built-in, in-memory processing will be implemented later.
    public class CsvWorkTrackingConnector : IWorkTrackingConnector
    {
        public Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            // CSV connector uses file uploads during team creation/edit - runtime refresh is a no-op.
            return Task.FromResult(Enumerable.Empty<WorkItem>());
        }

        public Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            return Task.FromResult(new List<Feature>());
        }

        public Task<List<Feature>> GetParentFeaturesDetails(Project project, IEnumerable<string> parentFeatureIds)
        {
            return Task.FromResult(new List<Feature>());
        }

        public Task<List<string>> GetWorkItemsIdsForTeamWithAdditionalQuery(Team team, string additionalQuery)
        {
            return Task.FromResult(new List<string>());
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            if (!existingItemsOrder.Any())
            {
                return "0";
            }

            var ints = existingItemsOrder.Select(s =>
            {
                if (int.TryParse(s, out var i)) return i;
                return 0;
            });

            return relativeOrder == RelativeOrder.Above ? (ints.Max() + 1).ToString() : (ints.Min() - 1).ToString();
        }

        public Task<bool> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            // CSV is a built-in connector with no external connection to validate.
            return Task.FromResult(true);
        }

        public Task<bool> ValidateTeamSettings(Team team)
        {
            // Team validation for CSV occurs during file upload; here we conservatively return true.
            return Task.FromResult(true);
        }

        public Task<bool> ValidateProjectSettings(Project project)
        {
            return Task.FromResult(true);
        }
    }
}
