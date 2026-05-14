using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.OAuth;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class OAuthCredentialRepository(LighthouseAppContext context, ILogger<OAuthCredentialRepository> logger)
        : RepositoryBase<OAuthCredential>(context, lighthouseAppContext => lighthouseAppContext.OAuthCredentials, logger);
}
