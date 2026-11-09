using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Seeding
{
    /// <summary>
    /// Stores the everyone-signed-in switch off when no row exists, after an install or a restore from an older backup.
    /// An existing row is never touched: rewriting it would change its token under a System Admin's open page.
    /// </summary>
    public class EveryoneSignedInSeeder(LighthouseAppContext context, ILogger<EveryoneSignedInSeeder> logger) : ISeeder
    {
        public async Task Seed()
        {
            if (await context.EveryoneSignedInSettings.AnyAsync())
            {
                return;
            }

            context.EveryoneSignedInSettings.Add(EveryoneSignedInSetting.Off());
            await context.SaveChangesAsync();
            logger.LogInformation("Stored everyone signed in as off");
        }
    }
}
