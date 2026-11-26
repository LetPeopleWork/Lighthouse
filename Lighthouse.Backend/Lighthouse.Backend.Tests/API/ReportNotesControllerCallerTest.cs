using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Auth;
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
    }
}
