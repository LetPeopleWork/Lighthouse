using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Tests.Modules.Reports.Domain
{
    public class ReportNoteCallerTest
    {
        [TestCase(null)]
        [TestCase(7)]
        public void A_caller_signed_in_whom_the_instance_cannot_name_may_change_no_note(int? signedBy)
        {
            var note = new ReportNote { Id = 1, ReportId = 42, Text = "Team grew to 9", AuthorUserProfileId = signedBy };

            Assert.That(new ReportNoteCaller.Unresolved().MayChange(note), Is.False);
        }

        [Test]
        public void An_anonymous_caller_may_change_a_note_nobody_signed_and_no_signed_one()
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
