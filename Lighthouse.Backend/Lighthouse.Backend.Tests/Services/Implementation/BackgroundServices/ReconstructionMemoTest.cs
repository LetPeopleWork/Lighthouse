using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices
{
    [Category("story-6053-reconstruct-over-time-history")]
    public class ReconstructionMemoTest
    {
        /// <summary>
        /// The memo's bounds, restated rather than read off the class: past either one what is held is
        /// dropped, and these tests pin that it is dropped at the bound and not one later or sooner.
        /// </summary>
        private const int OwnersRemembered = 256;

        private const int DaysRememberedPerOwner = 512;

        private const int OwnerId = 4;

        private static readonly DateOnly FirstFinished = new(2026, 3, 10);

        private ReconstructionMemo subject = null!;

        [SetUp]
        public void Setup()
        {
            subject = new ReconstructionMemo();
        }

        [Test]
        public void AnOwnerNoPassHasLookedAt_HasNoDayRuledOut()
        {
            Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, FirstFinished.AddYears(-10)), Is.False);
        }

        [Test]
        public void DaysBeforeTheFirstFinishedItem_AreRuledOut_AndTheDayItFinishedOnIsNot()
        {
            subject.TheWalkReachesBackNoFurtherThan(OwnerId, OwnerType.Team, FirstFinished);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, FirstFinished.AddDays(-1)), Is.True);
                Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, FirstFinished), Is.False);
                Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, FirstFinished.AddDays(1)), Is.False);
            }
        }

        [Test]
        public void AnOwnerThatHasNeverFinishedAnything_HasEveryDayRuledOut()
        {
            subject.TheWalkReachesBackNoFurtherThan(OwnerId, OwnerType.Team, null);

            Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, new DateOnly(2026, 9, 1)), Is.True);
        }

        [Test]
        public void ADayAlreadyWorkedOut_IsRuledOut_AndTheDayBesideItIsNot()
        {
            var workedOut = FirstFinished.AddDays(20);

            subject.TheWalkReachesBackNoFurtherThan(OwnerId, OwnerType.Team, FirstFinished);
            subject.TheWalkHasAlreadyWorkedOut(OwnerId, OwnerType.Team, workedOut);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, workedOut), Is.True);
                Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, workedOut.AddDays(1)), Is.False);
            }
        }

        [Test]
        public void WhatIsKnownAboutATeam_SaysNothingAboutThePortfolioWithTheSameId()
        {
            subject.TheWalkHasAlreadyWorkedOut(OwnerId, OwnerType.Team, FirstFinished);

            Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Portfolio, FirstFinished), Is.False);
        }

        [Test]
        public async Task ATeamRefresh_ForgetsWhatWasKnownAboutThatTeam_AndOnlyThatTeam()
        {
            const int otherTeam = OwnerId + 1;
            subject.TheWalkHasAlreadyWorkedOut(OwnerId, OwnerType.Team, FirstFinished);
            subject.TheWalkHasAlreadyWorkedOut(otherTeam, OwnerType.Team, FirstFinished);

            await subject.HandleAsync(new TeamDataRefreshed(OwnerId), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, FirstFinished), Is.False);
                Assert.That(subject.NoPassCanWrite(otherTeam, OwnerType.Team, FirstFinished), Is.True);
            }
        }

        [Test]
        public async Task APortfolioRefresh_ForgetsWhatWasKnownAboutThatPortfolio()
        {
            subject.TheWalkHasAlreadyWorkedOut(OwnerId, OwnerType.Portfolio, FirstFinished);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(OwnerId), CancellationToken.None);

            Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Portfolio, FirstFinished), Is.False);
        }

        [Test]
        public void AsManyOwnersAsAreRemembered_AreAllStillRemembered()
        {
            for (var ownerId = 1; ownerId <= OwnersRemembered; ownerId++)
            {
                subject.TheWalkHasAlreadyWorkedOut(ownerId, OwnerType.Team, FirstFinished);
            }

            Assert.That(subject.NoPassCanWrite(1, OwnerType.Team, FirstFinished), Is.True);
        }

        [Test]
        public void OneOwnerMoreThanAreRemembered_StartsTheMemoOver()
        {
            for (var ownerId = 1; ownerId <= OwnersRemembered + 1; ownerId++)
            {
                subject.TheWalkHasAlreadyWorkedOut(ownerId, OwnerType.Team, FirstFinished);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.NoPassCanWrite(1, OwnerType.Team, FirstFinished), Is.False);
                Assert.That(subject.NoPassCanWrite(OwnersRemembered + 1, OwnerType.Team, FirstFinished), Is.True);
            }
        }

        [Test]
        public void AsManyDaysAsAreRemembered_AreAllStillRemembered()
        {
            for (var offset = 0; offset < DaysRememberedPerOwner; offset++)
            {
                subject.TheWalkHasAlreadyWorkedOut(OwnerId, OwnerType.Team, FirstFinished.AddDays(offset));
            }

            Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, FirstFinished), Is.True);
        }

        [Test]
        public void OneDayMoreThanAreRemembered_StartsThatOwnersDaysOver_AndKeepsItsFirstFinishedDay()
        {
            subject.TheWalkReachesBackNoFurtherThan(OwnerId, OwnerType.Team, FirstFinished);
            for (var offset = 0; offset <= DaysRememberedPerOwner; offset++)
            {
                subject.TheWalkHasAlreadyWorkedOut(OwnerId, OwnerType.Team, FirstFinished.AddDays(offset));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, FirstFinished), Is.False);
                Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, FirstFinished.AddDays(DaysRememberedPerOwner)), Is.True);
                Assert.That(subject.NoPassCanWrite(OwnerId, OwnerType.Team, FirstFinished.AddDays(-1)), Is.True);
            }
        }
    }
}
