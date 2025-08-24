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

        // Common Required Columns
        private const string IdHeader = "ID";
        private const string NameHeader = "Name";
        private const string StateHeader = "State";
        private const string TypeHeader = "Type";
        private const string StartedDateHeader = "StartedDate";
        private const string ClosedDateHeader = "ClosedDate";

        // Common Optional Columns
        private const string CreatedDateHeader = "CreatedDate";
        private const string ParentReferenceIdHeader = "ParentReferenceId";
        private const string TagsHeader = "Tags";
        private const string UrlHeader = "Url";

        // Feature Optional Columns
        private const string OwningTeamHeader = "OwningTeam";
        private const string EstimatedSizeHeader = "EstimatedSize";

        private readonly ILogger<CsvWorkTrackingConnector> logger;

        private static int orderCounter = 0;

        private static readonly string[] requiredTeamColumns = [IdHeader, NameHeader, StateHeader, TypeHeader, StartedDateHeader, ClosedDateHeader];

        public CsvWorkTrackingConnector(ILogger<CsvWorkTrackingConnector> logger)
        {
            this.logger = logger;
        }

        public Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            var workItems = new List<WorkItem>();

            using var csv = ReadCsv(team.WorkItemQuery);

            while (csv.Read())
            {
                var workItemBase = CreateWorkItemBaseForRow(csv, team);
                var workItem = new WorkItem(workItemBase, team);

                workItems.Add(workItem);
            }

            return Task.FromResult(workItems.AsEnumerable());
        }

        public Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            var features = new List<Feature>();

            using var csv = ReadCsv(project.WorkItemQuery);

            while (csv.Read())
            {
                var workItemBase = CreateWorkItemBaseForRow(csv, project);
                var feature = new Feature(workItemBase);

                var owningTeam = csv.GetField(OwningTeamHeader)?.Trim() ?? string.Empty;
                var estimatedSizeString = csv.GetField(EstimatedSizeHeader)?.Trim();
                var estimatedSize = 0;

                if (!string.IsNullOrEmpty(estimatedSizeString) && int.TryParse(estimatedSizeString, out var parsedEstimatedSize))
                {
                    estimatedSize = parsedEstimatedSize;
                }

                feature.EstimatedSize = estimatedSize;
                feature.OwningTeam = owningTeam;

                features.Add(feature);
            }

            return Task.FromResult(features);
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
            return Task.FromResult(true);
        }

        public Task<bool> ValidateTeamSettings(Team team)
        {
            return ValidateCsv(team);
        }

        public Task<bool> ValidateProjectSettings(Project project)
        {
            return ValidateCsv(project);
        }

        private Task<bool> ValidateCsv(IWorkItemQueryOwner owner)
        {
            var csvContent = owner.WorkItemQuery;

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
                logger.LogInformation("Could not read CSV for {Name} - Validation failed", owner.Name);
                return Task.FromResult(true);
            }
        }

        private WorkItemBase CreateWorkItemBaseForRow(CsvReader csv, IWorkItemQueryOwner owner)
        {
            var referenceId = csv.GetField(IdHeader).Trim();
            var name = csv.GetField(NameHeader).Trim();
            var state = csv.GetField(StateHeader).Trim();
            var type = csv.GetField(TypeHeader).Trim();
            var startedDate = csv.GetField<DateTime?>(StartedDateHeader);
            var closedDate = csv.GetField<DateTime?>(ClosedDateHeader);
            var stateCategory = owner.MapStateToStateCategory(state);

            var createdDate = csv.GetField<DateTime?>(CreatedDateHeader);
            var parentReferenceId = csv.GetField(ParentReferenceIdHeader)?.Trim() ?? string.Empty;
            var tags = csv.GetField(TagsHeader)?.Split('|').Select(x => x.Trim()) ?? [];
            var url = csv.GetField(UrlHeader)?.Trim() ?? string.Empty;
            var order = $"{orderCounter++}";

            var workItemBase = new WorkItemBase
            {
                ReferenceId = referenceId,
                Name = name,
                State = state,
                StateCategory = stateCategory,
                Type = type,
                StartedDate = startedDate,
                ClosedDate = closedDate,
                CreatedDate = createdDate,
                ParentReferenceId = parentReferenceId,
                Tags = [.. tags],
                Url = url,
                Order = order,
            };

            return workItemBase;
        }

        private bool IsValidCsv(string csvContent)
        {
            using var reader = new StringReader(csvContent);
            var headerRow = reader.ReadLine();

            if (!headerRow.Contains(CsvDelimiter))
            {
                return false;
            }

            using var csv = ReadCsv(csvContent);
            var header = csv.HeaderRecord ?? [];
            var missing = requiredTeamColumns.Except(header, StringComparer.OrdinalIgnoreCase).ToArray();

            return missing.Length == 0;
        }

        private CsvReader ReadCsv(string csvContent)
        {
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = CsvDelimiter, IgnoreBlankLines = true, MissingFieldFound = null };
            var csv = new CsvReader(new StringReader(csvContent), csvConfig);

            csv.Read();
            csv.ReadHeader();

            return csv;
        }
    }
}
