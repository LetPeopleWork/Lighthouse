using CsvHelper;
using CsvHelper.Configuration;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using System.Globalization;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv
{
    public class CsvWorkTrackingConnector : IWorkTrackingConnector
    {
        private const string CsvDelimiter = ",";

        private const string IdHeader = "ID";
        private const string NameHeader = "Name";
        private const string StateHeader = "State";
        private const string TypeHeader = "Type";
        private const string StartedDateHeader = "StartedDate";
        private const string ClosedDateHeader = "ClosedDate";

        private readonly ILogger<CsvWorkTrackingConnector> logger;

        private static readonly string[] requiredTeamColumns = [IdHeader, NameHeader, StateHeader, TypeHeader, StartedDateHeader, ClosedDateHeader];

        public CsvWorkTrackingConnector(ILogger<CsvWorkTrackingConnector> logger)
        {
            this.logger = logger;
        }

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
            var csvContent = team.WorkItemQuery;

            if (string.IsNullOrEmpty(csvContent))
            {
                return Task.FromResult(false);
            }

            try
            {
                var isValid = IsValidCsv(csvContent);

                return Task.FromResult(isValid);
            }
            catch
            {
                logger.LogInformation("Could not read CSV for team {TeamName} - Validation failed", team.Name);
                return Task.FromResult(true);
            }
        }

        public Task<bool> ValidateProjectSettings(Project project)
        {
            return Task.FromResult(true);
        }

        private bool IsValidCsv(string csvContent)
        {
            using var reader = new StringReader(csvContent);
            var headerRow = reader.ReadLine();

            if (!headerRow.Contains(CsvDelimiter))
            {
                return false;
            }

            var csv = ReadCsv(csvContent);
            var header = csv.HeaderRecord ?? [];
            var missing = requiredTeamColumns.Except(header, StringComparer.OrdinalIgnoreCase).ToArray();

            return missing.Length == 0;
        }

        private CsvReader ReadCsv(string csvContent)
        {
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = CsvDelimiter, IgnoreBlankLines = true };
            using var csv = new CsvReader(new StringReader(csvContent), csvConfig);

            csv.Read();
            csv.ReadHeader();

            return csv;
        }
    }
}
