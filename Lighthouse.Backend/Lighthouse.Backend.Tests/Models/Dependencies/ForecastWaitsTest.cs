using Lighthouse.Backend.Models.Dependencies;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models.Dependencies
{
    /// <summary>
    /// The boundary the simulation trusts. Everything the forecast does with a wait rests on this carrying
    /// only what the one decision said could be acted on - a run that is handed a wait nobody honoured has
    /// no way to tell, and would hold work back for a reason the screen next to it is not showing.
    /// </summary>
    [TestFixture]
    [Category("epic-5792-dependency-aware-forecasting")]
    public class ForecastWaitsTest
    {
        private static readonly string[] JustTheSecond = ["F-2"];

        [Test]
        public void AWaitNobodyHonoured_NeverReachesTheForecast()
        {
            var waits = ForecastWaits.From(new HonouredDependencies(
            [
                AVerdict("F-1", "F-2", NotHonouredReason.InALoop),
                AVerdict("F-1", "F-4", NotHonouredReason.NotLicensed),
                AVerdict("F-1", "F-5", NotHonouredReason.OutsideThisPortfolio),
                AVerdict("F-1", "F-6", NotHonouredReason.BlockerCannotBeForecast),
                AVerdict("F-1", "F-7", NotHonouredReason.IgnoredByPortfolio),
            ]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(waits.Of("F-1"), Is.Empty);
                Assert.That(waits.NobodyWaitsForAnything, Is.True);
            }
        }

        [Test]
        public void AWaitTheDecisionHonoured_IsWhatTheForecastWaitsFor()
        {
            var waits = ForecastWaits.From(new HonouredDependencies(
            [
                AVerdict("F-1", "F-2", reason: null),
                AVerdict("F-1", "F-3", NotHonouredReason.InALoop),
            ]));

            Assert.That(waits.Of("F-1"), Is.EqualTo(JustTheSecond));
        }

        /// <summary>
        /// One Feature can be waited on through more than one entry - the same tracker link read from two
        /// places, say. Waiting for it twice is waiting for it once, and a duplicate would only cost the
        /// simulation a second look at a row it has already read.
        /// </summary>
        [Test]
        public void TheSameFeatureWaitedOnTwice_IsWaitedForOnce()
        {
            var waits = ForecastWaits.From(new HonouredDependencies(
            [
                AVerdict("F-1", "F-2", reason: null),
                AVerdict("F-1", "F-2", reason: null),
            ]));

            Assert.That(waits.Of("F-1"), Has.Count.EqualTo(1));
        }

        [Test]
        public void AFeatureNobodyRecordedAWaitFor_WaitsForNothing()
        {
            var waits = ForecastWaits.From(new HonouredDependencies([AVerdict("F-1", "F-2", reason: null)]));

            Assert.That(waits.Of("F-9"), Is.Empty);
        }

        [Test]
        public void TheAnswerForARunWithNoDependenciesAnywhere_SaysSoWithoutBeingAsked()
        {
            Assert.That(ForecastWaits.Nothing.NobodyWaitsForAnything, Is.True);
        }

        private static DependencyVerdict AVerdict(string dependent, string blocker, NotHonouredReason? reason)
            => new(dependent, blocker, reason, blockerPositionedBelow: false);
    }
}
