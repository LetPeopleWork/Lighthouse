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

        public async Task<IEnumerable<WorkItem>> UpdateWorkItemsForTeam(Team team)
        {
            return await Task.FromResult(Enumerable.Empty<WorkItem>());
        }

        public async Task<int[]> GetThroughputForTeam(Team team)
        {
            logger.LogInformation("Getting Closed Work Items for Team {TeamName}", team.Name);
            var client = GetJiraRestClient(team.WorkTrackingSystemConnection);

            return await GetClosedItemsPerDay(client, team);
        }

        public Task<string[]> GetClosedWorkItemsForTeam(Team team)
        {
            return Task.FromResult(Array.Empty<string>());
        }

        public async Task<List<string>> GetFeaturesForProject(Project project)
        {
            logger.LogInformation("Getting Open Work Items for Work Items {WorkItemTypes} and Query '{Query}'", string.Join(", ", project.WorkItemTypes), project.WorkItemQuery);

            var jiraRestClient = GetJiraRestClient(project.WorkTrackingSystemConnection);

            var query = PrepareAllItemsQuery(project);
            var issues = await GetIssuesByQuery(jiraRestClient, project, query);

            var workItems = issues.Where(i => project.WorkItemTypes.Contains(i.IssueType)).ToList();

            foreach (var parentKey in issues.Where(i => !string.IsNullOrEmpty(i.ParentKey)).Select(i => i.ParentKey))
            {
                var parentItem = await GetIssueById(jiraRestClient, project, parentKey);

                if (project.WorkItemTypes.Contains(parentItem.IssueType))
                {
                    logger.LogInformation("Found Issue with Key {Key}", parentItem.Key);
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

            var issues = await GetIssuesByQuery(jiraRestClient, project, jql);

            foreach (var issue in issues)
            {
                var childItems = 0;

                var tasks = project.Teams.Select(async team =>
                {
                    var (_, totalItems) = await GetRelatedWorkItems($"{issue.Key}", team);
                    return totalItems;
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

            var query = $"({team.WorkItemQuery}) {workItemQuery} {stateQuery} ";
            var issues = await GetIssuesByQuery(jiraRestClient, team, query, team.AdditionalRelatedField);

            return issues.Select(i => i.ParentKey).Distinct();
        }

        public async Task<(int remainingItems, int totalItems)> GetRelatedWorkItems(string featureId, Team team)
        {
            logger.LogInformation("Getting Related Issues for Feature {Id} and Team {TeamName}", featureId, team.Name);

            var jiraRestClient = GetJiraRestClient(team.WorkTrackingSystemConnection);

            var (remainingItems, totalItems) = await GetRelatedWorkItems(jiraRestClient, team, featureId);

            return (remainingItems, totalItems);
        }

        public async Task<(string name, string order, string url, string state, DateTime? startedDate, DateTime? closedDate)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation("Getting Issue Details for {IssueId} and Query {Query}", itemId, workItemQueryOwner.WorkItemQuery);

            var jiraRestClient = GetJiraRestClient(workItemQueryOwner.WorkTrackingSystemConnection);

            var issue = await GetIssueById(jiraRestClient, workItemQueryOwner, itemId);

            var url = $"{jiraRestClient.BaseAddress}browse/{issue.Key}";

            return (issue.Title, issue.Rank, url, issue.State, issue.StartedDate, issue.ClosedDate);
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
                $"AND ({team.WorkItemQuery})";

            var doneWorkItemsQuery = $"{baseQuery} {doneStateQuery}";
            var remainingWorkItemsQuery = $"{baseQuery} {notDoneStateQuery}";

            var doneIssues = await GetIssuesByQuery(jiraClient, team, doneWorkItemsQuery, team.AdditionalRelatedField);
            var remainingIssues = await GetIssuesByQuery(jiraClient, team, remainingWorkItemsQuery, team.AdditionalRelatedField);

            var doneWorkItemsIds = doneIssues.Select(x => x.Key).ToList();
            var remainingWorkItemIds = remainingIssues.Select(x => x.Key).ToList();

            logger.LogDebug("Found following Done Work Items {DoneWorkItems}", string.Join(", ", doneWorkItemsIds));
            logger.LogDebug("Found following Undone Work Items {RemainingWorkItems}", string.Join(", ", remainingWorkItemIds));

            return (remainingWorkItemIds, remainingWorkItemIds.Union(doneWorkItemsIds).ToList());
        }

        public async Task<bool> IsRelatedToFeature(string itemId, IEnumerable<string> featureIds, Team team)
        {
            logger.LogInformation("Checking if Issue {Key} of Team {TeamName} is related to {FeatureIDs}", itemId, team.Name, string.Join(", ", featureIds));

            var jiraClient = GetJiraRestClient(team.WorkTrackingSystemConnection);
            var issue = await GetIssueById(jiraClient, team, itemId, team.AdditionalRelatedField);

            var isRelated = featureIds.Any(f => IsIssueRelated(issue, f));
            logger.LogInformation("Is Issue {ID} related: {IsRelated}", itemId, isRelated);

            return isRelated;
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            logger.LogInformation("Getting Adjacent Order Index for Issues {Items} in order {RelativeOrder}", string.Join(", ", existingItemsOrder), relativeOrder);

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

            logger.LogInformation("Adjacent Order Index for issues {ExistingOrder} in order {RelativeOrder}: {Result}", string.Join(", ", existingItemsOrder), relativeOrder, result);

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

                var throughput = await GetClosedItemsPerDay(restClient, team);
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

        public async Task<bool> ValidateProjectSettings(Project project)
        {
            try
            {
                logger.LogInformation("Validating Project Settings for Project {ProjectName} and Query {Query}", project.Name, project.WorkItemQuery);

                var features = await GetFeaturesForProject(project);
                var totalFeatures = features.Count;

                logger.LogInformation("Found a total of {NumberOfFeature} Features with the specified Query", totalFeatures);

                return totalFeatures > 0;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Project Settings for Project {ProjectName}", project.Name);
                return false;
            }
        }

        public async Task<int> GetEstimatedSizeForItem(string referenceId, Project project)
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

        public async Task<string> GetFeatureOwnerByField(string referenceId, Project project)
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
            var issue = cache.Get(issueId);

            if (issue == null)
            {
                logger.LogDebug("Not Found in Cache - Getting from Jira");

                var url = $"rest/api/latest/issue/{issueId}?expand=changelog";

                var response = await jiraClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseBody);

                issue = issueFactory.CreateIssueFromJson(jsonResponse.RootElement, workitemQueryOwner, additionalRelatedField);

                UpdateCache(issue);
            }

            logger.LogDebug("Found Issue by Key: {Key}", issue.Key);

            return issue;
        }

        private async Task<(int remainingItems, int totalItems)> GetRelatedWorkItems(HttpClient jiraRestClient, Team team, string relatedWorkItemId)
        {
            // Jira does not support all operators for custom fields (depending on the type), so we try to use a "match", followed by a "fuzzy match" if the first one fails
            var parentFieldName = "parent";
            var operators = new[] { "=" };

            if (!string.IsNullOrEmpty(team.AdditionalRelatedField))
            {
                parentFieldName = team.AdditionalRelatedField;
                operators = ["=", "~"];
            }

            foreach (var customFieldOperator in operators)
            {
                var parentClause = $"AND {parentFieldName}{customFieldOperator}{relatedWorkItemId}";

                var remainingItemsQuery = $"{PrepareNotClosedItemsQuery(team)} {parentClause}";
                var closedItemsQuery = $"{PrepareClosedItemsQuery(team.WorkItemTypes, team)} {parentClause}";

                try
                {
                    logger.LogDebug("Getting Remaining Items by Query...");
                    var remainingIssues = (await GetIssuesByQuery(jiraRestClient, team, remainingItemsQuery, parentFieldName)).Select(i => i.Key).ToList();

                    logger.LogDebug("Getting Closed Items by Query...");
                    var closedIssues = (await GetIssuesByQuery(jiraRestClient, team, closedItemsQuery, parentFieldName)).Select(i => i.Key).ToList();

                    logger.LogInformation("Found following issues that are related to {FeatureId}: {RelatedKeys}", relatedWorkItemId, string.Join(", ", remainingIssues.Union(closedIssues)));

                    return (remainingIssues.Count, remainingIssues.Count + closedIssues.Count);
                }
                catch (HttpRequestException exception)
                {
                    logger.LogInformation(exception, "Failed to get related work items with operator {Operator}", customFieldOperator);
                }
            }

            return (0, 0);
        }

        private bool IsIssueRelated(Issue issue, string relatedWorkItemId)
        {
            logger.LogDebug("Checking if Issue {Key} is related to {RelatedWorkItemId}", issue.Key, relatedWorkItemId);
            if (issue.ParentKey == relatedWorkItemId)
            {
                return true;
            }

            return false;
        }

        private async Task<int[]> GetClosedItemsPerDay(HttpClient jiraRestClient, Team team)
        {
            var throughputSettings = team.GetThroughputSettings();
            var numberOfDays = throughputSettings.NumberOfDays;
            var closedItemsPerDay = new int[numberOfDays];

            var query = PrepareClosedItemsQuery(team.WorkItemTypes, team, throughputSettings);

            var issues = await GetIssuesByQuery(jiraRestClient, team, query);

            foreach (var issue in issues)
            {
                int index = (issue.ClosedDate.Date - throughputSettings.StartDate).Days;

                if (index >= 0 && index < numberOfDays)
                {
                    closedItemsPerDay[index]++;
                }
            }

            return closedItemsPerDay;
        }

        private async Task<IEnumerable<Issue>> GetIssuesByQuery(HttpClient client, IWorkItemQueryOwner workItemQueryOwner, string jqlQuery, string? additionalRelatedField = null)
        {
            logger.LogDebug("Getting Issues by JQL Query: '{Query}'", jqlQuery);
            var issues = new List<Issue>();

            var startAt = 0;
            var maxResults = 1000;
            var isLast = false;

            while (!isLast)
            {
                var url = $"rest/api/latest/search?jql={jqlQuery}&startAt={startAt}&maxResults={maxResults}&expand=changelog";

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
                    var issue = issueFactory.CreateIssueFromJson(jsonIssue, workItemQueryOwner, additionalRelatedField);

                    logger.LogDebug("Found Issue {Key}", issue.Key);

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
            Team team,
            ThroughputSettings? throughputSettings = null)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(issueTypes);
            var stateQuery = PrepareGenericQuery(team.DoneStates, JiraFieldNames.StatusFieldName, "OR", "=");

            var historyFilter = string.Empty;
            if (throughputSettings != null)
            {
                historyFilter = $"AND {JiraFieldNames.ResolvedFieldName} >= {throughputSettings.StartDate:yyyy-MM-dd} AND {JiraFieldNames.ResolvedFieldName} <= {throughputSettings.EndDate.AddDays(1):yyyy-MM-dd}";
            }

            var jql = $"({team.WorkItemQuery}) " +
                $"{workItemsQuery} " +
                $"{stateQuery} " +
                $"{historyFilter}";

            return jql;
        }

        private static string PrepareNotClosedItemsQuery(
            Team team)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(team.WorkItemTypes);
            var stateQuery = PrepareStateQuery(team.OpenStates);

            var jql = $"({team.WorkItemQuery}) " +
                $"{workItemsQuery} " +
                $"{stateQuery} ";

            return jql;
        }

        private static string PrepareAllItemsQuery(
            Project project)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(project.WorkItemTypes);
            var stateQuery = PrepareStateQuery(project.AllStates);

            var jql = $"({project.WorkItemQuery}) " +
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

            var client = new HttpClient
            {
                BaseAddress = new Uri(url.TrimEnd('/'))
            };

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
    }
}