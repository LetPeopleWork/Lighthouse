using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Dependencies
{
    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class DependencyHonourPolicyTest
    {
        private static readonly NotHonouredReason[] TheOnlyReasonsThereAre =
        [
            NotHonouredReason.OutsideThisPortfolio,
            NotHonouredReason.InALoop,
            NotHonouredReason.BlockerCannotBeForecast,
        ];

        [Test]
        public void TheReasonsNotToActOnADependency_AreOnlyEverThese()
        {
            Assert.That(Enum.GetValues<NotHonouredReason>(), Is.EqualTo(TheOnlyReasonsThereAre),
                "A reason was added or removed. Every screen that explains why Lighthouse left a dependency " +
                "out says one of these and nothing else, so a new one needs its wording and its warning " +
                "decided with it rather than appearing on its own.");
        }

        [Test]
        public void ADependencyWithNothingWrongWithIt_CarriesNoReasonAndNothingAboutWhereItSits()
        {
            var verdict = AVerdict(reason: null, blockerPositionedBelow: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.HasNothingWrongWithIt, Is.True);
                Assert.That(verdict.IsHonoured, Is.True);
            }
        }

        [Test]
        public void AFeatureWaitingOnOnePositionedBelowIt_IsWorthSayingAndIsNoReasonToLeaveItOut()
        {
            var verdict = AVerdict(reason: null, blockerPositionedBelow: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsHonoured, Is.True);
                Assert.That(verdict.HasNothingWrongWithIt, Is.False);
            }
        }

        [TestCase(NotHonouredReason.OutsideThisPortfolio)]
        [TestCase(NotHonouredReason.InALoop)]
        [TestCase(NotHonouredReason.BlockerCannotBeForecast)]
        public void ADependencyWithAReasonAgainstIt_IsNotOneLighthouseActsOn(NotHonouredReason reason)
        {
            var verdict = AVerdict(reason, blockerPositionedBelow: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsHonoured, Is.False);
                Assert.That(verdict.Reason, Is.EqualTo(reason));
            }
        }

        [Test]
        public void WhatWasDecided_StaysDecidedWhenTheListItWasBuiltFromIsEmptiedAfterwards()
        {
            var asDecided = new List<DependencyVerdict> { AVerdict(NotHonouredReason.InALoop, blockerPositionedBelow: false) };

            var honoured = new HonouredDependencies(asDecided);
            asDecided.Clear();

            Assert.That(honoured.Verdicts, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Nothing implements the policy yet, so this is red on purpose and the step that writes the
        /// implementation is the one that stops ignoring it. It is the cheapest form of the guarantee the
        /// whole design rests on: two answers to "can Lighthouse act on this dependency" is worse than
        /// either answer alone, because the screen then becomes evidence for something untrue.
        /// </summary>
        [Test]
        [Ignore("pending — DELIVER (epic-4365)")]
        public void OneTypeAndOnlyOne_DecidesWhetherADependencyCanBeActedOn()
        {
            var deciders = typeof(IDependencyHonourPolicy).Assembly
                .GetTypes()
                .Where(candidate => candidate.IsClass
                    && !candidate.IsAbstract
                    && typeof(IDependencyHonourPolicy).IsAssignableFrom(candidate))
                .Select(candidate => candidate.FullName)
                .ToList();

            Assert.That(deciders, Has.Count.EqualTo(1),
                "Found: " + string.Join(", ", deciders));
        }

        private static DependencyVerdict AVerdict(NotHonouredReason? reason, bool blockerPositionedBelow)
        {
            return new DependencyVerdict("F-1", "F-2", reason, blockerPositionedBelow);
        }
    }
}
