using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class AuthorizationControllerTest
    {
        private Mock<IRbacAdministrationService> rbacAdministrationService;

        [SetUp]
        public void SetUp()
        {
            rbacAdministrationService = new Mock<IRbacAdministrationService>();
        }

        [Test]
        public async Task GetStatus_ReturnsOkWithRbacStatus()
        {
            rbacAdministrationService
                .Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RbacStatus
                {
                    Enabled = false,
                    PremiumGateSatisfied = true,
                    HasSystemAdmin = false,
                    HasEmergencyAdminConfigured = false,
                    ReadyForEnablement = false,
                });

            var subject = CreateSubjectWithUser("auth0|operator");

            var result = await subject.GetStatus(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = (OkObjectResult)result;
                var status = okResult.Value as RbacStatus;

                Assert.That(status, Is.Not.Null);
                Assert.That(status!.ReadyForEnablement, Is.False);
            }
        }

        [Test]
        public async Task BootstrapCurrentUserAsSystemAdmin_AlreadyBootstrapped_ReturnsConflict()
        {
            rbacAdministrationService
                .Setup(s => s.BootstrapCurrentUserAsSystemAdminAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RbacOperationResult
                {
                    Succeeded = false,
                    ErrorCode = RbacOperationErrorCodes.AlreadyBootstrapped,
                    Message = "A System Admin already exists.",
                });

            var subject = CreateSubjectWithUser("auth0|existing-user");

            var result = await subject.BootstrapCurrentUserAsSystemAdmin(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
                var conflict = (ConflictObjectResult)result;
                Assert.That(conflict.Value, Is.EqualTo("A System Admin already exists."));
            }
        }

        [Test]
        public async Task RevokeSystemAdmin_LastSystemAdmin_ReturnsBadRequest()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageRbacAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            rbacAdministrationService
                .Setup(s => s.RevokeSystemAdminAsync(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RbacOperationResult
                {
                    Succeeded = false,
                    ErrorCode = RbacOperationErrorCodes.LastSystemAdmin,
                    Message = "Cannot remove the last System Admin.",
                });

            var subject = CreateSubjectWithUser("auth0|admin-user");

            var result = await subject.RevokeSystemAdmin(7, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequest = (BadRequestObjectResult)result;
                Assert.That(badRequest.Value, Is.EqualTo("Cannot remove the last System Admin."));
            }
        }

        private AuthorizationController CreateSubjectWithUser(string subjectClaim)
        {
            var identity = new ClaimsIdentity([new Claim("sub", subjectClaim)], "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var controller = new AuthorizationController(rbacAdministrationService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = principal,
                    },
                },
            };

            return controller;
        }
    }
}