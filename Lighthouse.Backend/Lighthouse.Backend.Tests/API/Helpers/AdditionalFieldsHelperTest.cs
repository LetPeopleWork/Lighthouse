using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Moq;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    public class AdditionalFieldsHelperTest
    {
        private Mock<ILicenseService> licenseServiceMock;
        
        [SetUp]
        public void Setup()
        {
            licenseServiceMock = new Mock<ILicenseService>();
        }
        
        [Test]
        [TestCase(0, false, true)]
        [TestCase(1, false, true)]
        [TestCase(2, false, false)]
        [TestCase(3, false, false)]
        [TestCase(0, true, true)]
        [TestCase(1, true, true)]
        [TestCase(2, true, true)]
        [TestCase(3, true, true)]
        public void SupportsAdditionalFields_FalseIfNoLicenseAndMoreThanOne(int additionalFieldCount, bool hasLicense, bool expectedResult)
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(hasLicense);
            
            var additionalFields = new List<AdditionalFieldDefinition>();
            for (var count = 0; count < additionalFieldCount; count++)
            {
                additionalFields.Add(new AdditionalFieldDefinition());
            }

            var supportsAdditionalFields = additionalFields.SupportsAdditionalFields(licenseServiceMock.Object);
            
            Assert.That(supportsAdditionalFields, Is.EqualTo(expectedResult));
        }
    }
}