using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.Models
{
    public class WorkItemBaseTest
    {

        /// <summary>
        /// Bug #5567: a fixed instant on a UTC instance. The expectations below are unchanged from
        /// before the anchor moved - the point is that they no longer RE-DERIVE the production
        /// expression, which is root cause D.
        /// </summary>
        private static readonly FakeLighthouseClock Clock =
            new(new DateTimeOffset(2026, 7, 27, 10, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);
        [Test]
        [TestCase(StateCategories.Unknown)]
        [TestCase(StateCategories.Doing)]
        [TestCase(StateCategories.ToDo)]
        public void GetCycleTime_GivenNotClosedItem_Returns0(StateCategories state)
        {
            var subject = CreateSubject();

            subject.StartedDate = DateTime.UtcNow.AddDays(-2);
            subject.ClosedDate = DateTime.UtcNow.AddDays(-1);
            subject.StateCategory = state;

            var cycleTime = subject.CycleTime(TestToday.Zone);

            Assert.That(cycleTime, Is.Zero);
        }

        [Test]
        public void GetCycleTime_ItemClosed_NoStartedDate_UsesCreatedDateForDifference()
        {
            var subject = CreateSubject();
            subject.ClosedDate = DateTime.UtcNow.AddDays(-1);
            subject.CreatedDate = DateTime.UtcNow.AddDays(-2);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime(TestToday.Zone);
            
            Assert.That(cycleTime, Is.EqualTo(2));
        }

        /// <summary>
        /// Cycle time measures the time spent WORKING on an item, so a started date always wins over
        /// the created date - reading them the other way round silently re-labels queue time as
        /// cycle time and inflates every percentile the product is built on.
        /// </summary>
        [Test]
        public void GetCycleTime_ItemClosed_HasBothDates_MeasuresFromStartedNotCreated()
        {
            var subject = CreateSubject();
            subject.CreatedDate = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);
            subject.StartedDate = new DateTime(2026, 7, 24, 8, 0, 0, DateTimeKind.Utc);
            subject.ClosedDate = new DateTime(2026, 7, 27, 16, 0, 0, DateTimeKind.Utc);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime(TimeZoneInfo.Utc);

            Assert.That(cycleTime, Is.EqualTo(4));
        }

        [Test]
        public void GetCycleTime_ItemClosed_NoStartedDate_NoCreatedDate_Returns1()
        {
            var subject = CreateSubject();
            subject.ClosedDate = DateTime.UtcNow.AddDays(-1);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime(TestToday.Zone);
            
            Assert.That(cycleTime, Is.EqualTo(1));
        }

        [Test]
        public void GetCycleTime_ItemClosed_NoClosedDate_Returns1()
        {
            var subject = CreateSubject();
            subject.StartedDate = DateTime.UtcNow.AddDays(-2);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime(TestToday.Zone);
            
            Assert.That(cycleTime, Is.EqualTo(1));
        }

        [Test]
        public void GetCycleTime_ItemClosed_StartedDateAfterClosedDate_Returns1()
        {
            var subject = CreateSubject();
            subject.ClosedDate = DateTime.UtcNow.AddDays(-15);
            subject.StartedDate = DateTime.UtcNow.AddDays(-1);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime(TestToday.Zone);
            
            Assert.That(cycleTime, Is.EqualTo(1));
        }

        [Test]
        public void GetCycleTime_ItemClosed_StartedDateBeforeClosedDate_ReturnsCycleTime()
        {
            var subject = CreateSubject();

            subject.ClosedDate = DateTime.UtcNow.AddDays(-1);
            subject.StartedDate = DateTime.UtcNow.AddDays(-2);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime(TestToday.Zone);

            Assert.That(cycleTime, Is.EqualTo(2));
        }

        [Test]
        public void GetCycleTime_ItemClosed_StartedDateAndClosedDateOnSameDay_Returns1()
        {
            var subject = CreateSubject();

            subject.ClosedDate = DateTime.UtcNow.AddDays(-1);
            subject.StartedDate = DateTime.UtcNow.AddDays(-1);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime(TestToday.Zone);

            Assert.That(cycleTime, Is.EqualTo(1));
        }

        [Test]
        public void GetCycleTime_ItemClosed_StartedDateAndClosedDateMinutesAwayOnDifferentDays_Returns1()
        {
            var subject = CreateSubject();

            subject.StartedDate = new DateTime(2024, 4, 7, 23, 59, 59, DateTimeKind.Utc);
            subject.ClosedDate = new DateTime(2024, 4, 8, 0, 0, 0, DateTimeKind.Utc);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime(TestToday.Zone);

            Assert.That(cycleTime, Is.EqualTo(2));
        }

        [Test]
        [TestCase(StateCategories.Unknown)]
        [TestCase(StateCategories.Done)]
        [TestCase(StateCategories.ToDo)]
        public void GetWorkItemAge_GivenNotInProgressItem_Returns0(StateCategories state)
        {
            var subject = CreateSubject();

            subject.StartedDate = DateTime.UtcNow.AddDays(-2);
            subject.StateCategory = state;

            var workItemAge = subject.WorkItemAge(Clock.Zone, Clock.Today);

            Assert.That(workItemAge, Is.Zero);
        }

        [Test]
        public void GetWorkItemAge_ItemInProgress_NoStartedDate_FallsBackToCreatedDate()
        {
            var subject = CreateSubject();
            subject.CreatedDate = Clock.TodayAsUtcMidnight.AddDays(-1);
            subject.StateCategory = StateCategories.Doing;

            var workItemAge = subject.WorkItemAge(Clock.Zone, Clock.Today);

            Assert.That(workItemAge, Is.EqualTo(2));
        }

        /// <summary>
        /// The fallback above is a fallback, not a preference: once an item HAS a started date, age
        /// is measured from when work began, never from when the item was filed. An item sitting in
        /// the backlog for a month before anyone picked it up is one day old on its first day of
        /// work, and it is that number Lighthouse writes back into Jira/ADO.
        /// </summary>
        [Test]
        public void GetWorkItemAge_ItemInProgress_HasBothDates_MeasuresFromStartedNotCreated()
        {
            var subject = CreateSubject();
            subject.CreatedDate = Clock.TodayAsUtcMidnight.AddDays(-30);
            subject.StartedDate = Clock.TodayAsUtcMidnight.AddDays(-3);
            subject.StateCategory = StateCategories.Doing;

            var workItemAge = subject.WorkItemAge(Clock.Zone, Clock.Today);

            Assert.That(workItemAge, Is.EqualTo(4));
        }

        [Test]
        public void GetWorkItemAge_ItemInProgress_NoStartedDate_NoCreatedDate_Returns1()
        {
            var subject = CreateSubject();
            subject.StateCategory = StateCategories.Doing;

            var workItemAge = subject.WorkItemAge(Clock.Zone, Clock.Today);

            Assert.That(workItemAge, Is.EqualTo(1));
        }

        [Test]
        public void GetWorkItemAge_ItemInProgress_StartedDateAfterToday_Returns1()
        {
            var subject = CreateSubject();
            subject.StartedDate = Clock.TodayAsUtcMidnight.AddDays(1);
            subject.StateCategory = StateCategories.Doing;

            var workItemAge = subject.WorkItemAge(Clock.Zone, Clock.Today);

            Assert.That(workItemAge, Is.EqualTo(1));
        }

        [Test]
        public void GetWorkItemAge_ItemInProgress_StartedDateBeforeToday_ReturnsWorkItemAge()
        {
            var subject = CreateSubject();

            subject.StartedDate = Clock.TodayAsUtcMidnight.AddDays(-1);
            subject.StateCategory = StateCategories.Doing;

            var workItemAge = subject.WorkItemAge(Clock.Zone, Clock.Today);

            Assert.That(workItemAge, Is.EqualTo(2));
        }

        [Test]
        public void GetWorkItemAge_ItemInProgress_StartedDateAndTodayOnSameDay_Returns1()
        {
            var subject = CreateSubject();

            subject.StartedDate = Clock.TodayAsUtcMidnight.AddHours(10);
            subject.StateCategory = StateCategories.Doing;

            var workItemAge = subject.WorkItemAge(Clock.Zone, Clock.Today);

            Assert.That(workItemAge, Is.EqualTo(1));
        }

        [Test]
        public void AdditionalFieldValues_EmptyByDefault()
        {
            var subject = CreateSubject();

            Assert.That(subject.AdditionalFieldValues, Is.Empty);
        }

        [Test]
        public void AdditionalFieldValues_CanSetAndRetrieveValues()
        {
            var subject = CreateSubject();
            subject.AdditionalFieldValues[1] = "value1";
            subject.AdditionalFieldValues[2] = "value2";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.AdditionalFieldValues, Has.Count.EqualTo(2));
                Assert.That(subject.AdditionalFieldValues[1], Is.EqualTo("value1"));
                Assert.That(subject.AdditionalFieldValues[2], Is.EqualTo("value2"));
            }
        }

        [Test]
        public void AdditionalFieldValues_SupportsNullValues()
        {
            var subject = CreateSubject();
            subject.AdditionalFieldValues[1] = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.AdditionalFieldValues.ContainsKey(1), Is.True);
                Assert.That(subject.AdditionalFieldValues[1], Is.Null);
            }
        }

        [Test]
        public void Update_CopiesAdditionalFieldValues()
        {
            var source = CreateSubject();
            source.AdditionalFieldValues[1] = "value1";
            source.AdditionalFieldValues[2] = "value2";

            var target = CreateSubject();
            target.Update(source);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(target.AdditionalFieldValues, Has.Count.EqualTo(2));
                Assert.That(target.AdditionalFieldValues[1], Is.EqualTo("value1"));
                Assert.That(target.AdditionalFieldValues[2], Is.EqualTo("value2"));
            }
        }

        private static WorkItemBase CreateSubject()
        {
            return new WorkItemBase();
        }
    }
}
