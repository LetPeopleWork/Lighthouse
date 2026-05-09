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

        [Test]
        public async Task GetAuthorizationSummary_ReturnsOkWithSummary()
        {
            var expectedSummary = new UserAuthorizationSummary
            {
                IsSystemAdmin = true,
                CanCreateTeam = true,
                CanCreatePortfolio = true,
                IsRbacEnabled = true,
            };

            rbacAdministrationService
                .Setup(s => s.GetAuthorizationSummaryAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSummary);

            var subject = CreateSubjectWithUser("auth0|system-admin");

            var result = await subject.GetAuthorizationSummary(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = (OkObjectResult)result;
                var summary = okResult.Value as UserAuthorizationSummary;
                Assert.That(summary, Is.Not.Null);
                Assert.That(summary!.IsSystemAdmin, Is.True);
                Assert.That(summary.CanCreateTeam, Is.True);
                Assert.That(summary.CanCreatePortfolio, Is.True);
                Assert.That(summary.IsRbacEnabled, Is.True);
            }
        }

        [Test]
        public async Task GetTeamMembers_WhenCallerCannotManageTeam_ReturnsForbid()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageTeamMembershipAsync(It.IsAny<ClaimsPrincipal>(), 12, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var subject = CreateSubjectWithUser("auth0|viewer");

            var result = await subject.GetTeamMembers(12, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<ForbidResult>());
        }

        [Test]
        public async Task UpsertTeamMember_WhenRoleInvalidForScope_ReturnsBadRequest()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageTeamMembershipAsync(It.IsAny<ClaimsPrincipal>(), 12, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            rbacAdministrationService
                .Setup(s => s.SetTeamMemberRoleAsync(7, 12, UserRole.PortfolioAdmin, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RbacOperationResult.Failure(RbacOperationErrorCodes.InvalidRoleForScope, "Role is invalid for team scope."));

            var subject = CreateSubjectWithUser("auth0|team-admin");

            var result = await subject.UpsertTeamMember(
                12,
                7,
                new ScopedMemberRoleRequest { Role = "PortfolioAdmin" },
                CancellationToken.None);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetPortfolioMembers_WhenCallerCanManagePortfolio_ReturnsOkWithMembers()
        {
            rbacAdministrationService
                .Setup(s => s.CanManagePortfolioMembershipAsync(It.IsAny<ClaimsPrincipal>(), 9, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            rbacAdministrationService
                .Setup(s => s.GetPortfolioMembersAsync(9, It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new RbacScopedMemberSummary
                    {
                        UserProfileId = 22,
                        Subject = "auth0|member",
                        DisplayName = "Member",
                        Role = UserRole.Viewer,
                    }
                ]);

            var subject = CreateSubjectWithUser("auth0|portfolio-admin");

            var result = await subject.GetPortfolioMembers(9, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = (OkObjectResult)result;
                var members = okResult.Value as IReadOnlyList<RbacScopedMemberSummary>;
                Assert.That(members, Is.Not.Null);
                Assert.That(members!, Has.Count.EqualTo(1));
                Assert.That(members[0].UserProfileId, Is.EqualTo(22));
            }
        }

        [Test]
        public async Task GetGroupMappings_WhenCallerCannotManageRbac_ReturnsForbid()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageRbacAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var subject = CreateSubjectWithUser("auth0|viewer");

            var result = await subject.GetGroupMappings(CancellationToken.None);

            Assert.That(result, Is.InstanceOf<ForbidResult>());
        }

        [Test]
        public async Task CreateGroupMapping_WhenRequestIsValid_ReturnsNoContent()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageRbacAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            rbacAdministrationService
                .Setup(s => s.CreateGroupMappingAsync(
                    "team-12-viewers",
                    UserRole.Viewer,
                    PermissionScopeType.Team,
                    12,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(RbacOperationResult.Success());

            var subject = CreateSubjectWithUser("auth0|system-admin");

            var result = await subject.CreateGroupMapping(
                new RbacGroupMappingRequest
                {
                    GroupValue = "team-12-viewers",
                    Role = "Viewer",
                    ScopeType = "Team",
                    ScopeId = 12,
                },
                CancellationToken.None);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task RemoveGroupMapping_WhenMappingDoesNotExist_ReturnsNotFound()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageRbacAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            rbacAdministrationService
                .Setup(s => s.RemoveGroupMappingAsync(13, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RbacOperationResult.Failure(RbacOperationErrorCodes.GroupMappingNotFound, "Group mapping was not found."));

            var subject = CreateSubjectWithUser("auth0|system-admin");

            var result = await subject.RemoveGroupMapping(13, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
                var notFound = (NotFoundObjectResult)result;
                Assert.That(notFound.Value, Is.EqualTo("Group mapping was not found."));
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