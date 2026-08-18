using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class FeatureDependencyReferenceTest
    {
        private static readonly string[] EveryDependencySource = ["TrackerLink", "PortfolioField"];

        private static readonly string[] EveryMemberOfTheReference = ["Id", "FeatureId", "ReferenceId", "Source"];

        [Test]
        public void Reference_CarriesTheFeatureTheReferenceAndItsSource()
        {
            var reference = new FeatureDependencyReference(42, "PROJ-17", DependencySource.TrackerLink);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reference.FeatureId, Is.EqualTo(42));
                Assert.That(reference.ReferenceId, Is.EqualTo("PROJ-17"));
                Assert.That(reference.Source, Is.EqualTo(DependencySource.TrackerLink));
                Assert.That(reference.Id, Is.Zero);
            }
        }

        [Test]
        public void Reference_HoldsAnUnresolvedReferenceIdSoAnEdgeSurvivesAnUnimportedTarget()
        {
            var reference = new FeatureDependencyReference(1, "not-imported-yet", DependencySource.TrackerLink);

            Assert.That(reference.ReferenceId, Is.EqualTo("not-imported-yet"));
        }

        [Test]
        public void Reference_CannotBeChangedByAnyoneButTheDatabase()
        {
            var settableFromOutside = typeof(FeatureDependencyReference)
                .GetProperties()
                .Where(property => property.SetMethod?.IsPublic == true)
                .Select(property => property.Name);

            Assert.That(settableFromOutside, Is.Empty);
        }

        [Test]
        public void Reference_CarriesNothingBeyondTheEdgeItself()
        {
            var members = typeof(FeatureDependencyReference)
                .GetProperties()
                .Select(property => property.Name);

            Assert.That(members, Is.EquivalentTo(EveryMemberOfTheReference));
        }

        [Test]
        public void Reference_IsStoredAsAnEntityOfItsOwn()
        {
            var reference = new FeatureDependencyReference(1, "PROJ-1", DependencySource.TrackerLink);

            Assert.That(reference, Is.InstanceOf<IEntity>());
        }

        [Test]
        public void DependencySource_NamesEveryPartOfTheTrackerAnEdgeCanBeReadFrom()
        {
            Assert.That(Enum.GetNames<DependencySource>(), Is.EquivalentTo(EveryDependencySource));
        }
    }
}
