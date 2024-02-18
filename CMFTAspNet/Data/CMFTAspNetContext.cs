using Microsoft.EntityFrameworkCore;
using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Data
{
    public class CMFTAspNetContext : DbContext
    {
        public CMFTAspNetContext (DbContextOptions<CMFTAspNetContext> options)
            : base(options)
        {
        }

        public DbSet<Team> Team { get; set; } = default!;
    }
}
