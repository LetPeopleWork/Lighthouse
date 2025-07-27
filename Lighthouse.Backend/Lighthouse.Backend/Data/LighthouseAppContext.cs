using Microsoft.EntityFrameworkCore;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Models.History;
using Lighthouse.Backend.Models.OptionalFeatures;

namespace Lighthouse.Backend.Data
{
    public class LighthouseAppContext : DbContext
    {
        private readonly ICryptoService cryptoService;
        private readonly ILogger<LighthouseAppContext> logger;

        public LighthouseAppContext(DbContextOptions<LighthouseAppContext> options, ICryptoService cryptoService, ILogger<LighthouseAppContext> logger)
            : base(options)
        {
            this.cryptoService = cryptoService;
            this.logger = logger;
        }

        public DbSet<Team> Teams { get; set; } = default!;

        public DbSet<Feature> Features { get; set; } = default!;

        public DbSet<Project> Projects { get; set; } = default!;

        public DbSet<FeatureHistoryEntry> FeatureHistory { get; set; } = default!;

        public DbSet<WorkTrackingSystemConnection> WorkTrackingSystemConnections { get; set; } = default!;

        public DbSet<AppSetting> AppSettings { get; set; } = default!;

        public DbSet<OptionalFeature> OptionalFeatures { get; set; } = default!;

        public DbSet<WorkItem> WorkItems { get; set; } = default!;

        public DbSet<TerminologyEntry> TerminologyEntries { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppSetting>().HasKey(a => a.Key);
            modelBuilder.Entity<OptionalFeature>().HasKey(a => a.Key);
            
            modelBuilder.Entity<TerminologyEntry>().HasKey(t => t.Id);
            modelBuilder.Entity<TerminologyEntry>()
                .Property(t => t.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<TerminologyEntry>()
                .Property(t => t.Key)
                .IsRequired();
            modelBuilder.Entity<TerminologyEntry>()
                .HasIndex(t => t.Key)
                .IsUnique();
            modelBuilder.Entity<TerminologyEntry>()
                .Property(t => t.Description)
                .IsRequired();
            modelBuilder.Entity<TerminologyEntry>()
                .Property(t => t.DefaultValue)
                .IsRequired();

            modelBuilder.Entity<Milestone>()
                .HasOne(m => m.Project)
                .WithMany(p => p.Milestones)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeatureWork>()
                .HasOne(fw => fw.Team)
                .WithMany()
                .HasForeignKey(fw => fw.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeatureWork>()
                .HasOne(fw => fw.Feature)
                .WithMany(f => f.FeatureWork)
                .HasForeignKey(fw => fw.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feature>()
                .HasMany(f => f.Forecasts)
                .WithOne(wf => wf.Feature)
                .HasForeignKey(wf => wf.FeatureId)
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

            modelBuilder.Entity<Team>()
                .HasMany(t => t.WorkItems)
                .WithOne(wi => wi.Team);

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
            logger.LogDebug("Saving Changes");
            PreprocessDataBeforeSave();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            logger.LogDebug("Saving Changes Async");
            PreprocessDataBeforeSave();
            return await SaveWithRetry(cancellationToken);
        }

        private async Task<int> SaveWithRetry(CancellationToken cancellationToken = default)
        {
            const int maxRetryCount = 3;
            int retryCount = 0;

            while (true)
            {
                try
                {
                    logger.LogDebug("Attempting to save changes, attempt {RetryCount}", retryCount + 1);
                    return await base.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex) when (retryCount < maxRetryCount)
                {
                    retryCount++;
                    logger.LogWarning(ex, "Concurrency exception occurred, retrying {RetryCount}/{MaxRetryCount}", retryCount, maxRetryCount);
                    foreach (var entry in ex.Entries)
                    {
                        // Refresh the original values to reflect the current values in the database
                        await entry.ReloadAsync(cancellationToken);
                    }
                }
            }
        }

        private void PreprocessDataBeforeSave()
        {
            logger.LogDebug("Preprocessing data before save");
            RemoveOrphanedFeatures();
            EncryptSecrets();
        }

        private void EncryptSecrets()
        {
            logger.LogDebug("Encrypting secrets");
            foreach (var option in ChangeTracker.Entries<WorkTrackingSystemConnectionOption>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity))
            {
                if (option.IsSecret)
                {
                    option.Value = cryptoService.Encrypt(option.Value);
                    logger.LogDebug("Encrypted secret for option {OptionId}", option.Id);
                }
            }
        }

        private void RemoveOrphanedFeatures()
        {
            logger.LogDebug("Removing orphaned features");
            var orphanedFeatures = Features
                .Where(f => !f.IsParentFeature)
                .Include(f => f.Projects)
                .Where(f => f.Projects.Count == 0)
                .ToList();

            if (orphanedFeatures.Count > 0)
            {
                Features.RemoveRange(orphanedFeatures);
                logger.LogInformation("Removed {OrphanedFeatureCount} orphaned features", orphanedFeatures.Count);
            }
        }
    }
}
