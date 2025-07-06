using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class WorkItemBaseTest
    {
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

            var cycleTime = subject.CycleTime;

            Assert.That(cycleTime, Is.Zero);
        }

        [Test]
        public void GetCycleTime_ItemClosed_NoStartedDate_Returns0()
        {
            var subject = CreateSubject();
            subject.ClosedDate = DateTime.UtcNow.AddDays(-1);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime;
            
            Assert.That(cycleTime, Is.Zero);
        }

        [Test]
        public void GetCycleTime_ItemClosed_NoClosedDate_Returns0()
        {
            var subject = CreateSubject();
            subject.StartedDate = DateTime.UtcNow.AddDays(-2);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime;
            
            Assert.That(cycleTime, Is.Zero);
        }

        [Test]
        public void GetCycleTime_ItemClosed_StartedDateAfterClosedDate_Returns0()
        {
            var subject = CreateSubject();
            subject.ClosedDate = DateTime.UtcNow.AddDays(-15);
            subject.StartedDate = DateTime.UtcNow.AddDays(-1);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime;
            
            Assert.That(cycleTime, Is.Zero);
        }

        [Test]
        public void GetCycleTime_ItemClosed_StartedDateBeforeClosedDate_ReturnsCycleTime()
        {
            var subject = CreateSubject();

            subject.ClosedDate = DateTime.UtcNow.AddDays(-1);
            subject.StartedDate = DateTime.UtcNow.AddDays(-2);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime;

            Assert.That(cycleTime, Is.EqualTo(2));
        }

        [Test]
        public void GetCycleTime_ItemClosed_StartedDateAndClosedDateOnSameDay_Returns1()
        {
            var subject = CreateSubject();

            subject.ClosedDate = DateTime.UtcNow.AddDays(-1);
            subject.StartedDate = DateTime.UtcNow.AddDays(-1);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime;

            Assert.That(cycleTime, Is.EqualTo(1));
        }

        [Test]
        public void GetCycleTime_ItemClosed_StartedDateAndClosedDateMinutesAwayOnDifferentDays_Returns1()
        {
            var subject = CreateSubject();

            subject.StartedDate = new DateTime(2024, 4, 7, 23, 59, 59, DateTimeKind.Utc);
            subject.ClosedDate = new DateTime(2024, 4, 8, 0, 0, 0, DateTimeKind.Utc);
            subject.StateCategory = StateCategories.Done;

            var cycleTime = subject.CycleTime;

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

            var workItemAge = subject.WorkItemAge;

            Assert.That(workItemAge, Is.Zero);
        }

        [Test]
        public void GetWorkItemAge_ItemInProgress_NoStartedDate_Returns0()
        {
            var subject = CreateSubject();
            subject.StateCategory = StateCategories.Doing;

            var workItemAge = subject.WorkItemAge;
            
            Assert.That(workItemAge, Is.Zero);
        }

        [Test]
        public void GetWorkItemAge_ItemInProgress_StartedDateAfterToday_Returns0()
        {
            var subject = CreateSubject();
            subject.StartedDate = DateTime.UtcNow.AddDays(1);
            subject.StateCategory = StateCategories.Doing;

            var workItemAge = subject.WorkItemAge;
            
            Assert.That(workItemAge, Is.Zero);
        }

        [Test]
        public void GetWorkItemAge_ItemInProgress_StartedDateBeforeToday_ReturnsWorkItemAge()
        {
            var subject = CreateSubject();

            subject.StartedDate = DateTime.UtcNow.AddDays(-1);
            subject.StateCategory = StateCategories.Doing;

            var workItemAge = subject.WorkItemAge;

            Assert.That(workItemAge, Is.EqualTo(2));
        }

        [Test]
        public void GetWorkItemAge_ItemInProgress_StartedDateAndTodayOnSameDay_Returns1()
        {
            var subject = CreateSubject();

            subject.StartedDate = DateTime.UtcNow;
            subject.StateCategory = StateCategories.Doing;

            var workItemAge = subject.WorkItemAge;

            Assert.That(workItemAge, Is.EqualTo(1));
        }

        private static WorkItemBase CreateSubject()
        {
            return new WorkItemBase();
        }
    }
}
