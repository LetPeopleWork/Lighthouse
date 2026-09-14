using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Epic #5511 slice 04, AC-04.3. These ordinals are not an implementation detail: the Redis status hash
    /// stores the number, the monotonic advance script compares numbers, and the browser has been sent them
    /// for as long as the queue has existed. A member inserted anywhere but the end relabels every value
    /// already written down and makes keys appear to move backwards on the next advance.
    ///
    /// So <c>Cancelled</c> appends after <c>Failed</c>, and this says so as a fact rather than as a comment
    /// that a future edit can be made past without noticing.
    /// </summary>
    [TestFixture]
    public class UpdateProgressOrdinalFreezeTest
    {
        [TestCase(UpdateProgress.Queued, 0)]
        [TestCase(UpdateProgress.InProgress, 1)]
        [TestCase(UpdateProgress.Completed, 2)]
        [TestCase(UpdateProgress.Failed, 3)]
        [TestCase(UpdateProgress.Cancelled, 4)]
        public void EveryProgress_KeepsTheNumberItHasAlwaysHad(UpdateProgress progress, int ordinal)
        {
            Assert.That((int)progress, Is.EqualTo(ordinal),
                "A key already sitting in the Redis hash carries this number and nothing rewrites it. Changing "
                + "what it means relabels work that is in flight right now.");
        }

        [Test]
        public void Cancelled_OutranksEveryStateAMonotonicAdvanceCanStartFrom()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)UpdateProgress.Cancelled, Is.GreaterThan((int)UpdateProgress.Queued),
                    "Cancelling work that is still waiting has to be able to move it.");
                Assert.That((int)UpdateProgress.Cancelled, Is.GreaterThan((int)UpdateProgress.InProgress),
                    "Cancelling work that is running is the whole point, and the advance refuses to go backwards.");
            }
        }

        [Test]
        public void NothingHasBeenAddedBeyondCancelled()
        {
            Assert.That(Enum.GetValues<UpdateProgress>(), Has.Length.EqualTo(5),
                "A sixth member is fine - appended. This is here so that adding one is a deliberate act with "
                + "this file open, rather than something noticed later by a browser reading an ordinal nobody "
                + "meant to move.");
        }
    }
}
