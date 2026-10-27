using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Which work an operator may stop. A removal may not: stopped half-way, its rows stay in the database
    /// while the caller that asked for it is told the entity has gone. Stopping one thing and stopping
    /// everything both ask here, so the two cannot come to disagree about what a removal is.
    /// </summary>
    public static class UpdateCancellationPolicy
    {
        public static bool CanBeStopped(UpdateType updateType)
            => updateType is not (UpdateType.TeamDelete or UpdateType.PortfolioDelete);
    }
}
