using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class LicenseControllerTest
    {
        private static readonly string[] WhatOnlyAnAdministratorMaySee =
        [
            nameof(LicenseStatusDto.Name),
            nameof(LicenseStatusDto.Email),
            nameof(LicenseStatusDto.Organization),
            nameof(LicenseStatusDto.LicenseNumber),
        ];

        private static readonly ClaimsPrincipal SomebodyWhoSignedIn =
            new(new ClaimsIdentity([new Claim("sub", "somebody")], "test"));

        private static readonly Exception[] ReasonsTheCheckCouldNotBeMade =
        [
            new InvalidOperationException("no database to ask"),
            new SqliteException("no database to ask", 1),
        ];

        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IRbacAdministrationService> rbacMock;
        private LicenseController subject;

        [SetUp]
        public void SetUp()
        {
            licenseServiceMock = new Mock<ILicenseService>();
            rbacMock = new Mock<IRbacAdministrationService>();
            AnsweringAsA(RbacGuardRequirement.SystemAdmin, satisfied: true);

            subject = CreateSubject(SomebodyWhoSignedIn);
        }

        [Test]
        public void LicenseController_HasAuthorizeAttribute()
        {
            var attribute = typeof(LicenseController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
        }

        [Test]
        public void ImportLicense_HasSystemAdminOrBootstrapRbacGuardAttribute()
        {
            var method = typeof(LicenseController).GetMethod(nameof(LicenseController.ImportLicense));
            var attribute = method?
                .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                .Cast<RbacGuardAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute!.Requirement, Is.EqualTo(RbacGuardRequirement.SystemAdminOrBootstrap));
        }

        [Test]
        public void ClearLicense_HasSystemAdminOrBootstrapRbacGuardAttribute()
        {
            var method = typeof(LicenseController).GetMethod(nameof(LicenseController.ClearLicense));
            var attribute = method?
                .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                .Cast<RbacGuardAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute!.Requirement, Is.EqualTo(RbacGuardRequirement.SystemAdminOrBootstrap));
        }

        [Test]
        public async Task ImportLicense_ValidJsonFile_ReturnsOkWithLicenseInformation()
        {
            var validLicenseJson = File.ReadAllText("Services/Implementation/Licensing/valid_expired_license.json");
            var expectedLicenseInfo = new LicenseInformation
            {
                Name = "Benjamin Huser-Berta",
                Email = "benjamin@letpeople.work",
                Organization = "LetPeopleWork GmbH",
                ExpiryDate = new DateTime(2025, 8, 1),
                Signature = "D+oa9Gs7ZyHkAcPaa2527TposTvJjJoqRmZ9bkWPs+exf2N6n5kIMcJuNWMfhEq24BuHaBO6idz9AfkLlsbJ9qMBf7ykOEQLu27PDkLG8TFX/5/L53OEiojte/A/AmO36UGttIS6cBxP6nbif7nfhSWNCmD8C5nksLy74yJdol4n7EVdIsowvSFoAYVFGSKex0CltlIyc4DNmFjy2MWS5FnTmVZs3fs0emxSUH9eHtnL5yah4h3A2+zwV7st76x5EZDDdLS9hOeb0Qp3LKSyLTMJg57tdD8eusU18OupmyOrY16SfoN54JkwCSpL6e2NF97w6btUQl4q8mvFKJB98w=="
            };

            var formFile = CreateMockFormFile("valid_expired_license.json", validLicenseJson);
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
            var result = await subject.ImportLicense(null!);

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
        public async Task GetLicenseStatus_NoLicense_ReturnsCorrectLicenseStatus()
        {
            licenseServiceMock.Setup(s => s.GetLicenseData())
                              .Returns((null, false));

            var result = await subject.GetLicenseStatus();
            
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
        public async Task GetLicenseStatus_WithLicense_ReturnsCorrectLicenseStatus(bool isValidLicense)
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

            var result = await subject.GetLicenseStatus();

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

        [Test]
        public async Task GetLicenseStatus_WithLicenseAndValidFrom_ReturnsValidFrom()
        {
            var validFromDate = new DateTime(2025, 1, 1);
            var licenseInfo = new LicenseInformation
            {
                Name = "John Doe",
                Email = "john@doe.com",
                Organization = "Doe Inc.",
                ExpiryDate = new DateTime(2025, 12, 31),
                ValidFrom = validFromDate,
                Signature = "valid_signature"
            };

            licenseServiceMock.Setup(s => s.GetLicenseData())
                              .Returns((licenseInfo, true));

            var result = await subject.GetLicenseStatus();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var licenseStatus = okResult.Value as LicenseStatusDto;
                Assert.That(licenseStatus, Is.Not.Null);
                Assert.That(licenseStatus.ValidFrom, Is.EqualTo(validFromDate));
            }
        }

        [Test]
        public async Task GetLicenseStatus_ACallerWhoIsNoSystemAdmin_LosesOnlyTheLicenseeDetails()
        {
            AnsweringAsA(RbacGuardRequirement.SystemAdmin, satisfied: false);
            licenseServiceMock.Setup(s => s.GetLicenseData()).Returns((ALicense(), true));
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            var whole = new LicenseStatusDto(ALicense(), true, true);
            var narrowed = await Answered(subject.GetLicenseStatus());

            var changed = typeof(LicenseStatusDto)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => !Equals(property.GetValue(whole), property.GetValue(narrowed)))
                .Select(property => property.Name)
                .ToList();

            Assert.That(changed, Is.EquivalentTo(WhatOnlyAnAdministratorMaySee));
        }

        [Test]
        public async Task GetLicenseStatus_ACallerWhoNeverSignedIn_IsNotAskedAboutAndGetsNoLicenseeDetails()
        {
            licenseServiceMock.Setup(s => s.GetLicenseData()).Returns((ALicense(), true));

            var answered = await Answered(CreateSubject(new ClaimsPrincipal(new ClaimsIdentity())).GetLicenseStatus());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answered.Name, Is.Null);
                Assert.That(answered.HasLicense, Is.True);
            }

            rbacMock.Verify(
                rbac => rbac.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<RbacGuardRequirement>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestCaseSource(nameof(ReasonsTheCheckCouldNotBeMade))]
        public async Task GetLicenseStatus_TheAdministratorCheckCouldNotBeMade_WithholdsAndSaysSo(Exception reason)
        {
            var logger = new Mock<ILogger<LicenseController>>();

            rbacMock
                .Setup(rbac => rbac.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.SystemAdmin,
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(reason);

            licenseServiceMock.Setup(s => s.GetLicenseData()).Returns((ALicense(), true));

            var answered = await Answered(CreateSubject(SomebodyWhoSignedIn, logger.Object).GetLicenseStatus());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answered.Name, Is.Null, "a question that cannot be answered is not a yes");
                Assert.That(answered.HasLicense, Is.True, "and the request still succeeds");

                logger.Verify(
                    written => written.Log(
                        LogLevel.Warning,
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        reason,
                        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                    Times.Once,
                    "the safe behaviour must not also be a silent one");
            }
        }

        // The import route lets anybody in while the instance has no System Admin yet. That must not
        // carry over to the details: a caller allowed to import is not thereby a System Admin.
        [Test]
        public async Task ImportLicense_ABootstrapImporter_GetsNoLicenseeDetails()
        {
            AnsweringAsA(RbacGuardRequirement.SystemAdmin, satisfied: false);
            AnsweringAsA(RbacGuardRequirement.SystemAdminOrBootstrap, satisfied: true);

            licenseServiceMock.Setup(s => s.ImportLicense("{}")).ReturnsAsync(ALicense());
            licenseServiceMock.Setup(s => s.GetLicenseData()).Returns((ALicense(), true));

            var answered = await Answered(subject.ImportLicense(CreateMockFormFile("license.json", "{}")));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answered.Name, Is.Null);
                Assert.That(answered.Email, Is.Null);
                Assert.That(answered.Organization, Is.Null);
                Assert.That(answered.LicenseNumber, Is.Null);
                Assert.That(answered.ExpiryDate, Is.EqualTo(ALicense().ExpiryDate));
            }
        }

        [Test]
        public async Task ClearLicense_Success_ReturnsOk()
        {
            licenseServiceMock.Setup(s => s.ClearLicense())
                              .Returns(Task.CompletedTask);

            var result = await subject.ClearLicense();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
            }

            licenseServiceMock.Verify(s => s.ClearLicense(), Times.Once);
        }

        [Test]
        public async Task ClearLicense_ServiceThrowsException_ReturnsBadRequestWithErrorMessage()
        {
            var exceptionMessage = "Database error";
            licenseServiceMock.Setup(s => s.ClearLicense())
                              .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            var result = await subject.ClearLicense();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequestResult = result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
                Assert.That(badRequestResult.Value, Is.EqualTo($"Error clearing license: {exceptionMessage}"));
            }

            licenseServiceMock.Verify(s => s.ClearLicense(), Times.Once);
        }

        private void AnsweringAsA(RbacGuardRequirement requirement, bool satisfied)
        {
            rbacMock
                .Setup(rbac => rbac.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    requirement,
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(satisfied);
        }

        private LicenseController CreateSubject(ClaimsPrincipal caller, ILogger<LicenseController>? logger = null)
        {
            return new LicenseController(
                licenseServiceMock.Object,
                rbacMock.Object,
                logger ?? NullLogger<LicenseController>.Instance)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = caller },
                },
            };
        }

        private static async Task<LicenseStatusDto> Answered(Task<IActionResult> response)
        {
            return (LicenseStatusDto)((OkObjectResult)await response).Value!;
        }

        private static LicenseInformation ALicense()
        {
            return new LicenseInformation
            {
                Name = "John Doe",
                Email = "john@doe.com",
                Organization = "Doe Inc.",
                LicenseNumber = "LN-1",
                ExpiryDate = new DateTime(2031, 12, 31),
                ValidFrom = new DateTime(2026, 1, 1),
                Signature = "valid_signature"
            };
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