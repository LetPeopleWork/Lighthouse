using System.Runtime.CompilerServices;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// Whether a Delivery broadcasts its forecast back to the Release it follows. The switch is a
    /// property of the binding rather than of the Portfolio or the connection: one Portfolio routinely
    /// holds Releases shared with a customer beside Releases nobody outside the team should read, and
    /// an all-or-nothing answer forces the coarser one on both.
    ///
    /// Living on the binding also makes an invariant free that is unrepresentable anywhere else - the
    /// switch means nothing without a Release to publish to, so a Delivery that follows nothing refuses
    /// it and letting go of the Release takes it with it.
    /// </summary>
    public class DeliveryForecastPublishingInvariantTest
    {
        private const string ReleaseSourceKey = "jira-release";
        private const string ReleaseId = "10412";
        private const string ReleaseName = "2026 Q4";

        private static readonly DateTime ReleaseDate = TestToday.AFutureDate;

        [Test]
        public void A_Delivery_following_a_Release_publishes_nothing_until_somebody_asks_for_it()
        {
            var delivery = ADeliveryFollowingARelease();

            Assert.That(delivery.PublishForecastToSource, Is.False);
        }

        [Test]
        public void A_Delivery_following_a_Release_can_be_asked_to_publish_its_forecast_there()
        {
            var delivery = ADeliveryFollowingARelease();

            delivery.SetForecastPublishing(true);

            Assert.That(delivery.PublishForecastToSource, Is.True);
        }

        [Test]
        public void A_Delivery_that_was_publishing_can_be_asked_to_stop()
        {
            var delivery = ADeliveryFollowingARelease();
            delivery.SetForecastPublishing(true);

            delivery.SetForecastPublishing(false);

            Assert.That(delivery.PublishForecastToSource, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void A_Delivery_that_follows_nothing_has_nowhere_to_publish_to_and_says_so(bool asked)
        {
            var delivery = ADeliveryChosenByHand();

            var refusal = Assert.Throws<DeliverySourceBoundException>(() => delivery.SetForecastPublishing(asked));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Code, Is.EqualTo("delivery-not-source-bound"));
                Assert.That(delivery.PublishForecastToSource, Is.False);
            }
        }

        /// <summary>
        /// Publishing is a statement about a Release this Delivery no longer follows. Left standing, it
        /// would sit on a Delivery somebody now maintains by hand and describe a broadcast that cannot
        /// happen - and would come back on by itself the moment the Delivery was pointed at a second
        /// Release, publishing to a Release nobody chose to publish to.
        /// </summary>
        [Test]
        public void Letting_go_of_the_Release_stops_the_publishing_with_it()
        {
            var delivery = ADeliveryFollowingARelease();
            delivery.SetForecastPublishing(true);

            delivery.Unbind();

            Assert.That(delivery.PublishForecastToSource, Is.False);
        }

        [Test]
        public void Following_a_second_Release_starts_from_publishing_nothing()
        {
            var delivery = ADeliveryFollowingARelease();
            delivery.SetForecastPublishing(true);
            delivery.Unbind();

            delivery.BindToSource(ReleaseSourceKey, "10999");

            Assert.That(delivery.PublishForecastToSource, Is.False);
        }

        /// <summary>
        /// Asked for what it is already doing, the Delivery does not move the version an open browser
        /// is holding: a request that changed nothing must not make somebody else's save fail with
        /// "this was changed by someone else" when nobody changed anything.
        /// </summary>
        [Test]
        public void Asking_for_the_publishing_a_Delivery_already_does_leaves_the_version_alone()
        {
            var delivery = ADeliveryFollowingARelease();
            delivery.SetForecastPublishing(true);
            var tokenBefore = delivery.ConcurrencyToken;

            delivery.SetForecastPublishing(true);

            Assert.That(delivery.ConcurrencyToken, Is.EqualTo(tokenBefore));
        }

        [Test]
        public void Switching_the_publishing_on_moves_the_version()
        {
            var delivery = ADeliveryFollowingARelease();
            var tokenBefore = delivery.ConcurrencyToken;

            delivery.SetForecastPublishing(true);

            Assert.That(delivery.ConcurrencyToken, Is.Not.EqualTo(tokenBefore));
        }

        /// <summary>
        /// Whether a Delivery publishes is decided by asking it, which is what lets it refuse when it
        /// follows nothing and when it has been retired. A settable property would put both refusals in
        /// the hands of whoever writes the next call site.
        /// </summary>
        [Test]
        public void Whether_a_Delivery_publishes_cannot_be_assigned_once_it_exists()
        {
            var setter = typeof(Delivery).GetProperty(nameof(Delivery.PublishForecastToSource))!.SetMethod!;

            var reachableFromOutside = setter.IsPublic
                || setter.ReturnParameter.GetRequiredCustomModifiers().Any(modifier => modifier == typeof(IsExternalInit));

            Assert.That(reachableFromOutside, Is.False, "PublishForecastToSource can be assigned after the Delivery exists");
        }

        private static Delivery ADeliveryChosenByHand()
        {
            return new Delivery(ReleaseName, ReleaseDate, 1);
        }

        private static Delivery ADeliveryFollowingARelease()
        {
            var delivery = ADeliveryChosenByHand();
            delivery.BindToSource(ReleaseSourceKey, ReleaseId);

            return delivery;
        }
    }
}
