using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Extensions;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    public class JiraWorkTrackingConnector(
        IIssueFactory issueFactory,
        ILogger<JiraWorkTrackingConnector> logger,
        ICryptoService cryptoService)
        : IJiraWorkTrackingConnector
    {
        private enum JiraDeployment { Unknown, Cloud, DataCenter }

        private static readonly Dictionary<int, Dictionary<string, string>> FieldNames = new();

        private const string BoardsEndpoint = "rest/agile/latest/board";

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

        public async Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            var workItems = new List<WorkItem>();

            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);

            var query = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, team.DataRetrievalValue, team.DoneItemsCutoffDays)}";
            var issues = await GetIssuesByQuery(team, query);

            var customFieldReferences = await GetCustomFieldReferences(team.WorkTrackingSystemConnection);

            var epicLinkFieldName = !team.ParentOverrideAdditionalFieldDefinitionId.HasValue ? FieldNames[team.WorkTrackingSystemConnectionId][JiraFieldNames.EpicLinkFieldName] : string.Empty;

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

        public async Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds)
        {
            logger.LogInformation("Getting Parent Features Details for Project {ProjectName} with Feature IDs {FeatureIds}", project.Name, string.Join(", ", parentFeatureIds));

            var query = string.Join(" OR ", parentFeatureIds.Select(id => $"key = \"{id}\""));
            var issues = await GetIssuesByQuery(project, query);
            return await CreateFeaturesFromIssues(project, issues);
        }

        public async Task<bool> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            try
            {
                var client = GetJiraRestClient(connection);
                var response = await client.GetAsync("rest/api/2/myself");
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Authentication is not valid for {Connection}", connection.Name);
                    return false;
                }

                var fieldsValid = await ValidateFields(connection);
                return fieldsValid;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<Board>> GetBoards(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            try
            {
                logger.LogInformation("Getting Boards for System {ConnectionName}", workTrackingSystemConnection.Name);
                var client = GetJiraRestClient(workTrackingSystemConnection);

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
                var client = GetJiraRestClient(workTrackingSystemConnection);

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

            var client = GetJiraRestClient(connection);
            var results = await UpdateItems(updates, client);

            return new WriteBackResult { ItemResults = results };
        }

        private async Task<List<WriteBackItemResult>> UpdateItems(IReadOnlyList<WriteBackFieldUpdate> updates, HttpClient client)
        {
            var results = new List<WriteBackItemResult>();
            foreach (var update in updates)
            {
                try
                {
                    object fieldValue = double.TryParse(update.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var numericValue)
                        ? numericValue
                        : update.Value;

                    var payload = JsonSerializer.Serialize(new
                    {
                        fields = new Dictionary<string, object> { [update.TargetFieldReference] = fieldValue }
                    });

                    var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    var response = await client.PutAsync($"rest/api/2/issue/{update.WorkItemId}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        results.Add(new WriteBackItemResult
                        {
                            WorkItemId = update.WorkItemId,
                            TargetFieldReference = update.TargetFieldReference,
                            Success = true
                        });
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        logger.LogDebug("Jira write-back failed for {IssueKey}, field {FieldReference}: {StatusCode} - {ErrorBody}",
                            update.WorkItemId, update.TargetFieldReference, response.StatusCode, errorBody);

                        results.Add(new WriteBackItemResult
                        {
                            WorkItemId = update.WorkItemId,
                            TargetFieldReference = update.TargetFieldReference,
                            Success = false,
                            ErrorMessage = $"Jira returned {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to write field {FieldReference} to issue {IssueKey}", update.TargetFieldReference, update.WorkItemId);

                    results.Add(new WriteBackItemResult
                    {
                        WorkItemId = update.WorkItemId,
                        TargetFieldReference = update.TargetFieldReference,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return results;
        }


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
                    logger.LogInformation("Authentication is not valid for {Connection}", workTrackingSystemConnection.Name);
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

        private async Task<bool> ValidateFields(WorkTrackingSystemConnection connection)
        {
            var customFieldReferences = await GetCustomFieldReferences(connection);

            var missingReference = 0;
            foreach (var customFieldReference in customFieldReferences.Where(cf => string.IsNullOrEmpty(cf.Value)))
            {
                logger.LogInformation("Additional Field {FieldName} does not exit", customFieldReference.Key);
                missingReference++;
            }

            return missingReference <= 0;
        }

        public async Task<bool> ValidateTeamSettings(Team team)
        {
            try
            {
                logger.LogInformation("Validating Team Settings for Team {TeamName} and Query {Query}", team.Name, team.DataRetrievalValue);

                var workItemsQuery = PrepareQuery(team.WorkItemTypes, team.AllStates, team.DataRetrievalValue, team.DoneItemsCutoffDays);
                var issues = await GetIssuesByQuery(team, workItemsQuery, 10);
                var totalItems = issues.Count();

                logger.LogInformation("Found a total of {NumberOfWorkItems} Issues with specified Query settings", totalItems);

                return totalItems > 0;
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
                var issues = await GetIssuesByQuery(portfolio, query, 10);
                var totalFeatures = issues.Count();

                logger.LogInformation("Found a total of {NumberOfFeature} Features with the specified Query", totalFeatures);

                return totalFeatures > 0;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Project Settings for Project {ProjectName}", portfolio.Name);
                return false;
            }
        }

        private async Task<List<Feature>> CreateFeaturesFromIssues(Portfolio portfolio, IEnumerable<Issue> issues)
        {
            var features = new List<Feature>();

            var customFieldReferences = await GetCustomFieldReferences(portfolio.WorkTrackingSystemConnection);

            var portfolioLinkFieldName = !portfolio.ParentOverrideAdditionalFieldDefinitionId.HasValue ? FieldNames[portfolio.WorkTrackingSystemConnectionId][JiraFieldNames.ParentLinkFieldName] : string.Empty;

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

            foreach (var customField in customFields)
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
                ClosedDate = issue.ClosedDate,
                StartedDate = issue.StartedDate,
                Order = issue.Rank,
                Type = issue.IssueType,
                State = issue.State,
                Url = url,
                Tags = issue.Labels,
                StateCategory = workItemQueryOwner.MapStateToStateCategory(issue.State),
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
            var client = GetJiraRestClient(connection);
            var additionalFieldDefinitions = connection.AdditionalFieldDefinitions;

            var customFieldReferences = await GetCustomFieldMappings(client,
                [JiraFieldNames.NamePropertyName, JiraFieldNames.IdPropertyName, JiraFieldNames.KeyPropertyName],
                additionalFieldDefinitions.Select(x => x.Reference));

            return customFieldReferences;
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQuery(IWorkItemQueryOwner workItemQueryOwner, string jqlQuery, int? maxResultsOverride = null)
        {
            logger.LogDebug("Getting Issues by JQL Query: '{Query}'", jqlQuery);
            var client = GetJiraRestClient(workItemQueryOwner.WorkTrackingSystemConnection);

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
            var maxResults = maxResultsOverride ?? 1000;
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
                var flaggedFieldName = FieldNames[owner.WorkTrackingSystemConnectionId][JiraFieldNames.FlaggedName];

                foreach (var jsonIssue in jsonResponse.RootElement.GetProperty("issues").EnumerateArray())
                {
                    var issue = issueFactory.CreateIssueFromJson(jsonIssue, owner, rankFieldName, flaggedFieldName);
                    issues.Add(issue);
                }
            }

            return issues;
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQueryFromCloud(HttpClient client, IWorkItemQueryOwner owner, string jqlQuery, int? maxResultsOverride)
        {
            var issues = new List<Issue>();
            string? nextPageToken = null;
            var pageLimit = maxResultsOverride ?? 1000;
            var pageCount = 0;

            do
            {
                var query = new StringBuilder("rest/api/3/search/jql?");
                query.Append("jql=").Append(Uri.EscapeDataString(jqlQuery));
                query.Append("&fields=").Append(Uri.EscapeDataString("*all"));
                query.Append("&expand=changelog");
                query.Append("&maxResults=").Append(pageLimit);

                if (!string.IsNullOrEmpty(nextPageToken))
                {
                    query.Append("&nextPageToken=").Append(Uri.EscapeDataString(nextPageToken));
                }

                var response = await client.GetAsync(query.ToString());
                if (!response.IsSuccessStatusCode)
                {
                    return await GetIssuesByQueryFromDataCenter(client, owner, jqlQuery, maxResultsOverride);
                }

                var body = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(body);

                var issuesArray = json.RootElement.GetProperty("issues");

                var rankFieldName = FieldNames[owner.WorkTrackingSystemConnectionId][JiraFieldNames.RankName];
                var flaggedFieldName = FieldNames[owner.WorkTrackingSystemConnectionId][JiraFieldNames.FlaggedName];

                foreach (var jsonIssue in issuesArray.EnumerateArray())
                {
                    var issueKey = jsonIssue.GetProperty(JiraFieldNames.KeyPropertyName).GetString() ?? string.Empty;
                    var needsFullChangelog = ShouldFetchFullChangelog(jsonIssue, issueKey, out var totalChangelogs);

                    JsonElement issueToProcess;
                    if (needsFullChangelog)
                    {
                        // Fetch complete changelog for this issue
                        var allChangelogHistories = await GetAllChangelogEntriesForIssue(client, issueKey);

                        // Merge complete changelog into the issue JSON
                        issueToProcess = MergeChangelogIntoIssueJson(jsonIssue, allChangelogHistories);
                        logger.LogDebug("Found Issue {Key} with {ChangelogCount} complete changelog entries", issueKey, allChangelogHistories.Count);
                    }
                    else
                    {
                        // Use changelog as-is from initial query
                        issueToProcess = jsonIssue;
                        logger.LogDebug("Found Issue {Key} with {ChangelogCount} changelog entries from initial query", issueKey, totalChangelogs);
                    }

                    var issue = issueFactory.CreateIssueFromJson(issueToProcess, owner, rankFieldName, flaggedFieldName);
                    issues.Add(issue);
                }

                nextPageToken = json.RootElement.TryGetProperty("nextPageToken", out var tokenEl) ? tokenEl.GetString() : null;
                pageCount++;
            }
            while (!maxResultsOverride.HasValue && !string.IsNullOrEmpty(nextPageToken) && pageCount < 100);

            return issues;
        }

        private static string PrepareQuery(IEnumerable<string> includedWorkItemTypes, IEnumerable<string> includedStates, string query, int cutOffDays)
        {
            var workItemsQuery = PrepareGenericQuery(includedWorkItemTypes, JiraFieldNames.IssueTypeFieldName, "OR", "=");
            var stateQuery = PrepareGenericQuery(includedStates, JiraFieldNames.StatusFieldName, "OR", "=");
            var cutoffDateFilter = PrepareCutoffDateFilter(cutOffDays);
            var jql = $"({query}) {workItemsQuery} {stateQuery} {cutoffDateFilter}";
            return jql;
        }

        private static string PrepareCutoffDateFilter(int cutOffDays)
        {
            if (cutOffDays <= 0)
            {
                return string.Empty;
            }

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

        private HttpClient GetJiraRestClient(WorkTrackingSystemConnection connection)
        {
            var url = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Url).TrimEnd('/');
            var encryptedApiToken = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.ApiToken);
            var apiToken = cryptoService.Decrypt(encryptedApiToken);
            var key = $"{url}|{encryptedApiToken}";

            var requestTimeoutInSeconds =
                connection.GetWorkTrackingSystemConnectionOptionByKey<int>(JiraWorkTrackingOptionNames.RequestTimeoutInSeconds) ?? 100;

            var client = ClientCache.GetOrAdd(key, _ =>
            {
                var c = new HttpClient(SharedHandler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = TimeSpan.FromSeconds(requestTimeoutInSeconds)
                };
                return c;
            });

            if (connection.AuthenticationMethodKey == AuthenticationMethodKeys.JiraCloud)
            {
                var username = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Username);
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{apiToken}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
            else
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
            }

            return client;
        }

        private async Task<JiraDeployment> GetDeploymentType(HttpClient client, WorkTrackingSystemConnection connection)
        {
            var baseUrl = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Url).TrimEnd('/');

            logger.LogDebug("Getting Deployment Type of Jira Instance for {Url}", baseUrl);

            if (DeploymentCache.TryGetValue(baseUrl, out var cached))
            {
                logger.LogDebug("Found Deployment Type in cache - {DeploymentType}", cached);
                return cached;
            }

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