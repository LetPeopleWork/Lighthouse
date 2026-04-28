using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class ApiKeyRepository(LighthouseAppContext context, ILogger<ApiKeyRepository> logger)
        : RepositoryBase<ApiKey>(context, ctx => ctx.ApiKeys, logger), IApiKeyRepository
    {
    }
}
