using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Dependencies;

namespace Lighthouse.Backend.Services.Implementation.Dependencies
{
    /// <summary>
    /// Decides, for every dependency among the Features handed in, whether Lighthouse can act on it. It reads
    /// nothing but those facts - no repository, no database, no log - so the warning a user sees and anything
    /// later working from the same dependency cannot disagree, and asking costs no queries.
    /// </summary>
    public sealed class DependencyHonourPolicy : IDependencyHonourPolicy
    {
        public HonouredDependencies Evaluate(DependencyHonourInput input)
        {
            var factsByReferenceId = FactsByReferenceId(input.FeaturesInScope);
            var loops = new DependencyCycleDetector(input.FeaturesInScope).Detect();

            var verdicts = input.FeaturesInScope
                .SelectMany(dependent => dependent.DependsOnReferenceIds
                    .Select(blocker => Decide(dependent, blocker, factsByReferenceId, loops)))
                .ToList();

            return new HonouredDependencies(verdicts);
        }

        /// <summary>
        /// A Feature that is not among the ones handed in is treated the same as one sharing no Portfolio,
        /// because that is what it is: the caller loaded a Portfolio's Features, and something waited on that
        /// is not in there is somewhere this Portfolio cannot see.
        /// </summary>
        private static DependencyVerdict Decide(
            FeatureDependencyFacts dependent,
            string blockerReferenceId,
            Dictionary<string, FeatureDependencyFacts> factsByReferenceId,
            DependencyLoops loops)
        {
            if (!factsByReferenceId.TryGetValue(blockerReferenceId, out var blocker)
                || !TheyShareAPortfolio(dependent, blocker))
            {
                return new DependencyVerdict(
                    dependent.ReferenceId,
                    blockerReferenceId,
                    NotHonouredReason.OutsideThisPortfolio,
                    blockerPositionedBelow: false);
            }

            return new DependencyVerdict(
                dependent.ReferenceId,
                blockerReferenceId,
                ReasonWithinThisPortfolio(dependent, blocker, loops),
                blockerPositionedBelow: blocker.Position > dependent.Position);
        }

        /// <summary>
        /// A circle is decided before anything about the Feature waited on, so every dependency going round
        /// the same circle reads the same way. Otherwise one member with no delivery to measure would report
        /// that instead, and the user would be told two different things about one circle.
        /// </summary>
        private static NotHonouredReason? ReasonWithinThisPortfolio(
            FeatureDependencyFacts dependent,
            FeatureDependencyFacts blocker,
            DependencyLoops loops)
        {
            if (TheyWaitOnEachOtherInACircle(loops, dependent.ReferenceId, blocker.ReferenceId))
            {
                return NotHonouredReason.InALoop;
            }

            if (!blocker.CanBeForecast)
            {
                return NotHonouredReason.BlockerCannotBeForecast;
            }

            return null;
        }

        private static bool TheyShareAPortfolio(FeatureDependencyFacts dependent, FeatureDependencyFacts blocker)
        {
            return dependent.PortfolioIds.Any(portfolioId => blocker.PortfolioIds.Contains(portfolioId));
        }

        /// <summary>
        /// A Feature naming itself is its own circle, and it is the one case where asking what else is round
        /// the circle answers nothing.
        /// </summary>
        private static bool TheyWaitOnEachOtherInACircle(
            DependencyLoops loops,
            string dependentReferenceId,
            string blockerReferenceId)
        {
            if (dependentReferenceId == blockerReferenceId)
            {
                return loops.IsInALoop(dependentReferenceId);
            }

            return loops.OthersInTheLoopWith(dependentReferenceId).Contains(blockerReferenceId);
        }

        private static Dictionary<string, FeatureDependencyFacts> FactsByReferenceId(
            IReadOnlyCollection<FeatureDependencyFacts> featuresInScope)
        {
            return featuresInScope
                .GroupBy(feature => feature.ReferenceId)
                .ToDictionary(byReferenceId => byReferenceId.Key, byReferenceId => byReferenceId.First());
        }
    }
}
