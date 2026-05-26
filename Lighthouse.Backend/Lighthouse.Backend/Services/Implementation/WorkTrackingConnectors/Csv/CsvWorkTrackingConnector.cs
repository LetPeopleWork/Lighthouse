using CsvHelper;
using CsvHelper.Configuration;
using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Models.Validation;
using System.Globalization;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv
{
    public class CsvWorkTrackingConnector(ILogger<CsvWorkTrackingConnector> logger) : IWorkTrackingConnector
    {
        private static int orderCounter;

        public bool SupportsTransitionHistory(WorkTrackingSystemConnection connection)
        {
            return !string.IsNullOrWhiteSpace(GetStateEnteredDateColumn(connection));
        }

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

        public Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            var optionsEmpty = connection.Options.Where(o => !o.IsOptional).Any(o => string.IsNullOrEmpty(o.Value));

            if (optionsEmpty)
            {
                return Task.FromResult(ConnectionValidationResult.Failure(
                    "missing_required_option",
                    "Some required CSV connection options are missing."));
            }

            return Task.FromResult(ConnectionValidationResult.Success());
        }

        public Task<ConnectionValidationResult> ValidateTeamSettings(Team team)
        {
            return ValidateQueryOwnerForCsv(team);
        }

        public Task<ConnectionValidationResult> ValidatePortfolioSettings(Portfolio portfolio)
        {
            return ValidateQueryOwnerForCsv(portfolio);
        }

        private Task<ConnectionValidationResult> ValidateQueryOwnerForCsv(IWorkItemQueryOwner owner)
        {
            var csvContent = owner.DataRetrievalValue;

            if (string.IsNullOrEmpty(csvContent))
            {
                return Task.FromResult(ConnectionValidationResult.Failure(
                    "missing_csv_content",
                    "No CSV content was provided.",
                    "Paste or upload CSV content before validating.",
                    "DataRetrievalValue"));
            }

            if (owner.WorkItemTypes.Count == 0)
            {
                return Task.FromResult(ConnectionValidationResult.Failure(
                    "missing_work_item_types",
                    "At least one work item type is required for CSV validation.",
                    fieldName: "WorkItemTypes"));
            }

            if (owner.ToDoStates.Count == 0 || owner.DoingStates.Count == 0 || owner.DoneStates.Count == 0)
            {
                return Task.FromResult(ConnectionValidationResult.Failure(
                    "missing_states",
                    "To Do, Doing, and Done states are required for CSV validation.",
                    fieldName: "States"));
            }

            try
            {
                var isValid = IsValidCsv(owner);

                if (!isValid)
                {
                    return Task.FromResult(ConnectionValidationResult.Failure(
                        "invalid_csv",
                        "CSV content is invalid or missing required columns.",
                        "Check CSV delimiter and required headers in your connection options."));
                }

                return Task.FromResult(ConnectionValidationResult.Success());
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex, "Could not read CSV for {Name} - Validation failed", owner.Name);
                return Task.FromResult(ConnectionValidationResult.Failure(
                    "validation_failed",
                    "CSV validation failed due to an unexpected parsing error.",
                    ex.Message));
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

            var mappedState = owner.MapRawStateToMappedName(state);

            var workItemBase = new WorkItemBase
            {
                ReferenceId = referenceId,
                Name = name,
                State = mappedState,
                StateCategory = stateCategory,
                Type = type,
                StartedDate = startedDate,
                ClosedDate = closedDate,
                CreatedDate = createdDate,
                ParentReferenceId = parentReferenceId,
                Tags = [.. tags],
                Url = url,
                Order = order,
                SyncedTransitions = BuildStateEnteredTransitions(csv, owner, mappedState, stateCategory, closedDate),
            };

            return workItemBase;
        }

        private static IReadOnlyList<WorkItemStateTransition> BuildStateEnteredTransitions(
            CsvReader csv, IWorkItemQueryOwner owner, string mappedState, StateCategories stateCategory, DateTime? closedDate)
        {
            var stateEnteredDateColumn = GetStateEnteredDateColumn(owner.WorkTrackingSystemConnection);

            if (string.IsNullOrWhiteSpace(stateEnteredDateColumn))
            {
                return [];
            }

            var journey = BuildMultiStateJourney(csv, owner, stateEnteredDateColumn, stateCategory, closedDate);

            if (journey.Count > 0)
            {
                return journey;
            }

            return BuildCurrentStateEnteredTransition(csv, stateEnteredDateColumn, mappedState, stateCategory);
        }

        private static IReadOnlyList<WorkItemStateTransition> BuildMultiStateJourney(
            CsvReader csv, IWorkItemQueryOwner owner, string stateEnteredDateColumn, StateCategories stateCategory, DateTime? closedDate)
        {
            var enteredStates = owner.DoingStates
                .Select(state => (State: state, EnteredAt: ReadPerStateEnteredDate(csv, stateEnteredDateColumn, state)))
                .Where(entry => entry.EnteredAt.HasValue)
                .Select(entry => (entry.State, EnteredAt: entry.EnteredAt!.Value))
                .ToList();

            if (enteredStates.Count == 0)
            {
                return [];
            }

            var exitTransitions = enteredStates
                .Zip(enteredStates.Skip(1), (from, to) => new WorkItemStateTransition
                {
                    FromState = from.State,
                    ToState = to.State,
                    TransitionedAt = to.EnteredAt,
                });

            if (stateCategory == StateCategories.Done && closedDate.HasValue)
            {
                var lastDoingState = enteredStates[^1].State;

                return
                [
                    .. exitTransitions,
                    new WorkItemStateTransition
                    {
                        FromState = lastDoingState,
                        ToState = MappedDoneState(owner),
                        TransitionedAt = DateTime.SpecifyKind(closedDate.Value, DateTimeKind.Utc),
                    }
                ];
            }

            return [.. exitTransitions];
        }

        private static string MappedDoneState(IWorkItemQueryOwner owner)
        {
            return owner.MapRawStateToMappedName(owner.DoneStates[0]);
        }

        private static DateTime? ReadPerStateEnteredDate(CsvReader csv, string stateEnteredDateColumn, string doingState)
        {
            var columnName = $"{stateEnteredDateColumn}{CsvWorkTrackingOptionNames.PerStateEnteredDateColumnSeparator}{doingState}";

            return csv.TryGetField<DateTime?>(columnName, out var enteredDate) && enteredDate.HasValue
                ? DateTime.SpecifyKind(enteredDate.Value, DateTimeKind.Utc)
                : null;
        }

        private static IReadOnlyList<WorkItemStateTransition> BuildCurrentStateEnteredTransition(
            CsvReader csv, string stateEnteredDateColumn, string mappedState, StateCategories stateCategory)
        {
            if (stateCategory != StateCategories.Doing)
            {
                return [];
            }

            var stateEnteredDate = csv.GetField<DateTime?>(stateEnteredDateColumn);

            if (!stateEnteredDate.HasValue)
            {
                return [];
            }

            return
            [
                new WorkItemStateTransition
                {
                    FromState = string.Empty,
                    ToState = mappedState,
                    TransitionedAt = DateTime.SpecifyKind(stateEnteredDate.Value, DateTimeKind.Utc),
                }
            ];
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

        private static string GetStateEnteredDateColumn(WorkTrackingSystemConnection connection)
        {
            return connection.Options.SingleOrDefault(o => o.Key == CsvWorkTrackingOptionNames.StateEnteredDateHeader)?.Value ?? string.Empty;
        }

        public Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            throw new NotSupportedException("Write-back is not supported for CSV.");
        }
    }
}
