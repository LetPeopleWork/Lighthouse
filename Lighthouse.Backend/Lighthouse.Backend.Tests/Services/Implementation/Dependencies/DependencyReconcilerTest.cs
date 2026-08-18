using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Dependencies
{
    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class DependencyReconcilerTest
    {
        private static readonly string[] TheOneTargetTheTrackerNowNames = ["F-3"];

        private static readonly string[] TheOneTargetItWaitsOn = ["F-2"];

        private static readonly string[] TheFeatureItself = ["F-1"];

        private static readonly DependencySource[] ReadFromATrackerLink = [DependencySource.TrackerLink];

        [Test]
        public void Reconcile_ReplacesWhatAFeatureWaitsOnWithWhatTheTrackerNowSays()
        {
            var feature = AFeatureWaitingOn("F-2");

            new DependencyReconciler().Reconcile(feature, [ATrackerLinkFrom(feature, "F-3")]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReferenceIdsOf(feature), Is.EqualTo(TheOneTargetTheTrackerNowNames));
                Assert.That(feature.DependsOnReferences.Select(reference => reference.Source), Is.EqualTo(ReadFromATrackerLink));
            }
        }

        [Test]
        public void Reconcile_ClearsWhatAFeatureWaitsOnWhenTheTrackerNamesNothing()
        {
            var feature = AFeatureWaitingOn("F-2");

            new DependencyReconciler().Reconcile(feature, []);

            Assert.That(feature.DependsOnReferences, Is.Empty);
        }

        [Test]
        public void Reconcile_CollapsesATargetTheTrackerNamesTwiceIntoOneReference()
        {
            var feature = AFeatureWaitingOn();

            new DependencyReconciler().Reconcile(
                feature, [ATrackerLinkFrom(feature, "F-2"), ATrackerLinkFrom(feature, "F-2")]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.DependsOnReferences, Has.Count.EqualTo(1));
                Assert.That(ReferenceIdsOf(feature), Is.EqualTo(TheOneTargetItWaitsOn));
            }
        }

        [Test]
        public void Reconcile_KeepsAFeatureThatWaitsOnItself()
        {
            var feature = AFeatureWaitingOn();

            new DependencyReconciler().Reconcile(feature, [ATrackerLinkFrom(feature, feature.ReferenceId)]);

            Assert.That(ReferenceIdsOf(feature), Is.EqualTo(TheFeatureItself));
        }

        private static Feature AFeatureWaitingOn(params string[] referenceIds)
        {
            var feature = new Feature { Id = 7, ReferenceId = "F-1", Name = "F-1" };
            feature.ReplaceDependsOnReferences(
                referenceIds.Select(referenceId => ATrackerLinkFrom(feature, referenceId)));

            return feature;
        }

        private static FeatureDependencyReference ATrackerLinkFrom(Feature feature, string referenceId)
        {
            return new FeatureDependencyReference(feature.Id, referenceId, DependencySource.TrackerLink);
        }

        private static List<string> ReferenceIdsOf(Feature feature)
        {
            return feature.DependsOnReferences.Select(reference => reference.ReferenceId).ToList();
        }
    }
}
