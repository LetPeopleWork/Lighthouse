using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IPortfolioRepository : IRepository<Portfolio>
    {
        /// <summary>
        /// The ids of the portfolios the given team does work for, and nothing else about them. Reading a
        /// portfolio brings every feature, forecast and simulation result with it, which is far more reading
        /// than a refresh trigger needs to decide which portfolios its team just made stale.
        /// </summary>
        IReadOnlyCollection<int> GetPortfolioIdsForTeam(int teamId);

        /// <summary>
        /// The ids of the portfolios any of the given teams does work for, each once, in one read.
        /// </summary>
        IReadOnlyCollection<int> GetPortfolioIdsForTeams(IReadOnlyCollection<int> teamIds);

        /// <summary>
        /// Records the inputs a forecast was computed from, but only if nobody changed the stored value since
        /// that forecast read it. Answers whether the value was recorded: a person who asked for a fresh
        /// forecast while this one ran has cleared it, and that request has to win.
        /// </summary>
        Task<bool> TryRecordForecastInputs(int portfolioId, string? readAtGather, string fresh, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces the stored inputs with a mark of this run, new on every call and never equal to any inputs,
        /// but only if nobody changed the stored value since the run read it. Taken just before a computed
        /// forecast is saved: until the run swaps the mark for what it computed from, no round can match it,
        /// so a run that fails or dies after saving leaves the next one to compute. Answers the mark, or null
        /// when the value had already changed.
        /// </summary>
        Task<string?> TryMarkForecastRunning(int portfolioId, string? readAtGather, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces the recorded inputs of each of these Portfolios with a value no forecast ever computed from,
        /// new on every call, so the next forecast of each one computes in full. A forecast already running read
        /// the value from before the call, nothing included, so it can no longer record its own.
        /// </summary>
        Task ForgetForecastInputs(IReadOnlyCollection<int> portfolioIds, CancellationToken cancellationToken);

        /// <summary>
        /// Forgets the recorded inputs of every Portfolio, in one statement. Run when the app starts, because
        /// another build may have computed forecasts in between without recording what it computed them from.
        /// </summary>
        Task ForgetAllForecastInputs(CancellationToken cancellationToken);
    }
}
