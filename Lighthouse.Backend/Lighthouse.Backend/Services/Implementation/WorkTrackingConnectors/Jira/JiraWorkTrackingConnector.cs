using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    public class JiraWorkTrackingConnector : IWorkTrackingConnector
    {
        private enum JiraDeployment { Unknown, Cloud, DataCenter }
        
        private readonly IIssueFactory issueFactory;

        private readonly ILogger<JiraWorkTrackingConnector> logger;
        
        private readonly ICryptoService cryptoService;

        private static string rankFieldName = string.Empty;

        private static string flaggedFieldName = string.Empty;

        private static readonly SocketsHttpHandler SharedHandler = new SocketsHttpHandler
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

        public JiraWorkTrackingConnector(IIssueFactory issueFactory, ILogger<JiraWorkTrackingConnector> logger, ICryptoService cryptoService)
        {
            this.issueFactory = issueFactory;
            this.logger = logger;
            this.cryptoService = cryptoService;
        }

        public async Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            var workItems = new List<WorkItem>();

            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);

            var query = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, team.DataRetrievalValue, team.DoneItemsCutoffDays)}";
            var issues = await GetIssuesByQuery(team, query, team.ParentOverrideField);

            foreach (var issue in issues)
            {
                var workItemBase = CreateWorkItemFromJiraIssue(issue, team);
                workItems.Add(new WorkItem(workItemBase, team));
            }

            return workItems;
        }

        public async Task<List<Feature>> GetFeaturesForProject(Portfolio project)
        {
            logger.LogInformation("Getting Features of Type {WorkItemTypes} and Query '{Query}'", string.Join(", ", project.WorkItemTypes), project.DataRetrievalValue);

            var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.DataRetrievalValue, project.DoneItemsCutoffDays);
            var issues = await GetIssuesByQuery(project, query, project.ParentOverrideField);
            return await CreateFeaturesFromIssues(project, issues);
        }

        public async Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds)
        {
            logger.LogInformation("Getting Parent Features Details for Project {ProjectName} with Feature IDs {FeatureIds}", project.Name, string.Join(", ", parentFeatureIds));

            var query = string.Join(" OR ", parentFeatureIds.Select(id => $"key = \"{id}\""));
            var issues = await GetIssuesByQuery(project, query, project.ParentOverrideField);
            return await CreateFeaturesFromIssues(project, issues);
        }

        public async Task<bool> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            try
            {
                var client = GetJiraRestClient(connection);
                var response = await client.GetAsync("rest/api/2/myself");
                return response.IsSuccessStatusCode;
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

                var workItemsQuery = PrepareQuery(team.WorkItemTypes, team.AllStates, team.DataRetrievalValue, team.DoneItemsCutoffDays);
                var issues = await GetIssuesByQuery(team, workItemsQuery, team.ParentOverrideField, 10);
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
                var issues = await GetIssuesByQuery(portfolio, query, null, 10);
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

        private async Task<List<Feature>> CreateFeaturesFromIssues(Portfolio project, IEnumerable<Issue> issues)
        {
            var features = new List<Feature>();
            foreach (var issue in issues)
            {
                var feature = new Feature(CreateWorkItemFromJiraIssue(issue, project));

                var size = await GetEstimatedSizeForItem(issue.Key, project);
                var owner = await GetFeatureOwnerByField(issue.Key, project);

                feature.EstimatedSize = size;
                feature.OwningTeam = owner;

                features.Add(feature);
            }

            return features;
        }

        private async Task<int> GetEstimatedSizeForItem(string referenceId, Portfolio project)
        {
            if (string.IsNullOrEmpty(project.SizeEstimateField))
            {
                return 0;
            }

            var estimateRawValue = await GetFieldValue(referenceId, project, project.SizeEstimateField);

            // Try parsing double because for sure someone will have the brilliant idea to make this a decimal
            if (double.TryParse(estimateRawValue, out var estimateAsDouble))
            {
                return (int)estimateAsDouble;
            }

            return 0;
        }

        private async Task<string> GetFeatureOwnerByField(string referenceId, Portfolio project)
        {
            if (string.IsNullOrEmpty(project.FeatureOwnerField))
            {
                return string.Empty;
            }

            return await GetFieldValue(referenceId, project, project.FeatureOwnerField);
        }

        private async Task<string> GetFieldValue(string referenceId, Portfolio project, string fieldName)
        {
            try
            {
                var jiraClient = GetJiraRestClient(project.WorkTrackingSystemConnection);
                var issue = await GetIssueById(jiraClient, project, referenceId);
                return issue.Fields.GetFieldValue(fieldName) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<Issue> GetIssueById(HttpClient jiraClient, IWorkItemQueryOwner workitemQueryOwner, string issueId, string? additionalRelatedField = null)
        {
            logger.LogDebug("Getting Issue by Key '{Key}'", issueId);

            // First, get the issue without changelog expansion (or with limited changelog)
            var issueUrl = $"rest/api/latest/issue/{issueId}";
            var issueResponse = await jiraClient.GetAsync(issueUrl);
            issueResponse.EnsureSuccessStatusCode();

            var issueBody = await issueResponse.Content.ReadAsStringAsync();
            using var issueJsonDoc = JsonDocument.Parse(issueBody);

            // Fetch all changelog entries via pagination
            var allChangelogHistories = await GetAllChangelogEntriesForIssue(jiraClient, issueId);

            // Merge the complete changelog back into the issue JSON structure
            var issueWithCompleteChangelog = MergeChangelogIntoIssueJson(issueJsonDoc.RootElement, allChangelogHistories);

            var rankField = await GetRankField(jiraClient);

            var flaggedField = await GetFlaggedFieldName(jiraClient, workitemQueryOwner);
            var issue = issueFactory.CreateIssueFromJson(issueWithCompleteChangelog, workitemQueryOwner, additionalRelatedField, rankField, flaggedField);

            logger.LogDebug("Found Issue by Key: {Key} with {ChangelogCount} changelog entries", issue.Key, allChangelogHistories.Count);

            return issue;
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

        private static async Task<string> GetFlaggedFieldName(HttpClient jiraClient, IWorkItemQueryOwner workItemQueryOwner)
        {
            var blockedTags = workItemQueryOwner.BlockedTags.Select(bt => bt.ToLower());

            if (!blockedTags.Contains(JiraFieldNames.FlaggedName.ToLower()))
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(flaggedFieldName))
            {
                return flaggedFieldName;
            }

            flaggedFieldName = await GetCustomFieldByName(jiraClient, JiraFieldNames.FlaggedName);
            return flaggedFieldName;
        }

        private static async Task<string> GetRankField(HttpClient jiraClient)
        {
            if (!string.IsNullOrEmpty(rankFieldName))
            {
                return rankFieldName;
            }

            rankFieldName = await GetCustomFieldByName(jiraClient, JiraFieldNames.RankName);
            return rankFieldName;
        }

        private static async Task<string> GetCustomFieldByName(HttpClient jiraClient, string customFieldName)
        {
            var url = "rest/api/latest/field";

            var response = await jiraClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseBody);

            var element = jsonResponse.RootElement.EnumerateArray().SingleOrDefault(
                f => f.GetProperty(JiraFieldNames.NamePropertyName).GetString() == customFieldName
            );

            return element.GetProperty(JiraFieldNames.IdPropertyName).GetString() ?? string.Empty;
        }

        private WorkItemBase CreateWorkItemFromJiraIssue(Issue issue, IWorkItemQueryOwner workItemQueryOwner)
        {
            var baseAddress = workItemQueryOwner.WorkTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Url);
            var url = $"{baseAddress.TrimEnd('/')}/browse/{issue.Key}";

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
            return workItem;
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQuery(IWorkItemQueryOwner workItemQueryOwner, string jqlQuery, string? additionalRelatedField = null, int? maxResultsOverride = null)
        {
            logger.LogDebug("Getting Issues by JQL Query: '{Query}'", jqlQuery);
            var client = GetJiraRestClient(workItemQueryOwner.WorkTrackingSystemConnection);

            var deployment = await GetDeploymentType(client, workItemQueryOwner.WorkTrackingSystemConnection);
            
            var rankField = await GetRankField(client);
            var flaggedField = await GetFlaggedFieldName(client, workItemQueryOwner);

            if (deployment == JiraDeployment.Cloud)
            {
                return await GetIssuesByQueryFromCloud(client, workItemQueryOwner, jqlQuery, additionalRelatedField, maxResultsOverride, rankField, flaggedField);
            }

            return await GetIssuesByQueryFromDataCenter(client, workItemQueryOwner, jqlQuery, additionalRelatedField, maxResultsOverride, rankField, flaggedField);
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

        private async Task<IEnumerable<Issue>> GetIssuesByQueryFromDataCenter(HttpClient client, IWorkItemQueryOwner owner, string jqlQuery, string? additionalRelatedField, int? maxResultsOverride, string rankField, string flaggedField)
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
                    var issue = issueFactory.CreateIssueFromJson(jsonIssue, owner, additionalRelatedField, rankField, flaggedField);
                    issues.Add(issue);
                }
            }

            return issues;
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQueryFromCloud(HttpClient client, IWorkItemQueryOwner owner, string jqlQuery, string? additionalRelatedField, int? maxResultsOverride, string rankField, string flaggedField)
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
                    return await GetIssuesByQueryFromDataCenter(client, owner, jqlQuery, additionalRelatedField, maxResultsOverride, rankField, flaggedField);
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
                    
                    var issue = issueFactory.CreateIssueFromJson(issueToProcess, owner, additionalRelatedField, rankField, flaggedField);
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
            var q = string.Join($" {queryOperator} ", options.Select(o => $"{fieldName} {queryComparison} \"{o}\""));
            if (options.Any()) q = $"AND ({q}) "; else q = string.Empty;
            return q;
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