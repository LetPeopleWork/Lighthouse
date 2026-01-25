using CsvHelper;
using CsvHelper.Configuration;
using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using System.Globalization;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv
{
    public class CsvWorkTrackingConnector(ILogger<CsvWorkTrackingConnector> logger) : IWorkTrackingConnector
    {
        private static int orderCounter;

        public Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            var workItems = new List<WorkItem>();

            using var csv = ReadCsv(team);

            while (csv.Read())
            {
                var workItemBase = CreateWorkItemBaseForRow(csv, team);

                if (workItemBase == null)
                {
                    continue;
                }

                var workItem = new WorkItem(workItemBase, team);

                workItems.Add(workItem);
            }

            return Task.FromResult(workItems.AsEnumerable());
        }

        public Task<List<Feature>> GetFeaturesForProject(Portfolio project)
        {
            var features = new List<Feature>();

            using var csv = ReadCsv(project);

            while (csv.Read())
            {
                var workItemBase = CreateWorkItemBaseForRow(csv, project);

                if (workItemBase != null)
                {
                    var feature = new Feature(workItemBase);

                    var owningTeam = csv.GetField(GetOptionByKey(project.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.OwningTeamHeader))?.Trim() ?? string.Empty;
                    var estimatedSizeString = csv.GetField(GetOptionByKey(project.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.EstimatedSizeHeader))?.Trim();
                    var estimatedSize = 0;

                    if (!string.IsNullOrEmpty(estimatedSizeString) && int.TryParse(estimatedSizeString, out var parsedEstimatedSize))
                    {
                        estimatedSize = parsedEstimatedSize;
                    }

                    feature.EstimatedSize = estimatedSize;
                    feature.OwningTeam = owningTeam;

                    features.Add(feature);
                }
            }

            return Task.FromResult(features);
        }

        public Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds)
        {
            return Task.FromResult(new List<Feature>());
        }

        public Task<bool> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            var optionsEmpty = connection.Options.Where(o => !o.IsOptional).Any(o => string.IsNullOrEmpty(o.Value));
            return Task.FromResult(!optionsEmpty);
        }

        public Task<bool> ValidateTeamSettings(Team team)
        {
            return ValidateCsv(team);
        }

        public Task<bool> ValidatePortfolioSettings(Portfolio portfolio)
        {
            return ValidateCsv(portfolio);
        }

        private Task<bool> ValidateCsv(IWorkItemQueryOwner owner)
        {
            var csvContent = owner.DataRetrievalValue;

            if (string.IsNullOrEmpty(csvContent))
            {
                return Task.FromResult(false);
            }

            try
            {
                var isValid = IsValidCsv(owner);

                return Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex, "Could not read CSV for {Name} - Validation failed", owner.Name);
                return Task.FromResult(true);
            }
        }

        private static WorkItemBase? CreateWorkItemBaseForRow(CsvReader csv, IWorkItemQueryOwner owner)
        {
            var referenceId = csv.GetField(GetOptionByKey(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.IdHeader)).Trim();
            var name = csv.GetField(GetOptionByKey(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.NameHeader)).Trim();
            var state = csv.GetField(GetOptionByKey(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.StateHeader)).Trim();
            var type = csv.GetField(GetOptionByKey(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.TypeHeader)).Trim();
            var parentReferenceId = csv.GetField(GetOptionByKey(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.ParentReferenceIdHeader))?.Trim() ?? string.Empty;
            var url = csv.GetField(GetOptionByKey(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.UrlHeader))?.Trim() ?? string.Empty;

            var startedDate = ParseDateTime(csv, owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.StartedDateHeader);
            var closedDate = ParseDateTime(csv, owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.ClosedDateHeader);
            var createdDate = ParseDateTime(csv, owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.CreatedDateHeader);

            var tagSeperator = GetTagSeparator(owner.WorkTrackingSystemConnection);
            var tags = csv.GetField(GetOptionByKey(owner.WorkTrackingSystemConnection, CsvWorkTrackingOptionNames.TagsHeader))?.Split(tagSeperator).Select(x => x.Trim()) ?? [];

            var stateCategory = owner.MapStateToStateCategory(state);

            var order = $"{orderCounter++}";

            if (!owner.AllStates.IsItemInList(state) || !owner.WorkItemTypes.IsItemInList(type))
            {
                return null;
            }

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

        private static DateTime? ParseDateTime(CsvReader reader, WorkTrackingSystemConnection connection, string columnName)
        {
            var dateTime = reader.GetField<DateTime?>(GetOptionByKey(connection, columnName));

            if (dateTime.HasValue)
            {
                dateTime = DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
            }

            return dateTime;
        }

        private bool IsValidCsv(IWorkItemQueryOwner owner)
        {
            var csvContent = owner.DataRetrievalValue;
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
            var csv = new CsvReader(new StringReader(owner.DataRetrievalValue), csvConfig);

            var dateFormat = GetAdditionalDateTimeFormat(owner.WorkTrackingSystemConnection);

            var formats = string.IsNullOrWhiteSpace(dateFormat)
                ? ["yyyy-MM-dd"]
                : new[] { dateFormat, "yyyy-MM-dd" };

            csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = formats;
            csv.Context.TypeConverterOptionsCache.GetOptions<DateTime?>().Formats = formats;

            csv.Read();
            csv.ReadHeader();

            return csv;
        }

        private static string[] GetRequiredColumns(WorkTrackingSystemConnection connection)
        {
            return [
                GetOptionByKey(connection, CsvWorkTrackingOptionNames.IdHeader),
                GetOptionByKey(connection, CsvWorkTrackingOptionNames.NameHeader),
                GetOptionByKey(connection, CsvWorkTrackingOptionNames.StateHeader),
                GetOptionByKey(connection, CsvWorkTrackingOptionNames.TypeHeader),
                GetOptionByKey(connection, CsvWorkTrackingOptionNames.StartedDateHeader),
                GetOptionByKey(connection, CsvWorkTrackingOptionNames.ClosedDateHeader)
                ];
        }

        private static string GetOptionByKey(WorkTrackingSystemConnection connection, string key)
        {
            return connection.Options.Single(o => o.Key == key).Value;
        }

        private static string GetDelimiter(WorkTrackingSystemConnection connection)
        {
            return GetOptionByKey(connection, CsvWorkTrackingOptionNames.Delimiter);
        }

        private static string GetAdditionalDateTimeFormat(WorkTrackingSystemConnection connection)
        {
            return GetOptionByKey(connection, CsvWorkTrackingOptionNames.DateTimeFormat);
        }

        private static string GetTagSeparator(WorkTrackingSystemConnection connection)
        {
            return GetOptionByKey(connection, CsvWorkTrackingOptionNames.TagSeparator);
        }
    }
}
