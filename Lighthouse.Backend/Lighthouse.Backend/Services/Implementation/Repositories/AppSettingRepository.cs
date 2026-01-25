using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class AppSettingRepository(LighthouseAppContext context, ILogger<AppSettingRepository> logger)
        : RepositoryBase<AppSetting>(context, lighthouseAppContext => lighthouseAppContext.AppSettings, logger);
}
