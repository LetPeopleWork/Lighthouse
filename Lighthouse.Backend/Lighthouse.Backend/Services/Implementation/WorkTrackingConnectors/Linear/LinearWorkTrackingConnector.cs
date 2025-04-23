using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear
{
    public partial class LinearWorkTrackingConnector : IWorkTrackingConnector
    {
        private readonly ILogger<LinearWorkTrackingConnector> logger;
        private readonly ICryptoService cryptoService;

        public LinearWorkTrackingConnector(ILogger<LinearWorkTrackingConnector> logger, ICryptoService cryptoService)
        {
            this.logger = logger;
            this.cryptoService = cryptoService;
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            // Will implement later - placeholder for now
            throw new NotImplementedException();
        }

        public async Task<Dictionary<string, int>> GetHistoricalFeatureSize(Project project)
        {
            // Will implement later - placeholder for now
            throw new NotImplementedException();
        }

        public async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            // Will implement later - placeholder for now
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
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

                var query = @"
                    query {
                        viewer {
                            id
                        }
                    }";

                var response = await SendQuery<LinearResponses.ViewerResponse>(connection, query);
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

                var query = @"
                    query {
                        teams {
                            nodes {
                                id
                                name
                            }
                        }
                    }";

                var response = await SendQuery<LinearResponses.TeamsResponse>(team.WorkTrackingSystemConnection, query);

                if (response?.Teams?.Nodes == null)
                {
                    logger.LogError("Failed to get teams from Linear API");
                    return false;
                }

                var teamNode = response.Teams.Nodes.FirstOrDefault(t => t.Name == team.WorkItemQuery);

                if (teamNode == null)
                {
                    logger.LogInformation("Team with name '{TeamName}' not found in Linear", team.WorkItemQuery);
                    return false;
                }

                var teamId = teamNode.Id;

                var issueQuery = $@"
                    query {{
                        team(id: ""{teamId}"") {{
                            id
                            name
                            issues {{
                                nodes {{
                                    id
                                    title
                                    state {{
                                        id
                                        name
                                      }}
                                }}
                            }}
                        }}
                    }}";

                var issueResponse = await SendQuery<LinearResponses.TeamResponse>(team.WorkTrackingSystemConnection, issueQuery);

                var issueCount = issueResponse.Team.Issues.Nodes.Count;
                logger.LogInformation("Found a total of {NumberOfWorkItems} Work Items for team {TeamName}", issueCount, team.WorkItemQuery);

                return issueCount > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Validation of Team Settings for Team {TeamName}", team.Name);
                return false;
            }
        }

        private async Task<T> SendQuery<T>(WorkTrackingSystemConnection connection, string query) where T : class
        {
            var client = GetLinearGraphQLClient(connection);

            var response = await client.SendQueryAsync<T>(query);
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