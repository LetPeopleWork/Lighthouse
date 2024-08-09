using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.WorkTracking.Jira;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkItemServices
{
    public class JiraWorkItemService : IWorkItemService
    {
        private readonly Cache<string, Issue> cache = new Cache<string, Issue>();

        private readonly string DoneStatusCategory = "Done";

        private readonly ILexoRankService lexoRankService;
        private readonly IIssueFactory issueFactory;
        private readonly ILogger<JiraWorkItemService> logger;
        private readonly ICryptoService cryptoService;

        public JiraWorkItemService(ILexoRankService lexoRankService, IIssueFactory issueFactory, ILogger<JiraWorkItemService> logger, ICryptoService cryptoService)
        {
            this.lexoRankService = lexoRankService;
            this.issueFactory = issueFactory;
            this.logger = logger;
            this.cryptoService = cryptoService;
        }

        public async Task<int[]> GetClosedWorkItems(int history, Team team)
        {
            logger.LogInformation("Getting Closed Work Items for Team {TeamName}", team.Name);
            var client = GetJiraRestClient(team.WorkTrackingSystemConnection);

            return await GetClosedItemsPerDay(client, history, team);
        }

        public async Task<List<string>> GetOpenWorkItems(IEnumerable<string> workItemTypes, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation("Getting Open Work Items for Work Items {WorkItemTypes} and Query '{Query}'", string.Join(", ", workItemTypes), workItemQueryOwner.WorkItemQuery);

            var jiraRestClient = GetJiraRestClient(workItemQueryOwner.WorkTrackingSystemConnection);

            var query = PrepareNotClosedItemsQuery(workItemTypes, workItemQueryOwner);
            var issues = await GetIssuesByQuery(jiraRestClient, query);

            var workItems = issues.Where(i => workItemTypes.Contains(i.IssueType)).ToList();

            foreach (var parentKey in issues.Where(i => !string.IsNullOrEmpty(i.ParentKey)).Select(i => i.ParentKey))
            {
                var parentItem = await GetIssueById(jiraRestClient, parentKey);

                if (workItemTypes.Contains(parentItem.IssueType))
                {
                    logger.LogInformation("Found Issue with Key {key}", parentItem.Key);
                    workItems.Add(parentItem);
                }
            }

            return workItems.Select(x => x.Key).ToList();
        }

        public Task<(int remainingItems, int totalItems)> GetRelatedWorkItems(string featureId, Team team)
        {
            logger.LogInformation("Getting Related Issues for Feature {Id} and Team {TeamName}", featureId, team.Name);

            var jiraRestClient = GetJiraRestClient(team.WorkTrackingSystemConnection);

            var relatedWorkItems = GetRelatedWorkItems(jiraRestClient, team, featureId);

            return relatedWorkItems;
        }

        public async Task<(string name, string order, string url)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation("Getting Issue Details for {IssueId} and Query {Query}", itemId, workItemQueryOwner.WorkItemQuery);

            var jiraRestClient = GetJiraRestClient(workItemQueryOwner.WorkTrackingSystemConnection);

            var issue = await GetIssueById(jiraRestClient, itemId);

            var url = $"{jiraRestClient.BaseAddress}browse/{issue.Key}";

            return (issue.Title, issue.Rank, url);
        }

        public async Task<List<string>> GetOpenWorkItemsByQuery(List<string> workItemTypes, Team team, string unparentedItemsQuery)
        {
            logger.LogInformation("Getting Open Work Items for Team {TeamName}, Item Types {WorkItemTypes} and Unaprented Items Query '{Query}'", team.Name, string.Join(", ", workItemTypes), unparentedItemsQuery);

            var jiraClient = GetJiraRestClient(team.WorkTrackingSystemConnection);

            var workItemsQuery = PrepareWorkItemTypeQuery(workItemTypes);
            var stateQuery = PrepareStateQuery();

            var jql = $"{unparentedItemsQuery} " +
                $"{workItemsQuery} " +
                $"{stateQuery} " +
                $"AND {team.WorkItemQuery}";

            var issues = await GetIssuesByQuery(jiraClient, jql);

            var openWorkItemKeys = issues.Select(x => x.Key).ToList();
            logger.LogInformation("Found following Open Issues: {IDs}", string.Join(", ", openWorkItemKeys));

            return openWorkItemKeys;
        }

        public async Task<bool> IsRelatedToFeature(string itemId, IEnumerable<string> featureIds, Team team)
        {
            logger.LogInformation("Checking if Issue {Key} of Team {TeamName} is related to {FeatureIDs}", itemId, team.Name, string.Join(", ", featureIds));

            var jiraClient = GetJiraRestClient(team.WorkTrackingSystemConnection);
            var issue = await GetIssueById(jiraClient, itemId);

            var isRelated = featureIds.Any(f => IsIssueRelated(issue, f, team.AdditionalRelatedField));
            logger.LogInformation("Is Issue {ID} related: {isRelated}", itemId, isRelated);

            return isRelated;
        }

        public async Task<bool> ItemHasChildren(string referenceId, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            logger.LogInformation("Checking if Issue {Key} has Children", referenceId);

            var jiraClient = GetJiraRestClient(workTrackingSystemOptionsOwner.WorkTrackingSystemConnection);
            var jql = $"parent = \"{referenceId}\"";

            var issues = await GetIssuesByQuery(jiraClient, jql);

            var hasChildren = issues.Any();
            logger.LogInformation("Issue {Key} has Children: {hasChildren}", referenceId, hasChildren);

            return hasChildren;
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            logger.LogInformation("Getting Adjacent Order Index for Issues {Items} in order {relativeOrder}", string.Join(", ", existingItemsOrder), relativeOrder);

            var result = string.Empty;

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

            logger.LogInformation("Adjacent Order Index for issues {ExistingOrder} in order {relativeOrder}: {result}", string.Join(", ", existingItemsOrder), relativeOrder, result);

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

        public async Task<int> GetEstimatedSizeForItem(string referenceId, Project project)
        {
            if (string.IsNullOrEmpty(project.SizeEstimateField))
            {
                return 0;
            }

            try
            {
                var jiraClient = GetJiraRestClient(project.WorkTrackingSystemConnection);
                var issue = await GetIssueById(jiraClient, referenceId);

                var estimateRawValue = issue.Fields.GetFieldValue(project.SizeEstimateField);

                // Try parsing double because for sure someone will have the brilliant idea to make this a decimal
                if (double.TryParse(estimateRawValue, out var estimateAsDouble))
                {
                    return (int)estimateAsDouble;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<Issue> GetIssueById(HttpClient jiraClient, string issueId)
        {
            logger.LogDebug("Getting Issue by Key '{Key}'", issueId);
            var issue = cache.Get(issueId);

            if (issue == null)
            {
                logger.LogDebug("Not Found in Cache - Getting from Jira");

                var url = $"rest/api/3/issue/{issueId}";

                var response = await jiraClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseBody);

                issue = issueFactory.CreateIssueFromJson(jsonResponse.RootElement);

                UpdateCache(issue);
            }

            logger.LogDebug("Found Issue by Key: {Key}", issue.Key);

            return issue;
        }

        private async Task<(int remainingItems, int totalItems)> GetRelatedWorkItems(HttpClient jiraRestClient, Team team, string relatedWorkItemId)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(team.WorkItemTypes);
            var query = $"{team.WorkItemQuery} " +
                $"{workItemsQuery}";

            var issues = await GetIssuesByQuery(jiraRestClient, query);

            var relatedItems = issues.Where(i => IsIssueRelated(i, relatedWorkItemId, team.AdditionalRelatedField)).ToList();
            logger.LogInformation("Found following issues that are related to {FeatureId}: {RelatedKeys}", relatedWorkItemId, string.Join(", ", relatedItems.Select(i => i.Key)));

            var remainingItems = relatedItems.Count(i => !DoneStatusCategory.Contains(i.StatusCategory));

            return (remainingItems, relatedItems.Count);
        }

        private bool IsIssueRelated(Issue issue, string relatedWorkItemId, string? additionalRelatedField)
        {
            logger.LogDebug("Checking if Issue {Key} is related to {relatedWorkItemId}", issue.Key, relatedWorkItemId);
            if (issue.ParentKey == relatedWorkItemId)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(additionalRelatedField))
            {
                var relatedFieldValue = issue.Fields.GetFieldValue(additionalRelatedField);

                return relatedFieldValue == relatedWorkItemId;
            }

            return false;
        }

        private async Task<int[]> GetClosedItemsPerDay(HttpClient jiraRestClient, int history, Team team)
        {
            var closedItemsPerDay = new int[history];
            var startDate = DateTime.UtcNow.Date.AddDays(-(history - 1));

            var query = PrepareClosedItemsQuery(team.WorkItemTypes, team, history);

            var issues = await GetIssuesByQuery(jiraRestClient, query);

            foreach (var issue in issues)
            {
                int index = (issue.ResolutionDate.Date - startDate).Days;

                if (index >= 0 && index < history)
                {
                    closedItemsPerDay[index]++;
                }
            }

            return closedItemsPerDay;
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQuery(HttpClient client, string jqlQuery)
        {
            logger.LogDebug("Getting Issues by JQL Query: '{query}'", jqlQuery);
            var issues = new List<Issue>();

            var url = $"rest/api/3/search?jql={jqlQuery}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseBody);

            foreach (var jsonIssue in jsonResponse.RootElement.GetProperty("issues").EnumerateArray())
            {
                var issue = issueFactory.CreateIssueFromJson(jsonIssue);

                logger.LogDebug("Found Issue {key}", issue.Key);

                issues.Add(issue);

                UpdateCache(issue);
            }

            return issues;
        }

        private void UpdateCache(Issue issue)
        {
            cache.Store(issue.Key, issue, TimeSpan.FromMinutes(5));
        }

        private string PrepareClosedItemsQuery(
            IEnumerable<string> issueTypes,
            IWorkItemQueryOwner workitemQueryOwner,
            int history)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(issueTypes);
            var stateQuery = PrepareGenericQuery([DoneStatusCategory], JiraFieldNames.StatusCategoryFieldName, "AND", "=");

            var jql = $"{workitemQueryOwner.WorkItemQuery} " +
                $"{workItemsQuery} " +
                $"{stateQuery} " +
                $"AND {JiraFieldNames.ResolvedFieldName} >= -{history}d";

            return jql;
        }

        private string PrepareNotClosedItemsQuery(
            IEnumerable<string> issueTypes,
            IWorkItemQueryOwner workitemQueryOwner)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(issueTypes);
            var stateQuery = PrepareStateQuery();

            var jql = $"{workitemQueryOwner.WorkItemQuery} " +
                $"{workItemsQuery} " +
                $"{stateQuery} ";

            return jql;
        }

        private string PrepareWorkItemTypeQuery(IEnumerable<string> issueTypes)
        {
            return PrepareGenericQuery(issueTypes, JiraFieldNames.IssueTypeFieldName, "OR", "=");
        }

        private string PrepareStateQuery()
        {
            return PrepareGenericQuery([DoneStatusCategory], JiraFieldNames.StatusCategoryFieldName, "AND", "!=");
        }

        private string PrepareGenericQuery(IEnumerable<string> options, string fieldName, string queryOperator, string queryComparison)
        {
            var query = string.Join($" {queryOperator} ", options.Select(options => $"{fieldName} {queryComparison} \"{options}\""));

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

        private HttpClient GetJiraRestClient(WorkTrackingSystemConnection connection)
        {
            var url = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Url);
            var username = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Username);
            var encryptedApiToken = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.ApiToken);
            var apiToken = cryptoService.Decrypt(encryptedApiToken);
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{apiToken}");

            var client = new HttpClient();
            client.BaseAddress = new Uri(url.TrimEnd('/'));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            return client;
        }
    }
}