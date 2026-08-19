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

        private FeatureDependencyDto(DependencySource source, NotHonouredReason? notHonouredReason)
        {
            ReferenceId = string.Empty;
            Name = string.Empty;
            State = string.Empty;
            StateCategory = StateCategories.Unknown;
            Source = source;
            NotHonouredReason = notHonouredReason;
            IsWithheld = true;
        }

        /// <summary>
        /// An entry for a Feature this reader may not see. It is here rather than left out because the
        /// number on the row counts it: dropping it would leave the list shorter than the number above it
        /// with nothing on screen to say why. It carries nothing that would name the Feature or say where
        /// it lives, only that something is being waited on.
        /// </summary>
        public static FeatureDependencyDto Withheld(DependencySource source, NotHonouredReason? notHonouredReason)
            => new(source, notHonouredReason);

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

        // True when the reader may not see this Feature. Everything else on the entry is then empty, so a
        // client that ignored this would render a nameless row rather than disclose anything.
        public bool IsWithheld { get; }

        public List<EntityReferenceDto> Portfolios { get; } = [];
    }
}
