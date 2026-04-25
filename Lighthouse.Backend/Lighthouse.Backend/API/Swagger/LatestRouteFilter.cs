using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lighthouse.Backend.API.Swagger
{
    public class LatestRouteFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var latestPaths = swaggerDoc.Paths.Keys
                .Where(p => p.StartsWith("/api/latest/"))
                .ToList();

            foreach (var path in latestPaths)
            {
                swaggerDoc.Paths.Remove(path);
            }
        }
    }
}