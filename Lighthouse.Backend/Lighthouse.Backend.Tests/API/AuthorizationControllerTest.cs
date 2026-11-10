using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class AuthorizationControllerTest
    {
        private const int ActorProfileId = 42;

        private Mock<IRbacAdministrationService> rbacAdministrationService;

        private Mock<ILogger<AuthorizationController>> logger;

        private Mock<ICurrentUserProfileService> currentUserProfileService;

        [SetUp]
        public void SetUp()
        {
            rbacAdministrationService = new Mock<IRbacAdministrationService>();
            logger = new Mock<ILogger<AuthorizationController>>();
            currentUserProfileService = new Mock<ICurrentUserProfileService>();
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Id = ActorProfileId });
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

        [Test]
        public async Task GetTeamGroupMappings_TeamAdmin_ReturnsOkWithScopedMappings()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageTeamMembershipAsync(It.IsAny<ClaimsPrincipal>(), 12, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            rbacAdministrationService
                .Setup(s => s.GetTeamGroupMappingsAsync(12, It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new RbacGroupMappingSummary
                    {
                        Id = 1,
                        GroupValue = "team-12-admins",
                        Role = UserRole.TeamAdmin,
                        ScopeType = PermissionScopeType.Team,
                        ScopeId = 12,
                    }
                ]);

            var subject = CreateSubjectWithUser("auth0|team-admin");

            var result = await subject.GetTeamGroupMappings(12, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = (OkObjectResult)result;
                var mappings = okResult.Value as IReadOnlyList<RbacGroupMappingSummary>;
                Assert.That(mappings, Is.Not.Null);
                Assert.That(mappings!, Has.Count.EqualTo(1));
                Assert.That(mappings[0].GroupValue, Is.EqualTo("team-12-admins"));
                Assert.That(mappings[0].ScopeId, Is.EqualTo(12));
            }
        }

        [Test]
        public async Task GetTeamGroupMappings_WhenCallerCannotManageTeam_ReturnsForbid()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageTeamMembershipAsync(It.IsAny<ClaimsPrincipal>(), 12, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var subject = CreateSubjectWithUser("auth0|viewer");

            var result = await subject.GetTeamGroupMappings(12, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<ForbidResult>());
        }

        [Test]
        public async Task GetPortfolioGroupMappings_PortfolioAdmin_ReturnsOkWithScopedMappings()
        {
            rbacAdministrationService
                .Setup(s => s.CanManagePortfolioMembershipAsync(It.IsAny<ClaimsPrincipal>(), 9, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            rbacAdministrationService
                .Setup(s => s.GetPortfolioGroupMappingsAsync(9, It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new RbacGroupMappingSummary
                    {
                        Id = 5,
                        GroupValue = "portfolio-9-viewers",
                        Role = UserRole.Viewer,
                        ScopeType = PermissionScopeType.Portfolio,
                        ScopeId = 9,
                    }
                ]);

            var subject = CreateSubjectWithUser("auth0|portfolio-admin");

            var result = await subject.GetPortfolioGroupMappings(9, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = (OkObjectResult)result;
                var mappings = okResult.Value as IReadOnlyList<RbacGroupMappingSummary>;
                Assert.That(mappings, Is.Not.Null);
                Assert.That(mappings!, Has.Count.EqualTo(1));
                Assert.That(mappings[0].GroupValue, Is.EqualTo("portfolio-9-viewers"));
                Assert.That(mappings[0].ScopeId, Is.EqualTo(9));
            }
        }

        [Test]
        public async Task GetPortfolioGroupMappings_WhenCallerCannotManagePortfolio_ReturnsForbid()
        {
            rbacAdministrationService
                .Setup(s => s.CanManagePortfolioMembershipAsync(It.IsAny<ClaimsPrincipal>(), 9, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var subject = CreateSubjectWithUser("auth0|viewer");

            var result = await subject.GetPortfolioGroupMappings(9, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<ForbidResult>());
        }

        [Test]
        public async Task DeleteUser_ExistingUser_ReturnsNoContent_AndRemovesAllRoleAssignments()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageRbacAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            rbacAdministrationService
                .Setup(s => s.DeleteUserAsync(42, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RbacOperationResult.Success());

            var subject = CreateSubjectWithUser("auth0|system-admin");

            var result = await subject.DeleteUser(42, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task DeleteUser_WhenCallerCannotManageRbac_ReturnsForbid()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageRbacAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var subject = CreateSubjectWithUser("auth0|viewer");

            var result = await subject.DeleteUser(42, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<ForbidResult>());
        }

        [Test]
        public async Task DeleteUser_WhenUserDoesNotExist_ReturnsNotFound()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageRbacAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            rbacAdministrationService
                .Setup(s => s.DeleteUserAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RbacOperationResult.Failure(RbacOperationErrorCodes.UserNotFound, "User profile was not found."));

            var subject = CreateSubjectWithUser("auth0|system-admin");

            var result = await subject.DeleteUser(99, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetUsers_ReturnsUsersWithIsEmergencyAdminPopulated()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageRbacAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            rbacAdministrationService
                .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new RbacUserSummary
                    {
                        Id = 1,
                        Subject = "auth0|break-glass",
                        DisplayName = "Emergency Admin",
                        IsEmergencyAdmin = true,
                    },
                    new RbacUserSummary
                    {
                        Id = 2,
                        Subject = "auth0|regular-user",
                        DisplayName = "Regular User",
                        IsEmergencyAdmin = false,
                    },
                ]);

            var subject = CreateSubjectWithUser("auth0|break-glass");

            var result = await subject.GetUsers(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = (OkObjectResult)result;
                var users = okResult.Value as IReadOnlyList<RbacUserSummary>;
                Assert.That(users, Is.Not.Null);
                Assert.That(users!, Has.Count.EqualTo(2));
                Assert.That(users[0].IsEmergencyAdmin, Is.True);
                Assert.That(users[1].IsEmergencyAdmin, Is.False);
            }
        }

        [Test]
        public async Task CreateGroupMapping_WhenAdded_LogsWhoAddedWhichRule()
        {
            rbacAdministrationService
                .Setup(s => s.CanManageRbacAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            rbacAdministrationService
                .Setup(s => s.CreateGroupMappingAsync(" delivery-leads ", UserRole.Viewer, PermissionScopeType.AllUnits, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RbacOperationResult.Success());

            var subject = CreateSubjectWithUser("auth0|system-admin");

            await subject.CreateGroupMapping(
                new RbacGroupMappingRequest { GroupValue = " delivery-leads ", Role = "Viewer", ScopeType = "AllUnits" },
                CancellationToken.None);

            AssertLoggedOnce("SSO group rule added by user profile 42: group delivery-leads, role Viewer, scope AllUnits");
        }

        [Test]
        public async Task RemoveGroupMapping_WhenRemoved_LogsWhoRemovedTheRuleWithThatId()
        {
            AnAdminWhoRemovesAndTheListShows(
                new RbacGroupMappingSummary { Id = 13, GroupValue = "team-12-viewers", Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 12 },
                new RbacGroupMappingSummary { Id = 14, GroupValue = "delivery-leads", Role = UserRole.UnitAdmin, ScopeType = PermissionScopeType.AllUnits });

            var result = await CreateSubjectWithUser("auth0|system-admin").RemoveGroupMapping(14, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            AssertLoggedOnce("SSO group rule removed by user profile 42: group delivery-leads, role UnitAdmin, scope AllUnits");
        }

        [Test]
        public async Task RemoveGroupMapping_OfATwinTheListDoesNotShow_IsRemovedAndLoggedWithoutItsRule()
        {
            AnAdminWhoRemovesAndTheListShows(
                new RbacGroupMappingSummary { Id = 13, GroupValue = "delivery-leads", Role = UserRole.Viewer, ScopeType = PermissionScopeType.AllUnits });

            var result = await CreateSubjectWithUser("auth0|system-admin").RemoveGroupMapping(14, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            AssertLoggedOnce("SSO group rule removed by user profile 42: group (null), role (null), scope (null)");
        }

        [Test]
        public async Task GetTeamSystemWideRules_ForAStoredTeam_ReturnsOkWithItsRules()
        {
            var rules = new[] { new SystemWideRule(UserRole.Viewer, "delivery-leads") };
            rbacAdministrationService.Setup(s => s.TeamExistsAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            rbacAdministrationService.Setup(s => s.GetTeamSystemWideRulesAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(rules);

            var result = await CreateSubjectWithUser("auth0|team-admin").GetTeamSystemWideRules(12, CancellationToken.None);

            Assert.That((result as OkObjectResult)?.Value, Is.SameAs(rules));
        }

        [Test]
        public async Task GetTeamSystemWideRules_ForATeamNotStored_ReturnsNotFoundNamingIt()
        {
            rbacAdministrationService.Setup(s => s.TeamExistsAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await CreateSubjectWithUser("auth0|team-admin").GetTeamSystemWideRules(12, CancellationToken.None);

            Assert.That((result as NotFoundObjectResult)?.Value, Is.EqualTo("Team 12 does not exist."));
        }

        [Test]
        public async Task GetPortfolioSystemWideRules_ForAStoredPortfolio_ReturnsOkWithItsRules()
        {
            var rules = new[] { SystemWideRule.EveryoneSignedIn };
            rbacAdministrationService.Setup(s => s.PortfolioExistsAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            rbacAdministrationService.Setup(s => s.GetPortfolioSystemWideRulesAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(rules);

            var result = await CreateSubjectWithUser("auth0|portfolio-admin").GetPortfolioSystemWideRules(9, CancellationToken.None);

            Assert.That((result as OkObjectResult)?.Value, Is.SameAs(rules));
        }

        [Test]
        public async Task GetPortfolioSystemWideRules_ForAPortfolioNotStored_ReturnsNotFoundNamingIt()
        {
            rbacAdministrationService.Setup(s => s.PortfolioExistsAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await CreateSubjectWithUser("auth0|portfolio-admin").GetPortfolioSystemWideRules(9, CancellationToken.None);

            Assert.That((result as NotFoundObjectResult)?.Value, Is.EqualTo("Portfolio 9 does not exist."));
        }

        [Test]
        public async Task GetEveryoneSignedIn_ReturnsOkWithTheStoredSwitch()
        {
            var stored = EveryoneSignedInSetting.StoredAs(true);
            rbacAdministrationService.Setup(s => s.GetEveryoneSignedInAsync(It.IsAny<CancellationToken>())).ReturnsAsync(stored);

            var result = await CreateSubjectWithUser("auth0|system-admin").GetEveryoneSignedIn(CancellationToken.None);

            Assert.That((result as OkObjectResult)?.Value, Is.SameAs(stored));
        }

        [Test]
        public async Task SetEveryoneSignedIn_WhenSaved_ReturnsNoContentAndLogsWhoTurnedItOn()
        {
            var token = Guid.NewGuid();
            rbacAdministrationService
                .Setup(s => s.SetEveryoneSignedInAsync(It.IsAny<ClaimsPrincipal>(), true, token, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RbacOperationResult.Success());

            var result = await CreateSubjectWithUser("auth0|system-admin").SetEveryoneSignedIn(
                new EveryoneSignedInRequest { EveryoneCanView = true, ConcurrencyToken = token },
                CancellationToken.None);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            AssertLoggedOnce("Everyone signed in can view all Teams and Portfolios set to True by user profile 42");
        }

        [Test]
        public async Task SetEveryoneSignedIn_ByACallerWhoIsNoSystemAdmin_ReturnsForbidAndLogsNothing()
        {
            ASaveOfTheSwitchAnswers(RbacOperationResult.Failure(RbacOperationErrorCodes.NotSystemAdmin, "Only a System Admin changes it."));

            var result = await CreateSubjectWithUser("auth0|team-admin").SetEveryoneSignedIn(
                new EveryoneSignedInRequest { EveryoneCanView = true, ConcurrencyToken = Guid.NewGuid() },
                CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ForbidResult>());
                Assert.That(logger.Invocations, Is.Empty, "A refused save changed nothing, so there is nothing to record.");
            }
        }

        [Test]
        public async Task SetEveryoneSignedIn_WithNoToken_ReturnsBadRequestWithTheServicesMessage()
        {
            ASaveOfTheSwitchAnswers(RbacOperationResult.Failure(RbacOperationErrorCodes.MissingConcurrencyToken, "Reload the page and try again."));

            var result = await CreateSubjectWithUser("auth0|system-admin").SetEveryoneSignedIn(
                new EveryoneSignedInRequest { EveryoneCanView = false },
                CancellationToken.None);

            Assert.That((result as BadRequestObjectResult)?.Value, Is.EqualTo("Reload the page and try again."));
        }

        private void ASaveOfTheSwitchAnswers(RbacOperationResult answer)
        {
            rbacAdministrationService
                .Setup(s => s.SetEveryoneSignedInAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(answer);
        }

        private void AnAdminWhoRemovesAndTheListShows(params RbacGroupMappingSummary[] listed)
        {
            rbacAdministrationService
                .Setup(s => s.CanManageRbacAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            rbacAdministrationService
                .Setup(s => s.GetGroupMappingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(listed);
            rbacAdministrationService
                .Setup(s => s.RemoveGroupMappingAsync(14, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RbacOperationResult.Success());
        }

        private void AssertLoggedOnce(string message)
        {
            logger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString() == message),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                $"An access change is recorded with who made it and what changed: expected the line '{message}'.");
        }

        private AuthorizationController CreateSubjectWithUser(string subjectClaim)
        {
            var identity = new ClaimsIdentity([new Claim("sub", subjectClaim)], "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var controller = new AuthorizationController(
                rbacAdministrationService.Object,
                currentUserProfileService.Object,
                logger.Object)
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