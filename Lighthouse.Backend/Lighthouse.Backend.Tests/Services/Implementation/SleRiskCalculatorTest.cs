using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// The arithmetic on its own, and the rule about when it is worth showing. The acceptance
    /// scenarios in API/Integration/SleRisk/ pin what a caller of the endpoint sees; these pin the
    /// two things those scenarios deliberately left open — which way a half-percent rounds, and what
    /// an unusable age answers — plus the boundaries the formula rests on, which are cheaper to state
    /// here than to seed a database for.
    /// </summary>
    public class SleRiskCalculatorTest
    {
        /// <summary>
        /// The worked distribution from the Epic, three times over. The proportions are the Epic's
        /// own — six in twenty ran past ten days — and tripling them is what puts enough finished
        /// work at each age for the answer to be shown at all.
        /// </summary>
        private static readonly int[] SixtyFinishedItems =
            [.. Enumerable.Repeat<int[]>([1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 8, 9, 11, 13, 15, 18, 22, 30], 3).SelectMany(x => x)];

        [TestCase(2, 32)]
        [TestCase(5, 46)]
        [TestCase(9, 86)]
        [TestCase(10, 100)]
        public void For_ItemStillOpen_IsTheShareOfComparableItemsThatWentOnToMiss(int ageInDays, int expected)
        {
            var verdict = SleRiskCalculator.For(ageInDays, 10, SixtyFinishedItems);

            Assert.That(verdict.Risk, Is.EqualTo(expected));
        }

        [Test]
        public void For_ItemOlderThanTheTarget_IsCertainWithoutASpecialCase()
        {
            // Everything that ran this long necessarily ran longer than ten days, so the numerator
            // and the denominator are the same set. No clamp and no rule — it is the division.
            var verdict = SleRiskCalculator.For(11, 10, SixtyFinishedItems);

            Assert.That(verdict.Risk, Is.EqualTo(100));
        }

        [Test]
        public void For_HalfwayBetweenTwoWholePercentages_RoundsToTheWorseOne()
        {
            // One of sixteen is 6.25%, and one of eight would be 12.5% — the halfway case. Sixteen
            // items are used because eight is below the minimum, so the rounding rule has to be
            // shown at a size the answer is allowed to be given at.
            int[] cycleTimes = [.. Enumerable.Repeat(5, 14), 12, 12];

            var verdict = SleRiskCalculator.For(5, 10, cycleTimes);

            // 2/16 = 12.5%, which rounds up rather than to the even 12.
            Assert.That(verdict.Risk, Is.EqualTo(13));
        }

        [Test]
        public void For_ItemThatFinishedOnTheTargetDay_IsNotCountedAsAMiss()
        {
            // Twelve comparable items; only the eight past ten days missed. Counting the four that
            // took exactly ten would make it twelve in twelve.
            int[] cycleTimes = [.. Enumerable.Repeat(10, 4), .. Enumerable.Repeat(12, 4), .. Enumerable.Repeat(14, 4)];

            var verdict = SleRiskCalculator.For(3, 10, cycleTimes);

            Assert.That(verdict.Risk, Is.EqualTo(67));
        }

        [Test]
        public void For_FinishedItemThatTookExactlyAsLongAsAnItemPastTheTarget_IsOneOfTheMisses()
        {
            // Past the target, every comparable item is a miss — including the ones that took exactly
            // as long as the open item has. Counting only the ones that outlasted it would report 50%
            // on an item that cannot still make the target, the one answer that must never be soft.
            int[] cycleTimes = [.. Enumerable.Repeat(12, 5), .. Enumerable.Repeat(15, 5)];

            var verdict = SleRiskCalculator.For(12, 10, cycleTimes);

            Assert.That(verdict.Risk, Is.EqualTo(100));
        }

        [Test]
        public void For_FinishedItemAsOldAsTheOpenOne_IsStillOneOfItsComparableItems()
        {
            // At age five the comparable items are the fives and the sevens. Dropping the ones that
            // took exactly five would leave only items that missed, and the answer would read 100.
            int[] cycleTimes = [.. Enumerable.Repeat(3, 4), .. Enumerable.Repeat(5, 5), .. Enumerable.Repeat(7, 5)];

            var verdict = SleRiskCalculator.For(5, 6, cycleTimes);

            Assert.That(verdict.Risk, Is.EqualTo(50));
        }

        // --- When the history is not worth dividing by ---

        [Test]
        public void For_NothingFinishedEverRanThisLong_HasNoAnswerAndNothingToCompareAgainst()
        {
            int[] cycleTimes = [2, 3, 4, 5, 6];

            var verdict = SleRiskCalculator.For(20, 30, cycleTimes);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Risk, Is.Null);
                Assert.That(verdict.ComparableItems, Is.Zero,
                    "Nothing ran this long, which is a different silence from too little having run this long.");
            }
        }

        [Test]
        public void For_TooLittleFinishedWorkRanThisLong_HasNoAnswerButSaysHowLittle()
        {
            // Nine items did run this long, and nine is not enough to divide by: one more or one
            // fewer would move the answer by eleven points overnight, on the item a coach is being
            // told to look at first.
            int[] cycleTimes = [.. Enumerable.Repeat(20, 9), .. Enumerable.Repeat(1, 50)];

            var verdict = SleRiskCalculator.For(20, 10, cycleTimes);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Risk, Is.Null,
                    "A hundred percent off nine observations reads as certainty and is not one.");
                Assert.That(verdict.ComparableItems, Is.EqualTo(9));
            }
        }

        [Test]
        public void For_ExactlyTheMinimumComparableItems_IsAnswered()
        {
            // The boundary itself. One fewer is silence, so this is the first age a reader is told
            // anything about at all.
            int[] cycleTimes = [.. Enumerable.Repeat(20, SleRiskCalculator.MinimumComparableItems), .. Enumerable.Repeat(1, 50)];

            var verdict = SleRiskCalculator.For(20, 10, cycleTimes);

            Assert.That(verdict.Risk, Is.EqualTo(100));
        }

        [Test]
        public void For_OneShortOfTheMinimum_IsNotAnswered()
        {
            int[] cycleTimes = [.. Enumerable.Repeat(20, SleRiskCalculator.MinimumComparableItems - 1), .. Enumerable.Repeat(1, 50)];

            var verdict = SleRiskCalculator.For(20, 10, cycleTimes);

            Assert.That(verdict.Risk, Is.Null);
        }

        [Test]
        public void For_NothingFinishedAtAll_HasNoAnswer()
        {
            var verdict = SleRiskCalculator.For(4, 10, []);

            Assert.That(verdict.Risk, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void For_AgeThatCannotBeRead_HasNoAnswer(int ageInDays)
        {
            // An item whose start is missing or still in the future. Nothing can be said about how
            // long it has survived, and a zero here would read as the safest item on the board.
            var verdict = SleRiskCalculator.For(ageInDays, 10, SixtyFinishedItems);

            Assert.That(verdict.Risk, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void For_NoTargetPublished_HasNoAnswer(int targetRangeInDays)
        {
            var verdict = SleRiskCalculator.For(5, targetRangeInDays, SixtyFinishedItems);

            Assert.That(verdict.Risk, Is.Null);
        }

        [Test]
        public void For_NoHistorySupplied_Refuses()
        {
            Assert.That(() => SleRiskCalculator.For(5, 10, null!), Throws.ArgumentNullException);
        }

        [Test]
        public void For_NoHistorySupplied_RefusesEvenWhenThereIsNoTargetToCompareAgainst()
        {
            // A caller who supplies nothing to divide by has made a mistake, and finding that out
            // must not depend on what else they passed. Without the guard this reads as an ordinary
            // "no answer" and the mistake travels on.
            Assert.That(() => SleRiskCalculator.For(5, 0, null!), Throws.ArgumentNullException);
        }
    }
}
