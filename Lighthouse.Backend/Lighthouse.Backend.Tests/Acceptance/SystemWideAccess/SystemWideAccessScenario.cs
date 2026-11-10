using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Lighthouse.Backend.API;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Lighthouse.Backend.Tests.Acceptance.SystemWideAccess
{
    /// <summary>
    /// Drives the access service the way a signed-in person, their API keys and a System Admin reach it, with no web
    /// host: a database of the scenario's own, access control switched on, a Premium licence and one System Admin, so
    /// every answer below comes from the rules and nothing else. People sign in with the groups their identity provider
    /// sends; a key carries no groups and is judged by its owner's stored groups and its own limit. A Team or Portfolio
    /// is named by its number, since the read checks never look a unit up; the cost scenario stores real Teams.
    /// </summary>
    public sealed class SystemWideAccessScenario : IDisposable
    {
        public const string GroupClaim = "groups";

        public const string TeamReaders = "team-readers";

        public const string FinanceStaff = "finance-staff";

        public const string Noor = "oidc|noor-haddad";

        public const string Sam = "oidc|sam-weber";

        public const string Alex = "oidc|alex-chen";

        public const int TeamZenith = 11;

        public const int TeamApollo = 12;

        public const int PortfolioOceanExplorer = 21;

        /// <summary>A Team whose number first appears after the rule was added.</summary>
        public const int TeamMeridianCreatedLater = 99;

        public const string BroaderScopeRefusal = "Cannot issue API key with broader scope than your own permissions.";

        private readonly ServicePortHarness harness;

        public SystemWideAccessScenario()
        {
            Commands = new CommandCount();
            harness = new ServicePortHarness(services =>
            {
                services.ConfigureDbContext<LighthouseAppContext>(options => options.AddInterceptors(Commands));
                services.AddSingleton(Options.Create(new AuthorizationConfiguration { Enabled = true, GroupClaimName = GroupClaim }));
                services.AddSingleton(AValidPremiumLicence());
                services.AddSingleton(TimeProvider.System);
                services.AddScoped<ICurrentUserProfileService, PeopleWhoSignedInBefore>();
                services.AddScoped<IRbacAdministrationService, RbacAdministrationService>();
            });

            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            AlexIsTheSystemAdmin(context);
            context.UserProfiles.Add(new UserProfile { Subject = Noor, SubjectClaimType = "sub", DisplayName = "Noor Haddad", LastKnownGroupClaimValues = JsonSerializer.Serialize(new[] { TeamReaders }) });
            context.UserProfiles.Add(new UserProfile { Subject = Sam, SubjectClaimType = "sub", DisplayName = "Sam Weber", LastKnownGroupClaimValues = JsonSerializer.Serialize(new[] { FinanceStaff }) });
            context.SaveChanges();
        }

        /// <summary>Every command sent to the database by the access service, reads included.</summary>
        public CommandCount Commands { get; }

        /// <summary>The scope a rule for every Team and Portfolio is stored under, as the server names it.</summary>
        public static PermissionScopeType AllUnits
        {
            get
            {
                PermissionScopeType allUnits;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(
                        Enum.TryParse<PermissionScopeType>("AllUnits", out allUnits),
                        Is.True,
                        "WHAT: there is no scope named AllUnits. WHY: a rule for every Team and Portfolio is a group mapping on that scope, and the browser sends it by that name. HOW: append AllUnits to the scopes a permission can have.");
                    Assert.That(
                        (int)allUnits,
                        Is.EqualTo(3),
                        "WHAT: AllUnits is not stored as 3. WHY: scopes are stored as numbers, and an older release reading a rule must never mistake it for a Team or Portfolio. HOW: append AllUnits after Portfolio, never renumber.");
                }

                return allUnits;
            }
        }

        public static ClaimsPrincipal SignedIn(string subject, params string[] groups)
        {
            var claims = new List<Claim> { new("sub", subject) };
            claims.AddRange(groups.Select(group => new Claim(GroupClaim, group)));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));
        }

        public static ClaimsPrincipal Noor_SignedIn() => SignedIn(Noor, TeamReaders);

        public static ClaimsPrincipal Sam_SignedIn() => SignedIn(Sam, FinanceStaff);

        public static ClaimsPrincipal TheKey(string owner, int apiKeyId)
            => new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "api-key-user"),
                new Claim("auth_method", "api-key"),
                new Claim("api_key_id", apiKeyId.ToString(CultureInfo.InvariantCulture)),
                new Claim("sub", owner),
            ], "ApiKey"));

        public async Task<RbacOperationResult> AlexAddsViewerOnAllFor(string group)
            => await Port(port => port.CreateGroupMappingAsync(group, UserRole.Viewer, AllUnits, null));

        public async Task<RbacOperationResult> AlexAddsAMapping(string group, UserRole role, PermissionScopeType scope, int? scopeId)
            => await Port(port => port.CreateGroupMappingAsync(group, role, scope, scopeId));

        public async Task AlexRemovesTheRulesOf(string group)
        {
            foreach (var line in await TheRulesList())
            {
                if (line.GroupValue == group)
                {
                    await Port(port => port.RemoveGroupMappingAsync(line.Id));
                }
            }
        }

        public Task<IReadOnlyList<RbacGroupMappingSummary>> TheRulesList() => Port(port => port.GetGroupMappingsAsync());

        public Task<RbacOperationResult> AlexRemovesOneLine(int mappingId) => Port(port => port.RemoveGroupMappingAsync(mappingId));

        public Task<RbacOperationResult> AlexChangesTheRole(int mappingId, UserRole role) => Port(port => port.UpdateGroupMappingRoleAsync(mappingId, role, null));

        /// <summary>Two equal rows, as two System Admins adding the same rule at the same moment can leave behind.</summary>
        /// <returns>The numbers the two rows were stored under, the first one stored first.</returns>
        public int[] TwoEqualRowsWereStored(string group, UserRole role, PermissionScopeType scope, int? scopeId)
        {
            using var scope_ = harness.NewScope();
            var context = scope_.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var first = new RbacGroupMapping { GroupValue = group, Role = role, ScopeType = scope, ScopeId = scopeId };
            context.RbacGroupMappings.Add(first);
            context.SaveChanges();
            var second = new RbacGroupMapping { GroupValue = group, Role = role, ScopeType = scope, ScopeId = scopeId };
            context.RbacGroupMappings.Add(second);
            context.SaveChanges();
            return [first.Id, second.Id];
        }

        public int StoredRowsFor(string group)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().RbacGroupMappings.Count(mapping => mapping.GroupValue == group);
        }

        public void NoorHoldsDirectly(UserRole role, PermissionScopeType scope, int scopeId)
        {
            using var scope_ = harness.NewScope();
            var context = scope_.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var noor = context.UserProfiles.Single(profile => profile.Subject == Noor);
            context.UserPermissions.Add(new UserPermission { UserProfileId = noor.Id, Role = role, ScopeType = scope, ScopeId = scopeId });
            context.SaveChanges();
        }

        /// <summary>The key the server stores once its creation was admitted: the owner, and one row per scope it is limited to.</summary>
        public int AKeyIsStoredFor(string owner, params (UserRole Role, PermissionScopeType Scope, int ScopeId)[] limits)
        {
            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var profile = context.UserProfiles.Single(p => p.Subject == owner);
            var key = new ApiKey
            {
                Name = $"key of {profile.DisplayName}",
                KeyHash = "hash",
                Salt = "salt",
                CreatedByUser = profile.DisplayName ?? owner,
                OwnerUserProfileId = profile.Id,
                OwnerSubject = owner,
                CreatedAt = TestToday.FrozenNow.UtcDateTime,
            };
            context.ApiKeys.Add(key);
            context.SaveChanges();
            foreach (var (role, unitScope, unitId) in limits)
            {
                context.ApiKeyPermissions.Add(new ApiKeyPermission { ApiKeyId = key.Id, Role = role, ScopeType = unitScope, ScopeId = unitId, GrantedAt = TestToday.FrozenNow.UtcDateTime });
            }

            context.SaveChanges();
            return key.Id;
        }

        /// <summary>What the server answers a person asking for a key limited to one role on one Team.</summary>
        public async Task<ObjectResult?> AsksForAKey(ClaimsPrincipal person, UserRole role, int teamId)
        {
            using var scope = harness.NewScope();
            var keys = new Mock<IApiKeyService>(MockBehavior.Strict);
            var controller = new ApiKeyController(keys.Object, scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = person } },
            };

            var answer = await controller.CreateApiKey(new CreateApiKeyRequest
            {
                Name = "zenith-report",
                Scope = [new ApiKeyScopeDto { Role = role, ScopeType = PermissionScopeType.Team, ScopeId = teamId }],
            });
            return answer as ObjectResult;
        }

        public Task<bool> MayIssueAKeyAs(ClaimsPrincipal person, RbacGuardRequirement requirement, int unitId)
            => Port(port => port.CanSatisfyRequirementAsync(person, requirement, unitId));

        public Task<bool> CanReadTeam(ClaimsPrincipal person, int teamId) => Port(port => port.CanReadTeamAsync(person, teamId));

        public Task<bool> CanWriteTeam(ClaimsPrincipal person, int teamId) => Port(port => port.CanWriteTeamAsync(person, teamId));

        public Task<bool> CanReadPortfolio(ClaimsPrincipal person, int portfolioId) => Port(port => port.CanReadPortfolioAsync(person, portfolioId));

        public Task<bool> CanWritePortfolio(ClaimsPrincipal person, int portfolioId) => Port(port => port.CanWritePortfolioAsync(person, portfolioId));

        public Task<bool> CanManageAccess(ClaimsPrincipal person) => Port(port => port.CanManageRbacAsync(person));

        public Task<IReadOnlyList<int>> TheTeamsListed(ClaimsPrincipal person, params int[] teamIds) => Port(port => port.GetReadableTeamIdsAsync(person, teamIds));

        public Task<IReadOnlyList<int>> ThePortfoliosListed(ClaimsPrincipal person, params int[] portfolioIds) => Port(port => port.GetReadablePortfolioIdsAsync(person, portfolioIds));

        public Task<UserAuthorizationSummary> TheSummaryOf(ClaimsPrincipal person) => Port(port => port.GetAuthorizationSummaryAsync(person));

        /// <summary>Stores this many Teams, each on its own connection, and answers their numbers.</summary>
        public IReadOnlyList<int> TeamsAreStored(int howMany)
        {
            using var scope = harness.NewScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var teams = Enumerable.Range(1, howMany).Select(number => new Team
            {
                Name = $"Team {number}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = $"Connection {number}", WorkTrackingSystem = WorkTrackingSystems.Jira },
                DataRetrievalValue = "project = LIGHTHOUSE",
            }).ToList();
            context.Teams.AddRange(teams);
            context.SaveChanges();
            return [.. teams.Select(team => team.Id)];
        }

        public void Dispose() => harness.Dispose();

        private async Task<T> Port<T>(Func<IRbacAdministrationService, Task<T>> step)
        {
            using var scope = harness.NewScope();
            return await step(scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>());
        }

        private static ILicenseService AValidPremiumLicence()
        {
            var licence = new Mock<ILicenseService>();
            licence.Setup(service => service.CanUsePremiumFeatures()).Returns(true);
            return licence.Object;
        }

        private static void AlexIsTheSystemAdmin(LighthouseAppContext context)
        {
            var alex = new UserProfile { Subject = Alex, SubjectClaimType = "sub", DisplayName = "Alex Chen" };
            context.UserProfiles.Add(alex);
            context.SaveChanges();
            context.UserPermissions.Add(new UserPermission { UserProfileId = alex.Id, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System, ScopeId = null });
        }

        /// <summary>Finds the person a principal speaks for among those who signed in before; a stranger, or a principal with no subject, is nobody.</summary>
        private sealed class PeopleWhoSignedInBefore(LighthouseAppContext context) : ICurrentUserProfileService
        {
            public async Task<UserProfile?> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(principal);
                var subject = principal.FindFirst("sub")?.Value;
                if (string.IsNullOrWhiteSpace(subject))
                {
                    return null;
                }

                return await context.UserProfiles.SingleOrDefaultAsync(profile => profile.Subject == subject, cancellationToken);
            }
        }

        /// <summary>Counts every command a step sends, so a list's cost can be compared for few and many units.</summary>
        public sealed class CommandCount : DbCommandInterceptor
        {
            private readonly ConcurrentQueue<string> sent = new();

            public int During(Func<Task> step)
            {
                sent.Clear();
                step().GetAwaiter().GetResult();
                return sent.Count;
            }

            public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            {
                sent.Enqueue(command.CommandText);
                return result;
            }

            public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
            {
                sent.Enqueue(command.CommandText);
                return ValueTask.FromResult(result);
            }

            public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
            {
                sent.Enqueue(command.CommandText);
                return result;
            }

            public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
            {
                sent.Enqueue(command.CommandText);
                return ValueTask.FromResult(result);
            }
        }
    }
}
