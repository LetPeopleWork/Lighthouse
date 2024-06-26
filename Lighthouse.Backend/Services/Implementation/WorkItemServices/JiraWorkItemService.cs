﻿using Lighthouse.Backend.Cache;
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

        private readonly string[] closedStates = ["Done", "Closed"];

        private readonly ILexoRankService lexoRankService;
        private readonly IIssueFactory issueFactory;
        private readonly ILogger<JiraWorkItemService> logger;

        public JiraWorkItemService(ILexoRankService lexoRankService, IIssueFactory issueFactory, ILogger<JiraWorkItemService> logger)
        {
            this.lexoRankService = lexoRankService;
            this.issueFactory = issueFactory;
            this.logger = logger;
        }

        public async Task<int[]> GetClosedWorkItems(int history, Team team)
        {
            logger.LogInformation("Getting Closed Work Items for Team {TeamName}", team.Name);
            var client = GetJiraRestClient(team);

            return await GetClosedItemsPerDay(client, history, team);
        }

        public async Task<List<string>> GetOpenWorkItems(IEnumerable<string> workItemTypes, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation("Getting Open Work Items for Work Items {WorkItemTypes} and Query '{Query}'", string.Join(", ", workItemTypes), workItemQueryOwner.WorkItemQuery);

            var jiraRestClient = GetJiraRestClient(workItemQueryOwner);

            var query = PrepareQuery(workItemTypes, closedStates, workItemQueryOwner);
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

        public Task<int> GetRemainingRelatedWorkItems(string featureId, Team team)
        {
            logger.LogInformation("Getting Related Issues for Feature {Id} and Team {TeamName}", featureId, team.Name);

            var jiraRestClient = GetJiraRestClient(team);

            var relatedWorkItems =  GetRelatedWorkItems(jiraRestClient, team, featureId);

            return relatedWorkItems;
        }

        public async Task<(string name, string order)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation("Getting Issue Details for {IssueId} and Query {Query}", itemId, workItemQueryOwner.WorkItemQuery);

            var jiraRestClient = GetJiraRestClient(workItemQueryOwner);

            var issue = await GetIssueById(jiraRestClient, itemId);

            return (issue.Title, issue.Rank);
        }

        public async Task<List<string>> GetOpenWorkItemsByQuery(List<string> workItemTypes, Team team, string unparentedItemsQuery)
        {
            logger.LogInformation("Getting Open Work Items for Team {TeamName}, Item Types {WorkItemTypes} and Unaprented Items Query '{Query}'", team.Name, string.Join(", ", workItemTypes), unparentedItemsQuery);

            var jiraClient = GetJiraRestClient(team);

            var workItemsQuery = PrepareWorkItemTypeQuery(workItemTypes);
            var stateQuery = PrepareStateQuery(closedStates);

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

            var jiraClient = GetJiraRestClient(team);
            var issue = await GetIssueById(jiraClient, itemId);

            var isRelated = featureIds.Any(f => IsIssueRelated(issue, f, team.AdditionalRelatedField));
            logger.LogInformation("Is Issue {ID} related: {isRelated}", itemId, isRelated);
            
            return isRelated;
        }

        public async Task<bool> ItemHasChildren(string referenceId, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            logger.LogInformation("Checking if Issue {Key} has Children", referenceId);

            var jiraClient = GetJiraRestClient(workTrackingSystemOptionsOwner);
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

        private async Task<int> GetRelatedWorkItems(HttpClient jiraRestClient, Team team, string relatedWorkItemId)
        {
            var query = PrepareQuery(team.WorkItemTypes, closedStates, team);
            var issues = await GetIssuesByQuery(jiraRestClient, query);

            var relatedItems = issues.Where(i => IsIssueRelated(i, relatedWorkItemId, team.AdditionalRelatedField)).Select(i => i.Key).ToList();
            logger.LogInformation("Found following issues that are related to {FeatureId}: {RelatedKeys}", relatedWorkItemId, string.Join(", ", relatedItems));

            return relatedItems.Count;
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
            return PrepareGenericQuery(issueTypes, JiraFieldNames.IssueTypeFieldName, "OR", "=");
        }

        private string PrepareStateQuery(IEnumerable<string> excludedStates)
        {
            return PrepareGenericQuery(excludedStates, JiraFieldNames.StatusFieldName, "AND", "!=");
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