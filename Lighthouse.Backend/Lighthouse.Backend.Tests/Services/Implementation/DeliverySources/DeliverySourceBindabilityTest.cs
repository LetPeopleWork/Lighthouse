using Lighthouse.Backend.Models.DeliverySources;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.DeliverySources
{
    /// <summary>
    /// DISTILL acceptance specifications for Epic 5565 slice 01a - which offered sources may be bound.
    ///
    /// One rule, shared by the picker and the create path, so a direct POST cannot bind what the picker
    /// greys out. The two block reasons are kept apart deliberately: one is fixed by setting a date on
    /// the remote, the other by choosing a different source, and collapsing them into a single boolean
    /// would send half the readers to the wrong place.
    ///
    /// Slice 00 measured why this matters: two of the three Releases on the demo instance carry no date
    /// at all, so the unbindable case is the common one rather than the exotic one.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-01a")]
    public class DeliverySourceBindabilityTest
    {
        // The only combination that binds.
        [Test]
        public void A_live_source_carrying_a_date_can_be_bound()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(DeliverySourceBindability.IsSelectable(hasDate: true, isRetiredAtSource: false), Is.True);
                Assert.That(DeliverySourceBindability.For(hasDate: true, isRetiredAtSource: false), Is.Null);
            }
        }

        // Listed, so the reader can see it and go and fix it, but not selectable.
        [Test]
        public void A_source_with_no_date_is_offered_but_says_what_is_missing()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(DeliverySourceBindability.IsSelectable(hasDate: false, isRetiredAtSource: false), Is.False);
                Assert.That(
                    DeliverySourceBindability.For(hasDate: false, isRetiredAtSource: false),
                    Is.EqualTo(SourceOptionBlockReason.NoDateSet));
            }
        }

        // A retired source is a different problem, and gets a different answer.
        [Test]
        public void A_retired_source_cannot_be_bound_even_when_it_has_a_date()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(DeliverySourceBindability.IsSelectable(hasDate: true, isRetiredAtSource: true), Is.False);
                Assert.That(
                    DeliverySourceBindability.For(hasDate: true, isRetiredAtSource: true),
                    Is.EqualTo(SourceOptionBlockReason.RetiredAtSource));
            }
        }

        // Both problems at once still names the one that has to be fixed first.
        [Test]
        public void A_retired_source_with_no_date_reports_being_retired()
        {
            Assert.That(
                DeliverySourceBindability.For(hasDate: false, isRetiredAtSource: true),
                Is.EqualTo(SourceOptionBlockReason.RetiredAtSource));
        }
    }
}
