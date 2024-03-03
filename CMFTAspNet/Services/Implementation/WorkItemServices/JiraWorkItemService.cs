using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.WorkTracking.Jira;
using Microsoft.VisualStudio.Services.DelegatedAuthorization;
using NuGet.Common;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CMFTAspNet.Services.Implementation.WorkItemServices
{
    public class JiraWorkItemService : IWorkItemService
    {

        public async Task<int[]> GetClosedWorkItemsForTeam(int history, Team team)
        {
            var closedItemsPerDay = new int[history];

            var client = GetJiraRestClient(team);
            var projectKey = GetProjectKey(team);

            var issueTypeQuery = string.Join(" OR ", team.WorkItemTypes.Select(type => $"issuetype = \"{type}\""));
            var resolvedQuery = $"resolved >= -{history}d";
            var jqlQuery = $"project = \"{projectKey}\" AND ({issueTypeQuery}) AND status = Closed AND {resolvedQuery}";

            var jsonDocument = await ExecuteJqlQuery(client, jqlQuery);

            var issueKeys = new List<string>();
            foreach (var issue in jsonDocument.RootElement.GetProperty("issues").EnumerateArray())
            {
                var issueKey = issue.GetProperty("key").GetString();
                issueKeys.Add(issueKey);
            }

            return [0];
        }

        public Task<List<int>> GetNotClosedWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            return Task.FromResult(new List<int>());
        }

        public Task<List<int>> GetNotClosedWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            return Task.FromResult(new List<int>());
        }

        public Task<int> GetRemainingRelatedWorkItems(int featureId, Team team)
        {
            return Task.FromResult(0);
        }

        public Task<(string name, int order)> GetWorkItemDetails(int itemId, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            return Task.FromResult(("name", 1));
        }

        public Task<List<int>> GetWorkItemsByArea(IEnumerable<string> workItemTypes, string area, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            return Task.FromResult(new List<int>());
        }

        public Task<List<int>> GetWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            return Task.FromResult(new List<int>());
        }

        public Task<bool> IsRelatedToFeature(int itemId, IEnumerable<int> featureIds, Team team)
        {
            return Task.FromResult(false);
        }

        private async Task<JsonDocument> ExecuteJqlQuery(HttpClient client, string jqlQuery)
        {
            var query = Uri.EscapeDataString(jqlQuery);
            var url = $"rest/api/3/search?jql={query}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseBody);

            return jsonResponse;
        }


        private string GetProjectKey(IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            return workTrackingSystemOptionsOwner.GetWorkTrackingSystemOptionByKey(JiraWorkTrackingOptionNames.ProjectKey);
        }

        private HttpClient GetJiraRestClient(IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            var url = workTrackingSystemOptionsOwner.GetWorkTrackingSystemOptionByKey(JiraWorkTrackingOptionNames.Url);
            var username = workTrackingSystemOptionsOwner.GetWorkTrackingSystemOptionByKey(JiraWorkTrackingOptionNames.Username);
            var apiToken = workTrackingSystemOptionsOwner.GetWorkTrackingSystemOptionByKey(JiraWorkTrackingOptionNames.ApiToken);

            var client = new HttpClient();
            client.BaseAddress = new Uri(url.TrimEnd('/'));
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{apiToken}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            return client;
        }
    }
}
