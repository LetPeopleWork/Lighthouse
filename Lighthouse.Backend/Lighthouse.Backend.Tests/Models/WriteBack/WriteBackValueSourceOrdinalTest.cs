using System.Reflection;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Tests.Models.WriteBack
{
    /// <summary>
    /// The write-back mapping stores its source as an int, so this enum's ordinals are data that has
    /// already shipped, on every customer database. Inserting a member - or reordering two - silently
    /// re-points every stored mapping at a different source: a field that was receiving an 85th
    /// percentile completion date starts receiving something else, with nothing to notice it by.
    ///
    /// These tests exist so that change cannot be made quietly. Appending is safe and is the only safe
    /// edit.
    /// </summary>
    public class WriteBackValueSourceOrdinalTest
    {
        [Test]
        [TestCase(WriteBackValueSource.WorkItemAgeCycleTime, 0)]
        [TestCase(WriteBackValueSource.FeatureSize, 1)]
        [TestCase(WriteBackValueSource.ForecastPercentile50, 2)]
        [TestCase(WriteBackValueSource.ForecastPercentile70, 3)]
        [TestCase(WriteBackValueSource.ForecastPercentile85, 4)]
        [TestCase(WriteBackValueSource.ForecastPercentile95, 5)]
        [TestCase(WriteBackValueSource.SleRisk, 6)]
        public void EveryShippedSourceKeepsTheOrdinalItWasStoredUnder(WriteBackValueSource source, int storedValue)
        {
            Assert.That((int)source, Is.EqualTo(storedValue),
                $"{source} has moved. Every mapping already stored as {storedValue} now points somewhere else.");
        }

        [Test]
        [TestCase(WriteBackValueSource.ForecastedStartPercentile50, 7)]
        [TestCase(WriteBackValueSource.ForecastedStartPercentile70, 8)]
        [TestCase(WriteBackValueSource.ForecastedStartPercentile85, 9)]
        [TestCase(WriteBackValueSource.ForecastedStartPercentile95, 10)]
        public void TheStartSourcesWereAppendedAfterEverythingThatCameBefore(WriteBackValueSource source, int storedValue)
        {
            Assert.That((int)source, Is.EqualTo(storedValue));
        }

        /// <summary>
        /// Every member is pinned by one of the tests above, and this is what keeps that true. Without it a
        /// member added tomorrow is simply unpinned - it has no stored ordinal to disagree with, so nothing
        /// reds, and the next member after that can be inserted in front of it freely.
        ///
        /// It names the unpinned member rather than counting, because a count fails on the one edit the
        /// class calls safe and teaches whoever reads it to bump a number until the message goes away.
        /// </summary>
        [Test]
        public void EveryMemberOfTheEnumIsPinnedByOneOfTheTestsAbove()
        {
            var pinned = TheOrdinalsThoseTestsPin();

            var unpinned = Enum.GetValues<WriteBackValueSource>()
                .Where(source => !pinned.ContainsKey(source))
                .ToList();

            Assert.That(unpinned, Is.Empty,
                $"Add a [TestCase] pinning the stored ordinal of: {string.Join(", ", unpinned)}. "
                + "Appending is safe; leaving a member unpinned is what lets the next edit stop being safe.");
        }

        /// <summary>
        /// Read off the attributes rather than written out again, so this cannot drift from the tests it
        /// claims to describe - a second hand-kept list would be the very thing these tests exist to stop.
        /// </summary>
        private static Dictionary<WriteBackValueSource, int> TheOrdinalsThoseTestsPin()
        {
            return typeof(WriteBackValueSourceOrdinalTest)
                .GetMethods()
                .SelectMany(method => method.GetCustomAttributes<TestCaseAttribute>())
                .Where(testCase => testCase.Arguments is [WriteBackValueSource, int])
                .ToDictionary(
                    testCase => (WriteBackValueSource)testCase.Arguments[0]!,
                    testCase => (int)testCase.Arguments[1]!);
        }
    }
}
