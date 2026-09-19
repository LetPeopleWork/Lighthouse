using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// The arithmetic on its own. The acceptance scenarios in API/Integration/SleRisk/ pin what a
    /// caller of the endpoint sees; these pin the boundaries the formula rests on, which are cheaper
    /// to state here than to seed a database for.
    /// </summary>
    public class SleRiskCalculatorTest
    {
        /// <summary>
        /// The worked distribution from the Epic, three times over. The proportions are the Epic's
        /// own — six in twenty ran past ten days.
        /// </summary>
        private static readonly int[] SixtyFinishedItems =
            [.. Enumerable.Repeat<int[]>([1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 8, 9, 11, 13, 15, 18, 22, 30], 3).SelectMany(x => x)];

        /// <summary>
        /// Holds work that finished on the target day and met it. Without such items the target day
        /// cannot be told apart from the day after — every comparable item is a breach either way —
        /// and a test written on a population without them passes whichever comparison the code uses.
        /// </summary>
        private static readonly int[] WorkThatFinishedOnTheTargetDay =
            [.. Enumerable.Repeat(3, 4), .. Enumerable.Repeat(6, 5), .. Enumerable.Repeat(7, 5)];

        [TestCase(2, 32)]
        [TestCase(5, 46)]
        [TestCase(9, 86)]
        public void For_ItemStillOpen_IsTheShareOfComparableItemsThatWentOnToMiss(int ageInDays, int expected)
        {
            var risk = SleRiskCalculator.For(ageInDays, 10, SixtyFinishedItems);

            Assert.That(risk, Is.EqualTo(expected));
        }

        // --- The day the item is due, and the day after ---

        [Test]
        public void For_ItemExactlyOnTheTargetDay_IsStillComputedFromTheHistory()
        {
            // Five items took exactly six days and met a six-day target; five took seven and missed.
            // An item at six days can still close today, so it is asked of the history like any
            // other — the answer is half, not certainty.
            //
            // This is the boundary the whole feature turns on, and it needs a population containing
            // work that finished exactly on the target. On one without any, the comparable set and
            // the breach set are identical and the assertion holds whether the code compares with
            // "greater than" or "greater than or equal".
            var risk = SleRiskCalculator.For(6, 6, WorkThatFinishedOnTheTargetDay);

            Assert.That(risk, Is.EqualTo(50),
                "An item on its target day can still meet the target, so the history answers for it.");
        }

        [Test]
        public void For_ItemOneDayPastTheTarget_IsCertainWithoutConsultingTheHistory()
        {
            // Nothing here ran seven days or more, so there is no comparable work at all. The answer
            // is a hundred regardless, because an item already past its target cannot finish inside
            // it — which is why the check sits ahead of the counting rather than after it.
            int[] nothingRanThatLong = [.. Enumerable.Repeat(3, 4), .. Enumerable.Repeat(6, 5)];

            var risk = SleRiskCalculator.For(7, 6, nothingRanThatLong);

            Assert.That(risk, Is.EqualTo(100),
                "Past the target the answer owes no evidence; a zero here would be the guard order being wrong.");
        }

        [Test]
        public void For_ItemFarPastTheTarget_IsCertainRatherThanUnanswered()
        {
            var risk = SleRiskCalculator.For(40, 10, SixtyFinishedItems);

            Assert.That(risk, Is.EqualTo(100));
        }

        // --- What the history is asked, and how it is counted ---

        [Test]
        public void For_HalfwayBetweenTwoWholePercentages_RoundsToTheWorseOne()
        {
            // Two of sixteen is 12.5%, the halfway case.
            int[] cycleTimes = [.. Enumerable.Repeat(5, 14), 12, 12];

            var risk = SleRiskCalculator.For(5, 10, cycleTimes);

            Assert.That(risk, Is.EqualTo(13), "12.5 rounds up rather than to the even 12.");
        }

        [Test]
        public void For_ItemThatFinishedOnTheTargetDay_IsNotCountedAsAMiss()
        {
            // Twelve comparable items; only the eight past ten days missed. Counting the four that
            // took exactly ten would make it twelve in twelve.
            int[] cycleTimes = [.. Enumerable.Repeat(10, 4), .. Enumerable.Repeat(12, 4), .. Enumerable.Repeat(14, 4)];

            var risk = SleRiskCalculator.For(3, 10, cycleTimes);

            Assert.That(risk, Is.EqualTo(67));
        }

        [Test]
        public void For_FinishedItemAsOldAsTheOpenOne_IsStillOneOfItsComparableItems()
        {
            // At age five the comparable items are the fives and the sevens. Dropping the ones that
            // took exactly five would leave only items that missed, and the answer would read 100.
            int[] cycleTimes = [.. Enumerable.Repeat(3, 4), .. Enumerable.Repeat(5, 5), .. Enumerable.Repeat(7, 5)];

            var risk = SleRiskCalculator.For(5, 6, cycleTimes);

            Assert.That(risk, Is.EqualTo(50));
        }

        // --- A history with nothing to say ---

        [Test]
        public void For_NothingFinishedEverRanThisLongAndTheItemIsInsideItsTarget_IsZero()
        {
            // A share of an empty set is undefined, and zero is the least wrong total answer: the
            // item can still meet its target, so a hundred would be false. The cost is a cliff —
            // zero up to the target and a hundred the day after, with nothing in between.
            int[] cycleTimes = [2, 3, 4, 5, 6];

            var risk = SleRiskCalculator.For(20, 30, cycleTimes);

            Assert.That(risk, Is.Zero);
        }

        [Test]
        public void For_ThinHistory_AnswersRatherThanStayingSilent()
        {
            // Nine comparable items is a small denominator and the answer moves sharply when one
            // arrives or leaves. It is still the team's own history, and showing it beats refusing
            // to say anything on the day an item is due.
            int[] cycleTimes = [.. Enumerable.Repeat(20, 9), .. Enumerable.Repeat(1, 50)];

            var risk = SleRiskCalculator.For(20, 30, cycleTimes);

            Assert.That(risk, Is.Zero, "Nine items, none of which missed a thirty-day target.");
        }

        [Test]
        public void For_NothingFinishedAtAllAndTheItemIsInsideItsTarget_IsZero()
        {
            var risk = SleRiskCalculator.For(4, 10, []);

            Assert.That(risk, Is.Zero);
        }

        // --- What the caller must not pass ---

        [TestCase(0)]
        [TestCase(-3)]
        public void For_AgeThatCannotBeRead_Refuses(int ageInDays)
        {
            // An item whose start is missing or still in the future. The service filters these out
            // before asking, so arriving here at all is a caller's mistake rather than a silence to
            // absorb — and a zero would read as the safest item on the board.
            Assert.That(() => SleRiskCalculator.For(ageInDays, 10, SixtyFinishedItems),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void For_NoTargetPublished_Refuses(int targetRangeInDays)
        {
            // A team with no target has made no promise; the service returns an empty collection
            // before reaching here rather than asking what the risk against nothing is.
            Assert.That(() => SleRiskCalculator.For(5, targetRangeInDays, SixtyFinishedItems),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void For_NoHistorySupplied_Refuses()
        {
            Assert.That(() => SleRiskCalculator.For(5, 10, null!), Throws.ArgumentNullException);
        }

        // --- The shape of the answer across every age ---

        [Test]
        public void For_AcrossEveryAgeUpToAndPastTheTarget_NeverFalls()
        {
            // The denominator only shrinks as the age rises while the numerator holds, and past the
            // target the answer is a hundred. So the curve can never dip.
            //
            // Nothing enumerated above catches a dip on its own. This exists as a tripwire for a
            // later change that smooths, clamps, or softens the number somewhere in the middle — the
            // kind of thing that looks kind on one screen and makes the column unsortable.
            var risks = Enumerable.Range(1, 12)
                .Select(age => SleRiskCalculator.For(age, 10, SixtyFinishedItems))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(risks, Is.Ordered, "The risk must never fall as an item gets older.");
                Assert.That(risks.Distinct().Count(), Is.GreaterThan(1),
                    "A flat walk would satisfy the ordering while testing nothing.");
            }
        }

        // --- What the risk rests on, which is a different question from what the risk is ---

        [Test]
        public void FinishedItemsStillOpenAtThisAge_WorkTheTeamFinishedThatRanAtLeastThisLong_IsCounted()
        {
            // Of the twenty distinct durations, repeated three times: nine, eleven, thirteen, fifteen,
            // eighteen and twenty-two are nine days or more, plus thirty. Seven each of three.
            var stillOpen = SleRiskCalculator.FinishedItemsStillOpenAtThisAge(9, SixtyFinishedItems);

            Assert.That(stillOpen, Is.EqualTo(21));
        }

        [Test]
        public void FinishedItemsStillOpenAtThisAge_NothingTheTeamFinishedEverRanThisLong_IsZero()
        {
            var stillOpen = SleRiskCalculator.FinishedItemsStillOpenAtThisAge(40, SixtyFinishedItems);

            Assert.That(stillOpen, Is.Zero,
                "Nothing the team finished ran forty days, and saying so is the point of the number.");
        }

        [Test]
        public void FinishedItemsStillOpenAtThisAge_ItemPastTheTarget_StillCountsTheHistory()
        {
            // The discriminating one. This method takes no target, so there is no certainty rule for
            // it to short-circuit on, and an age far past any plausible target still describes the
            // history rather than reporting nothing. It reds if somebody later "aligns" this method
            // with the risk by adding the past-the-target clause — which would zero the count for
            // exactly the items whose risk was decided without consulting the history at all.
            var stillOpen = SleRiskCalculator.FinishedItemsStillOpenAtThisAge(11, SixtyFinishedItems);

            Assert.That(stillOpen, Is.EqualTo(18),
                "Eleven days or more: eleven, thirteen, fifteen, eighteen, twenty-two and thirty, three times each.");
        }

        [Test]
        public void FinishedItemsStillOpenAtThisAge_WorkThatFinishedOnExactlyThisDay_IsCounted()
        {
            // At least as long, not longer. An item that took exactly six days was still open when
            // the sixth day began, so it is one of the items an answer at six days rests on — and it
            // is counted here for the same reason the risk counts it as comparable.
            var stillOpen = SleRiskCalculator.FinishedItemsStillOpenAtThisAge(6, WorkThatFinishedOnTheTargetDay);

            Assert.That(stillOpen, Is.EqualTo(10),
                "Five items finished on exactly the sixth day and five on the seventh.");
        }
    }
}
