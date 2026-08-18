using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class FeatureDependencyReferenceTest
    {
        private static readonly string[] EveryDependencySource = ["TrackerLink", "PortfolioField"];

        private static readonly string[] EveryMemberOfTheReference = ["Id", "FeatureId", "ReferenceId", "Source"];

        private static readonly string[] TheOneReferenceWaitedOn = ["F-2"];

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

        [Test]
        public void Feature_HandsOutTheReferencesItWaitsOnSoNoCallerCanChangeThem()
        {
            var property = typeof(Feature).GetProperty(nameof(Feature.DependsOnReferences));
            var publicMutators = typeof(Feature)
                .GetMethods()
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name.Contains("DependsOn", StringComparison.Ordinal))
                .Select(method => method.Name);

            Assert.That(property, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(property.PropertyType, Is.EqualTo(typeof(IReadOnlyCollection<FeatureDependencyReference>)));
                Assert.That(property.SetMethod, Is.Null);
                Assert.That(publicMutators, Is.Empty);
            }
        }

        [Test]
        public void Feature_Update_LeavesTheReferencesItWaitsOnAlone()
        {
            var feature = new Feature { ReferenceId = "F-1", Name = "F-1" };
            feature.ReplaceDependsOnReferences([new FeatureDependencyReference(feature.Id, "F-2", DependencySource.TrackerLink)]);

            feature.Update(new Feature { ReferenceId = "F-1", Name = "F-1 renamed" });

            Assert.That(feature.DependsOnReferences.Select(reference => reference.ReferenceId), Is.EquivalentTo(TheOneReferenceWaitedOn));
        }
    }

    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class FeatureDependencyReferenceStorageTest : IntegrationTestBase
    {
        private static readonly string[] TheOneReferenceWaitedOn = ["F-2"];

        private static readonly DependencySource[] ReadFromATrackerLink = [DependencySource.TrackerLink];

        [Test]
        public async Task GetAll_BringsBackTheReferencesAFeatureWaitsOn()
        {
            var subject = new FeatureRepository(DatabaseContext, FeatureOrderingTestHelper.FollowingTheTracker(), Mock.Of<ILogger<FeatureRepository>>());
            var feature = new Feature { ReferenceId = "F-1", Name = "F-1", Order = "1", Type = "Feature" };
            feature.ReplaceDependsOnReferences([new FeatureDependencyReference(feature.Id, "F-2", DependencySource.TrackerLink)]);
            subject.Add(feature);
            await subject.Save();

            // Without this the references are still tracked from the write, so the read path would look
            // like it loaded them even if it never asked the database for them.
            DatabaseContext.ChangeTracker.Clear();

            var stored = subject.GetAll().Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored.DependsOnReferences.Select(reference => reference.ReferenceId), Is.EquivalentTo(TheOneReferenceWaitedOn));
                Assert.That(stored.DependsOnReferences.Select(reference => reference.Source), Is.EquivalentTo(ReadFromATrackerLink));
            }
        }
    }
}
