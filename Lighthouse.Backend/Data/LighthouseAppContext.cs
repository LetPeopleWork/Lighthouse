using Microsoft.EntityFrameworkCore;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Data
{
    public class LighthouseAppContext : DbContext
    {
        private readonly ILogger<LighthouseAppContext> logger;
        private readonly ICryptoService cryptoService;

        public LighthouseAppContext(DbContextOptions<LighthouseAppContext> options, ILogger<LighthouseAppContext> logger, ICryptoService cryptoService)
            : base(options)
        {
            this.logger = logger;
            this.cryptoService = cryptoService;
        }

        public DbSet<Team> Teams { get; set; } = default!;

        public DbSet<Feature> Features { get; set; } = default!;

        public DbSet<Project> Projects { get; set; } = default!;

        public DbSet<WorkTrackingSystemConnection> WorkTrackingSystemConnections { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Milestone>()
                .HasOne(m => m.Project)
                .WithMany(p => p.Milestones)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RemainingWork>()
                .HasOne(rw => rw.Team)
                .WithMany()
                .HasForeignKey(rw => rw.TeamId);

            modelBuilder.Entity<RemainingWork>()
                .HasOne(rw => rw.Feature)
                .WithMany(f => f.RemainingWork)
                .HasForeignKey(rw => rw.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feature>()
                .HasOne(f => f.Forecast)
                .WithOne(wf => wf.Feature)
                .HasForeignKey<WhenForecast>(wf => wf.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<IndividualSimulationResult>()
                .HasOne(isr => isr.Forecast)
                .WithMany(f => f.SimulationResults)
                .HasForeignKey(isr => isr.ForecastId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feature>()
                .HasMany(f => f.Projects)
                .WithMany(p => p.Features);

            modelBuilder.Entity<WorkTrackingSystemConnection>(entity =>
            {
                entity.HasMany(e => e.Options)
                      .WithOne(option => option.WorkTrackingSystemConnection)
                      .HasForeignKey(option => option.WorkTrackingSystemConnectionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            logger.LogInformation("Migrating Database");
            Database.Migrate();
            logger.LogInformation("Migration of Database succeeded");
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
            foreach (var entry in ChangeTracker.Entries<WorkTrackingSystemConnectionOption>().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                var option = entry.Entity;
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
                .Where(f => !f.Projects.Any())
                .ToList();

            if (orphanedFeatures.Any())
            {
                Features.RemoveRange(orphanedFeatures);
            }
        }
    }
}