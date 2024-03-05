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

        public async Task<int[]> GetClosedWorkItemsForTeam(int history, Team team)
        {
            var closedItemsPerDay = new int[history];
            var startDate = DateTime.UtcNow.Date.AddDays(-(history - 1));

            var client = GetJiraRestClient(team);
            var projectKey = GetProjectKey(team);

            var issueTypeQuery = string.Join(" OR ", team.WorkItemTypes.Select(type => $"issuetype = \"{type}\""));
            var resolvedQuery = $"resolved >= -{history}d";
            var jqlQuery = $"project = \"{projectKey}\" AND ({issueTypeQuery}) AND status = Done AND {resolvedQuery}";

            var jsonDocument = await ExecuteJqlQuery(client, jqlQuery);

            foreach (var issue in jsonDocument.RootElement.GetProperty("issues").EnumerateArray())
            {
                var fields = issue.GetProperty("fields");
                var resolutionDateString = fields.GetProperty("resolutiondate").GetString();

                if (!string.IsNullOrEmpty(resolutionDateString))
                {
                    var resolutionDate = DateTime.Parse(resolutionDateString);

                    int index = (resolutionDate.Date - startDate).Days;

                    if (index >= 0 && index < history)
                    {
                        closedItemsPerDay[index]++;
                    }
                }

            }

            return closedItemsPerDay;
        }

        public Task<List<string>> GetNotClosedWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            return Task.FromResult(new List<string>());
        }

        public Task<List<string>> GetNotClosedWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            return Task.FromResult(new List<string>());
        }

        public Task<int> GetRemainingRelatedWorkItems(string featureId, Team team)
        {
            return Task.FromResult(0);
        }

        public Task<(string name, int order)> GetWorkItemDetails(string itemId, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            return Task.FromResult(("name", 1));
        }

        public Task<List<string>> GetWorkItemsByArea(IEnumerable<string> workItemTypes, string area, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            return Task.FromResult(new List<string>());
        }

        public Task<List<string>> GetWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            return Task.FromResult(new List<string>());
        }

        public Task<bool> IsRelatedToFeature(string itemId, IEnumerable<string> featureIds, Team team)
        {
            return Task.FromResult(false);
        }

        private async Task<JsonDocument> ExecuteJqlQuery(HttpClient client, string jqlQuery)
        {
            var url = $"rest/api/3/search?jql={jqlQuery}";

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
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{apiToken}");

            var client = new HttpClient();
            client.BaseAddress = new Uri(url.TrimEnd('/'));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            return client;
        }
    }
}
