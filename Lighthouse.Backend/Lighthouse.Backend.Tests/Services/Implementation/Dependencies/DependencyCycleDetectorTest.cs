using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Dependencies
{
    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class DependencyCycleDetectorTest
    {
        private static readonly int[] TheOnePortfolioEverythingHereBelongsTo = [1];

        private static readonly string[] NothingAtAll = [];

        private static readonly string[] TheOtherTwoInTheCircle = ["F-2", "F-3"];

        private static readonly string[] TheOtherOneInTheCircle = ["F-2"];

        private static readonly string[] JustTheFeatureItself = ["F-1"];

        private static readonly string[] TheFirstPairOfTwo = ["F-1", "F-2"];

        private static readonly string[] TheSecondPairOfTwo = ["F-3", "F-4"];

        private static readonly string[][] BothPairs = [TheFirstPairOfTwo, TheSecondPairOfTwo];

        private static readonly string[] TheFeatureAtTheHeadOfTheQueue = ["F-0"];

        [Test]
        public void EveryFeatureInACircle_IsAMemberOfItAndCanNameTheOthers()
        {
            var loops = Detect(
                WaitingOn("F-1", "F-2"),
                WaitingOn("F-2", "F-3"),
                WaitingOn("F-3", "F-1"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loops.Loops, Has.Count.EqualTo(1));
                Assert.That(loops.IsInALoop("F-1"), Is.True);
                Assert.That(loops.IsInALoop("F-2"), Is.True);
                Assert.That(loops.IsInALoop("F-3"), Is.True);
                Assert.That(loops.OthersInTheLoopWith("F-1"), Is.EquivalentTo(TheOtherTwoInTheCircle));
            }
        }

        [Test]
        public void AFeatureThatSaysItWaitsOnItself_IsACircleOfOneAndIsReportedRatherThanDropped()
        {
            var loops = Detect(WaitingOn("F-1", "F-1"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loops.Loops, Has.Count.EqualTo(1));
                Assert.That(loops.Loops.Single().MemberReferenceIds, Is.EquivalentTo(JustTheFeatureItself));
                Assert.That(loops.IsInALoop("F-1"), Is.True);
                Assert.That(loops.OthersInTheLoopWith("F-1"), Is.EquivalentTo(NothingAtAll));
            }
        }

        [Test]
        public void TwoCirclesWithNoFeatureInCommon_AreBothReportedAndNeitherHidesTheOther()
        {
            var loops = Detect(
                WaitingOn("F-1", "F-2"),
                WaitingOn("F-2", "F-1"),
                WaitingOn("F-3", "F-4"),
                WaitingOn("F-4", "F-3"),
                WaitingOn("F-5"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loops.Loops, Has.Count.EqualTo(2));
                Assert.That(loops.OthersInTheLoopWith("F-1"), Is.EquivalentTo(TheOtherOneInTheCircle));
                Assert.That(loops.Loops.Select(loop => loop.MemberReferenceIds.Order().ToArray()),
                    Is.EquivalentTo(BothPairs));
                Assert.That(loops.IsInALoop("F-5"), Is.False);
            }
        }

        [Test]
        public void AFeatureThatMerelyWaitsOnACircleWithoutBeingPartOfIt_IsNotReportedAsAMember()
        {
            var loops = Detect(
                WaitingOn("F-0", "F-1"),
                WaitingOn("F-1", "F-2"),
                WaitingOn("F-2", "F-1"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loops.IsInALoop("F-0"), Is.False);
                Assert.That(loops.OthersInTheLoopWith("F-0"), Is.EquivalentTo(NothingAtAll));
                Assert.That(loops.OthersInTheLoopWith("F-1"), Is.EquivalentTo(TheOtherOneInTheCircle));
            }
        }

        [Test]
        public void AHundredFeaturesEachWaitingOnTheNext_IsNoCircleHoweverLongTheQueueGets()
        {
            var loops = Detect(AQueueOf(100, closedIntoACircle: false));

            Assert.That(loops.Loops, Is.Empty);
        }

        [Test]
        public void AQueueFarLongerThanTheCallStackCouldHold_IsWalkedWithoutTakingTheProcessDown()
        {
            var loops = Detect(AQueueOf(20_000, closedIntoACircle: true));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loops.Loops, Has.Count.EqualTo(1));
                Assert.That(loops.Loops.Single().MemberReferenceIds, Has.Count.EqualTo(20_000));
                Assert.That(loops.IsInALoop("F-19999"), Is.True);
            }
        }

        [Test]
        public void AnEdgeNamingAFeatureLighthouseDoesNotHold_ContributesNoFeatureAndNoCircle()
        {
            var loops = Detect(
                WaitingOn("F-1", "F-2", "NOT-HELD-HERE"),
                WaitingOn("F-2", "F-1"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loops.Loops.Single().MemberReferenceIds, Is.EquivalentTo(TheFirstPairOfTwo));
                Assert.That(loops.IsInALoop("NOT-HELD-HERE"), Is.False);
            }
        }

        [Test]
        public void AFeatureWaitingOnNothingHeldHere_IsNoCircleOnItsOwn()
        {
            var loops = Detect(WaitingOn("F-1", "NOT-HELD-HERE"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loops.Loops, Is.Empty);
                Assert.That(loops.IsInALoop("F-1"), Is.False);
            }
        }

        private static DependencyLoops Detect(params FeatureDependencyFacts[] featuresInScope)
        {
            return new DependencyCycleDetector(featuresInScope).Detect();
        }

        private static FeatureDependencyFacts[] AQueueOf(int length, bool closedIntoACircle)
        {
            var features = new FeatureDependencyFacts[length];

            for (var position = 0; position < length; position++)
            {
                var isTheLast = position == length - 1;
                var waitsOn = isTheLast
                    ? (closedIntoACircle ? TheFeatureAtTheHeadOfTheQueue : [])
                    : new[] { $"F-{position + 1}" };

                features[position] = WaitingOn($"F-{position}", waitsOn);
            }

            return features;
        }

        private static FeatureDependencyFacts WaitingOn(string referenceId, params string[] blockerReferenceIds)
        {
            return new FeatureDependencyFacts(
                referenceId,
                TheOnePortfolioEverythingHereBelongsTo,
                Position: 0,
                CanBeForecast: true,
                blockerReferenceIds);
        }
    }
}
