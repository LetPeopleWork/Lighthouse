using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using System.Collections.Concurrent;
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
        : IWorkTrackingConnector
    {
        private enum JiraDeployment { Unknown, Cloud, DataCenter }

        private static string rankFieldName = string.Empty;

        private static string flaggedFieldName = string.Empty;

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

            foreach (var issue in issues)
            {
                var workItemBase = CreateWorkItemFromJiraIssue(issue, team, customFieldReferences);
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

        private async Task<bool> ValidateFields(WorkTrackingSystemConnection connection)
        {
            var customFieldReferences = await GetCustomFieldReferences(connection);

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
            
            foreach (var issue in issues)
            {
                var workItem = CreateWorkItemFromJiraIssue(issue, portfolio, customFieldReferences);

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

        private static async Task SetFlaggedAndRankField(HttpClient client)
        {
            if (!string.IsNullOrEmpty(flaggedFieldName) && !string.IsNullOrEmpty(rankFieldName))
            {
                return;
            }
            
            var customFields = await GetCustomFieldMappings(client, [JiraFieldNames.NamePropertyName],
                [JiraFieldNames.RankName, JiraFieldNames.FlaggedName]);
            
            rankFieldName = customFields[JiraFieldNames.RankName];
            flaggedFieldName = customFields[JiraFieldNames.FlaggedName];
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
            var element = allFields.RootElement.EnumerateArray().SingleOrDefault(
                f => f.GetProperty(propertyIdentifier).GetString() == customField
            );
            
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

            var additionalFieldDefs = workItemQueryOwner.WorkTrackingSystemConnection?.AdditionalFieldDefinitions ?? [];

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
            
            var customFieldReferences =  await GetCustomFieldMappings(client,
                [JiraFieldNames.NamePropertyName, JiraFieldNames.KeyPropertyName],
                additionalFieldDefinitions.Select(x => x.Reference));
            
            return  customFieldReferences;
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQuery(IWorkItemQueryOwner workItemQueryOwner, string jqlQuery, int? maxResultsOverride = null)
        {
            logger.LogDebug("Getting Issues by JQL Query: '{Query}'", jqlQuery);
            var client = GetJiraRestClient(workItemQueryOwner.WorkTrackingSystemConnection);

            var deployment = await GetDeploymentType(client, workItemQueryOwner.WorkTrackingSystemConnection);

            await SetFlaggedAndRankField(client);

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
            
            logger.LogDebug("Getting Deployment Type of Jira Instace for {Url}", baseUrl);
            
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