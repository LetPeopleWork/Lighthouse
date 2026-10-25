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
                .Setup(r => r.FeaturesWithReferenceIds(It.IsAny<IReadOnlyCollection<string>>()))
                .Returns((IReadOnlyCollection<string> referenceIds) => held.Where(feature => referenceIds.Contains(feature.ReferenceId)).ToList());
        }
    }
}
