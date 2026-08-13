using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Extensions;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    public class JiraWorkTrackingConnector(
        IIssueFactory issueFactory,
        ILogger<JiraWorkTrackingConnector> logger,
        IWorkTrackingAuthStrategyFactory authStrategyFactory,
        HttpMessageHandler? httpMessageHandlerForTesting = null)
        : IJiraWorkTrackingConnector
    {
        private readonly HttpMessageHandler? _httpMessageHandlerForTesting = httpMessageHandlerForTesting;

        private enum JiraDeployment { Unknown, Cloud, DataCenter }

        private static readonly Dictionary<int, Dictionary<string, string>> FieldNames = new();

        private const string DefaultFlaggedFieldReference = "customfield_10001";

        private const string BoardsEndpoint = "rest/agile/latest/board";

        private const string AllFields = "*all";

        // Phase 1 reads identity and the change stamp and nothing else - the changelog alone is the bulk of an issue.
        private const string SweepFields = "key,updated";

        private const int MaxCloudSearchPages = 100;

        private const int ReferenceIdsPerQuery = 200;

        private const string DeploymentNotKnownYet =
            "Lighthouse has not reached this Jira instance yet, so it does not know which kind of Jira it is.";

        private static readonly SocketsHttpHandler SharedHandler = new()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 100,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            EnableMultipleHttp2Connections = true,
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(10)
        };

        private static readonly ConcurrentDictionary<string, HttpClient> ClientCache = new();
        private static readonly ConcurrentDictionary<string, JiraDeployment> DeploymentCache = new();
        private static readonly ConcurrentDictionary<string, string> CloudIdCache = new();

        public bool SupportsTransitionHistory(WorkTrackingSystemConnection connection) => true;

        /// <summary>
        /// Yes for any instance Lighthouse has already reached, because the sweep needs to know which kind of
        /// Jira it is talking to - Cloud and Data Center page their search results in completely different ways.
        /// This cannot block on a round trip to go and find out, so an instance never reached answers no, and
        /// that cycle downloads everything instead. Slower, but never wrong.
        /// </summary>
        public bool SupportsIncrementalSync(WorkTrackingSystemConnection connection)
            => TryResolveKnownDeployment(connection, out var deployment) && deployment is not JiraDeployment.Unknown;

        /// <summary>
        /// Phase 1 of the two-phase fetch: the very query <see cref="GetWorkItemsForTeam(Team)"/> issues,
        /// narrowed to identity plus the change stamp. Removal is "stored minus swept", so reporting a
        /// half-walked query would delete whatever the missing pages held - a rejected page throws instead,
        /// and the caller falls back to the whole query.
        /// </summary>
        public Task<IReadOnlyList<RemoteRecordStamp>> SweepWorkItemsForTeam(Team team)
            => SweepIdentities(team, [SweepQueryFor(team)], $"Team {team.Name}");

        /// <summary>
        /// The one sweep every caller goes through: walk each query in full, keep identity and the change stamp,
        /// and refuse to answer at all when a page was rejected - a half-walked query reported as whole puts every
        /// record on the missing pages into "stored minus swept", which deletes them.
        ///
        /// The two deployments page their search results differently and neither endpoint exists on the other,
        /// so the transport is chosen here. The deployment is guaranteed to be known: the capability check just
        /// above is the question "has this instance been reached yet", and it answered yes.
        /// </summary>
        private async Task<IReadOnlyList<RemoteRecordStamp>> SweepIdentities(
            IWorkItemQueryOwner owner, IEnumerable<string> sweepQueries, string sweptDescription)
        {
            if (!SupportsIncrementalSync(owner.WorkTrackingSystemConnection))
            {
                throw new NotSupportedException(DeploymentNotKnownYet);
            }

            var sweepsOverCloud = TryResolveKnownDeployment(owner.WorkTrackingSystemConnection, out var deployment)
                && deployment == JiraDeployment.Cloud;

            var client = await GetJiraRestClientAsync(owner.WorkTrackingSystemConnection);
            var pageLimit = ResolveIssuesPerRequest(owner.WorkTrackingSystemConnection);
            var stamps = new List<RemoteRecordStamp>();

            foreach (var sweepQuery in sweepQueries)
            {
                var walkedEveryPage = sweepsOverCloud
                    ? await WalkCloudSearchPages(
                        client,
                        new CloudSearchRequest(sweepQuery, SweepFields, ExpandChangelog: false, pageLimit, SinglePage: false),
                        CollectStamp)
                    : await WalkDataCenterSearchOffsets(client, OrderedForOffsetPaging(sweepQuery), pageLimit, CollectStamp);

                if (!walkedEveryPage)
                {
                    throw new InvalidOperationException($"Jira rejected a page of the identity sweep for {sweptDescription}.");
                }
            }

            return stamps;

            Task CollectStamp(JsonElement jsonIssue)
            {
                stamps.Add(ReadStamp(jsonIssue));
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Parsed exactly the way <see cref="IssueFactory"/> parses it (D6), because D12 compares swept
        /// stamp against stored stamp per record with no watermark - a second reading of the same string
        /// would report every record as moved, forever. A record with no readable stamp still belongs in the
        /// sweep: leaving it out would put it in "stored minus swept" and delete it, where an unmatchable
        /// stamp merely re-downloads it (D8).
        /// </summary>
        private static RemoteRecordStamp ReadStamp(JsonElement jsonIssue)
        {
            var referenceId = jsonIssue.GetProperty(JiraFieldNames.KeyPropertyName).GetString() ?? string.Empty;

            if (!jsonIssue.TryGetProperty(JiraFieldNames.FieldsFieldName, out var fields))
            {
                return new RemoteRecordStamp(referenceId, default);
            }

            var updated = fields.GetFieldValue(JiraFieldNames.UpdatedFieldName);

            return DateTimeOffset.TryParse(updated, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var changedAt)
                ? new RemoteRecordStamp(referenceId, changedAt.UtcDateTime)
                : new RemoteRecordStamp(referenceId, default);
        }

        // Jira contributes exactly one system-owned (predefined) additional field: the flagged/impediment
        // field. Its Reference is resolved WITHOUT a live Jira call — from the connection's already-discovered
        // flagged field mapping when present, otherwise the stable Jira Cloud default. This keeps the field
        // deterministic and its Reference immutable across syncs (ADR-071).
        public IReadOnlyList<AdditionalFieldDefinition> GetPredefinedAdditionalFields(WorkTrackingSystemConnection connection)
        {
            return
            [
                new AdditionalFieldDefinition
                {
                    DisplayName = JiraFieldNames.FlaggedName,
                    Reference = ResolveFlaggedFieldReference(connection.Id),
                    IsPredefined = true,
                },
            ];
        }

        private static string ResolveFlaggedFieldReference(int workTrackingSystemConnectionId)
        {
            if (FieldNames.TryGetValue(workTrackingSystemConnectionId, out var mapping)
                && mapping.TryGetValue(JiraFieldNames.FlaggedName, out var flaggedReference)
                && !string.IsNullOrEmpty(flaggedReference))
            {
                return flaggedReference;
            }

            return DefaultFlaggedFieldReference;
        }

        // Idempotent get-or-create of the connector's predefined additional field(s) on the connection's
        // in-memory definitions — mirrors WorkTrackingSystemConnectionController.EnsurePredefinedAdditionalFieldsRegistered
        // (minus persistence, which the sync path neither owns nor needs). Matching on IsPredefined keeps the
        // resolved Reference authoritative across syncs and prevents a duplicate when a stale default-Reference
        // definition was persisted before the flagged field key was first resolved.
        private void EnsurePredefinedAdditionalFieldsRegistered(WorkTrackingSystemConnection connection)
        {
            foreach (var predefined in GetPredefinedAdditionalFields(connection))
            {
                var existing = connection.AdditionalFieldDefinitions
                    .FirstOrDefault(field => field.IsPredefined && field.DisplayName == predefined.DisplayName);

                if (existing is null)
                {
                    connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
                    {
                        DisplayName = predefined.DisplayName,
                        Reference = predefined.Reference,
                        IsPredefined = true,
                    });
                }
                else
                {
                    existing.Reference = predefined.Reference;
                }
            }
        }

        public async Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            var workItems = new List<WorkItem>();

            logger.LogDebug("Updating Work Items for Team {TeamName}", team.Name);

            var query = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, team.DataRetrievalValue, team.DoneItemsCutoffDays)}";
            var issues = await GetIssuesByQuery(team, query);

            workItems.AddRange(await CreateWorkItemsFromIssues(team, issues));

            return workItems;
        }

        /// <summary>
        /// The download half of the two-phase fetch: full payloads for the named records and for nothing else.
        /// The team's own filter is deliberately not re-applied - the sweep already decided what belongs to
        /// the query, and a second cutoff evaluation could drop an item the sweep just reported as changed.
        /// Sweeping and fetching by key are one capability, so an instance that cannot be swept never reaches here.
        /// </summary>
        public async Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team, IReadOnlyCollection<string> referenceIds)
        {
            if (!SupportsIncrementalSync(team.WorkTrackingSystemConnection))
            {
                throw new NotSupportedException(DeploymentNotKnownYet);
            }

            if (referenceIds.Count == 0)
            {
                return [];
            }

            logger.LogDebug("Updating {Count} Work Item(s) by reference id for Team {TeamName}", referenceIds.Count, team.Name);

            var issues = new List<Issue>();

            // A changed set can run to thousands of keys, and every key is another OR clause in a URL that has
            // to survive whatever proxy sits in front of Jira - chunked the way the Azure DevOps connector
            // chunks its id list.
            foreach (var chunk in referenceIds.Chunk(ReferenceIdsPerQuery))
            {
                issues.AddRange(await GetIssuesByQuery(team, PrepareIssueKeyQuery(chunk)));
            }

            return await CreateWorkItemsFromIssues(team, issues);
        }

        private async Task<List<WorkItem>> CreateWorkItemsFromIssues(Team team, IEnumerable<Issue> issues)
        {
            // The Jira flag flows into a work item ONLY through the predefined additional field (ADR-071).
            // GetWorkItemsForTeam does not pass through the controller, so the predefined field must be
            // registered on the connection here — after GetIssuesByQuery has resolved the flagged field key
            // (SetStoredFieldKeys) — so PopulateAdditionalFieldValues picks it up with the resolved Reference.
            EnsurePredefinedAdditionalFieldsRegistered(team.WorkTrackingSystemConnection);

            var customFieldReferences = await GetCustomFieldReferences(team.WorkTrackingSystemConnection);

            var epicLinkFieldName = !team.ParentOverrideAdditionalFieldDefinitionId.HasValue ? FieldNames[team.WorkTrackingSystemConnectionId][JiraFieldNames.EpicLinkFieldName] : string.Empty;

            var workItems = new List<WorkItem>();

            foreach (var issue in issues)
            {
                var workItemBase = CreateWorkItemFromJiraIssue(issue, team, customFieldReferences);

                TrySetParentForJiraDataCenter(workItemBase, issue, epicLinkFieldName);

                workItems.Add(new WorkItem(workItemBase, team));
            }

            return workItems;
        }

        public async Task<List<Feature>> GetFeaturesForProject(Portfolio project)
        {
            logger.LogInformation("Getting Features of Type {WorkItemTypes} and Query '{Query}'", string.Join(", ", project.WorkItemTypes), project.DataRetrievalValue);

            var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.DataRetrievalValue, project.DoneItemsCutoffDays);
            var issues = await GetIssuesByQuery(project, query);
            return await CreateFeaturesFromIssues(project, issues);
        }

        /// <summary>
        /// The download half of the two-phase fetch: full payloads for the named Features and for nothing else.
        /// The portfolio's own filter is deliberately not re-applied - the sweep already decided what belongs to
        /// the query, and a second cutoff evaluation could drop a Feature the sweep just reported as changed.
        /// Sweeping and fetching by key are one capability, so an instance that cannot be swept never reaches here.
        /// </summary>
        public async Task<List<Feature>> GetFeaturesForProject(Portfolio project, IReadOnlyCollection<string> referenceIds)
        {
            if (!SupportsIncrementalSync(project.WorkTrackingSystemConnection))
            {
                throw new NotSupportedException(DeploymentNotKnownYet);
            }

            if (referenceIds.Count == 0)
            {
                return [];
            }

            logger.LogDebug("Updating {Count} Feature(s) by reference id for Portfolio {PortfolioName}", referenceIds.Count, project.Name);

            var issues = new List<Issue>();

            // Chunked exactly like the by-key Work Item fetch: every key is another OR clause in a URL that has
            // to survive whatever proxy sits in front of Jira.
            foreach (var chunk in referenceIds.Chunk(ReferenceIdsPerQuery))
            {
                issues.AddRange(await GetIssuesByQuery(project, PrepareIssueKeyQuery(chunk)));
            }

            return await CreateFeaturesFromIssues(project, issues);
        }

        /// <summary>
        /// The sweep half of the two-phase fetch: the very query <see cref="GetFeaturesForProject(Portfolio)"/>
        /// issues, narrowed to identity plus the change stamp.
        /// </summary>
        public Task<IReadOnlyList<RemoteRecordStamp>> SweepFeaturesForPortfolio(Portfolio project)
            => SweepIdentities(project, [SweepQueryFor(project)], $"Portfolio {project.Name}");

        public async Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds)
        {
            logger.LogInformation("Getting Parent Features Details for Project {ProjectName} with Feature IDs {FeatureIds}", project.Name, string.Join(", ", parentFeatureIds));

            var issues = await GetIssuesByQuery(project, PrepareIssueKeyQuery(parentFeatureIds));
            return await CreateFeaturesFromIssues(project, issues);
        }

        /// <summary>
        /// Phase 1 for the parent Features: the very keys <see cref="GetParentFeaturesDetails"/> asks for,
        /// chunked the same way, narrowed to identity plus the change stamp.
        /// </summary>
        public Task<IReadOnlyList<RemoteRecordStamp>> SweepParentFeatures(Portfolio project, IEnumerable<string> parentFeatureIds)
            => SweepIdentities(
                project,
                parentFeatureIds.Chunk(ReferenceIdsPerQuery).Select(PrepareIssueKeyQuery),
                $"the parent Features of Portfolio {project.Name}");

        public async Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            var url = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Url);
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return ConnectionValidationResult.Failure(
                    "invalid_url",
                    "The Jira URL appears to be invalid.",
                    "Provide a full URL, for example https://your-domain.atlassian.net.",
                    JiraWorkTrackingOptionNames.Url);
            }

            try
            {
                var client = await GetJiraRestClientAsync(connection);
                var response = await client.GetAsync("rest/api/2/myself");
                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    logger.LogInformation(
                        "Authentication is not valid for {Connection}. Jira returned {StatusCode} {Reason} at {RequestUri}. Body: {Body}",
                        connection.Name,
                        (int)response.StatusCode,
                        response.ReasonPhrase,
                        response.RequestMessage?.RequestUri,
                        responseBody);

                    return ConnectionValidationResult.Failure(
                        "authentication_failed",
                        "Authentication failed for Jira.",
                        $"Jira returned {(int)response.StatusCode} {response.ReasonPhrase}.",
                        JiraWorkTrackingOptionNames.ApiToken);
                }

                var missingFields = await GetMissingAdditionalFields(connection);
                if (missingFields.Count > 0)
                {
                    return ConnectionValidationResult.Failure(
                        "additional_fields_invalid",
                        $"Some additional fields could not be found: {string.Join(", ", missingFields)}",
                        "Verify field names or references in Jira and update the additional field configuration.",
                        "Additional Fields");
                }

                return ConnectionValidationResult.Success();
            }
            catch (HttpRequestException ex)
            {
                logger.LogInformation(ex, "Jira endpoint validation failed for connection {ConnectionName}", connection.Name);

                return ConnectionValidationResult.Failure(
                    "connection_failed",
                    "Could not reach Jira with the provided URL.",
                    ex.Message,
                    JiraWorkTrackingOptionNames.Url);
            }
            catch (UriFormatException ex)
            {
                logger.LogInformation(ex, "Jira URL parsing failed for connection {ConnectionName}", connection.Name);

                return ConnectionValidationResult.Failure(
                    "invalid_url",
                    "The Jira URL appears to be invalid.",
                    ex.Message,
                    JiraWorkTrackingOptionNames.Url);
            }
            catch
            {
                return ConnectionValidationResult.Failure(
                    "validation_failed",
                    "Connection validation failed due to an unexpected error.");
            }
        }

        public async Task<IEnumerable<Board>> GetBoards(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            try
            {
                logger.LogInformation("Getting Boards for System {ConnectionName}", workTrackingSystemConnection.Name);
                var client = await GetJiraRestClientAsync(workTrackingSystemConnection);

                return await GetBoardsFromJira(workTrackingSystemConnection, client);
            }
            catch
            {
                return [];
            }
        }

        public async Task<BoardInformation> GetBoardInformation(WorkTrackingSystemConnection workTrackingSystemConnection, string boardId)
        {
            try
            {
                logger.LogInformation("Getting Board Information for Board {BoardId}", boardId);
                var client = await GetJiraRestClientAsync(workTrackingSystemConnection);

                return await GetBoardInformationFromJira(client, boardId);
            }
            catch
            {
                return new BoardInformation();
            }
        }

        public async Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            if (updates.Count == 0)
            {
                return new WriteBackResult();
            }

            var client = await GetJiraRestClientAsync(connection);
            var results = await UpdateItems(updates, client);

            return new WriteBackResult { ItemResults = results };
        }

        private async Task<List<WriteBackItemResult>> UpdateItems(IReadOnlyList<WriteBackFieldUpdate> updates, HttpClient client)
        {
            var results = new List<WriteBackItemResult>();

            var additionalFieldReferences = await GetCustomFieldMappings(client,
                [JiraFieldNames.NamePropertyName, JiraFieldNames.IdPropertyName, JiraFieldNames.KeyPropertyName],
                updates.Select(f => f.TargetFieldReference));

            foreach (var issueUpdates in updates.GroupBy(update => update.WorkItemId))
            {
                results.AddRange(await UpdateIssue(client, issueUpdates.Key, [.. issueUpdates], additionalFieldReferences));
            }

            return results;
        }

        /// <summary>
        /// One PUT carrying every changed field on the issue (ADR-143). Jira rejects a mixed-validity
        /// `fields` object whole — measured, not assumed — so a rejected batch is re-sent field by field:
        /// the valid ones land and the offending one fails alone, which is the isolation the per-field
        /// calls used to give for free.
        /// </summary>
        private async Task<List<WriteBackItemResult>> UpdateIssue(HttpClient client, string issueKey,
            IReadOnlyList<WriteBackFieldUpdate> updates, Dictionary<string, string> additionalFieldReferences)
        {
            var batch = await TryWriteFields(client, issueKey, updates, additionalFieldReferences);

            if (batch.Succeeded)
            {
                return [.. updates.Select(update => WriteBackItemResult.Written(update, batch.Suppression))];
            }

            // A single field is already as isolated as it gets; re-sending it would only repeat the failure.
            if (updates.Count == 1)
            {
                return [WriteBackItemResult.Refused(updates[0], batch.ErrorMessage, batch.Suppression)];
            }

            var results = new List<WriteBackItemResult>();

            foreach (var update in updates)
            {
                var single = await TryWriteFields(client, issueKey, [update], additionalFieldReferences);
                results.Add(single.Succeeded
                    ? WriteBackItemResult.Written(update, single.Suppression)
                    : WriteBackItemResult.Refused(update, single.ErrorMessage, single.Suppression));
            }

            return results;
        }

        /// <summary>
        /// One attempt to write a set of fields, plus what it learned about staying quiet. The 403 retry
        /// lives here rather than in <see cref="UpdateIssue"/> so the two degradations compose: dropping
        /// suppression keeps the batch, and dropping the batch keeps the suppression request (ADR-142,
        /// ADR-143 §2).
        /// </summary>
        private async Task<WriteAttempt> TryWriteFields(HttpClient client, string issueKey,
            IReadOnlyList<WriteBackFieldUpdate> updates, Dictionary<string, string> additionalFieldReferences)
        {
            try
            {
                var fields = new Dictionary<string, object>();
                foreach (var update in updates)
                {
                    var fieldReference = ResolveFieldReference(update, additionalFieldReferences);

                    // Two mappings can name the same Jira field by different routes - one by display name,
                    // one by reference - and dedup upstream keys on the target as configured, not as
                    // resolved. Silently keeping the last would report both as written.
                    // Stryker disable all: diagnostic log text is not behaviour — the staged value below is
                    if (fields.ContainsKey(fieldReference))
                    {
                        logger.LogWarning(
                            "Two write-back mappings resolve to the same Jira field {FieldReference} on issue {IssueKey}; keeping the last value staged.",
                            fieldReference, issueKey);
                    }
                    // Stryker restore all

                    fields[fieldReference] = CoerceFieldValue(update);
                }

                var payload = JsonSerializer.Serialize(new { fields });

                var suppressed = await Put(client, issueKey, payload, updates.Count, silently: true);

                if (suppressed.Succeeded)
                {
                    // Stryker disable once String: an ErrorMessage on a success path is never read — Written() does not carry one
                    return new WriteAttempt(true, string.Empty, NotificationSuppression.Suppressed);
                }

                if (suppressed.StatusCode != HttpStatusCode.Forbidden)
                {
                    return new WriteAttempt(false, suppressed.ErrorMessage, NotificationSuppression.Unknown);
                }

                // A 403 on the suppressed attempt is not yet a diagnosis: Jira answers the same way when
                // the credential cannot edit the issue at all. The retry's outcome is what tells the two
                // apart (ADR-142 §3), so nothing is reported as a suppression problem until it comes back.
                var audible = await Put(client, issueKey, payload, updates.Count, silently: false);

                // Stryker disable once String: an ErrorMessage on a success path is never read — Written() does not carry one
                return audible.Succeeded
                    ? new WriteAttempt(true, string.Empty, NotificationSuppression.NotSuppressed)
                    : new WriteAttempt(false, audible.ErrorMessage, NotificationSuppression.Unknown);
            }
            // Cancellation is how shutdown reaches us; reporting it as a write failure would both lose the
            // signal and blame the tracker for a decision we made.
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to write {FieldCount} field(s) to issue {IssueKey}", updates.Count, issueKey);
                return new WriteAttempt(false, ex.Message, NotificationSuppression.Unknown);
            }
        }

        private async Task<PutOutcome> Put(HttpClient client, string issueKey, string payload, int fieldCount, bool silently)
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            // Mirrors what Azure DevOps has always done (`suppressNotifications: true`): a value Lighthouse
            // recalculated is not news anyone needs mailed. Needs Administer Jira, or Administer Projects
            // on that project — hence the caller's retry.
            var url = silently
                ? $"rest/api/latest/issue/{issueKey}?notifyUsers=false"
                : $"rest/api/latest/issue/{issueKey}";

            var response = await client.PutAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                // Stryker disable once String: an ErrorMessage on a success path is never read
                return new PutOutcome(true, response.StatusCode, string.Empty);
            }

            var errorBody = await response.Content.ReadAsStringAsync();

            // Stryker disable all: diagnostic log text is not behaviour — the returned message below is
            logger.LogDebug("Jira write-back failed for {IssueKey}, {FieldCount} field(s), notifications {Notifications}: {StatusCode} - {ErrorBody}",
                issueKey, fieldCount, silently ? "suppressed" : "allowed", response.StatusCode, errorBody);
            // Stryker restore all

            return new PutOutcome(false, response.StatusCode,
                $"Jira returned {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        private sealed record WriteAttempt(bool Succeeded, string ErrorMessage, NotificationSuppression Suppression);

        private sealed record PutOutcome(bool Succeeded, HttpStatusCode StatusCode, string ErrorMessage);

        /// <summary>
        /// GetCustomFieldMappings always adds an entry, using an empty string for a field it could not
        /// resolve — so an unresolved mapping used to be written as `{"fields":{"": value}}`. Fall back to
        /// the reference we were given, which is what a field already stored by reference needs anyway.
        /// </summary>
        private static string ResolveFieldReference(WriteBackFieldUpdate update, Dictionary<string, string> additionalFieldReferences)
            => additionalFieldReferences.TryGetValue(update.TargetFieldReference, out var reference) && !string.IsNullOrEmpty(reference)
                ? reference
                : update.TargetFieldReference;

        private static object CoerceFieldValue(WriteBackFieldUpdate update)
            => double.TryParse(update.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var numericValue)
                ? numericValue
                : update.Value;

        private static async Task<BoardInformation> GetBoardInformationFromJira(HttpClient client, string boardId)
        {
            var boardInformation = new BoardInformation();

            var response = await client.GetAsync($"{BoardsEndpoint}/{boardId}/configuration");

            if (!response.IsSuccessStatusCode)
            {
                return boardInformation;
            }

            var boardConfigResponseBody = await response.Content.ReadAsStringAsync();
            var boardConfigJson = JsonDocument.Parse(boardConfigResponseBody);

            var jqlTask = ExtractJqlFromBoardConfiguration(boardConfigJson, client);
            var workItemTypesTask = GetItemTypesForBoard(client, boardId);
            var statusMappingTask = GetStateMappingForBoard(boardConfigJson, client);

            boardInformation.WorkItemTypes = await workItemTypesTask;
            boardInformation.DataRetrievalValue = await jqlTask;

            var (toDoStates, doingStates, doneStates) = await statusMappingTask;
            boardInformation.ToDoStates = toDoStates;
            boardInformation.DoingStates = doingStates;
            boardInformation.DoneStates = doneStates;

            return boardInformation;
        }

        private static async
            Task<(IEnumerable<string> toDoStates, IEnumerable<string> doingStates, IEnumerable<string> doneStates)>
            GetStateMappingForBoard(JsonDocument boardsJson, HttpClient client)
        {
            var root = boardsJson.RootElement;
            var distinctStatusIds = root
                .GetProperty("columnConfig")
                .GetProperty("columns")
                .EnumerateArray()
                .SelectMany(column =>
                    column.GetProperty("statuses").EnumerateArray()
                )
                .Select(status => status.GetProperty("id").GetString() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();

            return await MapStatusToCategory(client, distinctStatusIds);
        }

        private static async
            Task<(IEnumerable<string> toDoStates, IEnumerable<string> doingStates, IEnumerable<string> doneStates)>
            MapStatusToCategory(HttpClient client, List<string> statusList)
        {
            var response = await client.GetAsync("rest/api/latest/status");

            if (!response.IsSuccessStatusCode)
            {
                return ([], [], []);
            }
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);

            var root = data.RootElement;

            var todoStatuses = new List<string>();
            var inProgressStatuses = new List<string>();
            var doneStatuses = new List<string>();

            var statusListSet = new HashSet<string>(statusList);

            foreach (var status in root.EnumerateArray())
            {
                var id = status.GetProperty("id").GetString() ?? string.Empty;

                if (!statusListSet.Contains(id))
                {
                    continue;
                }

                var name = status.GetProperty("name").GetString() ?? string.Empty;
                var categoryName = status.GetProperty("statusCategory").GetProperty("name").GetString();

                switch (categoryName)
                {
                    case "To Do":
                        todoStatuses.Add(name);
                        break;
                    case "In Progress":
                        inProgressStatuses.Add(name);
                        break;
                    case "Done":
                        doneStatuses.Add(name);
                        break;
                }
            }

            return (todoStatuses, inProgressStatuses, doneStatuses);
        }

        private static async Task<IEnumerable<string>> GetItemTypesForBoard(HttpClient client, string boardId)
        {
            var response = await client.GetAsync($"{BoardsEndpoint}/{boardId}/issue?maxResults=1000");

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);

            var issueTypeNames = data.RootElement
                .GetProperty("issues")
                .EnumerateArray()
                .Select(issue => issue.GetProperty("fields")
                    .GetProperty("issuetype")
                    .GetProperty("name")
                    .GetString() ?? string.Empty)
                .Distinct()
                .ToList();

            return issueTypeNames;
        }

        private static async Task<string> ExtractJqlFromBoardConfiguration(JsonDocument boardsJson, HttpClient client)
        {
            var root = boardsJson.RootElement;

            var subQuery = string.Empty;

            var filter = await ExtractFilterQuery(client, root);

            subQuery = ExtractSubQuery(root, subQuery);

            return $"{filter} {subQuery}";
        }

        private static string ExtractSubQuery(JsonElement root, string subQuery)
        {
            if (!root.TryGetProperty("subQuery", out var subQueryElement) ||
                !subQueryElement.TryGetProperty("query", out var query))
            {
                return subQuery;
            }

            subQuery = query.GetString() ?? string.Empty;

            if (!string.IsNullOrEmpty(subQuery))
            {
                subQuery = $"AND ({subQuery})";
            }

            return subQuery;
        }

        private static async Task<string> ExtractFilterQuery(HttpClient client, JsonElement root)
        {
            var filter = string.Empty;

            if (!root.TryGetProperty("filter", out var filterProperty) ||
                !filterProperty.TryGetProperty("id", out var id))
            {
                return filter;
            }

            var filterId = id.ToString();
            filter = await GetFilterQueryById(client, filterId);

            return filter;
        }

        private static async Task<string> GetFilterQueryById(HttpClient client, string filterId)
        {
            var response = await client.GetAsync($"rest/api/2/filter/{filterId}");

            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var filterResponseBody = await response.Content.ReadAsStringAsync();
            var filterJson = JsonDocument.Parse(filterResponseBody);

            if (!filterJson.RootElement.TryGetProperty("jql", out var jqlQuery))
            {
                return string.Empty;
            }

            return RemoveOrderByClause(jqlQuery.GetString() ?? string.Empty);
        }

        private static string RemoveOrderByClause(string jql)
        {
            if (string.IsNullOrWhiteSpace(jql))
            {
                return string.Empty;
            }

            var orderByIndex = jql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);

            if (orderByIndex > 0)
            {
                return jql.Substring(0, orderByIndex).TrimEnd();
            }

            return jql;
        }

        private async Task<IEnumerable<Board>> GetBoardsFromJira(WorkTrackingSystemConnection workTrackingSystemConnection, HttpClient client)
        {
            const int maxResults = 50;
            var startAt = 0;
            var isLast = false;
            var boards = new List<Board>();

            while (!isLast)
            {
                var response = await client.GetAsync($"{BoardsEndpoint}?startAt={startAt}&maxResults={maxResults}");

                if (!response.IsSuccessStatusCode)
                {
                    var failBody = await response.Content.ReadAsStringAsync();
                    logger.LogInformation(
                        "Authentication is not valid for {Connection}. Jira returned {StatusCode} {Reason} at {RequestUri}. Body: {Body}",
                        workTrackingSystemConnection.Name,
                        (int)response.StatusCode,
                        response.ReasonPhrase,
                        response.RequestMessage?.RequestUri,
                        failBody);
                    return [];
                }

                var boardsResponseBody = await response.Content.ReadAsStringAsync();
                using var boardsJson = JsonDocument.Parse(boardsResponseBody);

                var parsedBoards = ParseJiraBoards(boardsJson);
                boards.AddRange(parsedBoards);

                isLast = CheckIfIsLast(boardsJson, maxResults);
                startAt += maxResults;
            }

            return boards;
        }

        private static bool CheckIfIsLast(JsonDocument boardsJson, int maxResults)
        {
            bool isLast;
            if (boardsJson.RootElement.TryGetProperty("isLast", out var isLastElement))
            {
                isLast = isLastElement.GetBoolean();
            }
            else
            {
                var currentPageSize = boardsJson.RootElement.TryGetProperty("values", out var values)
                    ? values.GetArrayLength()
                    : 0;
                isLast = currentPageSize < maxResults;
            }

            return isLast;
        }

        private static List<Board> ParseJiraBoards(JsonDocument boardsJson)
        {
            var boards = new List<Board>();

            var root = boardsJson.RootElement;

            if (!root.TryGetProperty("values", out var valuesElement))
            {
                return boards;
            }

            foreach (var boardElement in valuesElement.EnumerateArray())
            {
                var board = new Board
                {
                    Id = boardElement.GetProperty("id").GetInt32().ToString(),
                    Name = boardElement.GetProperty("name").GetString()
                };

                boards.Add(board);
            }

            return boards;
        }

        private async Task<List<string>> GetMissingAdditionalFields(WorkTrackingSystemConnection connection)
        {
            var customFieldReferences = await GetCustomFieldReferences(connection);

            return customFieldReferences
                .Where(cf => string.IsNullOrEmpty(cf.Value))
                .Select(cf => cf.Key)
                .ToList();
        }

        public async Task<ConnectionValidationResult> ValidateTeamSettings(Team team)
        {
            try
            {
                logger.LogInformation("Validating Team Settings for Team {TeamName} and Query {Query}", team.Name, team.DataRetrievalValue);

                var workItemsQuery = PrepareQuery(team.WorkItemTypes, team.AllStates, team.DataRetrievalValue, team.DoneItemsCutoffDays);
                var issues = await GetIssuesByQuery(team, workItemsQuery, 10);
                var totalItems = issues.Count();

                logger.LogInformation("Found a total of {NumberOfWorkItems} Issues with specified Query settings", totalItems);

                if (totalItems == 0)
                {
                    return ConnectionValidationResult.Failure(
                        "no_work_items_found",
                        "No work items were found for this team configuration.",
                        "Check your JQL query, selected issue types, and mapped states.",
                        "DataRetrievalValue");
                }

                return ConnectionValidationResult.Success();
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
                var issues = await GetIssuesByQuery(portfolio, query, 10);
                var totalFeatures = issues.Count();

                logger.LogInformation("Found a total of {NumberOfFeature} Features with the specified Query", totalFeatures);

                if (totalFeatures == 0)
                {
                    return ConnectionValidationResult.Failure(
                        "no_features_found",
                        "No portfolio features were found for this configuration.",
                        "Check your JQL query, selected issue types, and mapped states.",
                        "DataRetrievalValue");
                }

                return ConnectionValidationResult.Success();
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

        private async Task<List<Feature>> CreateFeaturesFromIssues(Portfolio portfolio, IEnumerable<Issue> issues)
        {
            var features = new List<Feature>();

            var customFieldReferences = await GetCustomFieldReferences(portfolio.WorkTrackingSystemConnection);
            var portfolioLinkFieldName = !portfolio.ParentOverrideAdditionalFieldDefinitionId.HasValue
                ? FieldNames[portfolio.WorkTrackingSystemConnectionId][JiraFieldNames.ParentLinkFieldName]
                : string.Empty;

            foreach (var issue in issues)
            {
                var workItem = CreateWorkItemFromJiraIssue(issue, portfolio, customFieldReferences);
                TrySetParentForJiraDataCenter(workItem, issue, portfolioLinkFieldName);

                var estimatedSize = GetEstimatedSize(portfolio, workItem);
                var owningTeam = GetOwningTeam(portfolio, workItem);

                var feature = new Feature(workItem)
                {
                    EstimatedSize = estimatedSize,
                    OwningTeam = owningTeam,
                };

                features.Add(feature);
            }

            return features;
        }

        private static string GetOwningTeam(Portfolio portfolio, WorkItemBase workItem)
        {
            var owningTeam = workItem.GetAdditionalFieldValue(portfolio.FeatureOwnerAdditionalFieldDefinitionId) ?? string.Empty;

            return owningTeam;
        }

        private static int GetEstimatedSize(Portfolio portfolio, WorkItemBase workItem)
        {
            var estimatedSizeRawValue = workItem.GetAdditionalFieldValue(portfolio.SizeEstimateAdditionalFieldDefinitionId);

            return ParseEstimatedSize(estimatedSizeRawValue);
        }

        private static int ParseEstimatedSize(string? estimateRawValue)
        {
            if (string.IsNullOrEmpty(estimateRawValue))
            {
                return 0;
            }

            // Try parsing double because for sure someone will have the brilliant idea to make this a decimal
            if (double.TryParse(estimateRawValue, out var estimateAsDouble))
            {
                return (int)estimateAsDouble;
            }

            return 0;
        }

        private async Task<List<JsonElement>> GetAllChangelogEntriesForIssue(HttpClient jiraClient, string issueId)
        {
            var allChangelogHistories = new List<JsonElement>();
            var startAt = 0;
            const int maxResults = 100; // Jira's max per page
            var isLast = false;
            var totalChangelogs = 0;

            while (!isLast)
            {
                var changelogUrl = $"rest/api/latest/issue/{issueId}/changelog?startAt={startAt}&maxResults={maxResults}";
                var changelogResponse = await jiraClient.GetAsync(changelogUrl);
                changelogResponse.EnsureSuccessStatusCode();

                var changelogBody = await changelogResponse.Content.ReadAsStringAsync();
                using var changelogJson = JsonDocument.Parse(changelogBody);

                // Try v3 format first (Cloud), then fall back to v2 format (Data Center)
                JsonElement historiesArray;
                if (changelogJson.RootElement.TryGetProperty(JiraFieldNames.ValuesFieldName, out var values))
                {
                    // v3 format (Cloud) - uses 'values' array
                    historiesArray = values;
                }
                else if (changelogJson.RootElement.TryGetProperty(JiraFieldNames.HistoriesFieldName, out var histories))
                {
                    // v2 format (Data Center) - uses 'histories' array
                    historiesArray = histories;
                }
                else
                {
                    // No changelog data found
                    break;
                }

                foreach (var changelogEntry in historiesArray.EnumerateArray())
                {
                    allChangelogHistories.Add(changelogEntry.Clone());
                }

                if (changelogJson.RootElement.TryGetProperty(JiraFieldNames.TotalFieldName, out var totalProp))
                {
                    totalChangelogs = totalProp.GetInt32();
                }

                // Check for 'isLast' (v3) or determine from pagination values (v2)
                if (changelogJson.RootElement.TryGetProperty(JiraFieldNames.IsLastFieldName, out var isLastProp))
                {
                    isLast = isLastProp.GetBoolean();
                }
                else
                {
                    // For v2 format, calculate if we're done
                    isLast = (startAt + maxResults) >= totalChangelogs;
                }

                startAt += maxResults;
            }

            logger.LogDebug("Retrieved {Count} of {Total} changelog entries for Issue {Key}", allChangelogHistories.Count, totalChangelogs, issueId);

            return allChangelogHistories;
        }

        private static JsonElement MergeChangelogIntoIssueJson(JsonElement issueJson, List<JsonElement> changelogHistories)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();

            // Copy all properties from the original issue JSON
            foreach (var property in issueJson.EnumerateObject())
            {
                if (property.Name == JiraFieldNames.ChangelogFieldName)
                {
                    // Replace the changelog with our complete paginated data
                    writer.WritePropertyName(JiraFieldNames.ChangelogFieldName);
                    writer.WriteStartObject();

                    writer.WriteNumber(JiraFieldNames.StartAtFieldName, 0);
                    writer.WriteNumber(JiraFieldNames.MaxResultsFieldName, changelogHistories.Count);
                    writer.WriteNumber(JiraFieldNames.TotalFieldName, changelogHistories.Count);

                    writer.WritePropertyName(JiraFieldNames.HistoriesFieldName);
                    writer.WriteStartArray();
                    foreach (var history in changelogHistories)
                    {
                        history.WriteTo(writer);
                    }
                    writer.WriteEndArray();

                    writer.WriteEndObject();
                }
                else
                {
                    property.WriteTo(writer);
                }
            }

            // If there was no changelog property, add it
            if (!issueJson.TryGetProperty(JiraFieldNames.ChangelogFieldName, out _))
            {
                writer.WritePropertyName(JiraFieldNames.ChangelogFieldName);
                writer.WriteStartObject();

                writer.WriteNumber(JiraFieldNames.StartAtFieldName, 0);
                writer.WriteNumber(JiraFieldNames.MaxResultsFieldName, changelogHistories.Count);
                writer.WriteNumber(JiraFieldNames.TotalFieldName, changelogHistories.Count);

                writer.WritePropertyName(JiraFieldNames.HistoriesFieldName);
                writer.WriteStartArray();
                foreach (var history in changelogHistories)
                {
                    history.WriteTo(writer);
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.Flush();

            return JsonDocument.Parse(stream.ToArray()).RootElement;
        }

        private static async Task SetStoredFieldKeys(HttpClient client, int workTrackingSystemConnectionId)
        {
            if (!FieldNames.ContainsKey(workTrackingSystemConnectionId))
            {
                var fields = new Dictionary<string, string>
                {
                    [JiraFieldNames.FlaggedName] = string.Empty,
                    [JiraFieldNames.RankName] = string.Empty,
                    [JiraFieldNames.EpicLinkFieldName] = string.Empty,
                    [JiraFieldNames.ParentLinkFieldName] = string.Empty,
                };

                FieldNames[workTrackingSystemConnectionId] = fields;
            }

            var connectionSpecificMapping = FieldNames[workTrackingSystemConnectionId];

            if (!string.IsNullOrEmpty(connectionSpecificMapping[JiraFieldNames.RankName]) &&
                !string.IsNullOrEmpty(connectionSpecificMapping[JiraFieldNames.FlaggedName]))
            {
                return;
            }

            var customFields = await GetCustomFieldMappings(client, [JiraFieldNames.NamePropertyName],
                [JiraFieldNames.RankName, JiraFieldNames.FlaggedName, JiraFieldNames.EpicLinkFieldName, JiraFieldNames.ParentLinkFieldName]);

            connectionSpecificMapping[JiraFieldNames.FlaggedName] = customFields[JiraFieldNames.FlaggedName];
            connectionSpecificMapping[JiraFieldNames.RankName] = customFields[JiraFieldNames.RankName];
            connectionSpecificMapping[JiraFieldNames.EpicLinkFieldName] = customFields[JiraFieldNames.EpicLinkFieldName];
            connectionSpecificMapping[JiraFieldNames.ParentLinkFieldName] = customFields[JiraFieldNames.ParentLinkFieldName];
        }

        private static async Task<Dictionary<string, string>> GetCustomFieldMappings(HttpClient jiraClient, string[] propertyIdentifiers,
            IEnumerable<string> customFields)
        {
            const string url = "rest/api/latest/field";

            var response = await jiraClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseBody);

            var customFieldMappings = new Dictionary<string, string>();

            foreach (var customField in customFields.Distinct())
            {
                var customFieldId = string.Empty;

                foreach (var propertyIdentifier in propertyIdentifiers)
                {
                    customFieldId = GetIdForCustomFieldByProperty(customField, propertyIdentifier, jsonResponse);

                    if (!string.IsNullOrEmpty(customFieldId))
                    {
                        break;
                    }
                }

                customFieldMappings.Add(customField, customFieldId);
            }

            return customFieldMappings;
        }

        private static string GetIdForCustomFieldByProperty(string customField, string propertyIdentifier, JsonDocument allFields)
        {
            var elements = allFields.RootElement.EnumerateArray()
                .Where(f => string.Equals(
                    f.GetProperty(propertyIdentifier).GetString(),
                    customField,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            var element = new JsonElement();

            switch (elements.Count)
            {
                case 0:
                    // No Match - Return
                    return string.Empty;
                case 1:
                    // Exactly one match - yaaay
                    element = elements[0];
                    break;
                case > 1:
                    // Multiple matches - prefer non-plugin system types
                    var nonPluginField = elements.FirstOrDefault(f =>
                    {
                        if (!f.TryGetProperty("schema", out var schema) ||
                            !schema.TryGetProperty("custom", out var custom))
                        {
                            return false;
                        }

                        var customType = custom.GetString() ?? "";
                        return !customType.Contains("plugin.system.customfieldtypes");
                    });

                    element = nonPluginField.ValueKind != JsonValueKind.Undefined
                        ? nonPluginField
                        : elements[0];
                    break;
            }

            if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return string.Empty;
            }

            if (element.TryGetProperty(JiraFieldNames.IdPropertyName, out var idProperty))
            {
                return idProperty.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static WorkItemBase CreateWorkItemFromJiraIssue(Issue issue, IWorkItemQueryOwner workItemQueryOwner, Dictionary<string, string> customFieldReferences)
        {
            var baseAddress = workItemQueryOwner.WorkTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Url);
            var url = $"{baseAddress.TrimEnd('/')}/browse/{issue.Key}";

            var additionalFieldDefs = workItemQueryOwner.WorkTrackingSystemConnection.AdditionalFieldDefinitions;

            var workItem = new WorkItemBase
            {
                ReferenceId = issue.Key,
                ParentReferenceId = issue.ParentKey,
                Name = issue.Title,
                CreatedDate = issue.CreatedDate,
                LastChangedRemote = issue.Updated,
                ClosedDate = issue.ClosedDate,
                StartedDate = issue.StartedDate,
                Order = issue.Rank,
                Type = issue.IssueType,
                State = workItemQueryOwner.MapRawStateToMappedName(issue.State),
                Url = url,
                Tags = issue.Labels,
                StateCategory = workItemQueryOwner.MapStateToStateCategory(issue.State),
                SyncedTransitions = WorkItemStateTransitionMapper.MapToMappedStates(issue.StateTransitions, workItemQueryOwner),
            };

            PopulateAdditionalFieldValues(issue, workItem, additionalFieldDefs, customFieldReferences);

            var parentReference = workItem.GetAdditionalFieldValue(workItemQueryOwner.ParentOverrideAdditionalFieldDefinitionId);

            if (!string.IsNullOrEmpty(parentReference))
            {
                workItem.ParentReferenceId = parentReference;
            }

            return workItem;
        }

        private static void TrySetParentForJiraDataCenter(WorkItemBase workItemBase, Issue issue, string linkFieldName)
        {
            if (!string.IsNullOrEmpty(workItemBase.ParentReferenceId) || string.IsNullOrEmpty(linkFieldName))
            {
                // Parent already set or no link field (= we are on Jira Cloud) - do not overwrite!
                return;
            }

            var parentReferenceId = issue.Fields.GetFieldValue(linkFieldName);
            if (!string.IsNullOrEmpty(parentReferenceId))
            {
                workItemBase.ParentReferenceId = parentReferenceId;
            }
        }

        private static void PopulateAdditionalFieldValues(Issue issue, WorkItemBase workItem, List<AdditionalFieldDefinition> additionalFieldDefs, Dictionary<string, string> customFields)
        {
            foreach (var fieldDef in additionalFieldDefs)
            {
                var customFieldId = customFields[fieldDef.Reference];

                var value = issue.Fields.GetFieldValue(customFieldId);
                workItem.AdditionalFieldValues[fieldDef.Id] = value;
            }
        }

        private async Task<Dictionary<string, string>> GetCustomFieldReferences(WorkTrackingSystemConnection connection)
        {
            var client = await GetJiraRestClientAsync(connection);
            var additionalFieldDefinitions = connection.AdditionalFieldDefinitions;

            var customFieldReferences = await GetCustomFieldMappings(client,
                [JiraFieldNames.NamePropertyName, JiraFieldNames.IdPropertyName, JiraFieldNames.KeyPropertyName],
                additionalFieldDefinitions.Select(x => x.Reference));

            return customFieldReferences;
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQuery(IWorkItemQueryOwner workItemQueryOwner, string jqlQuery, int? maxResultsOverride = null)
        {
            logger.LogDebug("Getting Issues by JQL Query: '{Query}'", jqlQuery);
            var client = await GetJiraRestClientAsync(workItemQueryOwner.WorkTrackingSystemConnection);

            var deployment = await GetDeploymentType(client, workItemQueryOwner.WorkTrackingSystemConnection);

            await SetStoredFieldKeys(client, workItemQueryOwner.WorkTrackingSystemConnectionId);

            if (deployment == JiraDeployment.Cloud)
            {
                return await GetIssuesByQueryFromCloud(client, workItemQueryOwner, jqlQuery, maxResultsOverride);
            }

            return await GetIssuesByQueryFromDataCenter(client, workItemQueryOwner, jqlQuery, maxResultsOverride);
        }

        private bool ShouldFetchFullChangelog(JsonElement jsonIssue, string issueKey, out int totalChangelogs)
        {
            totalChangelogs = 0;

            if (jsonIssue.TryGetProperty(JiraFieldNames.ChangelogFieldName, out var changelogProp) &&
                changelogProp.TryGetProperty(JiraFieldNames.TotalFieldName, out var totalProp))
            {
                totalChangelogs = totalProp.GetInt32();
                var needsFullChangelog = totalChangelogs > 30;

                if (needsFullChangelog)
                {
                    logger.LogDebug("Issue {Key} has {Total} changelog entries, fetching complete changelog", issueKey, totalChangelogs);
                }

                return needsFullChangelog;
            }

            return false;
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQueryFromDataCenter(HttpClient client, IWorkItemQueryOwner owner, string jqlQuery, int? maxResultsOverride)
        {
            var issues = new List<Issue>();
            var startAt = 0;
            var maxResults = maxResultsOverride ?? ResolveIssuesPerRequest(owner.WorkTrackingSystemConnection);
            var isLast = false;
            var encodedJqlQuery = Uri.EscapeDataString(jqlQuery);

            while (!isLast)
            {
                var url = $"rest/api/latest/search?jql={encodedJqlQuery}&startAt={startAt}&maxResults={maxResults}&expand=changelog";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                using var jsonResponse = JsonDocument.Parse(responseBody);

                var maxResultActual = jsonResponse.RootElement.GetProperty("maxResults").GetInt32();
                var totalResultsActual = jsonResponse.RootElement.GetProperty("total").GetInt32();

                maxResults = maxResultActual;
                startAt += maxResults;
                isLast = maxResultsOverride.HasValue || totalResultsActual < startAt;

                var rankFieldName = FieldNames[owner.WorkTrackingSystemConnectionId][JiraFieldNames.RankName];

                foreach (var jsonIssue in jsonResponse.RootElement.GetProperty("issues").EnumerateArray())
                {
                    var issue = issueFactory.CreateIssueFromJson(jsonIssue, owner, rankFieldName);
                    issues.Add(issue);
                }
            }

            return issues;
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQueryFromCloud(HttpClient client, IWorkItemQueryOwner owner, string jqlQuery, int? maxResultsOverride)
        {
            var issues = new List<Issue>();
            var rankFieldName = FieldNames[owner.WorkTrackingSystemConnectionId][JiraFieldNames.RankName];

            var request = new CloudSearchRequest(
                jqlQuery,
                AllFields,
                ExpandChangelog: true,
                maxResultsOverride ?? ResolveIssuesPerRequest(owner.WorkTrackingSystemConnection),
                SinglePage: maxResultsOverride.HasValue);

            var walkedEveryPage = await WalkCloudSearchPages(client, request, async jsonIssue =>
                issues.Add(await CreateIssueWithCompleteChangelog(client, jsonIssue, owner, rankFieldName)));

            if (!walkedEveryPage)
            {
                return await GetIssuesByQueryFromDataCenter(client, owner, jqlQuery, maxResultsOverride);
            }

            return issues;
        }

        private async Task<Issue> CreateIssueWithCompleteChangelog(HttpClient client, JsonElement jsonIssue, IWorkItemQueryOwner owner, string rankFieldName)
        {
            var issueKey = jsonIssue.GetProperty(JiraFieldNames.KeyPropertyName).GetString() ?? string.Empty;
            var issueToProcess = jsonIssue;

            if (ShouldFetchFullChangelog(jsonIssue, issueKey, out var totalChangelogs))
            {
                var allChangelogHistories = await GetAllChangelogEntriesForIssue(client, issueKey);
                issueToProcess = MergeChangelogIntoIssueJson(jsonIssue, allChangelogHistories);
                logger.LogDebug("Found Issue {Key} with {ChangelogCount} complete changelog entries", issueKey, allChangelogHistories.Count);
            }
            else
            {
                logger.LogDebug("Found Issue {Key} with {ChangelogCount} changelog entries from initial query", issueKey, totalChangelogs);
            }

            return issueFactory.CreateIssueFromJson(issueToProcess, owner, rankFieldName);
        }

        private sealed record CloudSearchRequest(string Jql, string Fields, bool ExpandChangelog, int PageLimit, bool SinglePage);

        /// <summary>
        /// The one token-paged walk over Jira Cloud's search endpoint. Both the whole-query download and the
        /// identity sweep go through it, which is what keeps the two enumerating the same result set (D2).
        /// Answers false when Jira rejects a page, leaving the caller to decide between falling back and failing.
        /// </summary>
        private static async Task<bool> WalkCloudSearchPages(HttpClient client, CloudSearchRequest request, Func<JsonElement, Task> onIssue)
        {
            string? nextPageToken = null;
            var pageCount = 0;

            do
            {
                var url = new StringBuilder("rest/api/3/search/jql?");
                url.Append("jql=").Append(Uri.EscapeDataString(request.Jql));
                url.Append("&fields=").Append(Uri.EscapeDataString(request.Fields));

                if (request.ExpandChangelog)
                {
                    url.Append("&expand=changelog");
                }

                url.Append("&maxResults=").Append(request.PageLimit);

                if (!string.IsNullOrEmpty(nextPageToken))
                {
                    url.Append("&nextPageToken=").Append(Uri.EscapeDataString(nextPageToken));
                }

                var response = await client.GetAsync(url.ToString());
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var body = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(body);

                foreach (var jsonIssue in json.RootElement.GetProperty("issues").EnumerateArray())
                {
                    await onIssue(jsonIssue);
                }

                nextPageToken = json.RootElement.TryGetProperty("nextPageToken", out var tokenEl) ? tokenEl.GetString() : null;
                pageCount++;
            }
            while (!request.SinglePage && !string.IsNullOrEmpty(nextPageToken) && pageCount < MaxCloudSearchPages);

            return true;
        }

        /// <summary>
        /// The offset walk over Jira Data Center's search endpoint, which has no page token: the only way to
        /// reach the next page is to ask for the offset after the one just read, and to keep asking until the
        /// offset has passed the total the instance reports back. Reads identity and the change stamp only -
        /// naming no field at all would make Data Center return every one of them, which is the whole cost the
        /// sweep exists to avoid. Answers false when Jira rejects a page, leaving the caller to decide between
        /// falling back and failing.
        /// </summary>
        private static async Task<bool> WalkDataCenterSearchOffsets(HttpClient client, string jql, int pageLimit, Func<JsonElement, Task> onIssue)
        {
            var encodedJql = Uri.EscapeDataString(jql);
            var startAt = 0;
            var pageSize = pageLimit;
            var total = int.MaxValue;

            while (startAt < total)
            {
                var url = $"rest/api/latest/search?jql={encodedJql}&fields={SweepFields}&startAt={startAt}&maxResults={pageSize}";

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var body = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(body);

                // Jira decides the page size, not the caller, and an instance that answered zero would leave the
                // offset exactly where it is - the same page asked for forever.
                pageSize = Math.Max(1, json.RootElement.GetProperty("maxResults").GetInt32());
                total = json.RootElement.GetProperty("total").GetInt32();
                startAt += pageSize;

                foreach (var jsonIssue in json.RootElement.GetProperty("issues").EnumerateArray())
                {
                    await onIssue(jsonIssue);
                }
            }

            return true;
        }

        /// <summary>
        /// Offset paging asks Jira for "the issues at positions 500 to 549 of this answer", which only holds
        /// together if the answer stays in the same order between pages. Jira's default ordering does not:
        /// someone editing an issue while the walk is running can slide a record onto a page already read, so
        /// the sweep never sees it - and because removal is "stored minus swept", that deletes live work. An
        /// issue's key never changes, so ordering on it is an order no edit can disturb. Cloud walks by page
        /// token rather than by offset and has no such exposure, and neither does the full download.
        /// </summary>
        private static string OrderedForOffsetPaging(string jql) => $"{jql.TrimEnd()} ORDER BY key ASC";

        /// <summary>The very query the full download issues, which is the only thing a sweep may enumerate.</summary>
        private static string SweepQueryFor(IWorkItemQueryOwner owner)
            => PrepareQuery(owner.WorkItemTypes, owner.AllStates, owner.DataRetrievalValue, owner.DoneItemsCutoffDays);

        /// <summary>Names issues by key and nothing else - shared by the parent lookup and by phase 2 of the two-phase fetch.</summary>
        private static string PrepareIssueKeyQuery(IEnumerable<string> referenceIds)
            => string.Join(" OR ", referenceIds.Select(id => $"key = \"{id}\""));

        /// <summary>
        /// The operator's query is wrapped in brackets and the type, state and cutoff filters are hung off it,
        /// so an ordering they wrote would end up in the middle of an expression - which Jira rejects, and the
        /// team or portfolio then fetches nothing at all. An ordering says nothing about what a query selects,
        /// so it comes off here, the same way it already comes off the JQL of a saved Jira filter.
        /// </summary>
        private static string PrepareQuery(IEnumerable<string> includedWorkItemTypes, IEnumerable<string> includedStates, string query, int cutOffDays)
        {
            var workItemsQuery = PrepareGenericQuery(includedWorkItemTypes, JiraFieldNames.IssueTypeFieldName, "OR", "=");
            var stateQuery = PrepareGenericQuery(includedStates, JiraFieldNames.StatusFieldName, "OR", "=");
            var cutoffDateFilter = PrepareCutoffDateFilter(cutOffDays);

            return $"({RemoveOrderByClause(query)}) {workItemsQuery} {stateQuery} {cutoffDateFilter}";
        }

        private static string PrepareCutoffDateFilter(int cutOffDays)
        {
            if (cutOffDays <= 0)
            {
                return string.Empty;
            }

            // Bug #5567 decision 4: stays UTC. A tracker's history window is an instant offset, not
            // a calendar day, and the remote system has its own zone; an off-by-one only over-fetches.
            var cutoffDate = DateTime.UtcNow.AddDays(-cutOffDays);
            var cutoffDateString = cutoffDate.ToString("yyyy-MM-dd");

            return $"AND (resolved IS EMPTY OR resolved >= '{cutoffDateString}') ";
        }

        private static string PrepareGenericQuery(IEnumerable<string> options, string fieldName, string queryOperator, string queryComparison)
        {
            var query = string.Join($" {queryOperator} ", options.Select(o => $"{fieldName} {queryComparison} \"{o}\""));
            query = options.Any() ? $"AND ({query}) " : string.Empty;
            return query;
        }

        public static bool RoutesViaAtlassianCloudGateway(string authenticationMethodKey) =>
            authenticationMethodKey == AuthenticationMethodKeys.JiraScopedToken
            || authenticationMethodKey == AuthenticationMethodKeys.JiraOAuth;

        private const int DefaultIssuesPerRequest = 1000;

        private static int ResolveIssuesPerRequest(WorkTrackingSystemConnection connection)
        {
            var configured = connection.GetWorkTrackingSystemConnectionOptionByKey<int>(JiraWorkTrackingOptionNames.IssuesPerRequest);
            return configured is > 0 ? configured.Value : DefaultIssuesPerRequest;
        }

        private async Task<HttpClient> GetJiraRestClientAsync(WorkTrackingSystemConnection connection)
        {
            var requestTimeoutInSeconds =
                connection.GetWorkTrackingSystemConnectionOptionByKey<int>(JiraWorkTrackingOptionNames.RequestTimeoutInSeconds) ?? 100;

            var (baseUrl, cacheKey) = await ResolveBaseUrlAndCacheKeyAsync(connection);
            var client = GetOrCreateClient(cacheKey, baseUrl, requestTimeoutInSeconds);

            await SetAuthorizationHeader(client, connection);

            return client;
        }


        private async Task<(string BaseUrl, string CacheKey)> ResolveBaseUrlAndCacheKeyAsync(
            WorkTrackingSystemConnection connection)
        {
            if (RoutesViaAtlassianCloudGateway(connection.AuthenticationMethodKey))
            {
                var jiraUrl = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Url).TrimEnd('/');
                var cloudId = await ResolveCloudIdAsync(jiraUrl);
                var baseUrl = $"https://api.atlassian.com/ex/jira/{cloudId}/";
                return (baseUrl, $"{baseUrl}|conn:{connection.Id}");
            }

            var encryptedApiToken = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.ApiToken);
            var url = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Url).TrimEnd('/');
            return (url, $"{url}|{encryptedApiToken}");
        }

        private async Task<string> ResolveCloudIdAsync(string jiraUrl)
        {
            if (CloudIdCache.TryGetValue(jiraUrl, out var cachedCloudId))
            {
                logger.LogDebug("Resolved Cloud ID from cache for {Url}: {CloudId}", jiraUrl, cachedCloudId);
                return cachedCloudId;
            }

            logger.LogDebug("Fetching Cloud ID from {Url}/_edge/tenant_info", jiraUrl);

            using var tempClient = new HttpClient();
            var response = await tempClient.GetAsync($"{jiraUrl}/_edge/tenant_info");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);

            var cloudId = json.RootElement.GetProperty("cloudId").GetString()
                ?? throw new InvalidOperationException(
                    $"Could not resolve Jira Cloud ID from {jiraUrl}/_edge/tenant_info. " +
                    "Ensure the URL points to a valid Jira Cloud instance.");

            CloudIdCache[jiraUrl] = cloudId;
            logger.LogDebug("Resolved Cloud ID for {Url}: {CloudId}", jiraUrl, cloudId);

            return cloudId;
        }

        private HttpClient GetOrCreateClient(string cacheKey, string baseUrl, int timeoutInSeconds)
        {
            if (_httpMessageHandlerForTesting is not null)
            {
                return new HttpClient(_httpMessageHandlerForTesting, disposeHandler: false)
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(timeoutInSeconds)
                };
            }

            return ClientCache.GetOrAdd(cacheKey, _ => new HttpClient(SharedHandler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(timeoutInSeconds)
            });
        }

        private async Task SetAuthorizationHeader(HttpClient client, WorkTrackingSystemConnection connection)
        {
            using var probeRequest = new HttpRequestMessage();
            var strategy = authStrategyFactory.Resolve(connection.AuthenticationMethodKey);
            await strategy.ApplyAsync(probeRequest, connection, CancellationToken.None);

            client.DefaultRequestHeaders.Authorization = probeRequest.Headers.Authorization;
        }

        /// <summary>
        /// The deployment as far as it is known without calling Jira: the Atlassian gateway can only be Cloud,
        /// and anything else has to have been discovered by an earlier <see cref="GetDeploymentType"/>. This is
        /// what lets the synchronous capability probe answer at all.
        /// </summary>
        private static bool TryResolveKnownDeployment(WorkTrackingSystemConnection connection, out JiraDeployment deployment)
        {
            if (RoutesViaAtlassianCloudGateway(connection.AuthenticationMethodKey))
            {
                deployment = JiraDeployment.Cloud;
                return true;
            }

            var baseUrl = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Url).TrimEnd('/');

            return DeploymentCache.TryGetValue(baseUrl, out deployment);
        }

        private async Task<JiraDeployment> GetDeploymentType(HttpClient client, WorkTrackingSystemConnection connection)
        {
            if (TryResolveKnownDeployment(connection, out var known))
            {
                logger.LogDebug("Deployment Type already known - {DeploymentType}", known);
                return known;
            }

            var baseUrl = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Url).TrimEnd('/');

            logger.LogDebug("Getting Deployment Type of Jira Instance for {Url}", baseUrl);

            try
            {
                var response = await client.GetAsync("rest/api/2/serverInfo");
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogDebug("Could not determine deployment type");
                    DeploymentCache[baseUrl] = JiraDeployment.Unknown;
                    return JiraDeployment.Unknown;
                }

                var body = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(body);

                if (json.RootElement.TryGetProperty("deploymentType", out var dep) && string.Equals(dep.GetString(), "Cloud", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogDebug("Found Deployment Type: {Type}", JiraDeployment.Cloud);
                    DeploymentCache[baseUrl] = JiraDeployment.Cloud;
                    return JiraDeployment.Cloud;
                }

                logger.LogDebug("Found Deployment Type: {Type}", JiraDeployment.DataCenter);
                DeploymentCache[baseUrl] = JiraDeployment.DataCenter;
                return JiraDeployment.DataCenter;
            }
            catch
            {
                DeploymentCache[baseUrl] = JiraDeployment.Unknown;
                return JiraDeployment.Unknown;
            }
        }
    }
}