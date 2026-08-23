using Lighthouse.Backend.Services.Implementation.Forecast;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// The numbers a forecast draws from, written out in Lighthouse rather than taken from the runtime.
    /// Nothing here can be taken on trust: the whole safety net under the simulation restructure is that
    /// two runs can be compared number for number, and that net is only as good as this class.
    /// </summary>
    [TestFixture]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class AddressableDrawStreamTest
    {
        private const long AStartingNumber = 20260824;

        private const int HowManyDrawsAShapeIsJudgedOver = 200_000;

        /// <summary>
        /// The property the whole restructure rests on. If a draw depended on how many draws came before it,
        /// putting the Teams on one clock would change every Team's numbers the moment another Team's draws
        /// were taken in between, and there would be nothing left to compare a restructured run against.
        /// </summary>
        [Test]
        public void ADraw_IsDecidedByWhereItSits_NotByWhatWasDrawnBefore()
        {
            var stream = new AddressableDrawStream(AStartingNumber);

            var askedForFirst = stream.Draw(17, 3, 42, 1, 97);

            for (var noise = 0; noise < 50; noise++)
            {
                stream.Draw(noise, noise, noise, noise, 13);
            }

            var askedForAgain = stream.Draw(17, 3, 42, 1, 97);

            Assert.That(askedForAgain, Is.EqualTo(askedForFirst),
                "The same place returned two different numbers once other draws were taken in between, so a " +
                "Team's numbers depend on how the Teams are interleaved.");
        }

        [Test]
        public void TwoStreamsFromTheSameStartingNumber_AgreeEverywhere()
        {
            var one = new AddressableDrawStream(AStartingNumber);
            var another = new AddressableDrawStream(AStartingNumber);

            var disagreements = AllTheCoordinates()
                .Where(at => one.Draw(at.Trial, at.Team, at.Day, at.Ordinal, 31) != another.Draw(at.Trial, at.Team, at.Day, at.Ordinal, 31))
                .ToList();

            Assert.That(disagreements, Is.Empty,
                "Two runs pinned to the same starting number drew different numbers, so no two runs in this " +
                "product can be compared number for number.");
        }

        [Test]
        public void TwoStreamsFromDifferentStartingNumbers_DoNotAgreeEverywhere()
        {
            var one = new AddressableDrawStream(AStartingNumber);
            var another = new AddressableDrawStream(AStartingNumber + 1);

            var agreements = AllTheCoordinates()
                .Count(at => one.Draw(at.Trial, at.Team, at.Day, at.Ordinal, 31) == another.Draw(at.Trial, at.Team, at.Day, at.Ordinal, 31));

            Assert.That(agreements, Is.LessThan(AllTheCoordinates().Count() / 2),
                "Two different starting numbers produced nearly the same run, so pinning the starting number " +
                "is not what decides the numbers and every forecast is drawing from the same place.");
        }

        /// <summary>
        /// Each coordinate has to be able to move the answer on its own. A coordinate that is quietly ignored
        /// - a day that never reaches the mixing, say - would give every day of a Team's run the same
        /// delivery, and the shape of the resulting distribution would look ordinary.
        /// </summary>
        [Test]
        [TestCase("trial")]
        [TestCase("team")]
        [TestCase("day")]
        [TestCase("ordinal")]
        public void MovingOneCoordinate_ChangesTheNumberAboutHalfTheTime(string coordinate)
        {
            var stream = new AddressableDrawStream(AStartingNumber);

            var sameAsItsNeighbour = AllTheCoordinates()
                .Count(at => stream.Draw(at.Trial, at.Team, at.Day, at.Ordinal, 2)
                    == DrawOneAlong(stream, at, coordinate));

            var howMany = AllTheCoordinates().Count();

            Assert.That(sameAsItsNeighbour, Is.InRange(howMany * 0.45, howMany * 0.55),
                $"Moving only the {coordinate} left the draw where it was far more, or far less, often than a " +
                "coin would. That coordinate is not deciding the number.");
        }

        /// <summary>
        /// A number scaled onto two ranges is readable from itself: over a range of a hundred, whether it
        /// landed in the top half would say exactly what the same draw over a range of two returned. Two
        /// draws at one place over two ranges have to be unrelated, because the forecast asks for both - how
        /// much a Team delivers is drawn over the length of its history, which Feature received an item over
        /// how many Features it is working.
        /// </summary>
        [Test]
        public void TwoDrawsAtOnePlaceOverDifferentRanges_AreUnrelated()
        {
            var stream = new AddressableDrawStream(AStartingNumber);

            var agreeingOnTheTopHalf = AllTheCoordinates()
                .Count(at => (stream.Draw(at.Trial, at.Team, at.Day, at.Ordinal, 2) == 1)
                    == (stream.Draw(at.Trial, at.Team, at.Day, at.Ordinal, 100) >= 50));

            var howMany = AllTheCoordinates().Count();

            Assert.That(agreeingOnTheTopHalf, Is.InRange(howMany * 0.45, howMany * 0.55),
                "The draw over a range of two can be read off the draw over a range of a hundred, so the two " +
                "questions the forecast asks at one place are answered by the same number.");
        }

        [Test]
        [TestCase(2)]
        [TestCase(7)]
        [TestCase(31)]
        [TestCase(100)]
        public void TheNumbersDrawn_AreSpreadEvenlyOverTheRangeAskedFor(int range)
        {
            var stream = new AddressableDrawStream(AStartingNumber);

            var howOftenEachCameUp = new int[range];

            for (var draw = 0; draw < HowManyDrawsAShapeIsJudgedOver; draw++)
            {
                howOftenEachCameUp[stream.Draw(draw / 997, draw % 13, draw / 7, draw % 5, range)]++;
            }

            var evenShare = (double)HowManyDrawsAShapeIsJudgedOver / range;

            // Four standard deviations of what an even spread would itself wander by. A fixed band would
            // either fail on a perfectly good spread over a wide range, or pass a badly skewed one over a
            // narrow one, because how far the counts scatter depends on how many buckets there are.
            var howFarAnEvenSpreadWanders = 4 * Math.Sqrt(HowManyDrawsAShapeIsJudgedOver * (1.0 / range) * (1 - 1.0 / range));

            Assert.That(howOftenEachCameUp, Is.All.InRange(evenShare - howFarAnEvenSpreadWanders, evenShare + howFarAnEvenSpreadWanders),
                $"Some numbers in [0, {range}) came up far more often than others: " +
                string.Join(", ", howOftenEachCameUp));
        }

        [Test]
        public void EveryNumberInTheRange_CanComeUp()
        {
            var stream = new AddressableDrawStream(AStartingNumber);

            var seen = Enumerable
                .Range(0, 5_000)
                .Select(draw => stream.Draw(draw, 1, draw / 3, draw % 4, 50))
                .ToHashSet();

            Assert.That(seen, Is.EquivalentTo(Enumerable.Range(0, 50)));
        }

        /// <summary>
        /// A Team with one Feature left to work on, or one day of measured delivery, has nothing to choose
        /// between. Drawing over a range of nothing has to answer zero rather than throw, because the caller
        /// indexes a list with whatever comes back.
        /// </summary>
        [Test]
        [TestCase(0)]
        [TestCase(1)]
        public void AskingOverARangeWithNothingToChooseFrom_DrawsZero(int range)
        {
            var stream = new AddressableDrawStream(AStartingNumber);

            Assert.That(stream.Draw(1, 1, 1, 1, range), Is.Zero);
        }

        private static int DrawOneAlong(AddressableDrawStream stream, (int Trial, int Team, int Day, int Ordinal) at, string coordinate)
        {
            return coordinate switch
            {
                "trial" => stream.Draw(at.Trial + 1, at.Team, at.Day, at.Ordinal, 2),
                "team" => stream.Draw(at.Trial, at.Team + 1, at.Day, at.Ordinal, 2),
                "day" => stream.Draw(at.Trial, at.Team, at.Day + 1, at.Ordinal, 2),
                "ordinal" => stream.Draw(at.Trial, at.Team, at.Day, at.Ordinal + 1, 2),
                _ => throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Not a coordinate of a draw."),
            };
        }

        private static IEnumerable<(int Trial, int Team, int Day, int Ordinal)> AllTheCoordinates()
        {
            for (var trial = 0; trial < 40; trial++)
            {
                for (var team = 0; team < 5; team++)
                {
                    for (var day = 0; day < 20; day++)
                    {
                        for (var ordinal = 0; ordinal < 5; ordinal++)
                        {
                            yield return (trial, team, day, ordinal);
                        }
                    }
                }
            }
        }
    }
}
