using Microsoft.EntityFrameworkCore;
using CMFTAspNet.Models;
using CMFTAspNet.Models.Forecast;

namespace CMFTAspNet.Data
{
    public class AppContext : DbContext
    {
        public AppContext(DbContextOptions<AppContext> options)
            : base(options)
        {
        }

        public DbSet<Team> Teams { get; set; } = default!;

        public DbSet<Feature> Features { get; set; } = default!;

        public DbSet<Project> Projects { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RemainingWork>()
                .HasOne(r => r.Feature)
                .WithMany(f => f.RemainingWork)
                .HasForeignKey(r => r.FeatureId)
                .IsRequired();

            modelBuilder.Entity<RemainingWork>()
                .HasOne(r => r.Feature)
                .WithMany(f => f.RemainingWork)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feature>()
                .HasOne(f => f.Project)
                .WithMany(p => p.Features)
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feature>()
                .HasOne(f => f.Forecast)
                .WithOne(wf => wf.Feature)
                .HasForeignKey<WhenForecast>(wf => wf.FeatureId);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.InvolvedTeams)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            Database.Migrate();
        }
    }
}
