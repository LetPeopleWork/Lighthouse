using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Authorization;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class ApiKeyPermissionRepository(LighthouseAppContext context, ILogger<ApiKeyPermissionRepository> logger)
        : RepositoryBase<ApiKeyPermission>(context, ctx => ctx.ApiKeyPermissions, logger)
    {
    }
}
