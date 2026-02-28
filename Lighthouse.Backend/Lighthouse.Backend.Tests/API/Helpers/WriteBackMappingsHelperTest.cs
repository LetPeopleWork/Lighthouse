using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Moq;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    public class WriteBackMappingsHelperTest
    {
        [Test]
        [TestCase(0, false, true)]
        [TestCase(0, true, true)]
        [TestCase(1, true, true)]
        [TestCase(1, false, false)]
        [TestCase(3, true, true)]
        [TestCase(3, false, false)]
        public void SupportsWriteBackMappings_ReturnsExpected(int mappingCount, bool hasLicense, bool expectedResult)
        {
            var licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(hasLicense);

            var mappings = Enumerable.Range(0, mappingCount)
                .Select(_ => new WriteBackMappingDefinition
                {
                    ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                    AppliesTo = WriteBackAppliesTo.Team,
                    TargetFieldReference = "Custom.Field"
                })
                .ToList();

            var supportsWriteBackMappings = mappings.SupportsWriteBackMappings(licenseServiceMock.Object);

            Assert.That(supportsWriteBackMappings, Is.EqualTo(expectedResult));
        }
    }
}
