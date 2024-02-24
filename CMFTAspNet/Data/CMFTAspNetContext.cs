using Microsoft.EntityFrameworkCore;
using CMFTAspNet.Models;
using CMFTAspNet.Models.Forecast;

namespace CMFTAspNet.Data
{
    public class CMFTAspNetContext : DbContext
    {
        public CMFTAspNetContext(DbContextOptions<CMFTAspNetContext> options)
            : base(options)
        {
        }

        public DbSet<Team> Teams { get; set; } = default!;

        public DbSet<Feature> Features { get; set; } = default!;

        public DbSet<Project> Projects { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RemainingWork>()
                .HasOne(r => r.Feature)         // One RemainingWork is associated with one Feature
                .WithMany(f => f.RemainingWork) // One Feature can have many RemainingWork
                .HasForeignKey(r => r.FeatureId)
                .IsRequired();

            modelBuilder.Entity<Feature>()
                .HasOne(f => f.Forecast)
                .WithOne(wf => wf.Feature)
                .HasForeignKey<WhenForecast>(wf => wf.FeatureId);

            // Optionally, configure cascade delete behavior
            modelBuilder.Entity<Feature>()
                .HasMany(f => f.RemainingWork)
                .WithOne(r => r.Feature)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
