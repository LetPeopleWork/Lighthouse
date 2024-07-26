using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class AppSettingRepository : RepositoryBase<AppSetting>
    {
        public AppSettingRepository(LighthouseAppContext context, ILogger<RepositoryBase<AppSetting>> logger) : base(context, (LighthouseAppContext context) => context.AppSettings, logger)
        {
        }
    }
}
