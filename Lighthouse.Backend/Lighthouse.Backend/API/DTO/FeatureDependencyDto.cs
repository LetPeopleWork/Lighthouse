using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// One Feature another Feature is waiting on, as the reader is handed it: enough to tell whether the
    /// wait is nearly over, who to go and talk to, and where to go and look. Only Portfolios the reader may
    /// read are ever named, so opening this never discloses that a Portfolio exists.
    /// </summary>
    public class FeatureDependencyDto
    {
        public FeatureDependencyDto(Feature blocker, ISet<int> readablePortfolioIds)
        {
            Id = blocker.Id;
            ReferenceId = blocker.ReferenceId;
            Name = blocker.Name;
            State = blocker.State;
            StateCategory = blocker.StateCategory;
            Url = blocker.Url;

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

        public List<EntityReferenceDto> Portfolios { get; } = [];
    }
}
