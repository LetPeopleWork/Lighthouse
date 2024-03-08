using CMFTAspNet.Cache;
using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.WorkTracking.Jira;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CMFTAspNet.Services.Implementation.WorkItemServices
{
    public class JiraWorkItemService : IWorkItemService
    {
        private readonly Cache<string, Issue> cache = new Cache<string, Issue>();

        private readonly string[] closedStates = ["Done", "Closed"];

        public async Task<int[]> GetClosedWorkItems(int history, Team team)
        {
            var client = GetJiraRestClient(team);

            return await GetClosedItemsPerDay(client, history, team);
        }

        public async Task<List<string>> GetOpenWorkItems(IEnumerable<string> workItemTypes, IWorkItemQueryOwner workItemQueryOwner)
        {
            var jiraRestClient = GetJiraRestClient(workItemQueryOwner);

            var query = PrepareQuery([], closedStates, workItemQueryOwner);
            var issues = await GetIssuesByQuery(jiraRestClient, query);

            var workItems = issues.Where(i => workItemTypes.Contains(i.IssueType)).ToList();

            foreach (var issue in issues)
            {
                if (!string.IsNullOrEmpty(issue.ParentKey))
                {
                    var parentItem = await GetIssueById(jiraRestClient, issue.ParentKey);

                    if (workItemTypes.Contains(parentItem.IssueType))
                    {
                        workItems.Add(parentItem);
                    }
                }
            }

            return workItems.Select(x => x.Key).ToList();
        }

        public Task<int> GetRemainingRelatedWorkItems(string featureId, Team team)
        {
            var jiraRestClient = GetJiraRestClient(team);

            return GetRelatedWorkItems(jiraRestClient, team, featureId);
        }

        public async Task<(string name, int order)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            var jiraRestClient = GetJiraRestClient(workItemQueryOwner);

            var issue = await GetIssueById(jiraRestClient, itemId);

            return (issue.Title, issue.Rank);
        }

        private async Task<Issue> GetIssueById(HttpClient jiraClient, string issueId)
        {
            var issue = cache.Get(issueId);

            if (issue == null)
            {
                var url = $"rest/api/3/issue/{issueId}";

                var response = await jiraClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseBody);

                issue = new Issue(jsonResponse.RootElement);

                UpdateCache(issue);
            }

            return issue;
        }

        private async Task<int> GetRelatedWorkItems(HttpClient jiraRestClient, Team team, string relatedWorkItemId)
        {
            var remainingItems = 0;

            var query = PrepareQuery(team.WorkItemTypes, closedStates, team);
            var issues = await GetIssuesByQuery(jiraRestClient, query);

            foreach (var issue in issues)
            {
                if (IsIssueRelated(issue, relatedWorkItemId, team.AdditionalRelatedField))
                {
                    remainingItems += 1;
                }
            }

            return remainingItems;
        }

        private bool IsIssueRelated(Issue issue, string relatedWorkItemId, string? additionalRelatedField)
        {
            if (issue.ParentKey == relatedWorkItemId)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(additionalRelatedField))
            {
                var relatedFieldValue = issue.GetFieldValue(additionalRelatedField);

                return relatedFieldValue == relatedWorkItemId;
            }

            return false;
        }

        private async Task<int[]> GetClosedItemsPerDay(HttpClient jiraRestClient, int history, Team team)
        {
            var closedItemsPerDay = new int[history];
            var startDate = DateTime.UtcNow.Date.AddDays(-(history - 1));

            var query = PrepareQuery(team.WorkItemTypes, [], team);
            query += $" AND ({JiraFieldNames.StatusFieldName} = \"Done\") AND {JiraFieldNames.ResolvedFieldName} >= -{history}d";

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
            var issues = new List<Issue>();

            var url = $"rest/api/3/search?jql={jqlQuery} order by rank";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseBody);

            var rank = 1;
            foreach (var jsonIssue in jsonResponse.RootElement.GetProperty("issues").EnumerateArray())
            {
                var issue = new Issue(jsonIssue)
                {
                    Rank = rank,
                };

                issues.Add(issue);

                UpdateCache(issue);

                rank++;
            }

            return issues;
        }

        private void UpdateCache(Issue issue)
        {
            cache.Store(issue.Key, issue, TimeSpan.FromMinutes(5));
        }

        private string PrepareQuery(
            IEnumerable<string> issueTypes,
            IEnumerable<string> excludedStates,
            IWorkItemQueryOwner workitemQueryOwner)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(issueTypes);
            var stateQuery = PrepareStateQuery(excludedStates);

            var jql = $"{workitemQueryOwner.WorkItemQuery} " +
                $"{workItemsQuery} " +
                $"{stateQuery} ";

            return jql;
        }

        private string PrepareWorkItemTypeQuery(IEnumerable<string> issueTypes)
        {
            return PrepareQuery(issueTypes, JiraFieldNames.IssueTypeFieldName, "OR", "=");
        }

        private string PrepareStateQuery(IEnumerable<string> excludedStates)
        {
            return PrepareQuery(excludedStates, JiraFieldNames.StatusFieldName, "AND", "!=");
        }

        private string PrepareQuery(IEnumerable<string> options, string fieldName, string queryOperator, string queryComparison)
        {
            var query = string.Join($" {queryOperator} ", options.Select(options => $"{fieldName} {queryComparison} \"{options}\""));

            if (options.Count() == 0)
            {
                query = string.Empty;
            }
            else
            {
                query = $"AND ({query}) ";
            }

            return query;
        }

        private HttpClient GetJiraRestClient(IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            var url = workTrackingSystemOptionsOwner.GetWorkTrackingSystemOptionByKey(JiraWorkTrackingOptionNames.Url);
            var username = workTrackingSystemOptionsOwner.GetWorkTrackingSystemOptionByKey(JiraWorkTrackingOptionNames.Username);
            var apiToken = workTrackingSystemOptionsOwner.GetWorkTrackingSystemOptionByKey(JiraWorkTrackingOptionNames.ApiToken);
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{apiToken}");

            var client = new HttpClient();
            client.BaseAddress = new Uri(url.TrimEnd('/'));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            return client;
        }
    }
}