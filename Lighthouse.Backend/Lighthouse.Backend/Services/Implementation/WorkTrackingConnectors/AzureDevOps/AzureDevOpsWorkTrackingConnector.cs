using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Work.WebApi;
using AdoWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;
using Board = Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards.Board;
using LighthouseWorkItem = Lighthouse.Backend.Models.WorkItem;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    public class AzureDevOpsWorkTrackingConnector(
        ILogger<AzureDevOpsWorkTrackingConnector> logger,
        ICryptoService cryptoService)
        : IAzureDevOpsWorkTrackingConnector
    {
        private const int MaxChunkSize = 200;

        private static readonly ConcurrentDictionary<string, VssConnection> ConnectionCache = new();
        
        private static readonly ConcurrentDictionary<string, IVssHttpClient> ClientCache = new();
        
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> OrgLimiters = new();

        private static SemaphoreSlim GetLimiter(string url) => OrgLimiters.GetOrAdd(new Uri(url).Host, _ => new SemaphoreSlim(6));

        public async Task<IEnumerable<LighthouseWorkItem>> GetWorkItemsForTeam(Team team)
        {
            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);

            var workItemQuery = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, team.DataRetrievalValue, team.DoneItemsCutoffDays)}";

            var (adoWorkItems, additionalFieldReferences) = await FetchAdoWorkItemsByQuery(team, workItemQuery);
            var parentReferencesTask = GetParentReferenceForWorkItems(adoWorkItems, team);
            var workItems = await ConvertAdoWorkItemToLighthouseWorkItemBase(adoWorkItems, team, additionalFieldReferences);

            var parentReferences = await parentReferencesTask;

            foreach (var workItem in workItems)
            {
                var parentReference = GetParentReference(team, workItem, parentReferences);
                workItem.ParentReferenceId = parentReference;
            }

            return workItems.Select(workItem => new LighthouseWorkItem(workItem, team));
        }

        public async Task<List<Feature>> GetFeaturesForProject(Portfolio project)
        {
            logger.LogInformation("Getting Features of Type {WorkItemTypes} and Query '{Query}'", string.Join(", ", project.WorkItemTypes), project.DataRetrievalValue);

            var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.DataRetrievalValue, project.DoneItemsCutoffDays);
            var features = await GetFeaturesForProjectByQuery(project, query);

            logger.LogInformation("Found Features with IDs {FeatureIds}", string.Join(", ", features.Select(f => f.ReferenceId)));

            return features;
        }

        public async Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds)
        {
            logger.LogInformation("Getting Parent Features with IDs {ParentFeatureIds} for Project {ProjectName}", string.Join(", ", parentFeatureIds), project.Name);

            var whereClause = string.Join(" OR ", parentFeatureIds.Select(id => $"[{AzureDevOpsFieldNames.Id}] = {id}"));

            var query = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}] FROM WorkItems WHERE {whereClause}";

            var features = await GetFeaturesForProjectByQuery(project, query);

            logger.LogInformation("Found Parent Features with IDs {ParentFeatureIds}", string.Join(", ", features.Select(f => f.ReferenceId)));
            return features;
        }

        public async Task<bool> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            try
            {
                var witClient = GetWorkItemTrackingHttpClient(connection);
                
                await VerifyConnection(witClient);

                var fieldsValid = await VerifyFields(witClient, connection.AdditionalFieldDefinitions);
                return fieldsValid;
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
                logger.LogInformation("Validating Team Settings for Team {TeamName} and Query {Query}", team.Name, team.DataRetrievalValue);

                var query = PrepareQuery(team.WorkItemTypes, team.AllStates, team.DataRetrievalValue, team.DoneItemsCutoffDays);
                var witClient = GetWorkItemTrackingHttpClient(team.WorkTrackingSystemConnection);
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

        public async Task<bool> ValidatePortfolioSettings(Portfolio portfolio)
        {
            try
            {
                logger.LogInformation("Validating Project Settings for Project {ProjectName} and Query {Query}", portfolio.Name, portfolio.DataRetrievalValue);

                var query = PrepareQuery(portfolio.WorkItemTypes, portfolio.AllStates, portfolio.DataRetrievalValue, portfolio.DoneItemsCutoffDays);
                
                var (workItems, _) = await FetchAdoWorkItemsByQuery(portfolio, query);
                var workItemCount = workItems.Count();

                logger.LogInformation("Found a total of {NumberOfWorkItems} Features with specified Query", workItemCount);

                return workItemCount > 0;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Project Settings for Project {ProjectName}", portfolio.Name);
                return false;
            }
        }

        public async Task<IEnumerable<Board>> GetBoards(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            try
            {
                logger.LogInformation("Getting Boards for System {ConnectionName}", workTrackingSystemConnection.Name);

                var projects = await GetAllProjects(workTrackingSystemConnection);
                var boards = await GetBoardsForProjects(projects, workTrackingSystemConnection);

                return boards;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting boards");
                return [];
            }
        }

        public async Task<BoardInformation> GetBoardInformation(WorkTrackingSystemConnection workTrackingSystemConnection, string boardId)
        {
            var emptyInfo = new BoardInformation();
            
            try
            {
                var splitBoardId = boardId.Split('|');
                if (splitBoardId.Length != 2)
                {
                    return emptyInfo;
                }
                
                var projectId =  splitBoardId[0];
                var boardReference = splitBoardId[1];
                
                logger.LogInformation("Getting Board Information for Board {BoardId} in Project {ProjectId}", boardReference, projectId);
                
                var workClient = GetClient<WorkHttpClient>(workTrackingSystemConnection);
                var board = await GetBoardForProject(projectId, boardReference, workClient);

                var teamId = ExtractTeamIdFromBoard(board);
                var query = await ExtractWiqlQueryForBoard(workClient, projectId, teamId);
                
                var workItemTypes = ExtractWorkItemTypesFromBoard(board);
                
                var (toDoStates, doingStates, doneStates) = ExtractStateMappingFromBoard(board);
                
                return new BoardInformation
                {
                    DataRetrievalValue = query,
                    WorkItemTypes = workItemTypes,
                    ToDoStates = toDoStates,
                    DoingStates = doingStates,
                    DoneStates =  doneStates,
                };
            }  
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting board information for board {BoardId}", boardId);
                return emptyInfo;
            }
        }

        private string ExtractTeamIdFromBoard(Microsoft.TeamFoundation.Work.WebApi.Board board)
        {
            var team = (ReferenceLink)board.Links.Links["team"];
            var teamLink = team.Href;
            var teamId = teamLink.Split('/').Last();

            return teamId;
        }

        private static async Task<string> ExtractWiqlQueryForBoard(WorkHttpClient workClient, string projectId, string teamId)
        {
            
            var teamFieldValues = await workClient.GetTeamFieldValuesAsync(new TeamContext(projectId, teamId));
            var areaPathField = $"[{teamFieldValues.Field.ReferenceName}]";
            var allValues = teamFieldValues.Values;

            var queryParts = new List<string>();
            
            foreach (var fieldValue in allValues)
            {
                var operatorText = fieldValue.IncludeChildren ? "UNDER" : "=";
                
                queryParts.Add($"{areaPathField} {operatorText} \"{fieldValue.Value}\"");
            }

            return string.Join(" OR ",  queryParts);
        }

        private static (IEnumerable<string> toDoStates, IEnumerable<string> doingStates, IEnumerable<string> doneStates)
            ExtractStateMappingFromBoard(Microsoft.TeamFoundation.Work.WebApi.Board board)
        {
            var incomingColumns = board.Columns.Where(c => c.ColumnType == BoardColumnType.Incoming);
            var inProgressColumns =  board.Columns.Where(c => c.ColumnType == BoardColumnType.InProgress);
            var outgoingColumns = board.Columns.Where(c => c.ColumnType == BoardColumnType.Outgoing);

            var toDoStates = incomingColumns.SelectMany(c => c.StateMappings.Values).Distinct();
            var doingStates = inProgressColumns.SelectMany(c => c.StateMappings.Values).Distinct().Where(s => !toDoStates.Contains(s));
            var doneStates = outgoingColumns.SelectMany(c => c.StateMappings.Values).Distinct().Where(s => !toDoStates.Contains(s) &&  !doingStates.Contains(s));
            
            return (toDoStates, doingStates, doneStates);
        }

        private static IEnumerable<string> ExtractWorkItemTypesFromBoard(Microsoft.TeamFoundation.Work.WebApi.Board board)
        {
            return board.Columns.SelectMany(c => c.StateMappings).Select(sm => sm.Key).Distinct();
        }

        private async Task<Microsoft.TeamFoundation.Work.WebApi.Board> GetBoardForProject(string projectId,
            string boardId, WorkHttpClient workClient)
        {
            var board = await workClient.GetBoardAsync(new TeamContext(projectId), boardId);
            
            return board;
        }

        private async Task<IEnumerable<Board>> GetBoardsForProjects(IEnumerable<TeamProjectReference> projects,
            WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            var workClient = GetClient<WorkHttpClient>(workTrackingSystemConnection);

            var tasks = projects.Select(async project => await GetBoardsForProject(project, workClient));

            var boardsPerProject = await Task.WhenAll(tasks);
            return boardsPerProject.SelectMany(b => b);
        }

        private async Task<IEnumerable<Board>> GetBoardsForProject(TeamProjectReference project,
            WorkHttpClient workClient)
        {
            try
            {
                var boardsPerProject = await ExecuteWithThrottle(workClient.BaseAddress.ToString(),
                    () => workClient.GetBoardsAsync(new TeamContext(project.Id)));

                return boardsPerProject.Select(boardReference => new Board
                {
                    Id = $"{project.Id}|{boardReference.Id.ToString()}",
                    Name = $"{project.Name} - {boardReference.Name}"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Boards for Project {ProjectName}", project.Name);
                return [];
            }
        }

        private async Task<IEnumerable<TeamProjectReference>> GetAllProjects(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            var projectClient = GetClient<ProjectHttpClient>(workTrackingSystemConnection);

            var projects = await ExecuteWithThrottle(projectClient.BaseAddress.ToString(), 
                () => projectClient.GetProjects());

            return projects;
        }

        private async Task<List<Feature>> GetFeaturesForProjectByQuery(Portfolio portfolio, string query)
        {
            var (adoWorkItems, additionalFieldReferences) = await FetchAdoWorkItemsByQuery(portfolio, query);
            var parentReferencesTask = GetParentReferenceForWorkItems(adoWorkItems, portfolio);

            var workItemBase = await ConvertAdoWorkItemToLighthouseWorkItemBase(adoWorkItems, portfolio, additionalFieldReferences);
            
            var parentReferences = await parentReferencesTask;
            var features = new List<Feature>();

            foreach (var workItem in workItemBase)
            {
                var parentReference = GetParentReference(portfolio, workItem, parentReferences);
                var estimatedSize = GetEstimatedSize(portfolio, workItem);
                var owningTeam = GetOwningTeam(portfolio, workItem);

                var feature = new Feature(workItem)
                {
                    EstimatedSize = estimatedSize,
                    OwningTeam = owningTeam,
                    ParentReferenceId = parentReference,
                };

                features.Add(feature);
            }

            return features;
        }

        private static async Task VerifyConnection(WorkItemTrackingHttpClient witClient)
        {
            var query = $"SELECT [{AzureDevOpsFieldNames.Id}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.Id}] = 12";

            await witClient.QueryByWiqlAsync(new Wiql() { Query = query });
        }

        private async Task<bool> VerifyFields(WorkItemTrackingHttpClient witClient, IEnumerable<AdditionalFieldDefinition> additionalFieldDefinitions)
        {
            // Assign unique temp id's as they may all be 0 right now
            var tempId = -1;
            additionalFieldDefinitions.ForEach(f => f.Id = tempId--);
            
            var customFieldReferences = await GetCustomFieldReferences(witClient,  additionalFieldDefinitions);

            var missingReference = 0;
            foreach (var customFieldReference in customFieldReferences)
            {
                if (string.IsNullOrEmpty(customFieldReference.Value))
                {
                    logger.LogInformation("Additional Field {FieldName} does not exit", customFieldReference.Key);
                    missingReference++;
                }
            }

            return missingReference <= 0;
        }
        
        private async Task<Dictionary<int, string>> GetCustomFieldReferences(WorkItemTrackingHttpClient witClient, IEnumerable<AdditionalFieldDefinition> additionalFieldDefinitions)
        {
            var availableFields = await witClient.GetWorkItemFieldsAsync();
            
            var customFieldMappings = new Dictionary<int, string>();
            
            foreach (var additionalFieldDefinition in additionalFieldDefinitions)
            {
                var fieldReference = availableFields.SingleOrDefault(f =>
                    f.Name == additionalFieldDefinition.Reference ||
                    f.ReferenceName == additionalFieldDefinition.Reference)?.ReferenceName ?? string.Empty;
                
                customFieldMappings.Add(additionalFieldDefinition.Id, fieldReference);
            }
            
            return  customFieldMappings;
        }
        
        private async Task<(IEnumerable<AdoWorkItem>, Dictionary<int, string>)> FetchAdoWorkItemsByQuery(IWorkItemQueryOwner workItemQueryOwner, string query)
        {
            var additionalFieldsRef = new Dictionary<int, string>();
            
            try
            {
                var witClient = GetWorkItemTrackingHttpClient(workItemQueryOwner.WorkTrackingSystemConnection);
                var workItemReferences = await GetWorkItemReferencesByQuery(witClient, query);

                if (!workItemReferences.Any())
                {
                    return ([], additionalFieldsRef);
                }
                
                additionalFieldsRef = await GetCustomFieldReferences(witClient, workItemQueryOwner.WorkTrackingSystemConnection.AdditionalFieldDefinitions);

                var adoWOrkItemsById = await GetAdoWorkItemsById(workItemReferences.Select(wi => wi.Id), workItemQueryOwner, additionalFieldsRef.Select(f => f.Value));
                
                return (adoWOrkItemsById, additionalFieldsRef);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch ADO work items for query '{Query}'", query);
                return ([], new Dictionary<int, string>());
            }
        }

        private static async Task<T> ExecuteWithThrottle<T>(string url, Func<Task<T>> action)
        {
            var limiter = GetLimiter(url);
            await limiter.WaitAsync();
            try
            {
                return await ExecuteWithRetry(action);
            }
            finally
            {
                limiter.Release();
            }
        }

        private static bool IsRateLimited(VssServiceException ex)
        {
            var msg = ex.Message;
            return msg.Contains("Rate limits", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("exceeding usage of resource 'Concurrency'", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action)
        {
            var delay = TimeSpan.FromSeconds(1);
            for (var attempt = 0; attempt < 6; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (VssServiceException ex) when (IsRateLimited(ex))
                {
                    await Task.Delay(delay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)));
                    delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));
                }
                catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                {
                    await Task.Delay(delay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)));
                    delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));
                }
            }
            return await action();
        }

        private async Task<IEnumerable<WorkItemBase>> ConvertAdoWorkItemToLighthouseWorkItemBase(IEnumerable<AdoWorkItem> adoWorkItems, IWorkItemQueryOwner workItemQueryOwner, Dictionary<int, string> fieldReferences)
        {
            var throttler = new SemaphoreSlim(8);
            var tasks = adoWorkItems.Select(async wi =>
            {
                await throttler.WaitAsync();
                try { return await ConvertAdoWorkItemToLighthouseWorkItem(wi, workItemQueryOwner, fieldReferences); }
                finally { throttler.Release(); }
            });
            return await Task.WhenAll(tasks);
        }

        private async Task<IEnumerable<WorkItemReference>> GetWorkItemReferencesByQuery(WorkItemTrackingHttpClient witClient, string query)
        {
            try
            {
                var result = await ExecuteWithThrottle(witClient.BaseAddress!.ToString(),
                    () => witClient.QueryByWiqlAsync(new Wiql { Query = query }));

                return result.WorkItems ?? [];
            }
            catch (VssServiceException ex)
            {
                logger.LogError(ex, "Error while querying Work Items with Query '{Query}'", query);
                return [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while querying Work Items with Query '{Query}'", query);
                return [];
            }
        }

        private async Task<IEnumerable<AdoWorkItem>> GetAdoWorkItemsById(IEnumerable<int> workItemIds, IWorkItemQueryOwner workItemQueryOwner, IEnumerable<string> additionalFields)
        {
            if (!workItemIds.Any())
            {
                return [];
            }

            logger.LogDebug("Getting Work Item with IDs {ItemIds}", string.Join(",", workItemIds));

            var witClient = GetWorkItemTrackingHttpClient(workItemQueryOwner.WorkTrackingSystemConnection);

            var fields = new List<string>
            {
                AzureDevOpsFieldNames.State,
                AzureDevOpsFieldNames.Title,
                AzureDevOpsFieldNames.WorkItemType,
                AzureDevOpsFieldNames.StackRank,
                AzureDevOpsFieldNames.BacklogPriority,
                AzureDevOpsFieldNames.CreatedDate,
                AzureDevOpsFieldNames.Tags,
            };

            fields.AddRange(additionalFields.Where(f => !string.IsNullOrEmpty(f)));

            return await GetWorkItemsInChunks(workItemIds, witClient, WorkItemExpand.Links, fields);
        }

        private static async Task<IEnumerable<AdoWorkItem>> GetWorkItemsInChunks(IEnumerable<int> workItemIds, WorkItemTrackingHttpClient witClient, WorkItemExpand expand, IEnumerable<string> fields)
        {
            var url = witClient.BaseAddress!.ToString();
            var workItems = new List<AdoWorkItem>();

            foreach (var chunk in workItemIds.Chunk(MaxChunkSize))
            {
                var result = await ExecuteWithThrottle(url, () => witClient.GetWorkItemsAsync(chunk, fields, expand: expand));
                workItems.AddRange(result);
            }

            return workItems;
        }

        private async Task<WorkItemBase> ConvertAdoWorkItemToLighthouseWorkItem(AdoWorkItem workItem, IWorkItemQueryOwner workItemQueryOwner, Dictionary<int, string> additionalFieldDefinitions)
        {
            var state = workItem.ExtractStateFromWorkItem();
            var stateCategory = workItemQueryOwner.MapStateToStateCategory(state);

            var (startedDate, closedDate) = await GetStartedAndClosedDateForWorkItem(workItemQueryOwner, stateCategory, workItem.Id);

            var additionalFields = new Dictionary<int, string?>();
            foreach (var additionalField in additionalFieldDefinitions)
            {
                additionalFields[additionalField.Key] = ExtractFieldValue(workItem, additionalField.Value);
            }

            return new WorkItemBase
            {
                ReferenceId = $"{workItem.Id}",
                Name = workItem.ExtractTitleFromWorkItem(),
                Type = workItem.ExtractTypeFromWorkItem(),
                State = state,
                StateCategory = stateCategory,
                Url = workItem.ExtractUrlFromWorkItem(),
                Order = workItem.ExtractStackRankFromWorkItem(),
                CreatedDate = workItem.ExtractCreatedDateFromWorkItem(),
                StartedDate = startedDate,
                ClosedDate = closedDate,
                Tags = workItem.ExtractTagsFromWorkItem(),
                AdditionalFieldValues = additionalFields,
            };
        }

        private async Task<(DateTime? startedDate, DateTime? closedDate)> GetStartedAndClosedDateForWorkItem(IWorkItemQueryOwner workItemQueryOwner, StateCategories stateCategory, int? workItemId)
        {
            var witClient = GetWorkItemTrackingHttpClient(workItemQueryOwner.WorkTrackingSystemConnection);
            DateTime? startedDate = null;
            DateTime? closedDate = null;

            if (stateCategory == StateCategories.Done)
            {
                startedDate = await GetStateTransitionDateThrottled(witClient, workItemId, workItemQueryOwner.DoingStates, workItemQueryOwner.DoneStates);
                closedDate = await GetStateTransitionDateThrottled(witClient, workItemId, workItemQueryOwner.DoneStates, []);
            }
            else if (stateCategory == StateCategories.Doing)
            {
                startedDate = await GetStateTransitionDateThrottled(witClient, workItemId, workItemQueryOwner.DoingStates, workItemQueryOwner.DoneStates);
            }

            if (startedDate == null && closedDate != null)
            {
                startedDate = closedDate;
            }

            return (startedDate, closedDate);
        }

        private static async Task<DateTime?> GetStateTransitionDateThrottled(WorkItemTrackingHttpClient witClient, int? workItemId, List<string> targetStates, List<string> statesToIgnore)
        {
            if (!workItemId.HasValue) return null;

            var revisions = await ExecuteWithThrottle(witClient.BaseAddress!.ToString(), () => witClient.GetRevisionsAsync(workItemId.Value));
            var movedToStateCategory = new List<DateTime>();
            var previousState = string.Empty;

            foreach (var revision in revisions)
            {
                if (RevisionWasChangingState(revision, out var result))
                {
                    var isRelevantCategory = targetStates.IsItemInList(result.state) && !targetStates.IsItemInList(previousState) && !statesToIgnore.IsItemInList(previousState);
                    if (isRelevantCategory) movedToStateCategory.Add(result.changedDate);
                    previousState = result.state;
                }
            }

            var last = movedToStateCategory.OrderByDescending(d => d).FirstOrDefault();
            return last == default ? null : DateTime.SpecifyKind(last, DateTimeKind.Utc);
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

        private async Task<Dictionary<string, string>> GetParentReferenceForWorkItems(IEnumerable<AdoWorkItem> adoWorkItems, WorkTrackingSystemOptionsOwner workTrackingSystemOptionOwner)
        {
            if (workTrackingSystemOptionOwner.ParentOverrideAdditionalFieldDefinitionId.HasValue)
            {
                // No need to load stuff if we have an override anyway.
                return new Dictionary<string, string>();
            }
            
            var itemIds = adoWorkItems.Select(wi => wi.Id ?? -1).Where(i => i >= 0).ToList();

            if (itemIds.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            logger.LogDebug("Getting Parent Ids for Work Items with IDs {ItemIds}", string.Join(",", itemIds));

            return await GetParentReferencesFromRelationFields(workTrackingSystemOptionOwner, itemIds);
        }

        private async Task<Dictionary<string, string>> GetParentReferencesFromRelationFields(WorkTrackingSystemOptionsOwner workTrackingSystemOptionOwner, List<int> itemIds)
        {
            logger.LogDebug("Getting Parent Ids for Work Items with Parent Field");

            var parentReferences = new Dictionary<string, string>();
            foreach (var id in itemIds)
            {
                parentReferences.Add($"{id}", string.Empty);
            }

            var witClient = GetWorkItemTrackingHttpClient(workTrackingSystemOptionOwner.WorkTrackingSystemConnection);
            var workItemsWithParentRelation = await GetWorkItemsInChunks(itemIds, witClient, WorkItemExpand.Relations, []);

            foreach (var adoWorkItem in workItemsWithParentRelation)
            {
                var parentReference = adoWorkItem.ExtractParentFromWorkItem();
                parentReferences[adoWorkItem.Id.ToString() ?? "-1"] = parentReference;
            }

            return parentReferences;
        }

        private static string GetOwningTeam(Portfolio portfolio, WorkItemBase workItem)
        {
            var owningTeam = workItem.GetAdditionalFieldValue(portfolio.FeatureOwnerAdditionalFieldDefinitionId) ?? string.Empty;

            return owningTeam;
        }

        private static int GetEstimatedSize(Portfolio portfolio, WorkItemBase workItem)
        {
            var estimatedSize = 0;
            var estimatedSizeRawValue = workItem.GetAdditionalFieldValue(portfolio.SizeEstimateAdditionalFieldDefinitionId);
            
            if (!string.IsNullOrEmpty(estimatedSizeRawValue))
            {
                estimatedSize = GetEstimatedSizeForItem(estimatedSizeRawValue);
            }

            return estimatedSize;
        }

        private static int GetEstimatedSizeForItem(string estimatedSizeRawValue)
        {
            if (string.IsNullOrEmpty(estimatedSizeRawValue))
            {
                return 0;
            }

            if (int.TryParse(estimatedSizeRawValue, out var estimatedSize))
            {
                return estimatedSize;
            }

            // Try parsing double because for sure someone will have the brilliant idea to make this a decimal
            if (double.TryParse(estimatedSizeRawValue, out var estimateAsDouble))
            {
                return (int)estimateAsDouble;
            }

            return 0;
        }

        private static string GetParentReference(IWorkItemQueryOwner owner, WorkItemBase workItem, Dictionary<string, string> parentReferences)
        {
            var parentReference =
                workItem.GetAdditionalFieldValue(owner.ParentOverrideAdditionalFieldDefinitionId);

            return string.IsNullOrEmpty(parentReference) ? parentReferences[workItem.ReferenceId] : parentReference;
        }

        private static string ExtractFieldValue(AdoWorkItem adoWorkItem, string fieldName)
        {
            if (!string.IsNullOrEmpty(fieldName) && adoWorkItem.Fields.TryGetValue(fieldName, out var fieldValue))
            {
                return fieldValue.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static string PrepareQuery(
            IEnumerable<string> includedWorkItemTypes,
            IEnumerable<string> includedStates,
            string query,
            int cutOffDays)
        {
            return PrepareQuery(includedWorkItemTypes, includedStates, query, null, cutOffDays);
        }

        private static string PrepareQuery(
            IEnumerable<string> includedWorkItemTypes,
            IEnumerable<string> includedStates,
            string query,
            string? extraField,
            int cutOffDays)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(includedWorkItemTypes);
            var stateQuery = PrepareStateQuery(includedStates);

            var extraFieldsQuery = string.Empty;
            if (!string.IsNullOrEmpty(extraField))
            {
                extraFieldsQuery = $", [{extraField}]";
            }

            var cutoffDateFilter = PrepareCutoffDateFilter(cutOffDays);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}]{extraFieldsQuery} FROM WorkItems WHERE ({query}) " +
                $"{workItemsQuery} " +
                $"{stateQuery} " +
                $"{cutoffDateFilter}";

            return wiql;
        }

        private static string PrepareCutoffDateFilter(int cutOffDays)
        {
            if (cutOffDays <= 0)
            {
                return string.Empty;
            }
            
            var cutoffDate = DateTime.UtcNow.AddDays(-cutOffDays);
            
            var cutoffDateString = cutoffDate.ToString("yyyy-MM-dd");

            return $"AND ([{AzureDevOpsFieldNames.ClosedDate}] = '' OR [{AzureDevOpsFieldNames.ClosedDate}] >= '{cutoffDateString}') ";
        }

        private static string PrepareWorkItemTypeQuery(IEnumerable<string> workItemTypes)
        {
            return PrepareGenericQuery(workItemTypes, AzureDevOpsFieldNames.WorkItemType, "OR", "=");
        }

        private static string PrepareStateQuery(IEnumerable<string> includedStates)
        {
            return PrepareGenericQuery(includedStates, AzureDevOpsFieldNames.State, "OR", "=");
        }

        private static string PrepareGenericQuery(IEnumerable<string> options, string fieldName, string queryOperator, string queryComparison)
        {
            var query = string.Join($" {queryOperator} ", options.Select(opt => $"[{fieldName}] {queryComparison} '{opt}'"));

            query = options.Any() ? $"AND ({query}) " : string.Empty;

            return query;
        }
        
        private WorkItemTrackingHttpClient GetWorkItemTrackingHttpClient(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            return GetClient<WorkItemTrackingHttpClient>(workTrackingSystemConnection);
        }

        private TClient GetClient<TClient>(WorkTrackingSystemConnection workTrackingSystemConnection) where TClient : IVssHttpClient
        {
            var (connection, key) = GetConnectionForWorkTrackingSystem(workTrackingSystemConnection);
            var cacheKey = $"{typeof(TClient).FullName}_{key}";
            
            return (TClient)ClientCache.GetOrAdd(cacheKey, _ => connection.GetClient<TClient>());
        }

        private (VssConnection connection, string key) GetConnectionForWorkTrackingSystem(
            WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            var url = workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.Url);
            var encryptedPersonalAccessToken = workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken);
            var personalAccessToken = cryptoService.Decrypt(encryptedPersonalAccessToken);
            var key = $"{url}|{personalAccessToken}";

            var requestTimeoutInSeconds =
                workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey<int>(AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds) ?? 100;

            var connection = ConnectionCache.GetOrAdd(key, _ =>
            {
                var c = CreateConnection(url, personalAccessToken);
                c.Settings.SendTimeout = TimeSpan.FromSeconds(requestTimeoutInSeconds);
                return c;
            });
            
            return (connection, key);
        }

        private static VssConnection CreateConnection(string azureDevOpsUrl, string personalAccessToken)
        {
            var azureDevOpsUri = new Uri(azureDevOpsUrl);
            var credentials = new VssBasicCredential(personalAccessToken, string.Empty);
            return new VssConnection(azureDevOpsUri, credentials);
        }
    }
}
