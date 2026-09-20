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
        /// The per-member tests above cannot see a member added in the middle and every later member
        /// shifted, because the shifted ones were never named. Counting catches that: a new member
        /// changes this number, and the only way to change it without reddening the tests above is to
        /// have appended.
        /// </summary>
        [Test]
        public void TheEnumHasExactlyTheMembersThoseOrdinalsAccountFor()
        {
            Assert.That(Enum.GetValues<WriteBackValueSource>(), Has.Length.EqualTo(11),
                "A member was added or removed. Appending is safe - anything else re-points stored mappings.");
        }
    }
}
