using CsvHelper;
using CsvHelper.Configuration;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using System.Globalization;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv
{
    public class CsvWorkTrackingConnector : IWorkTrackingConnector
    {
        private readonly ILogger<CsvWorkTrackingConnector> logger;

        private static int orderCounter = 0;

        public CsvWorkTrackingConnector(ILogger<CsvWorkTrackingConnector> logger)
        {
            this.logger = logger;
        }

        public Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            var workItems = new List<WorkItem>();

            using var csv = ReadCsv(team);

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

            using var csv = ReadCsv(project);

            while (csv.Read())
            {
                var workItemBase = CreateWorkItemBaseForRow(csv, project);
                var feature = new Feature(workItemBase);

                var owningTeam = csv.GetField(GetHeaderName(project.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.OwningTeamHeader))?.Trim() ?? string.Empty;
                var estimatedSizeString = csv.GetField(GetHeaderName(project.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.EstimatedSizeHeader))?.Trim();
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
            // TODO: Validate CSV Options
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
                var isValid = IsValidCsv(owner);

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
            var referenceId = csv.GetField(GetHeaderName(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.IdHeader)).Trim();
            var name = csv.GetField(GetHeaderName(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.NameHeader)).Trim();
            var state = csv.GetField(GetHeaderName(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.StateHeader)).Trim();
            var type = csv.GetField(GetHeaderName(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.TypeHeader)).Trim();
            var startedDate = csv.GetField<DateTime?>(GetHeaderName(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.StartedDateHeader));
            var closedDate = csv.GetField<DateTime?>(GetHeaderName(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.ClosedDateHeader));
            var stateCategory = owner.MapStateToStateCategory(state);

            var createdDate = csv.GetField<DateTime?>(GetHeaderName(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.CreatedDateHeader));
            var parentReferenceId = csv.GetField(GetHeaderName(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.ParentReferenceIdHeader))?.Trim() ?? string.Empty;
            var tags = csv.GetField(GetHeaderName(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.TagsHeader))?.Split('|').Select(x => x.Trim()) ?? [];
            var url = csv.GetField(GetHeaderName(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.UrlHeader))?.Trim() ?? string.Empty;
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

        private bool IsValidCsv(IWorkItemQueryOwner owner)
        {
            var csvContent = owner.WorkItemQuery;
            if (string.IsNullOrEmpty(csvContent))
            {
                return false;
            }

            using var reader = new StringReader(csvContent);
            var headerRow = reader.ReadLine();

            var delimiter = GetDelimiter(owner.WorkTrackingSystemConnection);

            if (!headerRow.Contains(delimiter))
            {
                return false;
            }

            using var csv = ReadCsv(owner);
            var header = csv.HeaderRecord ?? [];


            var requiredColumns = GetRequiredColumns(owner.WorkTrackingSystemConnection);
            var missing = requiredColumns.Except(header, StringComparer.OrdinalIgnoreCase).ToArray();

            return missing.Length == 0;
        }

        private CsvReader ReadCsv(IWorkItemQueryOwner owner)
        {
            string delimiter = GetDelimiter(owner.WorkTrackingSystemConnection);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter, IgnoreBlankLines = true, MissingFieldFound = null };
            var csv = new CsvReader(new StringReader(owner.WorkItemQuery), csvConfig);

            ConfigureDateTimeParsing(csv);

            csv.Read();
            csv.ReadHeader();

            return csv;
        }
        private void ConfigureDateTimeParsing(CsvReader csv)
        {
            var options = csv.Context.TypeConverterOptionsCache.GetOptions<DateTime?>();
            options.Formats =
            [
                "yyyy-MM-dd",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mm:ss.fff",
                "yyyy-MM-ddTHH:mm:ss.fffZ",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss.fff",
                "MM/dd/yyyy",
                "MM/dd/yyyy HH:mm:ss",
                "MM/dd/yyyy hh:mm:ss tt",
                "dd.MM.yyyy",
                "dd.MM.yyyy HH:mm:ss",
                "yyyyMMdd",
                "yyyyMMdd HHmmss",
                "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                "yyyy-MM-ddTHH:mm:ssK",
                "yyyy-MM-ddTHH:mm:ss.fffK"
            ];
        }

        private string[] GetRequiredColumns(WorkTrackingSystemConnection connection)
        {
            return [
                GetHeaderName(connection, CsvWorkTrackingOptionNames.IdHeader),
                GetHeaderName(connection, CsvWorkTrackingOptionNames.NameHeader),
                GetHeaderName(connection, CsvWorkTrackingOptionNames.StateHeader),
                GetHeaderName(connection, CsvWorkTrackingOptionNames.TypeHeader),
                GetHeaderName(connection, CsvWorkTrackingOptionNames.StartedDateHeader),
                GetHeaderName(connection, CsvWorkTrackingOptionNames.ClosedDateHeader)
                ];
        }


        private string GetHeaderName(WorkTrackingSystemConnection connection, string headerKey)
        {
            return connection.Options.Single(o => o.Key == headerKey).Value;
        }

        private string GetDelimiter(WorkTrackingSystemConnection connection)
        {
            return connection.Options.Single(o => o.Key == CsvWorkTrackingOptionNames.Delimiter).Value;
        }
    }
}
