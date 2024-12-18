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

        public async Task<IEnumerable<int>> GetChildItemsForFeaturesInProject(Project project)
        {
            var childItemList = new List<int>();

            logger.LogInformation("Getting Child Items for Features in Project {Project} for Work Item Types {WorkItemTypes} and Query '{Query}'", project.Name, string.Join(", ", project.WorkItemTypes), project.HistoricalFeaturesWorkItemQuery);

            var jiraRestClient = GetJiraRestClient(project.WorkTrackingSystemConnection);

            var workItemsQuery = PrepareWorkItemTypeQuery(project.WorkItemTypes);
            var jql = $"{project.HistoricalFeaturesWorkItemQuery} " +
            $"{workItemsQuery} ";

            var issues = await GetIssuesByQuery(jiraRestClient, jql);

            foreach (var issue in issues)
            {
                var childItems = 0;

                var tasks = project.Teams.Select(async team =>
                {
                    var childItemForTeam = await GetRelatedWorkItems($"{issue.Key}", team);
                    return childItemForTeam.totalItems;
                }).ToList();

                var results = await Task.WhenAll(tasks);

                childItems = results.Sum();

                childItemList.Add(childItems);
            }

            return childItemList.Where(i => i > 0);
        }

        public async Task<IEnumerable<string>> GetFeaturesInProgressForTeam(Team team)
        {
            logger.LogInformation("Getting Features in Progress for Team {TeamName}", team.Name);

            var jiraRestClient = GetJiraRestClient(team.WorkTrackingSystemConnection);

            var workItemQuery = PrepareWorkItemTypeQuery(team.WorkItemTypes);
            var stateQuery = PrepareGenericQuery(team.DoingStates, JiraFieldNames.StatusFieldName, "OR", "=");

            var query = $"{team.WorkItemQuery} {workItemQuery} {stateQuery} ";
            var issues = await GetIssuesByQuery(jiraRestClient, query);

            return issues.Select(i => i.ParentKey).Distinct();
        }

        public async Task<(int remainingItems, int totalItems)> GetRelatedWorkItems(string featureId, Team team)
        {
            logger.LogInformation("Getting Related Issues for Feature {Id} and Team {TeamName}", featureId, team.Name);

            var jiraRestClient = GetJiraRestClient(team.WorkTrackingSystemConnection);

            var (remainingItems, totalItems) = await GetRelatedWorkItems(jiraRestClient, team, featureId);

            return (remainingItems, totalItems);
        }

        public async Task<(string name, string order, string url, string state)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation("Getting Issue Details for {IssueId} and Query {Query}", itemId, workItemQueryOwner.WorkItemQuery);

            var jiraRestClient = GetJiraRestClient(workItemQueryOwner.WorkTrackingSystemConnection);

            var issue = await GetIssueById(jiraRestClient, itemId);

            var url = $"{jiraRestClient.BaseAddress}browse/{issue.Key}";

            return (issue.Title, issue.Rank, url, issue.State);
        }

        public async Task<(List<string> remainingWorkItems, List<string> allWorkItems)> GetWorkItemsByQuery(List<string> workItemTypes, Team team, string unparentedItemsQuery)
        {
            logger.LogInformation("Getting Work Items for Team {TeamName}, Item Types {WorkItemTypes} and Unaprented Items Query '{Query}'", team.Name, string.Join(", ", workItemTypes), unparentedItemsQuery);

            var jiraClient = GetJiraRestClient(team.WorkTrackingSystemConnection);

            var workItemsQuery = PrepareWorkItemTypeQuery(workItemTypes);
            var notDoneStateQuery = PrepareStateQuery(team.OpenStates);
            var doneStateQuery = PrepareStateQuery(team.DoneStates);

            var baseQuery = $"{unparentedItemsQuery} " +
                $"{workItemsQuery} " +
                $"AND {team.WorkItemQuery}";

            var doneWorkItemsQuery = $"{baseQuery} {doneStateQuery}";
            var remainingWorkItemsQuery = $"{baseQuery} {notDoneStateQuery}";

            var doneIssues = await GetIssuesByQuery(jiraClient, doneWorkItemsQuery);
            var remainingIssues = await GetIssuesByQuery(jiraClient, remainingWorkItemsQuery);

            var doneWorkItemsIds = doneIssues.Select(x => x.Key).ToList();
            var remainingWorkItemIds = remainingIssues.Select(x => x.Key).ToList();

            logger.LogDebug("Found following Done Work Items {DoneWorkItems}", string.Join(", ", doneWorkItemsIds));
            logger.LogDebug("Found following Undone Work Items {RemainingWorkItems}", string.Join(", ", remainingWorkItemIds));

            return (remainingWorkItemIds, doneWorkItemsIds);
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

        public async Task<bool> ValidateTeamSettings(Team team)
        {
            try
            {
                logger.LogInformation("Validating Team Settings for Team {TeamName} and Query {Query}", team.Name, team.WorkItemQuery);
                var restClient = GetJiraRestClient(team.WorkTrackingSystemConnection);

                var throughput = await GetClosedItemsPerDay(restClient, team.ThroughputHistory, team);
                var totalThroughput = throughput.Sum();

                logger.LogInformation("Found a total of {NumberOfWorkItems} Closed Work Items with specified Query in the last {Days} days", totalThroughput, team.ThroughputHistory);

                return totalThroughput > 0;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Team Settings for Team {TeamName}", team.Name);
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
            var parentClause = $"AND (parent = {relatedWorkItemId}";
            if (!string.IsNullOrEmpty(team.AdditionalRelatedField))
            {
                parentClause += $" OR {team.AdditionalRelatedField} = {relatedWorkItemId})";
            }
            else
            {
                parentClause += ")";
            }

            var remainingItemsQuery = $"{PrepareNotClosedItemsQuery(team.WorkItemTypes, team)} {parentClause}";
            var closedItemsQuery = $"{PrepareClosedItemsQuery(team.WorkItemTypes, team)} {parentClause}";

            logger.LogDebug("Getting Remaining Items by Query...");
            var remainingIssues = (await GetIssuesByQuery(jiraRestClient, remainingItemsQuery)).Select(i => i.Key).ToList();

            logger.LogDebug("Getting Closed Items by Query...");
            var closedIssues = (await GetIssuesByQuery(jiraRestClient, closedItemsQuery)).Select(i => i.Key).ToList();

            logger.LogInformation("Found following issues that are related to {FeatureId}: {RelatedKeys}", relatedWorkItemId, string.Join(", ", remainingIssues.Union(closedIssues)));

            return (remainingIssues.Count, remainingIssues.Count + closedIssues.Count);
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

            var startAt = 0;
            var maxResults = 1000;
            var isLast = false;

            while (!isLast)
            {
                var url = $"rest/api/3/search?jql={jqlQuery}&startAt={startAt}&maxResults={maxResults}";

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseBody);

                // Max Results might differ - response will tell how many can be delivered max. Use this value.
                var maxResultActual = jsonResponse.RootElement.GetProperty("maxResults").ToString();
                var totalResultsActual = jsonResponse.RootElement.GetProperty("total").ToString();

                maxResults = int.Parse(maxResultActual);
                startAt += maxResults;
                isLast = int.Parse(totalResultsActual) < startAt;

                foreach (var jsonIssue in jsonResponse.RootElement.GetProperty("issues").EnumerateArray())
                {
                    var issue = issueFactory.CreateIssueFromJson(jsonIssue);

                    logger.LogDebug("Found Issue {key}", issue.Key);

                    issues.Add(issue);

                    UpdateCache(issue);
                }
            }

            return issues;
        }

        private void UpdateCache(Issue issue)
        {
            cache.Store(issue.Key, issue, TimeSpan.FromMinutes(5));
        }

        private static string PrepareClosedItemsQuery(
            IEnumerable<string> issueTypes,
            IWorkItemQueryOwner workitemQueryOwner,
            int? history = null)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(issueTypes);
            var stateQuery = PrepareGenericQuery(workitemQueryOwner.DoneStates, JiraFieldNames.StatusFieldName, "OR", "=");

            var historyFilter = string.Empty;
            if (history != null)
            {
                historyFilter = $"AND {JiraFieldNames.ResolvedFieldName} >= -{history}d";
            }

            var jql = $"{workitemQueryOwner.WorkItemQuery} " +
                $"{workItemsQuery} " +
                $"{stateQuery} " +
                $"{historyFilter}";

            return jql;
        }

        private string PrepareNotClosedItemsQuery(
            IEnumerable<string> issueTypes,
            IWorkItemQueryOwner workitemQueryOwner)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(issueTypes);
            var stateQuery = PrepareStateQuery(workitemQueryOwner.OpenStates);

            var jql = $"{workitemQueryOwner.WorkItemQuery} " +
                $"{workItemsQuery} " +
                $"{stateQuery} ";

            return jql;
        }

        private static string PrepareWorkItemTypeQuery(IEnumerable<string> issueTypes)
        {
            return PrepareGenericQuery(issueTypes, JiraFieldNames.IssueTypeFieldName, "OR", "=");
        }

        private static string PrepareStateQuery(IEnumerable<string> doneStates)
        {
            return PrepareGenericQuery(doneStates, JiraFieldNames.StatusFieldName, "OR", "=");
        }

        private static string PrepareGenericQuery(IEnumerable<string> options, string fieldName, string queryOperator, string queryComparison)
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