using ModelContextProtocol.Protocol;

namespace Lighthouse.Backend.MCP
{
    public sealed class LighthouseResources(IServiceScopeFactory serviceScopeFactory)
        : LighthouseToolsBase(serviceScopeFactory), IDisposable
    {
        private readonly HttpClient httpClient = new();

        private const string BaseUri = "https://docs.lighthouse.letpeople.work";
        private const string HtmlMimeType = "text/html";
        
        public ValueTask<ListResourcesResult> ListDocumentationResources()
        {

            var resources = new List<Resource>
            {
                new Resource
                {
                    Uri = BaseUri + "/",
                    Name = "Lighthouse Documentation",
                    Description = "Main Lighthouse documentation homepage",
                    MimeType = HtmlMimeType
                },
                new Resource
                {
                    Uri = BaseUri + "/concepts/concepts.html",
                    Name = "Concepts",
                    Description = "Core concepts and components of Lighthouse",
                    MimeType = HtmlMimeType
                },
                new Resource
                {
                    Uri = BaseUri + "/teams/teams.html",
                    Name = "Teams",
                    Description = "How to work with teams in Lighthouse",
                    MimeType = HtmlMimeType
                },
                new Resource
                {
                    Uri = BaseUri + "/projects/projects.html",
                    Name = "Projects",
                    Description = "How to work with projects in Lighthouse",
                    MimeType = HtmlMimeType
                },
                new Resource
                {
                    Uri = BaseUri + "/metrics/metrics.html",
                    Name = "Metrics",
                    Description = "Understanding and working with Lighthouse metrics",
                    MimeType = HtmlMimeType
                },
                new Resource
                {
                    Uri = BaseUri + "/aiintegration.html",
                    Name = "AI Integration",
                    Description = "How to integrate Lighthouse with AI systems including MCP",
                    MimeType = HtmlMimeType
                },
                new Resource
                {
                    Uri = BaseUri + "/concepts/howlighthouseforecasts.html",
                    Name = "How Lighthouse Forecasts",
                    Description = "Detailed explanation of Monte Carlo simulations and forecasting in Lighthouse",
                    MimeType = HtmlMimeType
                }
            };

            return ValueTask.FromResult(new ListResourcesResult { Resources = resources });
        }

        public async ValueTask<ReadResourceResult> ReadDocumentationResource(string uri, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await httpClient.GetAsync(uri, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Resource not found: {uri}");
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // Find resource metadata from our list
                var resourceMetadata = await ListDocumentationResources();
                var resource = resourceMetadata.Resources.FirstOrDefault(r => r.Uri == uri);

                var resourceContent = new BlobResourceContents
                {
                    Uri = uri,
                    MimeType = resource?.MimeType ?? "text/html",
                    Blob = System.Text.Encoding.UTF8.GetBytes(content)
                };

                return new ReadResourceResult
                {
                    Contents = [resourceContent]
                };
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException($"Resource not found: {uri}");
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
