using Lighthouse.Backend.Health;

namespace Lighthouse.Backend.Tests.Health
{
    [Category("epic-5305-k8s-readiness")]
    public class ReadinessStateTest
    {
        [Test]
        public void IsDraining_FreshState_IsFalse()
        {
            var subject = new ReadinessState();

            Assert.That(subject.IsDraining, Is.False);
        }

        [Test]
        public void IsDraining_AfterBeginDraining_IsTrue()
        {
            var subject = new ReadinessState();

            subject.BeginDraining();

            Assert.That(subject.IsDraining, Is.True);
        }

        [Test]
        public void BeginDraining_CalledTwice_RemainsDraining()
        {
            var subject = new ReadinessState();

            subject.BeginDraining();
            subject.BeginDraining();

            Assert.That(subject.IsDraining, Is.True);
        }
    }
}
