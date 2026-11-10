using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// A Portfolio forecast asks the repository only for the Features of the Portfolio's Teams, so the
    /// database does the filtering. This double answers that question the way the database would, from the
    /// Features it is given, so a test still sees a Feature of another Team left out.
    /// </summary>
    public static class FeatureRepositoryDoubles
    {
        public static void Holds(this Mock<IRepository<Feature>> repository, IEnumerable<Feature> features)
        {
            var held = features.ToList();

            repository
                .Setup(r => r.GetAllByPredicate(It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => held.AsQueryable().Where(predicate));
        }

        public static void Holds(this Mock<IFeatureRepository> repository, IEnumerable<Feature> features)
        {
            var held = features.ToList();

            repository
                .Setup(r => r.GetAllByPredicate(It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => held.AsQueryable().Where(predicate));
            repository
                .Setup(r => r.FeaturesWorkedOnBy(It.IsAny<IReadOnlyCollection<int>>()))
                .Returns((IReadOnlyCollection<int> teamIds) => held.Where(feature => feature.FeatureWork.Any(work => teamIds.Contains(work.TeamId))).ToList());
            repository
                .Setup(r => r.FeaturesWithReferenceIds(It.IsAny<IReadOnlyCollection<string>>()))
                .Returns((IReadOnlyCollection<string> referenceIds) => held.Where(feature => referenceIds.Contains(feature.ReferenceId)).ToList());
            repository
                .Setup(r => r.FeaturesWithRealWorkForTeam(It.IsAny<int>(), It.IsAny<StateCategories?>()))
                .Returns((int teamId, StateCategories? onlyIn) => held.Where(feature =>
                    (onlyIn == null || feature.StateCategory == onlyIn) &&
                    !feature.IsUsingDefaultFeatureSize &&
                    feature.FeatureWork.Any(work => work.TeamId == teamId && work.TotalWorkItems > 0)).ToList());
        }

        /// <summary>
        /// Answers every read of a Portfolio's Features the way the database would, from the Features the
        /// test holds at the moment of the read. The list is asked for on each call, so a test may assign or
        /// add to it after the set-up and the next read still sees it.
        /// </summary>
        public static void AnswersPortfolioReadsFrom(this Mock<IFeatureRepository> repository, Func<IEnumerable<Feature>> features)
        {
            repository
                .Setup(r => r.DoneFeaturesOfPortfolio(It.IsAny<int>()))
                .Returns((int portfolioId) => OfPortfolio(features(), portfolioId)
                    .Where(f => f.StateCategory == StateCategories.Done).ToList());
            repository
                .Setup(r => r.DoingOrDoneFeaturesOfPortfolio(It.IsAny<int>()))
                .Returns((int portfolioId) => OfPortfolio(features(), portfolioId)
                    .Where(f => f.StateCategory == StateCategories.Doing || f.StateCategory == StateCategories.Done).ToList());
            repository
                .Setup(r => r.ToDoOrDoingFeaturesOfPortfolio(It.IsAny<int>()))
                .Returns((int portfolioId) => OfPortfolio(features(), portfolioId)
                    .Where(f => f.StateCategory == StateCategories.ToDo || f.StateCategory == StateCategories.Doing).ToList());
            repository
                .Setup(r => r.ToDoDoingOrDoneFeaturesOfPortfolio(It.IsAny<int>()))
                .Returns((int portfolioId) => OfPortfolio(features(), portfolioId)
                    .Where(f => f.StateCategory == StateCategories.Done || f.StateCategory == StateCategories.ToDo || f.StateCategory == StateCategories.Doing).ToList());
            repository
                .Setup(r => r.EveryFeatureOfPortfolio(It.IsAny<int>()))
                .Returns((int portfolioId) => OfPortfolio(features(), portfolioId).ToList());
        }

        public static void AnswersTransitionReadsFrom(this Mock<IFeatureStateTransitionRepository> repository, IEnumerable<FeatureStateTransition> transitions)
        {
            repository
                .Setup(r => r.TransitionsOfFeatures(It.IsAny<IReadOnlyCollection<int>>()))
                .Returns((IReadOnlyCollection<int> featureIds) => transitions.Where(t => featureIds.Contains(t.FeatureId)).ToList());
        }

        private static IEnumerable<Feature> OfPortfolio(IEnumerable<Feature> features, int portfolioId)
            => features.Where(f => f.Portfolios.Any(p => p.Id == portfolioId));
    }
}
