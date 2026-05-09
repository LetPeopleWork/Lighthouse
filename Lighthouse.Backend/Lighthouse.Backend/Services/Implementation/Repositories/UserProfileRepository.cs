using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class UserProfileRepository(LighthouseAppContext context, ILogger<UserProfileRepository> logger)
            : RepositoryBase<UserProfile>(context, ctx => ctx.UserProfiles, logger)
    {
    }
}