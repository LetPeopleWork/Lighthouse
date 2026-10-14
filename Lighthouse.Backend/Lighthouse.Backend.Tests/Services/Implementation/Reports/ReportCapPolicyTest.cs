using Lighthouse.Backend.Services.Implementation.Reports;

namespace Lighthouse.Backend.Tests.Services.Implementation.Reports
{
    public class ReportCapPolicyTest
    {
        [TestCase(false, 0, true)]
        [TestCase(false, 1, false)]
        [TestCase(false, 4, false)]
        [TestCase(true, 0, true)]
        [TestCase(true, 1, true)]
        [TestCase(true, 4, true)]
        public void Community_creates_one_report_of_a_type_and_Premium_as_many_as_it_needs(bool premium, int countForOwnerOfThatType, bool mayCreate)
        {
            Assert.That(ReportCapPolicy.MayCreate(premium, countForOwnerOfThatType), Is.EqualTo(mayCreate));
        }
    }
}
