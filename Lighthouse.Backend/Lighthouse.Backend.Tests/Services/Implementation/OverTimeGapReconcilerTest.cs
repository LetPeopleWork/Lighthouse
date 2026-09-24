using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.BackgroundServices;
using Lighthouse.Backend.Tests.TestDoubles;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [Category("story-6053-reconstruct-over-time-history")]
    public class OverTimeGapReconcilerTest
    {
        private static readonly DateOnly Today = new(2026, 9, 22);

        /// <summary>
        /// A picker can be set to end after today. No day after today has happened, so none is missing:
        /// asking for one would hand a pass a day that nothing observed.
        /// </summary>
        [Test]
        public void APeriodReachingPastToday_AsksForNoDayAfterToday()
        {
            OverTimeFillRequest? asked = null;
            var filler = new Mock<IOverTimeHistoryFiller>();
            filler.Setup(f => f.AskFor(It.IsAny<OverTimeFillRequest>())).Callback<OverTimeFillRequest>(request => asked = request);

            var fillSwitch = new Mock<IOverTimeHistoryFillSwitch>();
            fillSwitch.Setup(s => s.IsSwitchedOn()).Returns(true);

            var clock = new FakeLighthouseClock(new DateTimeOffset(2026, 9, 22, 9, 0, 0, TimeSpan.Zero));
            var subject = new OverTimeGapReconciler(clock, filler.Object, new ReconstructionMemo(), fillSwitch.Object);

            subject.AskForTheDaysThatAreMissing(1, OwnerType.Team, Today.AddDays(-2), Today.AddDays(3), []);

            Assert.That(asked?.CandidateDays, Is.EqualTo(new List<DateOnly> { Today.AddDays(-2), Today.AddDays(-1), Today }));
        }
    }
}
