using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Names a cached metric after its owner. Teams and Portfolios are numbered in separate tables, so
    /// Team 3 and Portfolio 3 both exist; a key built from the id alone would let one owner be served
    /// the other's numbers. The key and the prefix that clears an owner's entries come from here
    /// together, so they cannot disagree about whose entry is whose. The owner's kind is the type
    /// the caller names, Team or Portfolio, so callers pass the owner as its own type.
    /// </summary>
    public static class MetricsCacheKey
    {
        public static string For<TEntity>(TEntity owner, string metricIdentifier) where TEntity : class, IEntity
            => $"{PrefixFor(owner)}{metricIdentifier}";

        public static string PrefixFor<TEntity>(TEntity owner) where TEntity : class, IEntity
            => $"{typeof(TEntity).Name}_{owner.Id}_";
    }
}
