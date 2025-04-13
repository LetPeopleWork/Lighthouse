using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using static Lighthouse.Backend.API.ForecastController;

namespace Lighthouse.Backend.MCP
{
    [McpServerToolType]
    public sealed class LightouseTeamTools
    {
        private readonly HttpClient httpClient;

        public LightouseTeamTools(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            httpClient = httpClientFactory.CreateClient();

            // Trust untrusted SSL certificates
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            httpClient = new HttpClient(handler);

            // Set the base address based on configuration
            string baseAddress = "/api/";

            // Try to get the URL from Kestrel configuration
            string? httpsUrl = configuration["Kestrel:Endpoints:Https:Url"];
            string? httpUrl = configuration["Kestrel:Endpoints:Http:Url"];

            // Prefer HTTPS URL if available
            if (!string.IsNullOrEmpty(httpsUrl))
            {
                // Format the URL properly - remove the wildcard and add api prefix
                baseAddress = httpsUrl.Replace("*", "localhost") + "/api/";
            }
            else if (!string.IsNullOrEmpty(httpUrl))
            {
                // Format the URL properly - remove the wildcard and add api prefix
                baseAddress = httpUrl.Replace("*", "localhost") + "/api/";
            }

            // Custom API base URL override if specified
            if (!string.IsNullOrEmpty(configuration["Api:BaseUrl"]))
            {
                baseAddress = configuration["Api:BaseUrl"];
            }

            // Ensure baseAddress is not null before creating a Uri
            if (!string.IsNullOrEmpty(baseAddress))
            {
                httpClient.BaseAddress = new Uri(baseAddress, UriKind.RelativeOrAbsolute);
            }
        }

        [McpServerTool, Description("Get a list of teams.")]
        public async Task<string> GetTeams()
        {
            var response = await httpClient.GetAsync("teams");
            return await response.Content.ReadAsStringAsync();
        }

        [McpServerTool, Description("Get a team by ID.")]
        public async Task<string> GetTeamById(int id)
        {
            var response = await httpClient.GetAsync($"teams/{id}");
            return await response.Content.ReadAsStringAsync();
        }

        [McpServerTool, Description("Run a Forecast for a team")]
        public async Task<string> RunManualForecastForTeam(int teamId, DateTime? untilWhen, int remainingItems = 0)
        {
            var manulForecastInputDto = new ManualForecastInputDto
            {
                RemainingItems = remainingItems,
                TargetDate = untilWhen ?? DateTime.MinValue
            };

            var content = new StringContent(JsonSerializer.Serialize(manulForecastInputDto), System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"forecast/manual/{teamId}", content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
