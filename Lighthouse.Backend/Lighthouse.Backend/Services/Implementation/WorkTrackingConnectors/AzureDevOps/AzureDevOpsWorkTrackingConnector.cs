using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Models.Validation;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Work.WebApi;
using AdoWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;
using Board = Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards.Board;
using LighthouseWorkItem = Lighthouse.Backend.Models.WorkItem;
using WorkItemStateTransition = Lighthouse.Backend.Models.WorkItemStateTransition;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    public class AzureDevOpsWorkTrackingConnector(
        ILogger<AzureDevOpsWorkTrackingConnector> logger,
        IWorkTrackingAuthStrategyFactory authStrategyFactory,
        TimeProvider timeProvider)
        : IAzureDevOpsWorkTrackingConnector
    {
        private const int MaxChunkSize = 200;

        private static readonly string[] TheChangeStampOnly = [AzureDevOpsFieldNames.ChangedDate];

        private static readonly string[] NoFieldsBesideTheRelations = [];

        private static readonly HashSet<string> ResourceLinks = new(StringComparer.OrdinalIgnoreCase) { "AttachedFile", "Hyperlink", "ArtifactLink" };

        private static readonly HashSet<string> HierarchyLinks = new(StringComparer.OrdinalIgnoreCase) { "System.LinkTypes.Hierarchy-Forward", "System.LinkTypes.Hierarchy-Reverse" };

        /// <summary>
        /// What one read of a batch of work items' relations yields, per work item: the parent it hangs
        /// under, and the other items it is waiting on. Both come out of the same response because that
        /// response is expensive - a second read of the same items to fetch the other half would double
        /// the cost of every refresh.
        /// </summary>
        private sealed record RelationsOfWorkItems(
            Dictionary<string, string> ParentReferences,
            Dictionary<string, List<string>> DependencyReferences,
            List<string> RelationTypesSeen)
        {
            public static RelationsOfWorkItems None() => new([], [], []);
        }

        /// <summary>
        /// Whether a fetch needs the relations for the dependencies, and which end it reads them off: the
        /// relation end the Portfolio's Dependency Field named, or the Predecessor link when that is null.
        /// A named relation type that cannot say which end waits is read as nothing, and says why: it names no
        /// end, so the Feature falls back to its Dependency Field's own value, which holds nothing.
        /// </summary>
        private sealed record DependenciesFromRelations(bool AreRead, string? NamedRelationEnd, WhyNothingIsRead? WhyNothingIsRead)
        {
            public static DependenciesFromRelations NotRead() => new(false, null, null);

            public static DependenciesFromRelations For(Portfolio portfolio, AdditionalFieldResolution resolution)
            {
                var dependencyField = portfolio.DependencyOverrideAdditionalFieldDefinitionId;
                var namedRelationEnd = resolution.TheRelationEndNamedBy(dependencyField);
                var whyNothingIsRead = resolution.WhyNothingIsReadFor(dependencyField);
                if (whyNothingIsRead is not null)
                {
                    return new(false, null, whyNothingIsRead);
                }

                var areRead = DependencySourceSelector.ReadsTheTrackersOwnLink(portfolio) || namedRelationEnd is not null;

                return new(areRead, namedRelationEnd, null);
            }
        }

        /// <summary>Why a relation type the Dependency Field names is read as nothing, and what it saw there.</summary>
        private sealed record WhyNothingIsRead(RefreshFindingKind Kind, List<string> Seen);

        public bool SupportsTransitionHistory(WorkTrackingSystemConnection connection) => true;

        /// <summary>
        /// Yes, unconditionally: a WIQL is the same request on every Azure DevOps deployment, so unlike Jira
        /// there is nothing about the instance the sweep has to discover first.
        /// </summary>
        public bool SupportsIncrementalSync(WorkTrackingSystemConnection connection) => true;

        /// <summary>
        /// Phase one of the two-phase fetch: the very query <see cref="GetWorkItemsForTeam(Team)"/> issues,
        /// answered with identity and the change stamp only. Removal is "stored minus swept", so a sweep that
        /// answers for fewer records than the query returned would delete the difference - it refuses instead,
        /// and the caller falls back to the whole query.
        /// </summary>
        public Task<IReadOnlyList<RemoteRecordStamp>> SweepWorkItemsForTeam(Team team, CancellationToken cancellationToken)
            => SweepIdentities(team, TheQueryATeamIsFetchedBy(team), $"Team {team.Name}", cancellationToken);

        public IReadOnlyList<AdditionalFieldDefinition> GetPredefinedAdditionalFields(WorkTrackingSystemConnection connection) => [];

        private static readonly ConcurrentDictionary<string, VssConnection> ConnectionCache = new();

        private static readonly ConcurrentDictionary<string, IVssHttpClient> ClientCache = new();

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> OrgLimiters = new();

        private static SemaphoreSlim GetLimiter(string url) => OrgLimiters.GetOrAdd(new Uri(url).Host, _ => new SemaphoreSlim(6));

        public async Task<IEnumerable<LighthouseWorkItem>> GetWorkItemsForTeam(Team team, CancellationToken cancellationToken)
        {
            logger.LogDebug("Updating Work Items for Team {TeamName}", team.Name);

            var (adoWorkItems, additionalFieldResolution) = await FetchAdoWorkItemsByQuery(team, TheQueryATeamIsFetchedBy(team), cancellationToken);

            return await TheTeamsWorkItemsFrom(team, adoWorkItems, additionalFieldResolution, cancellationToken);
        }

        /// <summary>
        /// Phase two for a team: the records the sweep reported as moved, and no others.
        /// </summary>
        public async Task<IEnumerable<LighthouseWorkItem>> GetWorkItemsForTeam(Team team, IReadOnlyCollection<string> referenceIds, CancellationToken cancellationToken)
        {
            var (adoWorkItems, additionalFieldResolution) = await FetchAdoWorkItemsById(team, referenceIds, cancellationToken);

            return await TheTeamsWorkItemsFrom(team, adoWorkItems, additionalFieldResolution, cancellationToken);
        }

        private async Task<IEnumerable<LighthouseWorkItem>> TheTeamsWorkItemsFrom(
            Team team, IEnumerable<AdoWorkItem> adoWorkItems, AdditionalFieldResolution additionalFieldResolution, CancellationToken cancellationToken)
        {
            var relationsTask = GetRelationsOfWorkItems(adoWorkItems, team, additionalFieldResolution, DependenciesFromRelations.NotRead(), cancellationToken);
            var workItems = await ConvertAdoWorkItemToLighthouseWorkItemBase(adoWorkItems, team, additionalFieldResolution.Fields, cancellationToken);
            var settingFindings = WhatReadingTheSettingsEveryOwnerHasFound(team, additionalFieldResolution);

            var relations = await relationsTask;

            foreach (var workItem in workItems)
            {
                var parentReference = GetParentReference(team, workItem, relations.ParentReferences);
                workItem.ParentReferenceId = parentReference;
                workItem.RefreshFindings = settingFindings;
            }

            return workItems.Select(workItem => new LighthouseWorkItem(workItem, team));
        }

        public async Task<List<Feature>> GetFeaturesForProject(Portfolio project, CancellationToken cancellationToken)
        {
            logger.LogInformation("Getting Features of Type {WorkItemTypes} and Query '{Query}'", string.Join(", ", project.WorkItemTypes), project.DataRetrievalValue);

            var features = await GetFeaturesForProjectByQuery(project, TheQueryAPortfolioIsFetchedBy(project), cancellationToken);

            logger.LogInformation("Found Features with IDs {FeatureIds}", string.Join(", ", features.Select(f => f.ReferenceId)));

            return features;
        }

        /// <summary>
        /// Phase two for a portfolio: the Features the sweep reported as moved, and no others.
        /// </summary>
        public async Task<List<Feature>> GetFeaturesForProject(Portfolio project, IReadOnlyCollection<string> referenceIds, CancellationToken cancellationToken)
        {
            var (adoWorkItems, additionalFieldResolution) = await FetchAdoWorkItemsById(project, referenceIds, cancellationToken);

            return await ThePortfoliosFeaturesFrom(project, adoWorkItems, additionalFieldResolution, cancellationToken);
        }

        public Task<IReadOnlyList<RemoteRecordStamp>> SweepFeaturesForPortfolio(Portfolio project, CancellationToken cancellationToken)
            => SweepIdentities(project, TheQueryAPortfolioIsFetchedBy(project), $"Portfolio {project.Name}", cancellationToken);

        public async Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds, CancellationToken cancellationToken)
        {
            logger.LogInformation("Getting Parent Features with IDs {ParentFeatureIds} for Project {ProjectName}", string.Join(", ", parentFeatureIds), project.Name);

            var features = await GetFeaturesForProjectByQuery(project, TheQueryParentFeaturesAreFetchedBy(parentFeatureIds), cancellationToken);

            logger.LogInformation("Found Parent Features with IDs {ParentFeatureIds}", string.Join(", ", features.Select(f => f.ReferenceId)));
            return features;
        }

        public Task<IReadOnlyList<RemoteRecordStamp>> SweepParentFeatures(Portfolio project, IEnumerable<string> parentFeatureIds, CancellationToken cancellationToken)
            => SweepIdentities(project, TheQueryParentFeaturesAreFetchedBy(parentFeatureIds), $"Parent Features of Portfolio {project.Name}", cancellationToken);

        /// <summary>
        /// The one sweep every caller goes through. A WIQL answers with ids alone, so the stamps cost a second
        /// read - narrowed to the one field, batched at the size the tracker accepts, and with nothing expanded,
        /// because every expansion there is would download what the sweep exists to skip.
        /// </summary>
        private async Task<IReadOnlyList<RemoteRecordStamp>> SweepIdentities(
            IWorkItemQueryOwner owner, string sweepQuery, string sweptDescription, CancellationToken cancellationToken)
        {
            var witClient = await GetWorkItemTrackingHttpClientAsync(owner.WorkTrackingSystemConnection);
            var url = witClient.BaseAddress!.ToString();

            // Not GetWorkItemReferencesByQuery, which answers with references alone: the sweep needs to keep
            // reading from the same answer, and the two refusals below are what stop an unanswerable sweep
            // from reading as "the query matches nothing" - which removes every record the entity has.
            var answer = await ExecuteWithThrottle(url, () => witClient.QueryByWiqlAsync(new Wiql { Query = sweepQuery }, cancellationToken: cancellationToken), cancellationToken);

            // An answer carrying no result set at all is not an answer of "nothing matched": reading it as one
            // would remove every record the entity has, on a cycle nothing else complains about.
            if (answer.WorkItems is null)
            {
                throw new InvalidOperationException(
                    $"Azure DevOps answered the identity sweep for {sweptDescription} without a result set.");
            }

            var swept = answer.WorkItems.Select(reference => reference.Id).ToList();
            var stamps = new List<RemoteRecordStamp>();

            foreach (var chunk in swept.Chunk(MaxChunkSize))
            {
                var answered = await ExecuteWithThrottle(url, () => witClient.GetWorkItemsAsync(chunk, TheChangeStampOnly, expand: null, cancellationToken: cancellationToken), cancellationToken);

                if (answered.Count != chunk.Length)
                {
                    throw new InvalidOperationException(
                        $"Azure DevOps answered for {answered.Count} of {chunk.Length} records in the identity sweep for {sweptDescription}.");
                }

                // A record the tracker reports no stamp for still belongs in the sweep: leaving it out puts it
                // in "stored minus swept" and deletes a live record, where an unmatchable stamp re-downloads it.
                stamps.AddRange(answered.Select(record =>
                    new RemoteRecordStamp($"{record.Id}", record.ExtractChangedDateFromWorkItem() ?? default)));
            }

            return stamps;
        }

        public async Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            // Asked rather than demanded: a stored connection carries whatever option rows it has, and a
            // missing one used to throw straight past this method's own guard and out of the request. The
            // administrator who pressed Test connection then learned nothing at all (Bug #6010).
            var url = connection.FindWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.Url);
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return ConnectionValidationResult.Failure(
                    "invalid_url",
                    "The Azure DevOps URL appears to be invalid.",
                    "Provide a full URL, for example https://dev.azure.com/your-org.",
                    AzureDevOpsWorkTrackingOptionNames.Url);
            }

            try
            {
                var witClient = await GetWorkItemTrackingHttpClientAsync(connection);

                await VerifyConnection(witClient);

                return await TheVerdictOnTheAdditionalFields(witClient, connection.AdditionalFieldDefinitions);
            }
            catch (WorkTrackingReadException refusal)
            {
                return refusal.Verdict;
            }
            catch (VssUnauthorizedException ex)
            {
                logger.LogInformation(ex, "Azure DevOps authentication failed for connection {ConnectionName}", connection.Name);

                return ConnectionValidationResult.Failure(
                    "authentication_failed",
                    "Authentication failed for Azure DevOps.",
                    "Check your Personal Access Token permissions and make sure it is still valid.",
                    AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken);
            }
            catch (VssServiceException ex)
            {
                logger.LogInformation(ex, "Azure DevOps service validation failed for connection {ConnectionName}", connection.Name);

                return ConnectionValidationResult.Failure(
                    "connection_failed",
                    "Azure DevOps rejected the connection settings.",
                    ex.Message);
            }
            catch (HttpRequestException ex)
            {
                logger.LogInformation(ex, "Azure DevOps endpoint validation failed for connection {ConnectionName}", connection.Name);

                return ConnectionValidationResult.Failure(
                    "connection_failed",
                    "Could not reach Azure DevOps with the provided URL.",
                    ex.Message,
                    AzureDevOpsWorkTrackingOptionNames.Url);
            }
            catch (UriFormatException ex)
            {
                logger.LogInformation(ex, "Azure DevOps URL parsing failed for connection {ConnectionName}", connection.Name);

                return ConnectionValidationResult.Failure(
                    "invalid_url",
                    "The Azure DevOps URL appears to be invalid.",
                    ex.Message,
                    AzureDevOpsWorkTrackingOptionNames.Url);
            }
            catch
            {
                return ConnectionValidationResult.Failure(
                    "validation_failed",
                    "Connection validation failed due to an unexpected error.");
            }
        }

        public async Task<ConnectionValidationResult> ValidateTeamSettings(Team team)
        {
            try
            {
                logger.LogInformation("Validating Team Settings for Team {TeamName} and Query {Query}", team.Name, team.DataRetrievalValue);

                var query = PrepareQuery(team.WorkItemTypes, team.AllStates, team.DataRetrievalValue, team.DoneItemsCutoffDays);
                var witClient = await GetWorkItemTrackingHttpClientAsync(team.WorkTrackingSystemConnection);
                var workItems = await GetWorkItemReferencesByQuery(witClient, query, CancellationToken.None);

                var workItemCount = workItems.Count();

                logger.LogInformation("Found a total of {NumberOfWorkItems} Work Items with specified Query", workItemCount);

                if (workItemCount == 0)
                {
                    return ConnectionValidationResult.Failure(
                        "no_work_items_found",
                        "No work items were found for this team configuration.",
                        "Check your query, selected work item types, and mapped states.",
                        "DataRetrievalValue");
                }

                return ConnectionValidationResult.Success();
            }
            catch (WorkTrackingReadException refusal)
            {
                return refusal.Verdict;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Team Settings for Team {TeamName}", team.Name);
                return ConnectionValidationResult.Failure(
                    "validation_failed",
                    "Team validation failed due to an unexpected error.",
                    exception.Message);
            }
        }

        public async Task<ConnectionValidationResult> ValidatePortfolioSettings(Portfolio portfolio)
        {
            try
            {
                logger.LogInformation("Validating Project Settings for Project {ProjectName} and Query {Query}", portfolio.Name, portfolio.DataRetrievalValue);

                var query = PrepareQuery(portfolio.WorkItemTypes, portfolio.AllStates, portfolio.DataRetrievalValue, portfolio.DoneItemsCutoffDays);

                var (workItems, _) = await FetchAdoWorkItemsByQuery(portfolio, query, CancellationToken.None);
                var workItemCount = workItems.Count();

                logger.LogInformation("Found a total of {NumberOfWorkItems} Features with specified Query", workItemCount);

                if (workItemCount == 0)
                {
                    return ConnectionValidationResult.Failure(
                        "no_features_found",
                        "No portfolio features were found for this configuration.",
                        "Check your query, selected work item types, and mapped states.",
                        "DataRetrievalValue");
                }

                return ConnectionValidationResult.Success();
            }
            catch (WorkTrackingReadException refusal)
            {
                return refusal.Verdict;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Project Settings for Project {ProjectName}", portfolio.Name);
                return ConnectionValidationResult.Failure(
                    "validation_failed",
                    "Portfolio validation failed due to an unexpected error.",
                    exception.Message);
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

                var projectId = splitBoardId[0];
                var boardReference = splitBoardId[1];

                logger.LogInformation("Getting Board Information for Board {BoardId} in Project {ProjectId}", boardReference, projectId);

                var workClient = await GetClientAsync<WorkHttpClient>(workTrackingSystemConnection);
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
                    DoneStates = doneStates,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting board information for board {BoardId}", boardId);
                return emptyInfo;
            }
        }

        public async Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            if (updates.Count == 0)
            {
                return new WriteBackResult();
            }

            var url = connection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.Url);
            var witClient = await GetWorkItemTrackingHttpClientAsync(connection);
            var results = await UpdateItemsInChunks(updates, url, witClient);

            return new WriteBackResult { ItemResults = results };
        }

        private async Task<List<WriteBackItemResult>> UpdateItemsInChunks(IReadOnlyList<WriteBackFieldUpdate> updates, string url, WorkItemTrackingHttpClient witClient)
        {
            var results = new List<WriteBackItemResult>();

            // Grouped by work item first so every field changing on one item travels in a single patch
            // document — Azure DevOps stamps a new revision per call, so writing six fields one at a time
            // buries the item's history under six revisions of our own making. Chunking and the throttle
            // still bound how many items are in flight at once.
            foreach (var batch in updates.GroupBy(update => update.WorkItemId).Chunk(MaxChunkSize))
            {
                var batchTasks = batch.Select(async itemUpdates => await UpdateItem(url, witClient, itemUpdates.Key, [.. itemUpdates]));

                results.AddRange((await Task.WhenAll(batchTasks)).SelectMany(itemResults => itemResults));
            }

            return results;
        }

        /// <summary>
        /// Azure DevOps rejects a mixed-validity patch document whole — including on permission failures,
        /// which was measured rather than assumed — so a rejected batch is re-sent one operation at a
        /// time. The valid fields land and the offending one fails alone.
        /// </summary>
        internal async Task<List<WriteBackItemResult>> UpdateItem(string url, WorkItemTrackingHttpClient witClient,
            string workItemReference, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            if (!int.TryParse(workItemReference, out var workItemId))
            {
                return [.. updates.Select(update => WriteBackItemResult.Refused(update,
                    $"Work item ID '{update.WorkItemId}' is not a valid integer for Azure DevOps."))];
            }

            var batch = await TryPatch(url, witClient, workItemId, updates);

            if (batch.Succeeded)
            {
                return [.. updates.Select(update => WriteBackItemResult.Written(update, NotificationSuppression.Suppressed))];
            }

            // A single operation is already as isolated as it gets.
            if (updates.Count == 1)
            {
                return [WriteBackItemResult.Refused(updates[0], batch.ErrorMessage, NotificationSuppression.Unknown)];
            }

            var results = new List<WriteBackItemResult>();

            foreach (var update in updates)
            {
                var single = await TryPatch(url, witClient, workItemId, [update]);
                results.Add(single.Succeeded
                    ? WriteBackItemResult.Written(update, NotificationSuppression.Suppressed)
                    : WriteBackItemResult.Refused(update, single.ErrorMessage, NotificationSuppression.Unknown));
            }

            return results;
        }

        private async Task<(bool Succeeded, string ErrorMessage)> TryPatch(string url, WorkItemTrackingHttpClient witClient,
            int workItemId, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            try
            {
                var patchDocument = new JsonPatchDocument();
                patchDocument.AddRange(updates.Select(update => new JsonPatchOperation
                {
                    Operation = Operation.Replace,
                    Path = $"/fields/{update.TargetFieldReference}",
                    Value = update.Value,
                }));

                await ExecuteWithThrottle(
                    url,
                    () => witClient.UpdateWorkItemAsync(patchDocument, workItemId, suppressNotifications: true),
                    CancellationToken.None);

                return (true, string.Empty);
            }
            // Cancellation is how shutdown reaches us; reporting it as a write failure would both lose the
            // signal and blame the tracker for a decision we made.
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to write {FieldCount} field(s) to work item {WorkItemId}", updates.Count, workItemId);
                return (false, ex.Message);
            }
        }

        private static string ExtractTeamIdFromBoard(Microsoft.TeamFoundation.Work.WebApi.Board board)
        {
            var team = (ReferenceLink)board.Links.Links["team"];
            var teamLink = team.Href;
            var teamId = teamLink.Split('/')[^1];

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

            return string.Join(" OR ", queryParts);
        }

        private static (IEnumerable<string> toDoStates, IEnumerable<string> doingStates, IEnumerable<string> doneStates)
            ExtractStateMappingFromBoard(Microsoft.TeamFoundation.Work.WebApi.Board board)
        {
            var incomingColumns = board.Columns.Where(c => c.ColumnType == BoardColumnType.Incoming);
            var inProgressColumns = board.Columns.Where(c => c.ColumnType == BoardColumnType.InProgress);
            var outgoingColumns = board.Columns.Where(c => c.ColumnType == BoardColumnType.Outgoing);

            var toDoStates = incomingColumns.SelectMany(c => c.StateMappings.Values).Distinct();
            var doingStates = inProgressColumns.SelectMany(c => c.StateMappings.Values).Distinct().Where(s => !toDoStates.Contains(s));
            var doneStates = outgoingColumns.SelectMany(c => c.StateMappings.Values).Distinct().Where(s => !toDoStates.Contains(s) && !doingStates.Contains(s));

            return (toDoStates, doingStates, doneStates);
        }

        private static IEnumerable<string> ExtractWorkItemTypesFromBoard(Microsoft.TeamFoundation.Work.WebApi.Board board)
        {
            return board.Columns.SelectMany(c => c.StateMappings).Select(sm => sm.Key).Distinct();
        }

        private static async Task<Microsoft.TeamFoundation.Work.WebApi.Board> GetBoardForProject(string projectId,
            string boardId, WorkHttpClient workClient)
        {
            var board = await workClient.GetBoardAsync(new TeamContext(projectId), boardId);

            return board;
        }

        private async Task<IEnumerable<Board>> GetBoardsForProjects(IEnumerable<TeamProjectReference> projects,
            WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            var workClient = await GetClientAsync<WorkHttpClient>(workTrackingSystemConnection);

            var tasks = projects.Select(async project => await GetBoardsForProject(project, workClient));

            var boardsPerProject = await Task.WhenAll(tasks);
            return boardsPerProject.SelectMany(b => b);
        }

        private async Task<IEnumerable<Board>> GetBoardsForProject(TeamProjectReference project,
            WorkHttpClient workClient)
        {
            try
            {
                var boardsPerProject = await ExecuteWithThrottle(
                    workClient.BaseAddress.ToString(),
                    () => workClient.GetBoardsAsync(new TeamContext(project.Id)),
                    CancellationToken.None);

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
            var projectClient = await GetClientAsync<ProjectHttpClient>(workTrackingSystemConnection);
            var allProjects = new List<TeamProjectReference>();
            string? continuationToken = null;

            do
            {
                var page = await ExecuteWithThrottle(
                    projectClient.BaseAddress.ToString(),
                    () => projectClient.GetProjects(continuationToken: continuationToken),
                    CancellationToken.None);

                allProjects.AddRange(page);
                continuationToken = (page as IPagedList<TeamProjectReference>)?.ContinuationToken;
            }
            while (!string.IsNullOrEmpty(continuationToken));

            return allProjects;
        }

        private async Task<List<Feature>> GetFeaturesForProjectByQuery(Portfolio portfolio, string query, CancellationToken cancellationToken)
        {
            var (adoWorkItems, additionalFieldResolution) = await FetchAdoWorkItemsByQuery(portfolio, query, cancellationToken);

            return await ThePortfoliosFeaturesFrom(portfolio, adoWorkItems, additionalFieldResolution, cancellationToken);
        }

        private async Task<List<Feature>> ThePortfoliosFeaturesFrom(
            Portfolio portfolio, IEnumerable<AdoWorkItem> adoWorkItems, AdditionalFieldResolution additionalFieldResolution, CancellationToken cancellationToken)
        {
            var dependencies = DependenciesFromRelations.For(portfolio, additionalFieldResolution);
            var relationsTask = GetRelationsOfWorkItems(adoWorkItems, portfolio, additionalFieldResolution, dependencies, cancellationToken);

            var workItemBase = await ConvertAdoWorkItemToLighthouseWorkItemBase(adoWorkItems, portfolio, additionalFieldResolution.Fields, cancellationToken);
            var relations = await relationsTask;

            var settingFindings = WhatReadingThePortfoliosSettingsFound(portfolio, additionalFieldResolution, dependencies, relations);
            var features = new List<Feature>();

            foreach (var workItem in workItemBase)
            {
                var parentReference = GetParentReference(portfolio, workItem, relations.ParentReferences);
                var estimatedSize = GetEstimatedSize(portfolio, workItem);
                var owningTeam = GetOwningTeam(portfolio, workItem);

                var feature = new Feature(workItem, TheDependenciesOf(portfolio, workItem, relations, dependencies.NamedRelationEnd))
                {
                    EstimatedSize = estimatedSize,
                    OwningTeam = owningTeam,
                    ParentReferenceId = parentReference,
                    RefreshFindings = settingFindings,
                };

                features.Add(feature);
            }

            return features;
        }

        // Having no dependencies is the ordinary case, so nothing is said when the Features carry no relations at all.
        private static bool NoWorkItemCarriesTheNamedEnd(RelationsOfWorkItems relations)
            => relations.RelationTypesSeen.Count > 0 && relations.DependencyReferences.Values.All(references => references.Count == 0);

        private static IEnumerable<RefreshFinding> WhatReadingTheDependencyFieldFound(
            Portfolio portfolio, DependenciesFromRelations dependencies, RelationsOfWorkItems relations)
        {
            if (dependencies.WhyNothingIsRead is { } whyNothingIsRead)
            {
                yield return ADependencyFieldFinding(portfolio, whyNothingIsRead.Kind, whyNothingIsRead.Seen);
            }
            else if (dependencies.NamedRelationEnd is not null && NoWorkItemCarriesTheNamedEnd(relations))
            {
                yield return ADependencyFieldFinding(portfolio, RefreshFindingKind.NamedLinkMatchedNothing, relations.RelationTypesSeen);
            }
        }

        private static RefreshFinding ADependencyFieldFinding(Portfolio portfolio, RefreshFindingKind kind, List<string> seen)
            => new(kind, RefreshFindingSetting.DependencyField, TheReferenceNamedBy(portfolio, portfolio.DependencyOverrideAdditionalFieldDefinitionId), seen);

        private static string TheReferenceNamedBy(WorkTrackingSystemOptionsOwner owner, int? definitionId)
            => owner.WorkTrackingSystemConnection.AdditionalFieldDefinitions
                .Find(definition => definition.Id == definitionId)?.Reference ?? string.Empty;

        /// <summary>
        /// A Dependency Field naming something that is not a field can only be read once the relation types
        /// are known. With that listing refused or unreadable, every Feature would be read as waiting on
        /// nothing, and each Feature handed back overwrites the dependencies stored for it. Stopping the
        /// refresh leaves those as they were until the listing can be read again. A Portfolio that ignores
        /// dependencies has none to lose, so it refreshes as usual.
        /// </summary>
        private void StopUnlessTheDependencyFieldCouldBeLookedUp(Portfolio portfolio, AdditionalFieldResolution resolution)
        {
            var dependencyField = portfolio.DependencyOverrideAdditionalFieldDefinitionId;

            if (portfolio.IgnoreDependencies || !resolution.CouldNotTellWhatItNames(dependencyField))
            {
                return;
            }

            var reference = TheReferenceNamedBy(portfolio, dependencyField);

            logger.LogWarning(
                "The refresh of Portfolio {PortfolioName} was stopped: its Dependency Field names '{Reference}', which is not a field, "
                + "and Azure DevOps would not list its relation types, so whether it names a relation type could not be told. "
                + "Its stored dependencies were left as they were.",
                portfolio.Name,
                reference);

            throw new AzureDevOpsReadException(ConnectionValidationResult.Failure(
                "relation_types_unreadable",
                $"The Dependency Field of {portfolio.Name} names '{reference}', which is not a field, and the relation types could not be listed, "
                + "so the refresh was stopped. Nothing Lighthouse already holds was changed.",
                "Give the Personal Access Token permission to read work items, or check whether a proxy in front of Azure DevOps is turning that request down.",
                AzureDevOpsReadException.AdditionalFieldsFieldName));
        }

        /// <summary>
        /// Only the Dependency Field reads a relation type. Every other setting reads a field and never a
        /// relation type, so naming one there reads as if nothing were named. That empty answer looks
        /// believable, so every record carries the finding and the refresh says it once.
        /// </summary>
        private static List<RefreshFinding> WhatReadingThePortfoliosSettingsFound(
            Portfolio portfolio, AdditionalFieldResolution resolution, DependenciesFromRelations dependencies, RelationsOfWorkItems relations)
            =>
            [
                .. WhatReadingTheSettingsEveryOwnerHasFound(portfolio, resolution),
                .. ARelationTypeNamedIn(portfolio, resolution, portfolio.SizeEstimateAdditionalFieldDefinitionId, RefreshFindingKind.RelationTypeCannotBeReadHere, RefreshFindingSetting.SizeEstimateField),
                .. ARelationTypeNamedIn(portfolio, resolution, portfolio.FeatureOwnerAdditionalFieldDefinitionId, RefreshFindingKind.RelationTypeCannotBeReadHere, RefreshFindingSetting.FeatureOwnerField),
                .. WhatReadingTheDependencyFieldFound(portfolio, dependencies, relations),
            ];

        private static List<RefreshFinding> WhatReadingTheSettingsEveryOwnerHasFound(WorkTrackingSystemOptionsOwner owner, AdditionalFieldResolution resolution)
            =>
            [
                .. ARelationTypeNamedIn(owner, resolution, owner.ParentOverrideAdditionalFieldDefinitionId, RefreshFindingKind.RelationTypeCannotBeReadHere, RefreshFindingSetting.ParentOverrideField),
                .. ARelationTypeNamedIn(owner, resolution, owner.EstimationAdditionalFieldDefinitionId, RefreshFindingKind.RelationTypeCannotBeReadHere, RefreshFindingSetting.EstimationField),
            ];

        private static IEnumerable<RefreshFinding> ARelationTypeNamedIn(
            WorkTrackingSystemOptionsOwner owner, AdditionalFieldResolution resolution, int? definitionId, RefreshFindingKind kind, RefreshFindingSetting setting)
        {
            if (!resolution.NamesARelationType(definitionId))
            {
                yield break;
            }

            yield return new RefreshFinding(kind, setting, TheReferenceNamedBy(owner, definitionId), []);
        }

        private static async Task VerifyConnection(WorkItemTrackingHttpClient witClient)
        {
            var query = $"SELECT [{AzureDevOpsFieldNames.Id}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.Id}] = 12";

            await witClient.QueryByWiqlAsync(new Wiql() { Query = query });
        }

        /// <summary>
        /// Whether every configured field matches a field, or failing that a relation type, in this
        /// organisation. Each one is resolved straight against the field list rather than through the
        /// id-keyed lookup below, and it has to stay that way: these definitions belong to a connection the
        /// caller loaded from the database, so the only way to make their ids unique enough to key a
        /// dictionary on would be to write to them - and writing to a stored row's identity is refused by
        /// the next save through that connection, wherever that next save happens to be (Bug #6010).
        /// </summary>
        private async Task<ConnectionValidationResult> TheVerdictOnTheAdditionalFields(WorkItemTrackingHttpClient witClient, IEnumerable<AdditionalFieldDefinition> additionalFieldDefinitions)
        {
            var availableFields = await TheFieldsTheOrganisationDefines(witClient, CancellationToken.None);

            var notAField = additionalFieldDefinitions
                .Where(field => string.IsNullOrEmpty(TheReferenceOfTheFieldNamed(field.Reference, availableFields)))
                .Select(field => field.Reference)
                .ToList();

            if (notAField.Count == 0)
            {
                return ConnectionValidationResult.Success();
            }

            var relationTypes = await TheRelationTypesTheOrganisationLists(witClient, CancellationToken.None);
            var resolvedToNothing = notAField.Where(reference => !relationTypes.Names(reference)).ToList();

            return resolvedToNothing.Count == 0
                ? ConnectionValidationResult.Success()
                : TheVerdictOnReferencesThatResolvedToNothing(resolvedToNothing, relationTypes);
        }

        /// <summary>
        /// A refused listing gets its own answer rather than the plain "not found": it says nothing about
        /// whether the reference is wrong, and calling it missing would send the admin to correct a
        /// configuration that may well be right.
        /// </summary>
        private static ConnectionValidationResult TheVerdictOnReferencesThatResolvedToNothing(
            List<string> resolvedToNothing, RelationTypeListing relationTypes)
        {
            var references = string.Join(", ", resolvedToNothing);

            if (relationTypes.WasRefused)
            {
                return ConnectionValidationResult.Failure(
                    "relation_types_unreadable",
                    $"Azure DevOps would not list its relation types, so the relation types could not be read and a reference naming one could not be checked at all. The references waiting on it are: {references}.",
                    "Give the Personal Access Token permission to read work items, or check whether a proxy in front of Azure DevOps is turning that request down. Nothing about the additional field configuration is known to be wrong.",
                    AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken);
            }

            var whatItDoesDefine = relationTypes.Ends.Count == 0
                ? "It listed no relation types at all to check them against."
                : $"The relation types it does define are: {string.Join(", ", relationTypes.Ends)}.";

            return ConnectionValidationResult.Failure(
                "additional_fields_invalid",
                $"Some additional fields could not be found: {references}. This Azure DevOps organisation defines no field and no relation type of that name. {whatItDoesDefine}",
                "Verify field names or references in Azure DevOps and update the additional field configuration.",
                AzureDevOpsReadException.AdditionalFieldsFieldName);
        }

        /// <summary>
        /// Asked for afresh on every call and handed down that call only: one connector serves every
        /// connection to Azure DevOps, so a listing kept between calls would answer for the wrong organisation.
        /// An answer that arrives without a list is read as an empty one, since nothing refused; a refusal or a
        /// failed request is kept apart, because it says nothing about which relation types exist. A request
        /// that timed out counts as failed, while one the caller cancelled still stops the caller.
        /// </summary>
        private async Task<RelationTypeListing> TheRelationTypesTheOrganisationLists(WorkItemTrackingHttpClient witClient, CancellationToken cancellationToken)
        {
            try
            {
                return RelationTypeListing.Of(await witClient.GetRelationTypesAsync(cancellationToken: cancellationToken) ?? []);
            }
            catch (Exception failure) when (failure is VssException or HttpRequestException or TimeoutException
                || (failure is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                logger.LogWarning(failure, "Azure DevOps would not list the relation types for this organisation");

                return RelationTypeListing.Refused;
            }
        }

        private async Task<List<WorkItemField2>> TheFieldsTheOrganisationDefines(WorkItemTrackingHttpClient witClient, CancellationToken cancellationToken)
        {
            try
            {
                return await witClient.GetWorkItemFieldsAsync(cancellationToken: cancellationToken);
            }
            catch (VssException refusal)
            {
                logger.LogWarning(refusal, "Azure DevOps would not return the field list for this organisation");

                throw AzureDevOpsReadException.FieldListRefused(refusal);
            }
        }

        /// <summary>
        /// The relation types are listed at most once per fetch, and only once a reference one of the owner's
        /// own settings names missed the field list, so an owner naming only fields costs no extra request -
        /// however stale the rest of the connection's Additional Fields are. Every definition is still resolved
        /// to its field, because the values of all of them are downloaded. The listing lives in this call alone,
        /// for the same reason Validate keeps it there.
        /// </summary>
        private async Task<AdditionalFieldResolution> ResolveTheAdditionalFields(
            WorkItemTrackingHttpClient witClient, WorkTrackingSystemOptionsOwner owner, CancellationToken cancellationToken)
        {
            var availableFields = await TheFieldsTheOrganisationDefines(witClient, cancellationToken);
            var namedByASetting = TheDefinitionsItsSettingsName(owner);

            var resolution = AdditionalFieldResolution.None();
            RelationTypeListing? relationTypes = null;

            foreach (var additionalFieldDefinition in owner.WorkTrackingSystemConnection.AdditionalFieldDefinitions)
            {
                var fieldReference = TheReferenceOfTheFieldNamed(additionalFieldDefinition.Reference, availableFields);
                resolution.Fields.Add(additionalFieldDefinition.Id, fieldReference);

                if (!string.IsNullOrEmpty(fieldReference))
                {
                    continue;
                }

                // Worded so it holds whether or not the reference is a relation type that another owner on the
                // connection names: either way no value is downloaded for it here.
                if (!namedByASetting.Contains(additionalFieldDefinition.Id))
                {
                    logger.LogWarning(
                        "{AdditionalField} is not a field in this Azure DevOps organisation, so no value is read for it on {Owner}",
                        additionalFieldDefinition.Reference,
                        owner.Name);
                    continue;
                }

                relationTypes ??= await TheRelationTypesTheOrganisationLists(witClient, cancellationToken);

                if (resolution.ResolvesToARelationType(additionalFieldDefinition, relationTypes))
                {
                    continue;
                }

                if (relationTypes.WasRefused)
                {
                    resolution.CouldNotTell.Add(additionalFieldDefinition.Id);
                }

                WarnThatNothingWillBeReadFor(additionalFieldDefinition, relationTypes);
            }

            return resolution;
        }

        // The settings whose findings are judged above, and the only ones whose reference decides what is read.
        private static HashSet<int> TheDefinitionsItsSettingsName(WorkTrackingSystemOptionsOwner owner)
        {
            int?[] named = owner is Portfolio portfolio
                ?
                [
                    owner.ParentOverrideAdditionalFieldDefinitionId,
                    owner.EstimationAdditionalFieldDefinitionId,
                    portfolio.DependencyOverrideAdditionalFieldDefinitionId,
                    portfolio.SizeEstimateAdditionalFieldDefinitionId,
                    portfolio.FeatureOwnerAdditionalFieldDefinitionId,
                ]
                : [owner.ParentOverrideAdditionalFieldDefinitionId, owner.EstimationAdditionalFieldDefinitionId];

            return [.. named.OfType<int>()];
        }

        private void WarnThatNothingWillBeReadFor(AdditionalFieldDefinition additionalFieldDefinition, RelationTypeListing relationTypes)
        {
            if (relationTypes.WasRefused)
            {
                logger.LogWarning(
                    "{AdditionalField} is not a field in this Azure DevOps organisation, and its relation types could not be listed to tell whether it names one, so nothing will be read for it",
                    additionalFieldDefinition.Reference);
                return;
            }

            logger.LogWarning(
                "No field in this Azure DevOps organisation is called {AdditionalField} or references it, so nothing will be read for it",
                additionalFieldDefinition.Reference);
        }

        /// <summary>
        /// A configured field can be named either by its reference name or by the name it is displayed under,
        /// and one organisation can hold both spellings for two different fields - so the reference name, being
        /// the exact identifier, wins rather than the match being ambiguous.
        ///
        /// A name matching nothing answers with no reference rather than refusing: a stale entry in a saved
        /// configuration is an ordinary mistake, and refusing would stop the refresh for everyone on that
        /// connection over a field nobody is reading. The caller that validates a connection reports it, and
        /// on a refresh the log line above is the only trace there is.
        /// </summary>
        private static string TheReferenceOfTheFieldNamed(string reference, List<WorkItemField2> availableFields)
            => (availableFields.Find(field => string.Equals(field.ReferenceName, reference, StringComparison.OrdinalIgnoreCase))
                ?? availableFields.Find(field => string.Equals(field.Name, reference, StringComparison.OrdinalIgnoreCase)))
                ?.ReferenceName ?? string.Empty;

        /// <summary>
        /// Every failure here travels on to the caller. Removal is a set difference against what this fetch
        /// returned, so answering a failed fetch with no records deletes every record the team or portfolio
        /// has - and their blocked spells, which no tracker can rebuild, do not come back with them.
        /// </summary>
        private async Task<(IEnumerable<AdoWorkItem>, AdditionalFieldResolution)> FetchAdoWorkItemsByQuery(
            WorkTrackingSystemOptionsOwner workItemQueryOwner, string query, CancellationToken cancellationToken)
        {
            var witClient = await GetWorkItemTrackingHttpClientAsync(workItemQueryOwner.WorkTrackingSystemConnection);
            var workItemReferences = await GetWorkItemReferencesByQuery(witClient, query, cancellationToken);

            return await ThePayloadsAndFieldsFor(workItemQueryOwner, witClient, workItemReferences.Select(reference => reference.Id).ToList(), cancellationToken);
        }

        /// <summary>
        /// Phase two's fetch: a lookup by id, never a query. What phase one enumerated, phase two may only look
        /// up - re-applying the team's own filter or its done-items cutoff would let the tracker drop a record
        /// the sweep just vouched for, and removal is "stored minus swept".
        ///
        /// It refuses on failure for the same reason the whole-query fetch does: records it answers for are
        /// the ones that survive the cycle.
        /// </summary>
        private async Task<(IEnumerable<AdoWorkItem>, AdditionalFieldResolution)> FetchAdoWorkItemsById(
            WorkTrackingSystemOptionsOwner workItemQueryOwner, IReadOnlyCollection<string> referenceIds, CancellationToken cancellationToken)
        {
            var witClient = await GetWorkItemTrackingHttpClientAsync(workItemQueryOwner.WorkTrackingSystemConnection);

            return await ThePayloadsAndFieldsFor(workItemQueryOwner, witClient, WorkItemIdsIn(referenceIds), cancellationToken);
        }

        /// <summary>
        /// What both fetches do once they know which ids they want: resolve the connection's additional fields,
        /// stop a Portfolio's refresh there if its Dependency Field could not be looked up, since nothing
        /// downloaded after that point could be kept, then read the payloads in batches. An empty id set asks
        /// for nothing at all, because a keyed read of no keys is still a remote round trip.
        /// </summary>
        private async Task<(IEnumerable<AdoWorkItem>, AdditionalFieldResolution)> ThePayloadsAndFieldsFor(
            WorkTrackingSystemOptionsOwner workItemQueryOwner, WorkItemTrackingHttpClient witClient, List<int> workItemIds, CancellationToken cancellationToken)
        {
            if (workItemIds.Count == 0)
            {
                return ([], AdditionalFieldResolution.None());
            }

            var additionalFieldResolution = await ResolveTheAdditionalFields(witClient, workItemQueryOwner, cancellationToken);

            if (workItemQueryOwner is Portfolio portfolio)
            {
                StopUnlessTheDependencyFieldCouldBeLookedUp(portfolio, additionalFieldResolution);
            }

            var adoWorkItems = await GetAdoWorkItemsById(workItemIds, workItemQueryOwner, additionalFieldResolution.Fields.Values, cancellationToken);

            return (adoWorkItems, additionalFieldResolution);
        }

        // A reference id on Azure DevOps is the tracker's own numeric work-item id, so anything else cannot
        // name a record there and is dropped rather than sent.
        private static List<int> WorkItemIdsIn(IEnumerable<string> referenceIds)
        {
            var workItemIds = new List<int>();

            foreach (var referenceId in referenceIds)
            {
                if (int.TryParse(referenceId, out var workItemId))
                {
                    workItemIds.Add(workItemId);
                }
            }

            return workItemIds;
        }

        /// <summary>
        /// Every round trip to Azure DevOps goes through here, which makes this the one place cancellation
        /// has to be honoured: the wait for a throttle slot refuses outright once the token is set, so a
        /// cancelled refresh stops before it spends the quota an operator cancelled to protect.
        /// </summary>
        private static async Task<T> ExecuteWithThrottle<T>(string url, Func<Task<T>> action, CancellationToken cancellationToken)
        {
            var limiter = GetLimiter(url);
            await limiter.WaitAsync(cancellationToken);
            try
            {
                return await ExecuteWithRetry(action, cancellationToken);
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

        private static async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action, CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromSeconds(1);
            for (var attempt = 0; attempt < 6; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await action();
                }
                catch (VssServiceException ex) when (IsRateLimited(ex))
                {
                    await Task.Delay(delay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)), cancellationToken);
                    delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));
                }
                catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                {
                    await Task.Delay(delay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)), cancellationToken);
                    delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return await action();
        }

        private async Task<IEnumerable<WorkItemBase>> ConvertAdoWorkItemToLighthouseWorkItemBase(
            IEnumerable<AdoWorkItem> adoWorkItems, IWorkItemQueryOwner workItemQueryOwner, Dictionary<int, string> fieldReferences, CancellationToken cancellationToken)
        {
            using var throttler = new SemaphoreSlim(8);
            var tasks = adoWorkItems.Select(async wi =>
            {
                await throttler.WaitAsync(cancellationToken);
                try { return await ConvertAdoWorkItemToLighthouseWorkItem(wi, workItemQueryOwner, fieldReferences, cancellationToken); }
                finally { throttler.Release(); }
            });
            return await Task.WhenAll(tasks);
        }

        private async Task<IEnumerable<WorkItemReference>> GetWorkItemReferencesByQuery(
            WorkItemTrackingHttpClient witClient, string query, CancellationToken cancellationToken)
        {
            var result = await ExecuteWithThrottle(witClient.BaseAddress!.ToString(),
                () => witClient.QueryByWiqlAsync(new Wiql { Query = query }, cancellationToken: cancellationToken), cancellationToken);

            // An answer carrying no result set at all is not an answer of "nothing matched": reading it as
            // one removes every record the query owner has, on a cycle nothing else complains about.
            if (result.WorkItems is null)
            {
                throw new InvalidOperationException(
                    $"Azure DevOps answered the query '{query}' without a result set.");
            }

            return result.WorkItems;
        }

        private async Task<IEnumerable<AdoWorkItem>> GetAdoWorkItemsById(
            IEnumerable<int> workItemIds, WorkTrackingSystemOptionsOwner workItemQueryOwner, IEnumerable<string> additionalFields, CancellationToken cancellationToken)
        {
            if (!workItemIds.Any())
            {
                return [];
            }

            logger.LogDebug("Getting Work Item with IDs {ItemIds}", string.Join(",", workItemIds));

            var witClient = await GetWorkItemTrackingHttpClientAsync(workItemQueryOwner.WorkTrackingSystemConnection);

            var fields = new List<string>
            {
                AzureDevOpsFieldNames.State,
                AzureDevOpsFieldNames.Title,
                AzureDevOpsFieldNames.WorkItemType,
                AzureDevOpsFieldNames.StackRank,
                AzureDevOpsFieldNames.BacklogPriority,
                AzureDevOpsFieldNames.CreatedDate,
                AzureDevOpsFieldNames.Tags,
                AzureDevOpsFieldNames.ChangedDate,
            };

            fields.AddRange(additionalFields.Where(f => !string.IsNullOrEmpty(f)));

            return await GetWorkItemsInChunks(workItemIds, witClient, WorkItemExpand.Links, fields, cancellationToken);
        }

        private static async Task<IEnumerable<AdoWorkItem>> GetWorkItemsInChunks(
            IEnumerable<int> workItemIds, WorkItemTrackingHttpClient witClient, WorkItemExpand expand, IEnumerable<string> fields, CancellationToken cancellationToken)
        {
            var url = witClient.BaseAddress!.ToString();
            var workItems = new List<AdoWorkItem>();

            foreach (var chunk in workItemIds.Chunk(MaxChunkSize))
            {
                var result = await ExecuteWithThrottle(url, () => witClient.GetWorkItemsAsync(chunk, fields, expand: expand, cancellationToken: cancellationToken), cancellationToken);
                workItems.AddRange(result);
            }

            return workItems;
        }

        private async Task<WorkItemBase> ConvertAdoWorkItemToLighthouseWorkItem(
            AdoWorkItem workItem, IWorkItemQueryOwner workItemQueryOwner, Dictionary<int, string> additionalFieldDefinitions, CancellationToken cancellationToken)
        {
            var state = workItem.ExtractStateFromWorkItem();
            var stateCategory = workItemQueryOwner.MapStateToStateCategory(state);

            var revisions = await RevisionsOfWorkItem(workItemQueryOwner, workItem.Id, cancellationToken);

            var (startedDate, closedDate) = StartedAndClosedDateFrom(revisions, workItemQueryOwner, stateCategory);

            var syncedTransitions = SyncedTransitionsFrom(revisions, workItemQueryOwner);

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
                State = workItemQueryOwner.MapRawStateToMappedName(state),
                StateCategory = stateCategory,
                Url = workItem.ExtractUrlFromWorkItem(),
                Order = workItem.ExtractStackRankFromWorkItem(),
                CreatedDate = workItem.ExtractCreatedDateFromWorkItem(),
                StartedDate = startedDate,
                ClosedDate = closedDate,
                Tags = workItem.ExtractTagsFromWorkItem(),
                LastChangedRemote = workItem.ExtractChangedDateFromWorkItem(),
                AdditionalFieldValues = additionalFields,
                SyncedTransitions = syncedTransitions,
            };
        }

        /// <summary>
        /// Azure DevOps has no endpoint that answers when an item crossed into a category, so the only way
        /// to know is to download every revision it has. Four separate questions are answered off this one
        /// list - started, closed, last re-entry into To Do, and the transitions to sync - and asking the
        /// tracker separately for each got four identical answers at four times the price, on every item of
        /// every refresh.
        /// </summary>
        private async Task<List<AdoWorkItem>> RevisionsOfWorkItem(
            IWorkItemQueryOwner workItemQueryOwner, int? workItemId, CancellationToken cancellationToken)
        {
            if (!workItemId.HasValue)
            {
                return [];
            }

            var witClient = await GetWorkItemTrackingHttpClientAsync(workItemQueryOwner.WorkTrackingSystemConnection);

            return await ExecuteWithThrottle(
                witClient.BaseAddress!.ToString(),
                () => witClient.GetRevisionsAsync(workItemId.Value, cancellationToken: cancellationToken),
                cancellationToken);
        }

        private static IReadOnlyList<WorkItemStateTransition> SyncedTransitionsFrom(
            List<AdoWorkItem> revisions, IWorkItemQueryOwner workItemQueryOwner)
            => WorkItemStateTransitionMapper.MapToMappedStates(StateTransitionsIn(revisions), workItemQueryOwner);

        private static (DateTime? StartedDate, DateTime? ClosedDate) StartedAndClosedDateFrom(
            List<AdoWorkItem> revisions, IWorkItemQueryOwner workItemQueryOwner, StateCategories stateCategory)
        {
            var rawToDoStates = workItemQueryOwner.GetRawStatesForCategory(workItemQueryOwner.ToDoStates);
            var rawDoingStates = workItemQueryOwner.GetRawStatesForCategory(workItemQueryOwner.DoingStates);
            var rawDoneStates = workItemQueryOwner.GetRawStatesForCategory(workItemQueryOwner.DoneStates);

            DateTime? startedDate = null;
            DateTime? closedDate = null;

            if (stateCategory == StateCategories.Done)
            {
                startedDate = StateTransitionDateFrom(revisions, rawDoingStates, rawDoneStates);
                closedDate = StateTransitionDateFrom(revisions, rawDoneStates, []);

                var lastToDoEntryDate = StateTransitionDateFrom(revisions, rawToDoStates, rawDoneStates);
                if (lastToDoEntryDate.HasValue && startedDate.HasValue && lastToDoEntryDate.Value > startedDate.Value)
                {
                    startedDate = null;
                }
            }
            else if (stateCategory == StateCategories.Doing)
            {
                startedDate = StateTransitionDateFrom(revisions, rawDoingStates, rawDoneStates);
            }

            if (startedDate == null && closedDate != null)
            {
                startedDate = closedDate;
            }

            return (startedDate, closedDate);
        }

        private static DateTime? StateTransitionDateFrom(
            List<AdoWorkItem> revisions, List<string> targetStates, List<string> statesToIgnore)
            => WorkItemCategoryCrossing.LastEntryInto(StateChangesIn(revisions), targetStates, statesToIgnore);

        // Every revision that changed the state, paired with the state it came from. The earliest one
        // comes from nothing, which is how an item created straight into a category is still dated.
        private static IEnumerable<WorkItemStateTransition> StateChangesIn(IEnumerable<AdoWorkItem> revisions)
        {
            var previousState = string.Empty;

            foreach (var revision in revisions)
            {
                if (!RevisionWasChangingState(revision, out var result))
                {
                    continue;
                }

                yield return new WorkItemStateTransition
                {
                    FromState = previousState,
                    ToState = result.state,
                    TransitionedAt = result.changedDate,
                };

                previousState = result.state;
            }
        }

        internal static async Task<IReadOnlyList<WorkItemStateTransition>> GetAllStateTransitionsThrottled(
            WorkItemTrackingHttpClient witClient, int workItemId, CancellationToken cancellationToken)
        {
            var revisions = await ExecuteWithThrottle(
                witClient.BaseAddress!.ToString(),
                () => witClient.GetRevisionsAsync(workItemId, cancellationToken: cancellationToken),
                cancellationToken);

            return StateTransitionsIn(revisions);
        }

        /// <summary>
        /// The state changes an item actually made, collapsed: a revision that re-saved the same state is
        /// not a transition, and the first state an item ever held is where the walk starts rather than
        /// something it moved from.
        /// </summary>
        private static List<WorkItemStateTransition> StateTransitionsIn(IEnumerable<AdoWorkItem> revisions)
        {
            var transitions = new List<WorkItemStateTransition>();
            var previousState = string.Empty;

            foreach (var revision in revisions)
            {
                if (!RevisionWasChangingState(revision, out var result))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(previousState) && !previousState.Equals(result.state, StringComparison.OrdinalIgnoreCase))
                {
                    transitions.Add(new WorkItemStateTransition
                    {
                        FromState = previousState,
                        ToState = result.state,
                        TransitionedAt = DateTime.SpecifyKind(result.changedDate, DateTimeKind.Utc),
                    });
                }

                previousState = result.state;
            }

            return transitions;
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

        private async Task<RelationsOfWorkItems> GetRelationsOfWorkItems(
            IEnumerable<AdoWorkItem> adoWorkItems,
            WorkTrackingSystemOptionsOwner workTrackingSystemOptionOwner,
            AdditionalFieldResolution additionalFieldResolution,
            DependenciesFromRelations dependencies,
            CancellationToken cancellationToken)
        {
            // The relations carry two different things at once: the link to the parent, and the links to
            // whatever this item is waiting on. So naming a field to read the parent from is no longer
            // reason enough on its own to skip reading them - skipping it while the dependencies still
            // live there leaves every dependency unread, for good, and an empty list of dependencies is a
            // believable answer, so nothing about it looks wrong. A Parent Override Field naming a relation
            // type reads no parent at all, so the native parent link still has to be read - and so it does
            // when the relation types could not be listed, since the reference may well be one.
            var parentOverride = workTrackingSystemOptionOwner.ParentOverrideAdditionalFieldDefinitionId;
            var theParentComesFromAField = parentOverride.HasValue
                && !additionalFieldResolution.NamesARelationType(parentOverride)
                && !additionalFieldResolution.CouldNotTellWhatItNames(parentOverride);

            if (theParentComesFromAField && !dependencies.AreRead)
            {
                return RelationsOfWorkItems.None();
            }

            var itemIds = adoWorkItems.Select(wi => wi.Id ?? -1).Where(i => i >= 0).ToList();

            if (itemIds.Count == 0)
            {
                return RelationsOfWorkItems.None();
            }

            logger.LogDebug("Getting Relations for Work Items with IDs {ItemIds}", string.Join(",", itemIds));

            return await ReadRelationsFromTracker(workTrackingSystemOptionOwner, itemIds, dependencies.NamedRelationEnd, cancellationToken);
        }

        private async Task<RelationsOfWorkItems> ReadRelationsFromTracker(
            WorkTrackingSystemOptionsOwner workTrackingSystemOptionOwner, List<int> itemIds, string? namedRelationEnd, CancellationToken cancellationToken)
        {
            logger.LogDebug("Reading the parent link and the dependency links off the relations of {ItemCount} Work Items", itemIds.Count);

            var relations = RelationsOfWorkItems.None();
            foreach (var id in itemIds)
            {
                relations.ParentReferences.Add($"{id}", string.Empty);
            }

            var witClient = await GetWorkItemTrackingHttpClientAsync(workTrackingSystemOptionOwner.WorkTrackingSystemConnection);
            var workItemsWithRelations = await GetWorkItemsInChunks(itemIds, witClient, WorkItemExpand.Relations, NoFieldsBesideTheRelations, cancellationToken);

            foreach (var adoWorkItem in workItemsWithRelations)
            {
                var referenceId = adoWorkItem.Id.ToString() ?? "-1";

                relations.ParentReferences[referenceId] = adoWorkItem.ExtractParentFromWorkItem();
                relations.DependencyReferences[referenceId] = namedRelationEnd is null
                    ? adoWorkItem.ExtractDependencyReferences()
                    : adoWorkItem.ExtractDependencyReferences(namedRelationEnd);
                relations.RelationTypesSeen.AddRange(TheWorkItemLinksItCarries(adoWorkItem).Except(relations.RelationTypesSeen, StringComparer.OrdinalIgnoreCase));
            }

            return relations;
        }

        // Attachments, hyperlinks and artifact links point at no work item, so they could never have matched.
        // The hierarchy places a work item under its parent rather than linking two pieces of work, and nearly
        // every Feature has children, so counting it would say "nothing matched" on almost every refresh.
        private static IEnumerable<string> TheWorkItemLinksItCarries(AdoWorkItem adoWorkItem)
            => (adoWorkItem.Relations ?? [])
                .Select(relation => relation.Rel)
                .Where(rel => !string.IsNullOrEmpty(rel) && !ResourceLinks.Contains(rel) && !HierarchyLinks.Contains(rel))
                .Distinct(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// What one work item waits on. Azure DevOps supplies what it read off the relations - the Predecessor
        /// links, or the links of the one relation end the Portfolio named instead; which of that is used, and
        /// what a Portfolio reading a field of its own gets instead, is not this tracker's decision to make.
        /// </summary>
        private static List<FeatureDependencyReference> TheDependenciesOf(
            Portfolio portfolio, WorkItemBase workItem, RelationsOfWorkItems relations, string? namedRelationEnd)
        {
            if (!relations.DependencyReferences.TryGetValue(workItem.ReferenceId, out var references))
            {
                references = [];
            }

            return namedRelationEnd is null
                ? DependencySourceSelector.TheDependenciesOf(portfolio, workItem, references)
                : DependencySourceSelector.TheDependenciesOf(portfolio, workItem, [], references);
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

            if (!string.IsNullOrEmpty(parentReference))
            {
                return parentReference;
            }

            return parentReferences.TryGetValue(workItem.ReferenceId, out var reference) ? reference : string.Empty;
        }

        private static string ExtractFieldValue(AdoWorkItem adoWorkItem, string fieldName)
        {
            if (!string.IsNullOrEmpty(fieldName) && adoWorkItem.Fields.TryGetValue(fieldName, out var fieldValue))
            {
                return fieldValue.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        // One place builds each query, because the sweep and the download have to ask the same question:
        // removal is "stored minus swept", so anything the two disagree about is deleted.
        private string TheQueryATeamIsFetchedBy(Team team)
            => PrepareQuery(team.WorkItemTypes, team.AllStates, team.DataRetrievalValue, team.DoneItemsCutoffDays);

        private string TheQueryAPortfolioIsFetchedBy(Portfolio portfolio)
            => PrepareQuery(portfolio.WorkItemTypes, portfolio.AllStates, portfolio.DataRetrievalValue, portfolio.DoneItemsCutoffDays);

        private static string TheQueryParentFeaturesAreFetchedBy(IEnumerable<string> parentFeatureIds)
        {
            var whereClause = string.Join(" OR ", parentFeatureIds.Select(id => $"[{AzureDevOpsFieldNames.Id}] = {id}"));

            return $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}] FROM WorkItems WHERE {whereClause}";
        }

        private string PrepareQuery(
            IEnumerable<string> includedWorkItemTypes,
            IEnumerable<string> includedStates,
            string query,
            int cutOffDays)
        {
            return PrepareQuery(includedWorkItemTypes, includedStates, query, null, cutOffDays);
        }

        /// <summary>
        /// A configuration that narrows nothing is refused rather than asked. The settings API accepts empty
        /// type and state lists, and a restored database can carry them too, and with no query of the
        /// operator's either the WHERE clause comes out with no condition under it. The cutoff does not count
        /// as narrowing: it only bounds how far back finished work is read, so a query made of nothing else
        /// still asks for the whole organisation.
        /// </summary>
        private string PrepareQuery(
            IEnumerable<string> includedWorkItemTypes,
            IEnumerable<string> includedStates,
            string query,
            string? extraField,
            int cutOffDays)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(includedWorkItemTypes);
            var stateQuery = PrepareStateQuery(includedStates);

            if (NothingNarrowsTheQuery(query, workItemsQuery, stateQuery))
            {
                throw AzureDevOpsReadException.NothingNarrowsTheQuery();
            }

            var extraFieldsQuery = string.Empty;
            if (!string.IsNullOrEmpty(extraField))
            {
                extraFieldsQuery = $", [{extraField}]";
            }

            var cutoffDateFilter = PrepareCutoffDateFilter(cutOffDays);
            var lighthousesOwnFilters = $"{workItemsQuery} {stateQuery} {cutoffDateFilter}";

            var conditions = string.IsNullOrWhiteSpace(query)
                ? WithoutTheLeadingConjunction(lighthousesOwnFilters)
                : $"({query}) {lighthousesOwnFilters}";

            return $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}]{extraFieldsQuery} FROM WorkItems WHERE {conditions}";
        }

        /// <summary>
        /// Nothing asks an owner for a query of its own, and one that has none has no filter to wrap. Wrapping
        /// it anyway writes an empty bracket pair, which Azure DevOps will not parse: a pair of brackets has to
        /// hold a condition, so every refresh that owner runs fails on a syntax error about a query nobody
        /// wrote. Leaving the pair out moves whatever came next to the front, and a WHERE that opens on AND is
        /// refused just as flatly, so the conjunction comes off with the brackets.
        /// </summary>
        private static string WithoutTheLeadingConjunction(string filters)
        {
            const string conjunction = "AND ";
            var withoutLeadingSpace = filters.TrimStart();

            return withoutLeadingSpace.StartsWith(conjunction, StringComparison.Ordinal)
                ? withoutLeadingSpace[conjunction.Length..]
                : withoutLeadingSpace;
        }

        private static bool NothingNarrowsTheQuery(string query, string workItemsQuery, string stateQuery)
            => string.IsNullOrWhiteSpace(query)
                && string.IsNullOrWhiteSpace(workItemsQuery)
                && string.IsNullOrWhiteSpace(stateQuery);

        private string PrepareCutoffDateFilter(int cutOffDays)
        {
            if (cutOffDays <= 0)
            {
                return string.Empty;
            }

            // Bug #5567 decision 4: stays UTC. A tracker's history window is an instant offset, not
            // a calendar day, and the remote system has its own zone; an off-by-one only over-fetches.
            var cutoffDate = timeProvider.GetUtcNow().UtcDateTime.AddDays(-cutOffDays);

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
            var conditions = options.Select(opt => $"[{fieldName}] {queryComparison} '{opt}'").ToList();

            return conditions.Count == 0
                ? string.Empty
                : $"AND ({string.Join($" {queryOperator} ", conditions)}) ";
        }

        // Overridable so a test can hand the connector a recording client. Which requests this connector
        // issues - and how many of them per work item - is otherwise only observable against a live
        // organisation, and that is the whole subject of the incremental sync.
        internal virtual Task<WorkItemTrackingHttpClient> GetWorkItemTrackingHttpClientAsync(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            return GetClientAsync<WorkItemTrackingHttpClient>(workTrackingSystemConnection);
        }

        private async Task<TClient> GetClientAsync<TClient>(WorkTrackingSystemConnection workTrackingSystemConnection) where TClient : IVssHttpClient
        {
            var (connection, key) = await GetConnectionForWorkTrackingSystemAsync(workTrackingSystemConnection);
            var cacheKey = $"{typeof(TClient).FullName}_{key}";

            return (TClient)ClientCache.GetOrAdd(cacheKey, _ => connection.GetClient<TClient>());
        }

        private async Task<(VssConnection connection, string key)> GetConnectionForWorkTrackingSystemAsync(
            WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            var url = workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.Url);
            var (credentials, credentialFingerprint) = await BuildVssCredentialsAsync(workTrackingSystemConnection);

            // Cache key MUST include a credential fingerprint, otherwise PAT rotation and OAuth token
            // refresh produce a fresh VssCredentials object that the cache silently discards in favour
            // of the originally-cached VssConnection — every subsequent ADO call then runs with the
            // stale credential and surfaces as a 401 (OAuth) or auth-required (rotated PAT) failure
            // until the process restarts.
            var key = $"{url}|{workTrackingSystemConnection.AuthenticationMethodKey}|conn:{workTrackingSystemConnection.Id}|{credentialFingerprint}";

            var requestTimeoutInSeconds =
                workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey<int>(AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds) ?? 100;

            var connection = ConnectionCache.GetOrAdd(key, _ =>
            {
                var c = new VssConnection(new Uri(url), credentials);
                c.Settings.SendTimeout = TimeSpan.FromSeconds(requestTimeoutInSeconds);
                return c;
            });

            return (connection, key);
        }

        private async Task<(VssCredentials credentials, string fingerprint)> BuildVssCredentialsAsync(WorkTrackingSystemConnection connection)
        {
            using var probeRequest = new HttpRequestMessage();
            var strategy = authStrategyFactory.Resolve(connection.AuthenticationMethodKey);
            await strategy.ApplyAsync(probeRequest, connection, CancellationToken.None);

            var header = probeRequest.Headers.Authorization
                ?? throw new InvalidOperationException(
                    $"Authentication strategy for '{connection.AuthenticationMethodKey}' did not set an Authorization header.");

            return (ConvertAuthorizationHeaderToVssCredentials(header), ComputeCredentialFingerprint(header));
        }

        private static string ComputeCredentialFingerprint(AuthenticationHeaderValue header)
        {
            var input = $"{header.Scheme}:{header.Parameter}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        private static VssCredentials ConvertAuthorizationHeaderToVssCredentials(AuthenticationHeaderValue header)
        {
            if (string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter ?? string.Empty));
                var separatorIndex = decoded.IndexOf(':');
                var username = separatorIndex >= 0 ? decoded[..separatorIndex] : string.Empty;
                var password = separatorIndex >= 0 ? decoded[(separatorIndex + 1)..] : decoded;
                return new VssBasicCredential(username, password);
            }

            if (string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
            {
                return new VssCredentials(new VssOAuthAccessTokenCredential(header.Parameter ?? string.Empty));
            }

            throw new NotSupportedException(
                $"Authorization scheme '{header.Scheme}' produced by the auth strategy is not supported by the Azure DevOps connector.");
        }

        /// <summary>
        /// What each additional field definition resolved to on one fetch: the field it reads, empty when it
        /// reads none, which of the definitions named a relation type instead, and which matched no field
        /// while the relation types could not be listed, so nobody can tell whether they name one. Never
        /// stored: it answers for the organisation of the fetch that built it.
        /// </summary>
        private sealed record AdditionalFieldResolution(
            Dictionary<int, string> Fields, Dictionary<int, string> RelationTypes, HashSet<int> CouldNotTell, Dictionary<int, WhyNothingIsRead> ReadsNothing)
        {
            public static AdditionalFieldResolution None() => new([], [], [], []);

            /// <summary>
            /// A base name whose two ends are both listed names a relation type but neither direction, and a type
            /// without direction cannot say which end waits. Either is still a relation type, so a setting that
            /// reads only fields says so, but the Dependency Field reads nothing off it rather than guess a direction.
            /// The ends are checked first, since a server may also list the type under its base name.
            /// </summary>
            public bool ResolvesToARelationType(AdditionalFieldDefinition definition, RelationTypeListing relationTypes)
            {
                var bothEnds = relationTypes.TheEndsOf(definition.Reference);
                if (bothEnds.Count > 0)
                {
                    RelationTypes.Add(definition.Id, definition.Reference);
                    ReadsNothing.Add(definition.Id, new WhyNothingIsRead(RefreshFindingKind.RelationTypeNamedWithoutItsEnd, bothEnds));
                    return true;
                }

                var relationEnd = relationTypes.TheEndCalled(definition.Reference);
                if (relationEnd is null)
                {
                    return false;
                }

                RelationTypes.Add(definition.Id, relationEnd);
                if (relationTypes.HasNoDirection(relationEnd))
                {
                    ReadsNothing.Add(definition.Id, new WhyNothingIsRead(RefreshFindingKind.RelationTypeHasNoDirection, []));
                }

                return true;
            }

            public WhyNothingIsRead? WhyNothingIsReadFor(int? definitionId)
                => definitionId.HasValue && ReadsNothing.TryGetValue(definitionId.Value, out var why) ? why : null;

            public bool NamesARelationType(int? definitionId) => definitionId.HasValue && RelationTypes.ContainsKey(definitionId.Value);

            // Spelled as the organisation lists it, since that is how its relations spell it too.
            public string? TheRelationEndNamedBy(int? definitionId)
                => definitionId.HasValue && RelationTypes.TryGetValue(definitionId.Value, out var relationEnd) ? relationEnd : null;

            public bool CouldNotTellWhatItNames(int? definitionId) => definitionId.HasValue && CouldNotTell.Contains(definitionId.Value);
        }
    }
}
