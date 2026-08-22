using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using System.Globalization;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public abstract class UpdateServiceBase<TEntity>(
        ILogger<UpdateServiceBase<TEntity>> logger,
        IServiceScopeFactory serviceScopeFactory,
        IUpdateQueueService updateQueueService,
        UpdateType updateType)
        : BackgroundService, IUpdateService
        where TEntity : class, IEntity
    {
        protected ILogger<UpdateServiceBase<TEntity>> Logger { get; } = logger;

        public virtual void TriggerUpdate(int id)
        {
            updateQueueService.EnqueueUpdate(updateType, id, async serviceProvider =>
            {
                try
                {
                    await Update(id, serviceProvider);
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "An exception occurred while updating {Entity} with ID {Id}: {Exception}", typeof(TEntity).Name, id, exception.Message);
                }
                finally
                {
                    await FlushWriteBack(id, serviceProvider);
                    WriteSummaryIfTheRoundIsOver(serviceProvider);
                }
            });
        }

        /// <summary>
        /// The one place write-back reaches the tracker. Every update type inherits it, so no updater can
        /// forget it and the ordering contract stays in the updater's own method body. It runs after every
        /// execution; whether anything is actually sent depends on whether the refresh round this
        /// execution belongs to still has work to come.
        /// </summary>
        private async Task FlushWriteBack(int id, IServiceProvider serviceProvider)
        {
            try
            {
                await serviceProvider.GetRequiredService<IWriteBackCollector>().FlushAsync();
            }
            // Parity with the swallow-and-log write-back has always had: a flush failure must not fail
            // the refresh round it rode in on.
#pragma warning disable CA1031
            catch (Exception exception)
#pragma warning restore CA1031
            {
                Logger.LogError(exception, "Write-back flush failed for {Entity} with ID {Id}: {Exception}", typeof(TEntity).Name, id, exception.Message);
            }
        }

        /// <summary>
        /// What this update did. Every update type inherits it, so no updater can forget to say what
        /// happened; the line itself is written once the whole refresh round is over, because a round can
        /// be more than one execution and an operator should read one line about it rather than one each.
        /// </summary>
        protected void ReportUpdateSummary(IServiceProvider serviceProvider, string entityName, SyncOutcome outcome, long durationMs, bool success)
        {
            var summary = new RefreshRoundSummary(typeof(TEntity).Name, entityName, durationMs, success) { Outcome = outcome };
            var round = RoundOf(serviceProvider);

            if (round == null)
            {
                WriteSummary(summary);
                return;
            }

            round.ReportRefresh(summary);
        }

        /// <summary>
        /// What the forecast of this round did. It reads what an earlier refresh already fetched, so the
        /// time it took is all it has to add to the round's line.
        /// </summary>
        protected void ReportForecastSummary(IServiceProvider serviceProvider, string entityName, long durationMs, bool success)
        {
            var summary = new RefreshRoundSummary(typeof(TEntity).Name, entityName, durationMs, success)
            {
                ForecastDurationMs = durationMs,
                ForecastSucceeded = success,
            };

            var round = RoundOf(serviceProvider);

            if (round == null)
            {
                WriteSummary(summary);
                return;
            }

            round.ReportForecast(summary);
        }

        private static WriteBackRound? RoundOf(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<WriteBackRoundContext>()?.Current;
        }

        /// <summary>
        /// The last execution out of a round is the one that owes it its line, the same way it owes it
        /// its write. Taking the summary empties it, so a round says its piece once.
        /// </summary>
        private void WriteSummaryIfTheRoundIsOver(IServiceProvider serviceProvider)
        {
            var round = RoundOf(serviceProvider);

            if (round is not { HasFinished: true })
            {
                return;
            }

            if (round.TakeSummary() is { } summary)
            {
                WriteSummary(summary);
            }
        }

        /// <summary>
        /// The one line an operator reads per refresh round. The reason and the forecast are appended
        /// whole or left out entirely - a cycle with nothing to explain would otherwise carry an empty
        /// <c>reason=</c> on every line, and this line exists precisely because the logs were too noisy
        /// to read.
        /// </summary>
        private void WriteSummary(RefreshRoundSummary summary)
        {
            var forecast = DescribeForecast(summary);

            if (summary.Outcome is not { } outcome)
            {
                Logger.LogInformation(
                    "Update completed | {EntityType:l} '{EntityName:l}'{Forecast:l} | duration={DurationMs}ms | success={Success}",
                    summary.EntityType, summary.EntityName, forecast, summary.DurationMs, summary.Success);
                return;
            }

            var reason = outcome.Reason == null ? string.Empty : $" | reason={outcome.Reason}";

            Logger.LogInformation(
                "Update completed | {EntityType:l} '{EntityName:l}' | mode={Mode} | scanned={RecordsScanned} | fetched={RecordsFetched} | duration={DurationMs}ms | success={Success}{Forecast:l}{Reason:l}",
                summary.EntityType, summary.EntityName, outcome.Mode, outcome.RecordsScanned, outcome.RecordsFetched, summary.DurationMs, summary.Success, forecast, reason);
        }

        private static string DescribeForecast(RefreshRoundSummary summary)
        {
            if (summary.ForecastDurationMs is not { } forecastMs)
            {
                return string.Empty;
            }

            return summary.ForecastSucceeded
                ? string.Create(CultureInfo.InvariantCulture, $" | forecast={forecastMs}ms")
                : string.Create(CultureInfo.InvariantCulture, $" | forecast=failed after {forecastMs}ms");
        }

        /// <summary>
        /// What an operator reads when a refresh stopped because this instance could not decrypt a credential
        /// it had stored. Without the connection and the field, the only plausible reading is that the work
        /// tracking system rejected a token - and that sends someone off to reissue a credential that was
        /// never the problem. The field is found by asking the same total reader the connection screen asks,
        /// so the two surfaces can never name different fields for the same connection.
        /// </summary>
        protected static string BuildUnreadableSecretReason(
            UnreadableSecretException exception,
            WorkTrackingSystemConnection connection,
            ICryptoService cryptoService)
        {
            var unreadableFields = ConnectionSecrets.FieldsThatCannotBeRead(connection, cryptoService).ToList();

            var subject = unreadableFields.Count > 0
                ? $"The stored {string.Join(", ", unreadableFields)}"
                : "A stored credential";

            var namedKey = string.IsNullOrEmpty(exception.ClaimedKeyId)
                ? string.Empty
                : $" (it names encryption key '{exception.ClaimedKeyId}')";

            return $"{subject} on connection '{connection.Name}' cannot be read with the current encryption key{namedKey}, " +
                "so this refresh stopped before contacting the work tracking system. " +
                "Enter the credential again to store it under the key this instance uses now.";
        }

        protected static T GetServiceFromServiceScope<T>(IServiceScope scope) where T : notnull
        {
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        protected IServiceScope CreateServiceScope()
        {
            return serviceScopeFactory.CreateScope();
        }

        protected abstract RefreshSettings GetRefreshSettings();

        protected abstract Task Update(int id, IServiceProvider serviceProvider);

        protected abstract bool ShouldUpdateEntity(TEntity entity, RefreshSettings refreshSettings);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Start Executing Background Service");

            await DelayStart(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await TryUpdating(stoppingToken);
            }

            Logger.LogInformation("Stopping Executing Background Service");
        }

        private async Task TryUpdating(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("Starting Update for {UpdateType}", updateType.ToString());
                UpdateAll();

                var refreshSettings = GetRefreshSettings();

                Logger.LogInformation("Done Updating {UpdateType} - Waiting {Interval} Minutes till next execution", updateType.ToString(), refreshSettings.Interval);
                await Task.Delay(TimeSpan.FromMinutes(refreshSettings.Interval), stoppingToken);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "An exception occured: {Exception}.", exception);
            }
        }

        private void UpdateAll()
        {
            using var scope = CreateServiceScope();
            var repository = GetServiceFromServiceScope<IRepository<TEntity>>(scope);
            var refreshSettings = GetRefreshSettings();

            foreach (var entity in repository.GetAll().ToList())
            {
                Logger.LogDebug("Checking last update for {Entity}", entity.Id);
                if (ShouldUpdateEntity(entity, refreshSettings))
                {
                    TriggerUpdate(entity.Id);
                }
            }
        }

        private async Task DelayStart(CancellationToken stoppingToken)
        {
            var refreshSettings = GetRefreshSettings();

            Logger.LogInformation("Wait {StartDelay} minutes before starting...", refreshSettings.StartDelay);
            await Task.Delay(TimeSpan.FromMinutes(refreshSettings.StartDelay), stoppingToken);
        }
    }
}
