using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// One Feature another Feature is waiting on, as the reader is handed it: enough to tell whether the
    /// wait is nearly over, who to go and talk to, and where to go and look. Only Portfolios the reader may
    /// read are ever named, so opening this never discloses that a Portfolio exists.
    /// </summary>
    public class FeatureDependencyDto
    {
        public FeatureDependencyDto(
            Feature blocker,
            DependencySource source,
            NotHonouredReason? notHonouredReason,
            ISet<int> readablePortfolioIds)
        {
            Id = blocker.Id;
            ReferenceId = blocker.ReferenceId;
            Name = blocker.Name;
            State = blocker.State;
            StateCategory = blocker.StateCategory;
            Url = blocker.Url;
            Source = source;
            NotHonouredReason = notHonouredReason;

            Portfolios.AddRange(blocker.Portfolios
                .Where(portfolio => readablePortfolioIds.Contains(portfolio.Id))
                .Select(portfolio => new EntityReferenceDto(portfolio.Id, portfolio.Name)));
        }

        public int Id { get; }

        public string ReferenceId { get; }

        public string Name { get; }

        public string State { get; }

        public StateCategories StateCategory { get; }

        public string? Url { get; }

        public DependencySource Source { get; }

        // Absent means there is nothing wrong with this dependency. A further code meaning "fine" would
        // read as one more thing to look into, which is the opposite of what it would be saying.
        public NotHonouredReason? NotHonouredReason { get; }

        public List<EntityReferenceDto> Portfolios { get; } = [];
    }
}
