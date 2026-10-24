using System.Collections.Concurrent;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lighthouse.Backend.Tests.TestDoubles
{
    /// <summary>
    /// Notes which threads a forecast round did its work on: every draw a simulated run takes, and every save
    /// that writes new forecasts. The draws come from the <see cref="CountedDrawStream"/> it wraps, so holding
    /// and counting runs works as it does without it.
    ///
    /// "The round runs on its own threads, never on the ones that answer pages" is a statement about threads,
    /// and these two moments are the ones a round cannot do without: it simulates and it saves what it found.
    /// Where the work runs is read off the thread itself, so the assertion holds however the round is wired.
    /// </summary>
    public sealed class ThreadsAForecastRoundRanOn(CountedDrawStream draws) : SaveChangesInterceptor, IDrawStreamFactory, IDrawStream
    {
        public readonly record struct SeenThread(int Id, bool OnTheSharedPool, bool InTheBackground, string? Name)
        {
            public override string ToString() => $"#{Id} {(OnTheSharedPool ? "shared pool" : "own thread")}{(InTheBackground ? ", background" : ", foreground")}{(Name is null ? string.Empty : $", '{Name}'")}";
        }

        private readonly ConcurrentDictionary<int, SeenThread> drewOn = new();

        private readonly ConcurrentDictionary<int, SeenThread> savedOn = new();

        public long StartingNumber => draws.StartingNumber;

        public IReadOnlyCollection<SeenThread> TheThreadsItDrewOn => [.. drewOn.Values];

        public IReadOnlyCollection<SeenThread> TheThreadsItSavedForecastsOn => [.. savedOn.Values];

        public IDrawStream ForOneRun()
        {
            draws.ForOneRun();
            return this;
        }

        public int Draw(int trial, int team, int day, int ordinal, int maxExclusive)
        {
            NoteTheCurrentThreadIn(drewOn);
            return draws.Draw(trial, team, day, ordinal, maxExclusive);
        }

        /// <summary>Forgets every thread seen so far, so a scenario reads only the round it is about.</summary>
        public void Forget()
        {
            drewOn.Clear();
            savedOn.Clear();
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            NoteTheThreadIfForecastsAreSaved(eventData);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            NoteTheThreadIfForecastsAreSaved(eventData);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void NoteTheThreadIfForecastsAreSaved(DbContextEventData eventData)
        {
            var savesNewForecasts = eventData.Context?.ChangeTracker.Entries<WhenForecast>().Any(entry => entry.State == EntityState.Added) ?? false;

            if (savesNewForecasts)
            {
                NoteTheCurrentThreadIn(savedOn);
            }
        }

        private static void NoteTheCurrentThreadIn(ConcurrentDictionary<int, SeenThread> seen)
        {
            var current = Thread.CurrentThread;
            seen.TryAdd(current.ManagedThreadId, new SeenThread(current.ManagedThreadId, current.IsThreadPoolThread, current.IsBackground, current.Name));
        }
    }
}
