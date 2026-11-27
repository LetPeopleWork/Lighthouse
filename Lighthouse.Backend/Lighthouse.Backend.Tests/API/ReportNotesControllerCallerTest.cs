using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class ReportNotesControllerCallerTest
    {
        private static readonly UserProfile Ann = new() { Id = 7, DisplayName = "Ann" };

        [Test]
        public void Nobody_is_named_when_sign_in_is_off_even_if_a_profile_is_at_hand()
        {
            Assert.That(ReportNotesControllerBase.CallerOf(AuthMode.Disabled, Ann), Is.EqualTo(new ReportNoteCaller.Anonymous()));
        }

        [Test]
        public void Nobody_is_named_when_sign_in_is_off_and_no_profile_is_at_hand()
        {
            Assert.That(ReportNotesControllerBase.CallerOf(AuthMode.Disabled, null), Is.EqualTo(new ReportNoteCaller.Anonymous()));
        }

        [Test]
        public void The_profile_signs_the_note_when_sign_in_is_on_and_the_writer_is_known()
        {
            Assert.That(ReportNotesControllerBase.CallerOf(AuthMode.Enabled, Ann), Is.EqualTo(new ReportNoteCaller.Signed(7, "Ann")));
        }

        [Test]
        public void Nobody_can_be_named_when_sign_in_is_on_and_no_profile_is_found()
        {
            Assert.That(ReportNotesControllerBase.CallerOf(AuthMode.Enabled, null), Is.EqualTo(new ReportNoteCaller.Unresolved()));
        }

        [TestCase(AuthMode.Misconfigured, true)]
        [TestCase(AuthMode.Misconfigured, false)]
        [TestCase(AuthMode.Blocked, true)]
        [TestCase(AuthMode.Blocked, false)]
        public void Nobody_can_be_named_when_sign_in_is_misconfigured_or_blocked(AuthMode mode, bool aProfileIsAtHand)
        {
            Assert.That(ReportNotesControllerBase.CallerOf(mode, aProfileIsAtHand ? Ann : null), Is.EqualTo(new ReportNoteCaller.Unresolved()));
        }

        [TestCase(null)]
        [TestCase(7)]
        public void A_caller_signed_in_whom_the_instance_cannot_name_may_change_no_note(int? signedBy)
        {
            var note = new ReportNote { Id = 1, ReportId = 42, Text = "Team grew to 9", AuthorUserProfileId = signedBy };

            Assert.That(new ReportNoteCaller.Unresolved().MayChange(note), Is.False);
        }

        [Test]
        public void A_caller_nobody_is_named_for_with_sign_in_off_may_change_a_note_nobody_signed_and_no_signed_one()
        {
            var unsigned = new ReportNote { Id = 1, ReportId = 42, Text = "Training took place" };
            var signed = new ReportNote { Id = 2, ReportId = 42, Text = "Team grew to 9", AuthorUserProfileId = 7 };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(new ReportNoteCaller.Anonymous().MayChange(unsigned), Is.True);
                Assert.That(new ReportNoteCaller.Anonymous().MayChange(signed), Is.False);
            }
        }
    }
}
