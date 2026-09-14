using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Epic #5511 slice 04. The lifetime rules for a cancellation source, and the races an adversarial
    /// review found the queue getting wrong.
    ///
    /// These were only reachable through the whole queue before, which meant the interesting cases - a run
    /// finishing underneath a cancel that is already on its way in - had to be raced rather than stated.
    /// </summary>
    [TestFixture]
    public class AdmittedCancellationsTest
    {
        private static readonly UpdateKey ATeamRefresh = new(UpdateType.Team, 1);

        private static readonly UpdateKey AnotherTeamRefresh = new(UpdateType.Team, 2);

        [Test]
        public void WorkNobodyAdmitted_IsNotCancellableAndSaysSoQuietly()
        {
            using var cancellations = new AdmittedCancellations();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cancellations.TokenFor(ATeamRefresh), Is.EqualTo(CancellationToken.None));
                Assert.DoesNotThrow(() => cancellations.Stop(ATeamRefresh),
                    "Cancelling something that was never admitted is the ordinary case, not a mistake to "
                    + "report back: the row was drawn before it was clicked.");
            }
        }

        [Test]
        public void AdmittedWork_IsCancellable()
        {
            using var cancellations = new AdmittedCancellations();
            cancellations.Admit(ATeamRefresh);

            cancellations.Stop(ATeamRefresh);

            Assert.That(cancellations.TokenFor(ATeamRefresh).IsCancellationRequested, Is.True);
        }

        [Test]
        public void StoppingOnePieceOfWork_LeavesEveryOtherAlone()
        {
            using var cancellations = new AdmittedCancellations();
            cancellations.Admit(ATeamRefresh);
            cancellations.Admit(AnotherTeamRefresh);

            cancellations.Stop(ATeamRefresh);

            Assert.That(cancellations.TokenFor(AnotherTeamRefresh).IsCancellationRequested, Is.False,
                "A button that stops one runaway refresh and takes the rest of the instance with it is too "
                + "dangerous to press, which makes it no better than no button.");
        }

        /// <summary>
        /// The reason the lifetime is the key's own. Work admitted later under the same name is a different
        /// refresh, and inheriting the old verdict would have it stop before it ever ran.
        /// </summary>
        [Test]
        public void WorkAdmittedAgainAfterACancel_StartsUncancelled()
        {
            using var cancellations = new AdmittedCancellations();
            cancellations.Admit(ATeamRefresh);
            cancellations.Stop(ATeamRefresh);

            cancellations.Forget(ATeamRefresh);
            cancellations.Admit(ATeamRefresh);

            Assert.That(cancellations.TokenFor(ATeamRefresh).IsCancellationRequested, Is.False);
        }

        [Test]
        public void AdmittingTheSameWorkTwice_KeepsTheSourceAlreadyInUse()
        {
            using var cancellations = new AdmittedCancellations();
            cancellations.Admit(ATeamRefresh);
            var inUse = cancellations.TokenFor(ATeamRefresh);

            cancellations.Admit(ATeamRefresh);
            cancellations.Stop(ATeamRefresh);

            Assert.That(inUse.IsCancellationRequested, Is.True,
                "A second admission that replaced the source would leave the run holding a token nothing "
                + "can reach, and the operator pressing Cancel on a refresh that never stops.");
        }

        /// <summary>
        /// The race the review found turning an explicitly idempotent cancel into a 500: the run disposes
        /// its own source as it ends, and an operator cancel can be mid-flight when it does.
        /// </summary>
        [Test]
        public void WorkThatFinishesWhileACancelIsOnItsWayIn_IsNotAnError()
        {
            using var cancellations = new AdmittedCancellations();
            cancellations.Admit(ATeamRefresh);
            cancellations.Forget(ATeamRefresh);

            using (Assert.EnterMultipleScope())
            {
                Assert.DoesNotThrow(() => cancellations.Stop(ATeamRefresh));
                Assert.DoesNotThrow(() => cancellations.TokenFor(ATeamRefresh),
                    "Read from the other side of the same race. Throwing here escapes where the caller "
                    + "cannot catch it and leaves the key admitted for good.");
            }
        }

        [Test]
        public void ForgettingWorkNobodyAdmitted_DoesNothing()
        {
            using var cancellations = new AdmittedCancellations();

            Assert.DoesNotThrow(() => cancellations.Forget(ATeamRefresh));
        }

        /// <summary>
        /// Every refresh this instance ever runs passes through here, so a source that is dropped from the
        /// dictionary without being released is a handle leaked per refresh, for the life of the process.
        /// The token outlives the lookup, which is what makes the release observable at all.
        /// </summary>
        [Test]
        public void ForgettingWork_ReleasesTheSourceRatherThanOnlyDroppingIt()
        {
            using var cancellations = new AdmittedCancellations();
            cancellations.Admit(ATeamRefresh);
            var heldByTheRun = cancellations.TokenFor(ATeamRefresh);

            cancellations.Forget(ATeamRefresh);

            Assert.That(() => heldByTheRun.WaitHandle, Throws.InstanceOf<ObjectDisposedException>(),
                "Dropping the source from the dictionary hides the leak without fixing it. The wait handle is "
                + "the part that holds an operating-system resource, so it is the part worth releasing.");
        }

        [Test]
        public void Disposing_ReleasesEverythingStillOutstanding()
        {
            var cancellations = new AdmittedCancellations();
            cancellations.Admit(ATeamRefresh);
            cancellations.Admit(AnotherTeamRefresh);

            cancellations.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cancellations.TokenFor(ATeamRefresh), Is.EqualTo(CancellationToken.None));
                Assert.That(cancellations.TokenFor(AnotherTeamRefresh), Is.EqualTo(CancellationToken.None));
            }
        }
    }
}
