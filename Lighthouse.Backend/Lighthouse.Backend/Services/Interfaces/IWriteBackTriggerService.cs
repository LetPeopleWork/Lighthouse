using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Resolves write-back intents from mappings and entities. A resolver, not a writer: it returns a
    /// plan and performs no I/O, so "did this write?" is answerable from the signature (ADR-144 D1).
    /// Staging and flushing belong to <see cref="IWriteBackCollector"/>.
    /// </summary>
    public interface IWriteBackTriggerService
    {
        IReadOnlyList<WriteBackFieldUpdate> ResolveWriteBackForTeam(Team team);

        IReadOnlyList<WriteBackFieldUpdate> ResolveForecastWriteBackForPortfolio(Portfolio portfolio);

        IReadOnlyList<WriteBackFieldUpdate> ResolveFeatureWriteBackForPortfolio(Portfolio portfolio);
    }
}
