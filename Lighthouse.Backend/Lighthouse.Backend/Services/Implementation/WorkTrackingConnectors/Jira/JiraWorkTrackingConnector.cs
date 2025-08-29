using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.Jira;
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

        private readonly ILexoRankService lexoRankService;
        private readonly IIssueFactory issueFactory;
        private readonly ILogger<JiraWorkTrackingConnector> logger;
        private readonly ICryptoService cryptoService;

        private readonly int requestTimeoutInSeconds = 100;

        private static string rankFieldName = string.Empty;

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

        public JiraWorkTrackingConnector(
            ILexoRankService lexoRankService, IIssueFactory issueFactory, ILogger<JiraWorkTrackingConnector> logger, ICryptoService cryptoService, IAppSettingService appSettingService)
        {
            this.lexoRankService = lexoRankService;
            this.issueFactory = issueFactory;
            this.logger = logger;
            this.cryptoService = cryptoService;

            var workTrackingSystemSettings = appSettingService.GetWorkTrackingSystemSettings();
            if (workTrackingSystemSettings.OverrideRequestTimeout)
            {
                requestTimeoutInSeconds = workTrackingSystemSettings.RequestTimeoutInSeconds;
            }
        }

        public async Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            var workItems = new List<WorkItem>();

            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);

            var query = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, team.WorkItemQuery)}";
            var issues = await GetIssuesByQuery(team, query, team.ParentOverrideField);

            foreach (var issue in issues)
            {
                var workItemBase = CreateWorkItemFromJiraIssue(issue, team);
                workItems.Add(new WorkItem(workItemBase, team));
            }

            return workItems;
        }

        public async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            logger.LogInformation("Getting Features of Type {WorkItemTypes} and Query '{Query}'", string.Join(", ", project.WorkItemTypes), project.WorkItemQuery);

            var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.WorkItemQuery);
            var issues = await GetIssuesByQuery(project, query, project.ParentOverrideField);
            return await CreateFeaturesFromIssues(project, issues);
        }

        public async Task<List<Feature>> GetParentFeaturesDetails(Project project, IEnumerable<string> parentFeatureIds)
        {
            logger.LogInformation("Getting Parent Features Details for Project {ProjectName} with Feature IDs {FeatureIds}", project.Name, string.Join(", ", parentFeatureIds));

            var query = string.Join(" OR ", parentFeatureIds.Select(id => $"key = \"{id}\""));
            var issues = await GetIssuesByQuery(project, query, project.ParentOverrideField);
            return await CreateFeaturesFromIssues(project, issues);
        }

        public async Task<List<string>> GetWorkItemsIdsForTeamWithAdditionalQuery(Team team, string additionalQuery)
        {
            logger.LogInformation("Getting Work Items for Team {TeamName}, Item Types {WorkItemTypes} and Unaprented Items Query '{Query}'", team.Name, string.Join(", ", team.WorkItemTypes), additionalQuery);

            var query = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, additionalQuery)} AND ({team.WorkItemQuery})";
            var issues = await GetIssuesByQuery(team, query, team.ParentOverrideField);

            var issueKeys = issues.Select(x => x.Key).ToList();

            logger.LogDebug("Found following Issues: {IssueKeys}", string.Join(", ", issueKeys));

            return issueKeys;
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            logger.LogInformation("Getting Adjacent Order Index for Issues {Items} in order {RelativeOrder}", string.Join(", ", existingItemsOrder), relativeOrder);

            string? result;
            if (!existingItemsOrder.Any())
            {
                result = lexoRankService.Default;
            }
            else
            {
                if (relativeOrder == RelativeOrder.Above)
                {
                    var highestRank = existingItemsOrder.Max() ?? lexoRankService.Default;
                    result = lexoRankService.GetHigherPriority(highestRank);
                }
                else
                {
                    var lowestRank = existingItemsOrder.Min() ?? lexoRankService.Default;
                    result = lexoRankService.GetLowerPriority(lowestRank);
                }
            }
            return result;
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
                logger.LogInformation("Validating Team Settings for Team {TeamName} and Query {Query}", team.Name, team.WorkItemQuery);

                var workItemsQuery = PrepareQuery(team.WorkItemTypes, team.AllStates, team.WorkItemQuery);
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

        public async Task<bool> ValidateProjectSettings(Project project)
        {
            try
            {
                logger.LogInformation("Validating Project Settings for Project {ProjectName} and Query {Query}", project.Name, project.WorkItemQuery);

                var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.WorkItemQuery);
                var issues = await GetIssuesByQuery(project, query, null, 10);
                var totalFeatures = issues.Count();

                logger.LogInformation("Found a total of {NumberOfFeature} Features with the specified Query", totalFeatures);

                return totalFeatures > 0;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Project Settings for Project {ProjectName}", project.Name);
                return false;
            }
        }

        private async Task<List<Feature>> CreateFeaturesFromIssues(Project project, IEnumerable<Issue> issues)
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

        private async Task<int> GetEstimatedSizeForItem(string referenceId, Project project)
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

        private async Task<string> GetFeatureOwnerByField(string referenceId, Project project)
        {
            if (string.IsNullOrEmpty(project.FeatureOwnerField))
            {
                return string.Empty;
            }

            return await GetFieldValue(referenceId, project, project.FeatureOwnerField);
        }

        private async Task<string> GetFieldValue(string referenceId, Project project, string fieldName)
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

            var url = $"rest/api/latest/issue/{issueId}?expand=changelog";

            var response = await jiraClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseBody);

            var rankField = await GetRankField(jiraClient);
            var issue = issueFactory.CreateIssueFromJson(jsonResponse.RootElement, workitemQueryOwner, additionalRelatedField, rankField);

            logger.LogDebug("Found Issue by Key: {Key}", issue.Key);

            return issue;
        }

        private static async Task<string> GetRankField(HttpClient jiraClient)
        {
            if (!string.IsNullOrEmpty(rankFieldName))
            {
                return rankFieldName;
            }

            var url = "rest/api/latest/field";

            var response = await jiraClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseBody);

            foreach (var field in jsonResponse.RootElement.EnumerateArray())
            {
                if (field.GetProperty(JiraFieldNames.NamePropertyName).GetString() == JiraFieldNames.RankName)
                {
                    rankFieldName = field.GetProperty(JiraFieldNames.IdPropertyName).GetString() ?? string.Empty;
                    break;
                }
            }

            return rankFieldName;
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

            if (deployment == JiraDeployment.Cloud)
            {
                return await GetIssuesByQueryFromCloud(client, workItemQueryOwner, jqlQuery, additionalRelatedField, maxResultsOverride, rankField);
            }

            return await GetIssuesByQueryFromDataCenter(client, workItemQueryOwner, jqlQuery, additionalRelatedField, maxResultsOverride, rankField);
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQueryFromDataCenter(HttpClient client, IWorkItemQueryOwner owner, string jqlQuery, string? additionalRelatedField, int? maxResultsOverride, string rankField)
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
                    var issue = issueFactory.CreateIssueFromJson(jsonIssue, owner, additionalRelatedField, rankField);
                    logger.LogDebug("Found Issue {Key}", issue.Key);
                    issues.Add(issue);
                }
            }

            return issues;
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQueryFromCloud(HttpClient client, IWorkItemQueryOwner owner, string jqlQuery, string? additionalRelatedField, int? maxResultsOverride, string rankField)
        {
            var issues = new List<Issue>();
            string? nextPageToken = null;
            var pageLimit = maxResultsOverride ?? 1000;
            var pageCount = 0;

            do
            {
                var query = new StringBuilder("rest/api/3/search/jql?");
                query.Append("jql=").Append(Uri.EscapeDataString(jqlQuery));
                query.Append("&expand=").Append(Uri.EscapeDataString("changelog"));
                query.Append("&fields=").Append(Uri.EscapeDataString("*all"));
                query.Append("&maxResults=").Append(pageLimit.ToString());

                if (!string.IsNullOrEmpty(nextPageToken))
                {
                    query.Append("&nextPageToken=").Append(Uri.EscapeDataString(nextPageToken));
                }

                var response = await client.GetAsync(query.ToString());
                if (!response.IsSuccessStatusCode)
                { 
                    return await GetIssuesByQueryFromDataCenter(client, owner, jqlQuery, additionalRelatedField, maxResultsOverride, rankField);
                }

                var body = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(body);

                var issuesArray = json.RootElement.GetProperty("issues");

                foreach (var jsonIssue in issuesArray.EnumerateArray())
                {
                    issues.Add(issueFactory.CreateIssueFromJson(jsonIssue, owner, additionalRelatedField, rankField));
                }

                nextPageToken = json.RootElement.TryGetProperty("nextPageToken", out var tokenEl) ? tokenEl.GetString() : null;
                pageCount++;
            }
            while (!maxResultsOverride.HasValue && !string.IsNullOrEmpty(nextPageToken) && pageCount < 100);

            return issues;
        }

        private static string PrepareQuery(IEnumerable<string> includedWorkItemTypes, IEnumerable<string> includedStates, string query)
        {
            var workItemsQuery = PrepareGenericQuery(includedWorkItemTypes, JiraFieldNames.IssueTypeFieldName, "OR", "=");
            var stateQuery = PrepareGenericQuery(includedStates, JiraFieldNames.StatusFieldName, "OR", "=");
            var jql = $"({query}) {workItemsQuery} {stateQuery} ";
            return jql;
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
            var username = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Username);
            var encryptedApiToken = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.ApiToken);
            var apiToken = cryptoService.Decrypt(encryptedApiToken);
            var key = $"{url}|{encryptedApiToken}";

            var client = ClientCache.GetOrAdd(key, _ =>
            {
                var c = new HttpClient(SharedHandler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = TimeSpan.FromSeconds(requestTimeoutInSeconds)
                };
                return c;
            });

            if (!string.IsNullOrEmpty(username))
            {
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