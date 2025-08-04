using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class LicenseControllerTest
    {
        private Mock<ILicenseService> licenseServiceMock;
        private LicenseController subject;

        [SetUp]
        public void SetUp()
        {
            licenseServiceMock = new Mock<ILicenseService>();
            subject = new LicenseController(licenseServiceMock.Object);
        }

        [Test]
        public async Task ImportLicense_ValidJsonFile_ReturnsOkWithLicenseInformation()
        {
            var validLicenseJson = File.ReadAllText("Services/Implementation/Licensing/valid_license.json");
            var expectedLicenseInfo = new LicenseInformation
            {
                Name = "Benjamin Huser-Berta",
                Email = "benjamin@letpeople.work",
                Organization = "LetPeopleWork GmbH",
                ExpiryDate = new DateTime(2025, 8, 1),
                Signature = "D+oa9Gs7ZyHkAcPaa2527TposTvJjJoqRmZ9bkWPs+exf2N6n5kIMcJuNWMfhEq24BuHaBO6idz9AfkLlsbJ9qMBf7ykOEQLu27PDkLG8TFX/5/L53OEiojte/A/AmO36UGttIS6cBxP6nbif7nfhSWNCmD8C5nksLy74yJdol4n7EVdIsowvSFoAYVFGSKex0CltlIyc4DNmFjy2MWS5FnTmVZs3fs0emxSUH9eHtnL5yah4h3A2+zwV7st76x5EZDDdLS9hOeb0Qp3LKSyLTMJg57tdD8eusU18OupmyOrY16SfoN54JkwCSpL6e2NF97w6btUQl4q8mvFKJB98w=="
            };

            var formFile = CreateMockFormFile("valid_license.json", validLicenseJson);
            licenseServiceMock.Setup(s => s.ImportLicense(validLicenseJson))
                              .ReturnsAsync(expectedLicenseInfo);

            licenseServiceMock.Setup(x => x.GetLicenseData())
                              .Returns((expectedLicenseInfo, true));

            var result = await subject.ImportLicense(formFile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                
                var returnedLicense = okResult.Value as LicenseStatusDto;
                Assert.That(returnedLicense, Is.Not.Null);
                Assert.That(returnedLicense.Name, Is.EqualTo(expectedLicenseInfo.Name));
                Assert.That(returnedLicense.Email, Is.EqualTo(expectedLicenseInfo.Email));
                Assert.That(returnedLicense.Organization, Is.EqualTo(expectedLicenseInfo.Organization));
                Assert.That(returnedLicense.ExpiryDate, Is.EqualTo(expectedLicenseInfo.ExpiryDate));
            }

            licenseServiceMock.Verify(s => s.ImportLicense(validLicenseJson), Times.Once);
        }

        [Test]
        public async Task ImportLicense_InvalidJsonFile_ReturnsBadRequest()
        {
            var invalidLicenseJson = File.ReadAllText("Services/Implementation/Licensing/invalid_license.json");
            var formFile = CreateMockFormFile("invalid_license.json", invalidLicenseJson);
            
            licenseServiceMock.Setup(s => s.ImportLicense(invalidLicenseJson))
                              .ReturnsAsync((LicenseInformation?)null);

            var result = await subject.ImportLicense(formFile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequestResult = result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
                Assert.That(badRequestResult.Value, Is.EqualTo("Invalid license file"));
            }

            licenseServiceMock.Verify(s => s.ImportLicense(invalidLicenseJson), Times.Once);
        }

        [Test]
        public async Task ImportLicense_NoFileProvided_ReturnsBadRequest()
        {
            var result = await subject.ImportLicense(null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequestResult = result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
                Assert.That(badRequestResult.Value, Is.EqualTo("No file provided"));
            }

            licenseServiceMock.Verify(s => s.ImportLicense(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ImportLicense_EmptyFile_ReturnsBadRequest()
        {
            var emptyFile = CreateMockFormFile("empty.json", "");

            var result = await subject.ImportLicense(emptyFile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequestResult = result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
                Assert.That(badRequestResult.Value, Is.EqualTo("No file provided"));
            }

            licenseServiceMock.Verify(s => s.ImportLicense(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ImportLicense_NonJsonFile_ReturnsBadRequest()
        {
            var textFile = CreateMockFormFile("license.txt", "some content");

            var result = await subject.ImportLicense(textFile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequestResult = result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
                Assert.That(badRequestResult.Value, Is.EqualTo("File must be a JSON file"));
            }

            licenseServiceMock.Verify(s => s.ImportLicense(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ImportLicense_ServiceThrowsException_ReturnsBadRequestWithErrorMessage()
        {
            var validLicenseJson = "{ \"test\": \"data\" }";
            var formFile = CreateMockFormFile("test.json", validLicenseJson);
            var exceptionMessage = "JSON parsing failed";
            
            licenseServiceMock.Setup(s => s.ImportLicense(validLicenseJson))
                              .Throws(new InvalidOperationException(exceptionMessage));

            var result = await subject.ImportLicense(formFile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequestResult = result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
                Assert.That(badRequestResult.Value, Is.EqualTo($"Error processing license file: {exceptionMessage}"));
            }

            licenseServiceMock.Verify(s => s.ImportLicense(validLicenseJson), Times.Once);
        }

        [Test]
        public async Task ImportLicense_MalformedJson_ReturnsBadRequest()
        {
            var malformedJson = "{ \"incomplete\": json";
            var formFile = CreateMockFormFile("malformed.json", malformedJson);
            
            licenseServiceMock.Setup(s => s.ImportLicense(malformedJson))
                              .ReturnsAsync((LicenseInformation?)null);

            var result = await subject.ImportLicense(formFile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequestResult = result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
                Assert.That(badRequestResult.Value, Is.EqualTo("Invalid license file"));
            }

            licenseServiceMock.Verify(s => s.ImportLicense(malformedJson), Times.Once);
        }

        [Test]
        public void GetLicenseStatus_NoLicense_ReturnsCorrectLicenseStatus()
        {
            licenseServiceMock.Setup(s => s.GetLicenseData())
                              .Returns((null, false));

            var result = subject.GetLicenseStatus();
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                
                var licenseStatus = okResult.Value as LicenseStatusDto;
                Assert.That(licenseStatus, Is.Not.Null);
                Assert.That(licenseStatus.HasLicense, Is.False);
                Assert.That(licenseStatus.IsValid, Is.False);
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void GetLicenseStatus_WithLicense_ReturnsCorrectLicenseStatus(bool isValidLicense)
        {
            var licenseInfo = new LicenseInformation
            {
                Name = "John Doe",
                Email = "john@doe.com",
                Organization = "Doe Inc.",
                ExpiryDate = new DateTime(2025, 12, 31),
                Signature = "valid_signature"
            };

            licenseServiceMock.Setup(s => s.GetLicenseData())
                              .Returns((licenseInfo, isValidLicense));

            var result = subject.GetLicenseStatus();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var licenseStatus = okResult.Value as LicenseStatusDto;
                Assert.That(licenseStatus, Is.Not.Null);
                Assert.That(licenseStatus.HasLicense, Is.True);
                Assert.That(licenseStatus.IsValid, Is.EqualTo(isValidLicense));
                Assert.That(licenseStatus.Name, Is.EqualTo(licenseInfo.Name));
                Assert.That(licenseStatus.Email, Is.EqualTo(licenseInfo.Email));
                Assert.That(licenseStatus.Organization, Is.EqualTo(licenseInfo.Organization));
                Assert.That(licenseStatus.ExpiryDate, Is.EqualTo(licenseInfo.ExpiryDate));
            }
        }

        private static IFormFile CreateMockFormFile(string fileName, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            var formFile = new Mock<IFormFile>();
            
            formFile.Setup(f => f.FileName).Returns(fileName);
            formFile.Setup(f => f.Length).Returns(bytes.Length);
            formFile.Setup(f => f.OpenReadStream()).Returns(stream);
            
            return formFile.Object;
        }
    }
}