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
    public class LinearWorkTrackingConnector : IWorkTrackingConnector
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

                var client = GetLinearGraphQLClient(connection);
                var query = @"
                    query {
                        viewer {
                            id
                        }
                    }";

                var response = await client.SendQueryAsync<ViewerResponse>(query);
                return response.Errors == null || response.Errors.Length == 0;
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
            // Will implement later - placeholder for now
            throw new NotImplementedException();
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


        public class ViewerResponse
        {
            public Viewer viewer { get; set; }
        }

        public class Viewer
        {
            public string id { get; set; }
        }
    }
}