using Microsoft.EntityFrameworkCore;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Models.OptionalFeatures;

namespace Lighthouse.Backend.Data
{
    public partial class LighthouseAppContext(
        DbContextOptions<LighthouseAppContext> options,
        ICryptoService cryptoService,
        ILogger<LighthouseAppContext> logger)
        : DbContext(options)
    {
        public DbSet<Team> Teams { get; set; } = null!;

        public DbSet<Feature> Features { get; set; } = null!;

        public DbSet<Portfolio> Portfolios { get; set; } = null!;

        public DbSet<WorkTrackingSystemConnection> WorkTrackingSystemConnections { get; set; } = null!;

        public DbSet<AppSetting> AppSettings { get; set; } = null!;

        public DbSet<OptionalFeature> OptionalFeatures { get; set; } = null!;

        public DbSet<WorkItem> WorkItems { get; set; } = null!;

        public DbSet<TerminologyEntry> TerminologyEntries { get; set; } = null!;

        public DbSet<LicenseInformation> LicenseInformation { get; set; } = null!;

        public DbSet<Delivery> Deliveries { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppSetting>().HasKey(a => a.Key);
            modelBuilder.Entity<OptionalFeature>().HasKey(a => a.Key);
            modelBuilder.Entity<LicenseInformation>().HasKey(li => li.Id);
            
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
                .HasMany(f => f.Portfolios)
                .WithMany(p => p.Features);

            modelBuilder.Entity<Team>()
                .HasMany(t => t.Portfolios)
                .WithMany(p => p.Teams);

            modelBuilder.Entity<Team>()
                .HasMany(t => t.WorkItems)
                .WithOne(wi => wi.Team);

            modelBuilder.Entity<Portfolio>()
                .HasOne(p => p.OwningTeam)
                .WithMany()
                .HasForeignKey(p => p.OwningTeamId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkTrackingSystemConnection>(entity =>
            {
                entity.HasMany(e => e.Options)
                      .WithOne(option => option.WorkTrackingSystemConnection)
                      .HasForeignKey(option => option.WorkTrackingSystemConnectionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.Portfolio)
                .WithMany()
                .HasForeignKey(d => d.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Delivery>()
                .HasMany(d => d.Features)
                .WithMany();
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
    
            // Only look at the change tracker, don't query the database
            var orphanedFeatures = ChangeTracker.Entries<Feature>()
                .Where(e => e.State != EntityState.Deleted && e.State != EntityState.Detached)
                .Select(e => e.Entity)
                .Where(f => !f.IsParentFeature && f.Portfolios.Count == 0)
                .ToList();

            if (orphanedFeatures.Count != 0)
            {
                Features.RemoveRange(orphanedFeatures);
                LogRemovedOrphanedFeatures(orphanedFeatures.Count);
            }
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Removed {count} orphaned features")]
        private partial void LogRemovedOrphanedFeatures(int count);
    }
}
