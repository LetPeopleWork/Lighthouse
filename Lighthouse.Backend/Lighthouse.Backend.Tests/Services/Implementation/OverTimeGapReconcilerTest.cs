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

        /// <summary>
        /// A period with no start is the whole history, and where that begins is only known to a pass.
        /// </summary>
        [Test]
        public void APeriodWithNoStart_AsksForNothing()
        {
            var filler = new Mock<IOverTimeHistoryFiller>();

            ReconcilerOver(filler, new ReconstructionMemo(), switchedOn: true)
                .AskForTheDaysThatAreMissing(1, OwnerType.Team, null, Today, []);

            filler.Verify(f => f.AskFor(It.IsAny<OverTimeFillRequest>()), Times.Never);
        }

        [Test]
        public void APeriodTheChartAlreadyHoldsInFull_AsksForNothing()
        {
            var filler = new Mock<IOverTimeHistoryFiller>();

            ReconcilerOver(filler, new ReconstructionMemo(), switchedOn: true)
                .AskForTheDaysThatAreMissing(1, OwnerType.Team, Today.AddDays(-1), Today, [Today.AddDays(-1), Today]);

            filler.Verify(f => f.AskFor(It.IsAny<OverTimeFillRequest>()), Times.Never);
        }

        [Test]
        public void MissingDaysWhileTheFillIsSwitchedOff_AreNotAskedFor()
        {
            var filler = new Mock<IOverTimeHistoryFiller>();

            ReconcilerOver(filler, new ReconstructionMemo(), switchedOn: false)
                .AskForTheDaysThatAreMissing(1, OwnerType.Team, Today.AddDays(-3), Today, []);

            filler.Verify(f => f.AskFor(It.IsAny<OverTimeFillRequest>()), Times.Never);
        }

        [Test]
        public void DaysTheChartHoldsAndDaysNoPassCanWrite_AreLeftOutOfTheAsk()
        {
            OverTimeFillRequest? asked = null;
            var filler = new Mock<IOverTimeHistoryFiller>();
            filler.Setup(f => f.AskFor(It.IsAny<OverTimeFillRequest>())).Callback<OverTimeFillRequest>(request => asked = request);
            var memo = new ReconstructionMemo();
            memo.TheWalkHasAlreadyWorkedOut(1, OwnerType.Team, Today.AddDays(-2));

            ReconcilerOver(filler, memo, switchedOn: true)
                .AskForTheDaysThatAreMissing(1, OwnerType.Team, Today.AddDays(-3), Today, [Today.AddDays(-1)]);

            Assert.That(asked?.CandidateDays, Is.EqualTo(new List<DateOnly> { Today.AddDays(-3), Today }));
        }

        /// <summary>
        /// A hand-typed range spanning centuries must not queue centuries of days. The bound sits at
        /// ten years of days, well past any period someone would actually open.
        /// </summary>
        [Test]
        public void APeriodSpanningCenturies_HandsOverTenYearsOfDaysAndNoMore()
        {
            const int tenYearsOfDays = 10 * 366;
            OverTimeFillRequest? asked = null;
            var filler = new Mock<IOverTimeHistoryFiller>();
            filler.Setup(f => f.AskFor(It.IsAny<OverTimeFillRequest>())).Callback<OverTimeFillRequest>(request => asked = request);

            ReconcilerOver(filler, new ReconstructionMemo(), switchedOn: true)
                .AskForTheDaysThatAreMissing(1, OwnerType.Team, Today.AddYears(-200), Today, []);

            Assert.That(asked?.CandidateDays, Has.Count.EqualTo(tenYearsOfDays));
        }

        private static OverTimeGapReconciler ReconcilerOver(Mock<IOverTimeHistoryFiller> filler, ReconstructionMemo memo, bool switchedOn)
        {
            var fillSwitch = new Mock<IOverTimeHistoryFillSwitch>();
            fillSwitch.Setup(s => s.IsSwitchedOn()).Returns(switchedOn);

            var clock = new FakeLighthouseClock(new DateTimeOffset(2026, 9, 22, 9, 0, 0, TimeSpan.Zero));

            return new OverTimeGapReconciler(clock, filler.Object, memo, fillSwitch.Object);
        }
    }
}
