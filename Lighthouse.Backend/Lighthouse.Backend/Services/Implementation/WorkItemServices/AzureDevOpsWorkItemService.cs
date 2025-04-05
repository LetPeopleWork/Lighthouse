using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.WorkTracking.AzureDevOps;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

using AdoWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;
using LighthouseWorkItem = Lighthouse.Backend.Models.WorkItem;

namespace Lighthouse.Backend.Services.Implementation.WorkItemServices
{
    public class AzureDevOpsWorkItemService : IWorkItemService
    {
        private readonly ILogger<AzureDevOpsWorkItemService> logger;
        private readonly ICryptoService cryptoService;

        public AzureDevOpsWorkItemService(ILogger<AzureDevOpsWorkItemService> logger, ICryptoService cryptoService)
        {
            this.logger = logger;
            this.cryptoService = cryptoService;
        }

        public async Task<IEnumerable<LighthouseWorkItem>> GetChangedWorkItemsSinceLastTeamUpdate(Team team)
        {
            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);

            var lastUpdatedFilter = PrepareLastUpdatedQuery(team.TeamUpdateTime);
            var workItemQuery = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, team.WorkItemQuery, team.AdditionalRelatedField ?? string.Empty)} {lastUpdatedFilter}";

            var workItemBase = await CreateWorkItemsForAllItemsMatchingQuery(team, workItemQuery);

            var workItems = new List<LighthouseWorkItem>();
            
            foreach (var workItem in workItemBase)
            {
                var parentReference = await GetParentIdForWorkItem(int.Parse(workItem.ReferenceId), team);

                workItems.Add(new LighthouseWorkItem(workItem, team, parentReference));
            }

            return workItems;
        }

        public async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            logger.LogInformation("Getting Features of Type {WorkItemTypes} and Query '{Query}'", string.Join(", ", project.WorkItemTypes), project.WorkItemQuery);

            var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.WorkItemQuery);
            var workItemBase = await CreateWorkItemsForAllItemsMatchingQuery(project, query);

            var features = new List<Feature>();

            foreach (var workItem in workItemBase)
            {
                var feature = new Feature(workItem)
                {
                    EstimatedSize = await GetEstimatedSizeForItem(workItem.ReferenceId, project),
                    OwningTeam = await GetFeatureOwnerByField(workItem.ReferenceId, project)
                };

                features.Add(feature);
            }

            logger.LogInformation("Found Features with IDs {FeatureIds}", string.Join(", ", features.Select(f => f.ReferenceId)));

            return features;
        }

        public async Task<Dictionary<string, int>> GetHistoricalFeatureSize(Project project)
        {
            var historicalFeatureSize = new Dictionary<string, int>();

            logger.LogInformation("Getting Child Items for Features in Project {Project} for Work Item Types {WorkItemTypes} and Query '{Query}'", project.Name, string.Join(", ", project.WorkItemTypes), project.HistoricalFeaturesWorkItemQuery);

            var witClient = GetClientService(project.WorkTrackingSystemConnection);

            var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.HistoricalFeaturesWorkItemQuery);
            var features = await GetWorkItemReferencesByQuery(witClient, query);

            foreach (var featureId in features.Select(f => f.Id.ToString()))
            {
                historicalFeatureSize.Add(featureId, 0);

                foreach (var team in project.Teams)
                {
                    var totalItems = await GetRelatedWorkItems(team, featureId);
                    historicalFeatureSize[featureId] += totalItems;
                }
            }

            var emptyFeatures = historicalFeatureSize.Where(kvp => kvp.Value <= 0).Select(kvp => kvp.Key).ToList();
            foreach (var featureId in emptyFeatures)
            {
                historicalFeatureSize.Remove(featureId);
            }

            return historicalFeatureSize;
        }

        public async Task<List<string>> GetWorkItemsIdsForTeamWithAdditionalQuery(Team team, string additionalQuery)
        {
            logger.LogInformation("Getting Work Items for Team {TeamName}, Item Types {WorkItemTypes} and Additional Items Query '{Query}'", team.Name, string.Join(", ", team.WorkItemTypes), additionalQuery);

            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            var workItemsQuery = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, additionalQuery, team.AdditionalRelatedField ?? string.Empty)} AND {team.WorkItemQuery}";

            var matchingWorkItems = await GetWorkItemReferencesByQuery(witClient, workItemsQuery);

            var matchingWorkItemsIds = matchingWorkItems.Select(x => x.Id.ToString()).ToList();
            logger.LogDebug("Found following Work Items {MatchingWorkItems}", string.Join(", ", matchingWorkItemsIds));

            return matchingWorkItemsIds;
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            logger.LogInformation("Getting Adjacent Order Index for items {ExistingItemsOrder} in order {RelativeOrder}", string.Join(", ", existingItemsOrder), relativeOrder);

            string? result;
            if (!existingItemsOrder.Any())
            {
                result = "0";
            }
            else
            {
                var orderAsInt = ConvertToIntegers(existingItemsOrder);

                if (relativeOrder == RelativeOrder.Above)
                {
                    var highestOrder = orderAsInt.Max();
                    result = $"{highestOrder + 1}";
                }
                else
                {
                    var lowestOrder = orderAsInt.Min();
                    result = $"{lowestOrder - 1}";
                }
            }

            logger.LogInformation("Adjacent Order Index for items {ExistingItemsOrder} in order {RelativeOrder}: {Result}", string.Join(", ", existingItemsOrder), relativeOrder, result);

            return result;
        }

        public async Task<bool> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            try
            {
                var witClient = GetClientService(connection);
                var query = $"SELECT [{AzureDevOpsFieldNames.Id}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.Id}] = 12";

                await witClient.QueryByWiqlAsync(new Wiql() { Query = query });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidateTeamSettings(Team team)
        {
            try
            {
                logger.LogInformation("Validating Team Settings for Team {TeamName} and Query {Query}", team.Name, team.WorkItemQuery);
                var witClient = GetClientService(team.WorkTrackingSystemConnection);

                var query = PrepareQuery(team.WorkItemTypes, team.AllStates, team.WorkItemQuery);
                var workItems = await GetWorkItemReferencesByQuery(witClient, query);

                var workItemCount = workItems.Count();

                logger.LogInformation("Found a total of {NumberOfWorkItems} Work Items with specified Query", workItemCount);

                return workItemCount > 0;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Team Settings for Team {TeamName}", team.Name);
                return false;
            }
        }

        public async Task<bool> ValidateProjectSettings(Project project)
        {
            try
            {
                logger.LogInformation("Validating Project Settings for Project {ProjectName} and Query {Query}", project.Name, project.WorkItemQuery);
                
                var witClient = GetClientService(project.WorkTrackingSystemConnection);
                var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.WorkItemQuery);

                var workItems = await GetWorkItemReferencesByQuery(witClient, query);
                var workItemCount = workItems.Count();

                logger.LogInformation("Found a total of {NumberOfWorkItems} Features with specified Query", workItemCount);

                return workItemCount > 0;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Project Settings for Project {ProjectName}", project.Name);
                return false;
            }
        }

        private async Task<int> GetRelatedWorkItems(Team team, string relatedWorkItemId)
        {
            var witClient = GetClientService(team.WorkTrackingSystemConnection);
            var allItemsQuery = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, team.WorkItemQuery)} {PrepareRelatedItemQuery(relatedWorkItemId, team.AdditionalRelatedField)}";

            var totalWorkItems = await GetWorkItemReferencesByQuery(witClient, allItemsQuery);

            return totalWorkItems.Count();
        }

        private async Task<IEnumerable<WorkItemBase>> CreateWorkItemsForAllItemsMatchingQuery(IWorkItemQueryOwner workItemQueryOwner, string query)
        {
            var witClient = GetClientService(workItemQueryOwner.WorkTrackingSystemConnection);
            var workItemReferences = await GetWorkItemReferencesByQuery(witClient, query);

            var tasks = workItemReferences.Select(async workItemReference =>
            {
                var workItem = await GetAdoWorkItemById(workItemReference.Id, workItemQueryOwner);
                var workItemBase = await CreateWorkItemFromAdoWorkItem(workItem, workItemQueryOwner);
                return workItemBase;
            });

            var workItems = await Task.WhenAll(tasks);

            return workItems;
        }

        private async Task<IEnumerable<WorkItemReference>> GetWorkItemReferencesByQuery(WorkItemTrackingHttpClient witClient, string query)
        {
            try
            {
                var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = query });
                return queryResult.WorkItems;
            }
            catch (VssServiceException ex)
            {
                logger.LogError(ex, "Error while querying Work Items with Query '{Query}'", query);
                return [];
            }
        }

        private async Task<AdoWorkItem> GetAdoWorkItemById(int workItemId, IWorkItemQueryOwner workItemQueryOwner, params string[] additionalFields)
        {
            logger.LogDebug("Getting Work Item with ID {ItemId}", workItemId);

            var witClient = GetClientService(workItemQueryOwner.WorkTrackingSystemConnection);
            
            var fields = new List<string>
            {
                AzureDevOpsFieldNames.State,
                AzureDevOpsFieldNames.Title,
                AzureDevOpsFieldNames.WorkItemType,
                AzureDevOpsFieldNames.StackRank,
                AzureDevOpsFieldNames.BacklogPriority,
            };

            fields.AddRange(additionalFields.Where(f => !string.IsNullOrEmpty(f)));

            var workItem = await witClient.GetWorkItemAsync(workItemId, fields);

            return workItem;
        }

       private async Task<WorkItemBase> CreateWorkItemFromAdoWorkItem(AdoWorkItem workItem, IWorkItemQueryOwner workItemQueryOwner)
        {
            var state = workItem.ExtractStateFromWorkItem();

            var (startedDate, closedDate) = await GetStartedAndClosedDateForWorkItem(workItemQueryOwner, workItem.Id);

            return new WorkItemBase
            {
                ReferenceId = $"{workItem.Id}",
                Name = workItem.ExtractTitleFromWorkItem(),
                Type = workItem.ExtractTypeFromWorkItem(),
                State = state,
                StateCategory = workItemQueryOwner.MapStateToStateCategory(state),
                Url = workItem.ExtractUrlFromWorkItem(),
                Order = workItem.ExtractStackRankFromWorkItem(),
                StartedDate = startedDate,
                ClosedDate = closedDate,
            };
        }

        private async Task<(DateTime? startedDate, DateTime? closedDate)> GetStartedAndClosedDateForWorkItem(IWorkItemQueryOwner workItemQueryOwner, int? workItemId)
        {
            var witClient = GetClientService(workItemQueryOwner.WorkTrackingSystemConnection);
            var startedDate = await GetStateTransitionDate(witClient, workItemId, workItemQueryOwner.DoingStates);
            var closedDate = await GetStateTransitionDate(witClient, workItemId, workItemQueryOwner.DoneStates);

            // It can happen that no started date is set if an item is created directly in closed state. Assume that the closed date is the started date in this case.
            if (startedDate == null && closedDate != null)
            {
                startedDate = closedDate;
            }

            return (startedDate, closedDate);
        }

        private static async Task<DateTime?> GetStateTransitionDate(WorkItemTrackingHttpClient witClient, int? workItemId, List<string> states)
        {
            DateTime? latestStateChangeDate = null;
            string? previousState = null;

            if (!workItemId.HasValue)
            {
                return latestStateChangeDate;
            }

            var revisions = await witClient.GetRevisionsAsync(workItemId.Value);

            foreach (var revision in revisions)
            {
                if (RevisionWasChangingState(revision, out (string state, DateTime changedDate) result))
                {
                    var isRelevantCategory = states.Contains(result.state) && (previousState == null || !states.Contains(previousState));
                    var isRelevantStateChange = !latestStateChangeDate.HasValue || result.changedDate > latestStateChangeDate.Value;

                    if (isRelevantStateChange && isRelevantCategory)
                    {
                        latestStateChangeDate = result.changedDate;
                    }

                    previousState = result.state;
                }
            }

            return latestStateChangeDate;
        }

        private static bool RevisionWasChangingState(AdoWorkItem revision, out (string state, DateTime changedDate) result)
        {
            result.state = string.Empty;
            result.changedDate = DateTime.MinValue;

            if (revision.Fields.TryGetValue(AzureDevOpsFieldNames.State, out var stateValue) &&
                    revision.Fields.TryGetValue(AzureDevOpsFieldNames.ChangedDate, out var changedDateValue))
            {
                result.state = stateValue.ToString() ?? string.Empty;
                result.changedDate = (DateTime?)changedDateValue ?? DateTime.MinValue;
            }

            return !string.IsNullOrEmpty(result.state) && result.changedDate != DateTime.MinValue;
        }

        private async Task<string> GetParentIdForWorkItem(int workItemId, Team team)
        {
            var witClient = GetClientService(team.WorkTrackingSystemConnection);
            var adoWorkItem = await witClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations);

            if (!string.IsNullOrEmpty(team.AdditionalRelatedField))
            {
                return await GetFieldValue(workItemId, team, team.AdditionalRelatedField);
            }

            return adoWorkItem.ExtractParentFromWorkItem();
        }
        private async Task<int> GetEstimatedSizeForItem(string referenceId, Project project)
        {
            if (string.IsNullOrEmpty(project.SizeEstimateField))
            {
                return 0;
            }

            var estimationFieldValue = await GetFieldValue(int.Parse(referenceId), project, project.SizeEstimateField);

            // Try parsing double because for sure someone will have the brilliant idea to make this a decimal
            if (double.TryParse(estimationFieldValue, out var estimateAsDouble))
            {
                return (int)estimateAsDouble;
            }

            return 0;
        }

        private async Task<string> GetFeatureOwnerByField(string referenceId, Project project)
        {
            if (string.IsNullOrEmpty(project.FeatureOwnerField))
            {
                return string.Empty;
            }

            var featureOwnerFieldValue = await GetFieldValue(int.Parse(referenceId), project, project.FeatureOwnerField);

            return featureOwnerFieldValue;
        }

        private async Task<string> GetFieldValue(int referenceId, IWorkItemQueryOwner workItemQueryOwner, string fieldName)
        {
            try
            {
                var workItem = await GetAdoWorkItemById(referenceId, workItemQueryOwner, fieldName);

                if (workItem == null)
                {
                    return string.Empty;
                }

                return workItem.Fields[fieldName].ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static List<int> ConvertToIntegers(IEnumerable<string> orderAsStrings)
        {
            var orderAsInt = new List<int>();

            foreach (var order in orderAsStrings)
            {
                if (int.TryParse(order, out int number))
                {
                    orderAsInt.Add(number);
                }
            }

            return orderAsInt;
        }

        private static string PrepareQuery(
            IEnumerable<string> includedWorkItemTypes,
            IEnumerable<string> includedStates,
            string query)
        {
            return PrepareQuery(includedWorkItemTypes, includedStates, query, string.Empty);
        }

        private static string PrepareQuery(
            IEnumerable<string> includedWorkItemTypes,
            IEnumerable<string> includedStates,
            string query,
            string extraField)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(includedWorkItemTypes);
            var stateQuery = PrepareStateQuery(includedStates);

            var extraFieldsQuery = string.Empty;
            if (!string.IsNullOrEmpty(extraField))
            {
                extraFieldsQuery = $", [{extraField}]";
            }

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}]{extraFieldsQuery} FROM WorkItems WHERE ({query}) " +
                $"{workItemsQuery} " +
                $"{stateQuery}";

            return wiql;
        }

        private static string PrepareWorkItemTypeQuery(IEnumerable<string> workItemTypes)
        {
            return PrepareGenericQuery(workItemTypes, AzureDevOpsFieldNames.WorkItemType, "OR", "=");
        }

        private static string PrepareStateQuery(IEnumerable<string> includedStates)
        {
            return PrepareGenericQuery(includedStates, AzureDevOpsFieldNames.State, "OR", "=");
        }

        private static string PrepareLastUpdatedQuery(DateTime lastUpdated)
        {
            var query = string.Empty;

            var updateHorizon = lastUpdated;
            if (lastUpdated != DateTime.MinValue)
            {
                query = $" AND ([{AzureDevOpsFieldNames.ChangedDate}] >= '{updateHorizon:yyyy-MM-dd}T00:00:00.0000000Z')";
            }

            return query;
        }

        private static string PrepareGenericQuery(IEnumerable<string> options, string fieldName, string queryOperator, string queryComparison)
        {
            var query = string.Join($" {queryOperator} ", options.Select(options => $"[{fieldName}] {queryComparison} '{options}'"));

            if (options.Any())
            {
                query = $"AND ({query}) ";
            }
            else
            {
                query = string.Empty;
            }

            return query;
        }

        private static string PrepareRelatedItemQuery(string relatedItemId, string? additionalRelatedField)
        {
            var additionalRelatedFieldQuery = string.Empty;
            if (!string.IsNullOrEmpty(additionalRelatedField))
            {
                additionalRelatedFieldQuery = $"OR [{additionalRelatedField}] = '{relatedItemId}'";
            }

            return $"AND ([System.Parent] = '{relatedItemId}' {additionalRelatedFieldQuery})";
        }

        private WorkItemTrackingHttpClient GetClientService(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            var url = workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.Url);
            var encryptedPersonalAccessToken = workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken);

            var personalAccessToken = cryptoService.Decrypt(encryptedPersonalAccessToken);

            var connection = CreateConnection(url, personalAccessToken);
            return connection.GetClient<WorkItemTrackingHttpClient>();
        }

        private static VssConnection CreateConnection(string azureDevOpsUrl, string personalAccessToken)
        {
            var azureDevOpsUri = new Uri(azureDevOpsUrl);
            var credentials = new VssBasicCredential(personalAccessToken, string.Empty);

            return new VssConnection(azureDevOpsUri, credentials);
        }
    }
}
