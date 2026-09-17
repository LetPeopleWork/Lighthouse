using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// The arithmetic on its own. The acceptance scenarios in API/Integration/SleRisk/ pin what a
    /// caller of the endpoint sees; these pin the two things those scenarios deliberately left open —
    /// which way a half-percent rounds, and what an unusable age answers — plus the identities the
    /// formula rests on, which are cheaper to state here than to seed a database for.
    /// </summary>
    public class SleRiskCalculatorTest
    {
        /// <summary>
        /// The worked distribution from the Epic: twenty finished items, six of which took longer
        /// than ten days.
        /// </summary>
        private static readonly int[] TwentyFinishedItems =
            [1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 8, 9, 11, 13, 15, 18, 22, 30];

        [TestCase(2, 32)]
        [TestCase(5, 46)]
        [TestCase(9, 86)]
        [TestCase(10, 100)]
        public void Risk_ItemStillOpen_IsTheShareOfSurvivorsThatWentOnToMiss(int ageInDays, int expected)
        {
            var risk = SleRiskCalculator.Risk(ageInDays, 10, TwentyFinishedItems);

            Assert.That(risk, Is.EqualTo(expected));
        }

        [Test]
        public void Risk_ItemOlderThanTheTarget_IsCertainWithoutASpecialCase()
        {
            // Everything that ran this long necessarily ran longer than ten days, so the numerator
            // and the denominator are the same set. No clamp and no rule — it is the division.
            var risk = SleRiskCalculator.Risk(14, 10, TwentyFinishedItems);

            Assert.That(risk, Is.EqualTo(100));
        }

        [Test]
        public void Risk_HalfwayBetweenTwoWholePercentages_RoundsToTheWorseOne()
        {
            // One of eight is 12.5%. Rounding to even would report 12 and understate the item.
            int[] cycleTimes = [5, 5, 5, 5, 5, 5, 5, 12];

            var risk = SleRiskCalculator.Risk(5, 10, cycleTimes);

            Assert.That(risk, Is.EqualTo(13));
        }

        [Test]
        public void Risk_ItemThatFinishedOnTheTargetDay_IsNotCountedAsAMiss()
        {
            // Three survivors at age three; only the two past ten days missed.
            int[] cycleTimes = [10, 12, 14];

            var risk = SleRiskCalculator.Risk(3, 10, cycleTimes);

            Assert.That(risk, Is.EqualTo(67));
        }

        [Test]
        public void Risk_FinishedItemThatTookExactlyAsLongAsAnItemPastTheTarget_IsOneOfTheMisses()
        {
            // Past the target, every survivor is a miss — including the one that took exactly as long
            // as the open item has. Counting only the ones that outlasted it would report 50% on an
            // item that cannot still make the target, which is the one answer that must never be soft.
            int[] cycleTimes = [12, 15];

            var risk = SleRiskCalculator.Risk(12, 10, cycleTimes);

            Assert.That(risk, Is.EqualTo(100));
        }

        [Test]
        public void Risk_FinishedItemAsOldAsTheOpenOne_IsStillOneOfItsSurvivors()
        {
            // At age five the survivors are {5, 7}. Dropping the one that took exactly five would
            // leave only the item that missed, and the answer would read 100 instead of 50.
            int[] cycleTimes = [3, 5, 7];

            var risk = SleRiskCalculator.Risk(5, 6, cycleTimes);

            Assert.That(risk, Is.EqualTo(50));
        }

        [Test]
        public void Risk_NothingFinishedEverRanThisLong_HasNoAnswer()
        {
            int[] cycleTimes = [2, 3, 4, 5, 6];

            var risk = SleRiskCalculator.Risk(20, 30, cycleTimes);

            Assert.That(risk, Is.Null);
        }

        [Test]
        public void Risk_NothingFinishedAtAll_HasNoAnswer()
        {
            var risk = SleRiskCalculator.Risk(4, 10, []);

            Assert.That(risk, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Risk_AgeThatCannotBeRead_HasNoAnswer(int ageInDays)
        {
            // An item whose start is missing or still in the future. Nothing can be said about how
            // long it has survived, and a zero here would read as the safest item on the board.
            var risk = SleRiskCalculator.Risk(ageInDays, 10, TwentyFinishedItems);

            Assert.That(risk, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Risk_NoTargetPublished_HasNoAnswer(int targetRangeInDays)
        {
            var risk = SleRiskCalculator.Risk(5, targetRangeInDays, TwentyFinishedItems);

            Assert.That(risk, Is.Null);
        }

        [Test]
        public void Risk_NoHistorySupplied_Refuses()
        {
            Assert.That(() => SleRiskCalculator.Risk(5, 10, null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Risk_NoHistorySupplied_RefusesEvenWhenThereIsNoTargetToCompareAgainst()
        {
            // A caller who supplies nothing to divide by has made a mistake, and finding that out
            // must not depend on what else they passed. Without the guard this reads as an ordinary
            // "no answer" and the mistake travels on.
            Assert.That(() => SleRiskCalculator.Risk(5, 0, null!), Throws.ArgumentNullException);
        }
    }
}
