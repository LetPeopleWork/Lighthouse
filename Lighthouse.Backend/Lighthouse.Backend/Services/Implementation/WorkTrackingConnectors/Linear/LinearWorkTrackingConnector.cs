using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using static Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear.LinearWorkTrackingConnector.LinearResponses;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear
{
    public partial class LinearWorkTrackingConnector : IWorkTrackingConnector
    {
        private const string UnknownStateIdentifier = "Unknown";
        private const string DefaultTemplateIdentifier = "Default";

        private readonly ILogger<LinearWorkTrackingConnector> logger;
        private readonly ICryptoService cryptoService;

        public LinearWorkTrackingConnector(ILogger<LinearWorkTrackingConnector> logger, ICryptoService cryptoService)
        {
            this.logger = logger;
            this.cryptoService = cryptoService;
        }

        public async Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            logger.LogInformation("Getting Work Items for Team {TeamName}", team.Name);

            try
            {
                var workItems = new List<WorkItem>();
                var issues = await GetIssuesForTeam(team.WorkTrackingSystemConnection, team.WorkItemQuery);

                if (issues == null || !issues.Any())
                {
                    logger.LogInformation("No issues found for team {TeamName}", team.Name);
                    return workItems;
                }

                var types = team.WorkItemTypes;
                var states = team.AllStates.ToList();

                foreach (var issue in issues.Where(i => types.Contains(i.LastAppliedTemplate.Name) && states.Contains(i.State.Name)))
                {
                    var workItem = CreateWorkItemFromIssue(issue, team);
                    workItems.Add(workItem);
                }

                return workItems;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting work items for team {TeamName}", team.Name);
                return Enumerable.Empty<WorkItem>();
            }
        }

        public async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            // Will implement later - placeholder for now
            throw new NotImplementedException();

            // Don't forget to adjust FeatureComparer to handle float order from Linear...
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            logger.LogInformation("Getting Adjacent Order Index for items {ExistingItemsOrder} in order {RelativeOrder}", string.Join(", ", existingItemsOrder), relativeOrder);

            var orderIndex = 0.0;

            var existingItems = existingItemsOrder
                .Select(x => double.TryParse(x, out var value) ? value : double.MaxValue)
                .Where(order => order != double.MaxValue)
                .ToList();

            if (existingItems.Count > 0)
            {
                if (relativeOrder == RelativeOrder.Above)
                {
                    var lowestOrder = existingItems.Min();
                    orderIndex = lowestOrder - 1;
                }
                else
                {
                    var highestOrder = existingItems.Max();
                    orderIndex = highestOrder + 1;
                }
            }

            return $"{orderIndex}";
        }

        public async Task<Dictionary<string, int>> GetHistoricalFeatureSize(Project project)
        {
            // Will implement later - placeholder for now
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetWorkItemsIdsForTeamWithAdditionalQuery(Team team, string additionalQuery)
        {
            // Will implement later - placeholder for now
            throw new NotImplementedException();
        }

        public async Task<bool> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            try
            {
                logger.LogInformation("Validating Linear connection");

                var viewerQuery = @"
                    query {
                        viewer {
                            id
                        }
                    }";

                var response = await SendQuery<ViewerResponse>(connection, viewerQuery);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to validate Linear connection");
                return false;
            }
        }

        public async Task<bool> ValidateProjectSettings(Project project)
        {
            // Will implement later - placeholder for now
            throw new NotImplementedException();
        }

        public async Task<bool> ValidateTeamSettings(Team team)
        {
            try
            {
                logger.LogInformation("Validating Team Settings for Team {TeamName} and Query {Query}", team.Name, team.WorkItemQuery);

                var issues = await GetIssuesForTeam(team.WorkTrackingSystemConnection, team.WorkItemQuery);
                logger.LogInformation("Found a total of {NumberOfWorkItems} Work Items for team {TeamName}", issues.Count, team.WorkItemQuery);

                return issues.Count > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Validation of Team Settings for Team {TeamName}", team.Name);
                return false;
            }
        }

        private WorkItem CreateWorkItemFromIssue(IssueNode issue, Team team)
        {
            var state = issue.State?.Name ?? UnknownStateIdentifier;

            var stateCategory = team.MapStateToStateCategory(state);

            if (issue.CompletedAt != null && issue.StartedAt == null)
            {
                issue.StartedAt = issue.CompletedAt;
            }

            var workItemBase = new WorkItemBase
            {
                ReferenceId = issue.Identifier?.ToLowerInvariant() ?? $"issue-{issue.Number}",
                Name = issue.Title,
                Type = issue.LastAppliedTemplate?.Name ?? UnknownStateIdentifier,
                State = state,
                StateCategory = stateCategory,
                Url = issue.Url,
                ParentReferenceId = issue.Parent?.Identifier ?? string.Empty,
                Order = issue.SortOrder.ToString(),
                CreatedDate = issue.CreatedAt,
                StartedDate = issue.StartedAt,
                ClosedDate = issue.CompletedAt,
            };

            return new WorkItem(workItemBase, team);
        }

        private async Task<List<IssueNode>> GetIssuesForTeam(WorkTrackingSystemConnection connection, string teamName)
        {
            var teamNode = await GetTeamByName(connection, teamName);

            if (teamNode == null)
            {
                logger.LogInformation("Team with name '{TeamName}' not found", teamName);
                return [];
            }

            var teamId = teamNode.Id;
            var teamDetails = await GetTeamDetails(connection, teamId);

            var issues = teamDetails?.Team?.Issues?.Nodes ?? [];

            // Items without template should be mapped to "Default"
            foreach (var issue in issues.Where(i => i.LastAppliedTemplate == null))
            {
                issue.LastAppliedTemplate = new TemplateNode { Id = UnknownStateIdentifier, Name = DefaultTemplateIdentifier, Type = DefaultTemplateIdentifier };
            }

            return issues;
        }

        private async Task<TeamResponse> GetTeamDetails(WorkTrackingSystemConnection connection, string teamId)
        {
            var issueQuery = $@"
                    query {{
                        team(id: ""{teamId}"") {{
                            id
                            name
                            issues {{
                                nodes {{
                                    id
                                    title
                                    identifier
                                    url
                                    number
                                    sortOrder
                                    createdAt
                                    startedAt
                                    completedAt
                                    parent {{ 
                                        id 
                                        identifier
                                    }}
                                    lastAppliedTemplate {{
                                        id
                                        name
                                        type
                                    }}
                                    state {{
                                        id
                                        name
                                    }}
                                }}
                            }}
                        }}
                    }}";

            return await SendQuery<TeamResponse>(connection, issueQuery);
        }

        private async Task<TeamNode?> GetTeamByName(WorkTrackingSystemConnection connection, string teamName)
        {
            var query = @"
                    query {
                        teams {
                            nodes {
                                id
                                name
                            }
                        }
                    }"
            ;

            var response = await SendQuery<TeamsResponse>(connection, query);

            return response?.Teams?.Nodes.FirstOrDefault(t => t.Name == teamName);
        }

        private async Task<T> SendQuery<T>(WorkTrackingSystemConnection connection, string query) where T : class
        {
            var client = GetLinearGraphQLClient(connection);

            var response = await client.SendQueryAsync<T>(query);

            foreach (var error in response.Errors ?? [])
            {
                logger.LogDebug("GraphQL Error: {ErrorMessage}", error.Message);
            }

            return response.Data;
        }

        private GraphQLHttpClient GetLinearGraphQLClient(WorkTrackingSystemConnection connection)
        {
            var encryptedApiKey = connection.GetWorkTrackingSystemConnectionOptionByKey(LinearWorkTrackingOptionNames.ApiKey);
            var apiKey = cryptoService.Decrypt(encryptedApiKey);

            var client = new HttpClient
            {
                BaseAddress = new Uri(LinearWorkTrackingOptionNames.ApiUrl)
            };
            client.DefaultRequestHeaders.Add("Authorization", apiKey);

            return new GraphQLHttpClient(new GraphQLHttpClientOptions
            {
                EndPoint = new Uri(LinearWorkTrackingOptionNames.ApiUrl)
            }, new NewtonsoftJsonSerializer(), client); ;
        }
    }
}