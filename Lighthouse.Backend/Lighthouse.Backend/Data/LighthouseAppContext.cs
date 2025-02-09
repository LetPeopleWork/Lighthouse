using Microsoft.EntityFrameworkCore;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Models.History;
using Lighthouse.Backend.Models.Preview;

namespace Lighthouse.Backend.Data
{
    public class LighthouseAppContext : DbContext
    {
        private readonly ICryptoService cryptoService;

        public LighthouseAppContext(DbContextOptions<LighthouseAppContext> options, ICryptoService cryptoService)
            : base(options)
        {
            this.cryptoService = cryptoService;
        }

        public DbSet<Team> Teams { get; set; } = default!;

        public DbSet<Feature> Features { get; set; } = default!;

        public DbSet<Project> Projects { get; set; } = default!;

        public DbSet<FeatureHistoryEntry> FeatureHistory { get; set; } = default!;

        public DbSet<WorkTrackingSystemConnection> WorkTrackingSystemConnections { get; set; } = default!;

        public DbSet<AppSetting> AppSettings { get; set; } = default!;

        public DbSet<PreviewFeature> PreviewFeatures { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppSetting>().HasKey(a => a.Key);

            modelBuilder.Entity<PreviewFeature>().HasKey(a => a.Key);

            modelBuilder.Entity<Milestone>()
                .HasOne(m => m.Project)
                .WithMany(p => p.Milestones)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeatureWork>()
                .HasOne(rw => rw.Team)
                .WithMany()
                .HasForeignKey(rw => rw.TeamId);

            modelBuilder.Entity<FeatureWork>()
                .HasOne(rw => rw.Feature)
                .WithMany(f => f.FeatureWork)
                .HasForeignKey(rw => rw.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feature>()
                .HasMany(f => f.Forecasts)
                .WithOne(wf => wf.Feature)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<IndividualSimulationResult>()
                .HasOne(isr => isr.Forecast)
                .WithMany(f => f.SimulationResults)
                .HasForeignKey(isr => isr.ForecastId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feature>()
                .HasMany(f => f.Projects)
                .WithMany(p => p.Features);

            modelBuilder.Entity<Team>()
                .HasMany(t => t.Projects)
                .WithMany(p => p.Teams);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.OwningTeam)
                .WithMany()
                .HasForeignKey(p => p.OwningTeamId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FeatureHistoryEntry>()
                .HasMany(f => f.Forecasts)
                .WithOne(wf => wf.FeatureHistoryEntry)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeatureWorkHistoryEntry>()
                .HasOne(rw => rw.FeatureHistoryEntry)
                .WithMany(f => f.FeatureWork)
                .HasForeignKey(rw => rw.FeatureHistoryEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkTrackingSystemConnection>(entity =>
            {
                entity.HasMany(e => e.Options)
                      .WithOne(option => option.WorkTrackingSystemConnection)
                      .HasForeignKey(option => option.WorkTrackingSystemConnectionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public override int SaveChanges()
        {
            PreprocessDataBeforeSave();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            PreprocessDataBeforeSave();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void PreprocessDataBeforeSave()
        {
            RemoveOrphanedFeatures();
            EncryptSecrets();
        }

        private void EncryptSecrets()
        {
            foreach (var option in ChangeTracker.Entries<WorkTrackingSystemConnectionOption>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity))
            {
                if (option.IsSecret)
                {
                    option.Value = cryptoService.Encrypt(option.Value);
                }
            }
        }

        private void RemoveOrphanedFeatures()
        {
            var orphanedFeatures = Features
                .Include(f => f.Projects)
                .Where(f => f.Projects.Count == 0)
                .ToList();

            if (orphanedFeatures.Count > 0)
            {
                Features.RemoveRange(orphanedFeatures);
            }
        }
    }
}