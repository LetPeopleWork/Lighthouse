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

        /// <summary>The levels the chart paints a zone above, calmest first.</summary>
        private static readonly int[] RiskLevels = [25, 50, 75, 100];

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

        // --- Where the odds turn, for the chart's background zones ---

        [Test]
        public void Zones_ARealDistribution_NamesTheFirstAgeAtEachLevel()
        {
            // Sixty items, target ten days. The share that missed rises with the age asked about, so
            // each level is reached once and stays reached.
            var zones = SleRiskCalculator.Zones(10, SixtyFinishedItems);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(zones.Select(zone => zone.Risk), Is.EqualTo(RiskLevels));
                Assert.That(zones.Select(zone => zone.FromAge), Is.Ordered,
                    "The risk only rises with age, so the ages it crosses at only rise too.");
            }
        }

        [Test]
        public void Zones_TheTopLevel_LeavesNothingAboveTheTargetOutsideIt()
        {
            // Every item past a ten-day target has certainly missed it, so the top zone must already
            // have begun by eleven days. It may begin earlier - it does here - when nothing the team
            // finished took the last day or two before the target.
            var zones = SleRiskCalculator.Zones(10, SixtyFinishedItems);

            Assert.That(zones.Single(zone => zone.Risk == 100).FromAge, Is.LessThanOrEqualTo(11));
        }

        [Test]
        public void Zones_AnItemThatFinishedOnTheTargetDay_HoldsTheTopLevelBackToTheDayAfter()
        {
            // Twelve items took exactly ten days and twelve took longer. At ten days the odds are
            // even, so certainty cannot begin until eleven - which is the first age at which every
            // item that ever got that far had already missed.
            int[] cycleTimes = [.. Enumerable.Repeat(10, 12), .. Enumerable.Repeat(14, 12)];

            var zones = SleRiskCalculator.Zones(10, cycleTimes);

            Assert.That(zones.Single(zone => zone.Risk == 100).FromAge, Is.EqualTo(11));
        }

        [Test]
        public void Zones_ALevelFirstReachedByTheLongestItemEver_IsStillRecorded()
        {
            // Twelve items took five days and twelve took six, against a five-day target. Up to five
            // days the odds are even; at six every survivor has missed. Six is also the longest
            // anything ever took, so a walk that stopped one age short would lose the two levels a
            // reader most needs.
            int[] cycleTimes = [.. Enumerable.Repeat(5, 12), .. Enumerable.Repeat(6, 12)];

            var zones = SleRiskCalculator.Zones(5, cycleTimes);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(zones.Select(zone => zone.Risk), Is.EqualTo(RiskLevels));
                Assert.That(zones.Single(zone => zone.Risk == 100).FromAge, Is.EqualTo(6));
            }
        }

        [Test]
        public void Zones_NoHistorySupplied_Refuses()
        {
            Assert.That(() => SleRiskCalculator.Zones(10, null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Zones_NoTargetPublished_HasNoneAtAll()
        {
            Assert.That(SleRiskCalculator.Zones(0, SixtyFinishedItems), Is.Empty);
        }

        [Test]
        public void Zones_NothingFinished_HasNoneAtAll()
        {
            Assert.That(SleRiskCalculator.Zones(10, []), Is.Empty);
        }

        [Test]
        public void Zones_AgesTooLittleHistoryCanSpeakFor_AreLeftUndrawn()
        {
            // Twelve items ran two days and only three ran longer, so nothing can be said about an
            // age above two. A boundary there would be a claim about where the odds turn, made from
            // three observations.
            int[] cycleTimes = [.. Enumerable.Repeat(2, 12), 11, 12, 13];

            var zones = SleRiskCalculator.Zones(10, cycleTimes);

            Assert.That(zones.Select(zone => zone.FromAge), Has.All.LessThanOrEqualTo(2),
                "Above the last age with enough evidence there is nothing to draw.");
        }

        [Test]
        public void Zones_ALevelNeverReachedWithinTheEvidence_IsNotInvented()
        {
            // Everything finished well inside a thirty-day target, so the risk never leaves its
            // calmest band while there is still history to say so.
            int[] cycleTimes = [.. Enumerable.Repeat(1, 40)];

            var zones = SleRiskCalculator.Zones(30, cycleTimes);

            Assert.That(zones, Is.Empty);
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
