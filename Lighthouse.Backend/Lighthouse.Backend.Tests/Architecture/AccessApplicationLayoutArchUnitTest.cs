using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.Access.Domain.LayoutFixtures.Clean;
using Lighthouse.Backend.Modules.Access.Domain.LayoutFixtures.ReachingARepository;
using Mono.Cecil;
using Lighthouse.Backend.Modules.Access.Contracts;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The core of Access has its own module folder. Its pure rules and the plain values they work on, the shared Models
    /// types Access takes with it included, live in the module's Domain and reach only the shared kernel and the base
    /// library; its services and their interfaces live in the module's Application. None is left under the old Services or
    /// Models folders. Every type name is read from the shared membership list and looked up in the compiled assembly, so a
    /// name that matches no type fails and names itself. The move changes namespaces only, so the public members of the RBAC
    /// administration service every controller calls are pinned as they were before the move.
    /// </summary>
    [TestFixture]
    public class AccessApplicationLayoutArchUnitTest
    {
        private const string DomainNamespace = "Lighthouse.Backend.Modules.Access.Domain";

        private const string ApplicationNamespace = "Lighthouse.Backend.Modules.Access.Application";

        private const string TheAccessDomain = @"^Lighthouse\.Backend\.Modules\.Access\.Domain(\.|$)";

        private const string WhereAccessLivedBefore =
            @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.(Auth|Authorization)|Models\.(Auth|Authorization))(\.|$)";

        private const string TheAdministrationService = "IRbacAdministrationService";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(AccessApplicationLayoutArchUnitTest).Assembly));

        // Read from the compiled interface before the move. The move changes the interface's namespace and nothing else,
        // so every line here holds before and after it.
        private static readonly string[] TheAdministrationServiceMembers =
        [
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacStatus> GetStatusAsync(System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> IsRbacEnforcedAsync(System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Collections.Generic.IReadOnlyList`1<Lighthouse.Backend.Models.Authorization.RbacUserSummary>> GetUsersAsync(System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Collections.Generic.IReadOnlyList`1<System.Int32>> GetReadableTeamIdsAsync(System.Security.Claims.ClaimsPrincipal, System.Collections.Generic.IEnumerable`1<System.Int32>, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Collections.Generic.IReadOnlyList`1<System.Int32>> GetReadablePortfolioIdsAsync(System.Security.Claims.ClaimsPrincipal, System.Collections.Generic.IEnumerable`1<System.Int32>, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Collections.Generic.IReadOnlyList`1<System.Int32>> GetWritablePortfolioIdsAsync(System.Security.Claims.ClaimsPrincipal, System.Collections.Generic.IEnumerable`1<System.Int32>, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> CanReadTeamAsync(System.Security.Claims.ClaimsPrincipal, System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> CanWriteTeamAsync(System.Security.Claims.ClaimsPrincipal, System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> CanReadPortfolioAsync(System.Security.Claims.ClaimsPrincipal, System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> CanWritePortfolioAsync(System.Security.Claims.ClaimsPrincipal, System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> CanCreateTeamAsync(System.Security.Claims.ClaimsPrincipal, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> CanCreatePortfolioAsync(System.Security.Claims.ClaimsPrincipal, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> CanSatisfyRequirementAsync(System.Security.Claims.ClaimsPrincipal, Lighthouse.Backend.Models.Authorization.RbacGuardRequirement, System.Nullable`1<System.Int32>, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.UserAuthorizationSummary> GetAuthorizationSummaryAsync(System.Security.Claims.ClaimsPrincipal, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> BootstrapCurrentUserAsSystemAdminAsync(System.Security.Claims.ClaimsPrincipal, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> GrantSystemAdminAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> RevokeSystemAdminAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> DeleteUserAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> CanManageRbacAsync(System.Security.Claims.ClaimsPrincipal, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> IsRestrictedApiKeyAsync(System.Security.Claims.ClaimsPrincipal, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> CanManageTeamMembershipAsync(System.Security.Claims.ClaimsPrincipal, System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> CanManagePortfolioMembershipAsync(System.Security.Claims.ClaimsPrincipal, System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Collections.Generic.IReadOnlyList`1<Lighthouse.Backend.Models.Authorization.RbacScopedMemberSummary>> GetTeamMembersAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Collections.Generic.IReadOnlyList`1<Lighthouse.Backend.Models.Authorization.RbacScopedMemberSummary>> GetPortfolioMembersAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> SetTeamMemberRoleAsync(System.Int32, System.Int32, Lighthouse.Backend.Models.Authorization.UserRole, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> SetPortfolioMemberRoleAsync(System.Int32, System.Int32, Lighthouse.Backend.Models.Authorization.UserRole, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> RemoveTeamMemberAsync(System.Int32, System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> RemovePortfolioMemberAsync(System.Int32, System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Collections.Generic.IReadOnlyList`1<Lighthouse.Backend.Models.Authorization.RbacGroupMappingSummary>> GetGroupMappingsAsync(System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Collections.Generic.IReadOnlyList`1<Lighthouse.Backend.Models.Authorization.RbacGroupMappingSummary>> GetTeamGroupMappingsAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Collections.Generic.IReadOnlyList`1<Lighthouse.Backend.Models.Authorization.RbacGroupMappingSummary>> GetPortfolioGroupMappingsAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> TeamExistsAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Boolean> PortfolioExistsAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Collections.Generic.IReadOnlyList`1<Lighthouse.Backend.Models.Authorization.SystemWideRule>> GetTeamSystemWideRulesAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<System.Collections.Generic.IReadOnlyList`1<Lighthouse.Backend.Models.Authorization.SystemWideRule>> GetPortfolioSystemWideRulesAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.EveryoneSignedInSetting> GetEveryoneSignedInAsync(System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> SetEveryoneSignedInAsync(System.Security.Claims.ClaimsPrincipal, System.Boolean, System.Nullable`1<System.Guid>, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> CreateGroupMappingAsync(System.String, Lighthouse.Backend.Models.Authorization.UserRole, Lighthouse.Backend.Models.Authorization.PermissionScopeType, System.Nullable`1<System.Int32>, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> RemoveGroupMappingAsync(System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> UpdateGroupMappingRoleAsync(System.Int32, Lighthouse.Backend.Models.Authorization.UserRole, System.Nullable`1<System.Guid>, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> GrantCreatorTeamAdminAsync(System.Int32, System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task`1<Lighthouse.Backend.Models.Authorization.RbacOperationResult> GrantCreatorPortfolioAdminAsync(System.Int32, System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task EnsureCreatorTeamAdminAsync(System.Security.Claims.ClaimsPrincipal, System.Int32, System.Threading.CancellationToken)",
            "System.Threading.Tasks.Task EnsureCreatorPortfolioAdminAsync(System.Security.Claims.ClaimsPrincipal, System.Int32, System.Threading.CancellationToken)",
        ];

        private static readonly string[] MadeUpAdministrationBeforeTheMove =
        [
            "System.String KeptAsync(System.String)",
            "System.Boolean WasRemovedAsync()",
            "System.Int32 CountAsync(System.Int32)",
        ];

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheAccessRulesAndTheValuesTheyWorkOn_LiveInTheAccessDomain_AndNoneIsLeftUnderServicesOrModels()
        {
            IReadOnlyCollection<string> domainTypes = [.. AccessAndLicensingMembership.AccessDomain, .. AccessAndLicensingMembership.ModelsTypesThatMoveIntoAccess];

            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, DomainNamespace, domainTypes, WhereAccessLivedBefore).Check();
        }

        [Test]
        public void TheAccessServicesAndTheirInterfaces_LiveInTheAccessApplication_AndNoneIsLeftUnderServices()
        {
            IReadOnlyCollection<string> applicationTypes = [.. AccessAndLicensingMembership.AccessApplication.Except(AccessAndLicensingMembership.AccessContracts)];

            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, applicationTypes, WhereAccessLivedBefore).Check();
        }

        [Test]
        public void TheAccessDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, TheAccessDomain).Check();
        }

        [Test]
        public void AnAccessDomainTypeThatReadsARepository_IsReported_AndItsTwinReachingOnlyTheKernelIsNot()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(FixtureModule.Value, TheAccessDomain);

            ThenTheRuleReportsOnlyTheOffender(verdict);
        }

        [Test]
        public void TheRbacAdministrationService_KeepsEveryPublicMember_ExactlyAsBeforeTheMove()
        {
            var members = WhenThePublicMembersAreRead(TheOnlyProductionTypeNamed(TheAdministrationService));

            ThenTheMembersAreExactly(members, TheAdministrationServiceMembers, TheAdministrationService);
        }

        [Test]
        public void AnInterfaceWithAMemberAddedRemovedAndRetyped_FailsTheMemberPin_NamingEachChange()
        {
            var members = WhenThePublicMembersAreRead(FixtureType(typeof(IAMadeUpAdministrationAfterAChangedMove)));

            var failure = Assert.Throws<AssertionException>(
                () => ThenTheMembersAreExactly(members, MadeUpAdministrationBeforeTheMove, nameof(IAMadeUpAdministrationAfterAChangedMove)),
                "A member pin passed for an interface that gained, lost and re-typed a member. Why it matters: a pin that cannot fail lets a move " +
                "change the surface every caller depends on. How to fix: compare the members read from the IL with the fixed list both ways.")!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain("System.Boolean IsNewlyAddedAsync()"),
                    "The member added after the move was not named. The pin must name a member the fixed list does not hold.");
                Assert.That(failure, Does.Contain("System.Boolean WasRemovedAsync()"),
                    "The member removed by the move was not named. The pin must name a fixed member the interface no longer has.");
                Assert.That(failure, Does.Contain("System.Int64 CountAsync(System.Int32)").And.Contain("System.Int32 CountAsync(System.Int32)"),
                    "The member whose return type changed was not named with both its old and its new type. A re-typed member must fail the pin.");
                Assert.That(failure, Does.Not.Contain("System.String KeptAsync(System.String)"),
                    "A member that kept its name, return type and parameter types was named. The pin must accept a member that did not change.");
            }
        }

        private static TypeDefinition TheOnlyProductionTypeNamed(string name)
        {
            var found = ProductionModule.Value.Types.Where(type => type.Name == name).ToList();

            Assert.That(
                found.Select(type => type.FullName).ToList(),
                Has.Count.EqualTo(1),
                $"Expected exactly one top-level type named {name} in the production assembly, found [{string.Join(", ", found.Select(type => type.FullName))}]. " +
                "Why it matters: the member pin reads the one interface every controller calls; none means it was renamed, two means a copy was left " +
                "behind by the move. How to fix: keep one interface of that name, moved with git mv, with only its namespace line changed.");

            return found[0];
        }

        private static TypeDefinition FixtureType(Type type) => FixtureModule.Value.GetType(type.FullName!.Replace('+', '/'));

        private static List<string> WhenThePublicMembersAreRead(TypeDefinition type)
            => [.. type.Methods
                .Where(method => method.IsPublic)
                .Select(method => $"{method.ReturnType.FullName} {method.Name}({string.Join(", ", method.Parameters.Select(parameter => parameter.ParameterType.FullName))})")];

        private static void ThenTheMembersAreExactly(IReadOnlyCollection<string> actual, IReadOnlyCollection<string> fixedBeforeTheMove, string typeName)
        {
            var added = actual.Except(fixedBeforeTheMove, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
            var removed = fixedBeforeTheMove.Except(actual, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

            Assert.That(
                added.Concat(removed),
                Is.Empty,
                $"The public members of {typeName} changed. Not on the fixed list: [{string.Join("; ", added)}]. On the fixed list but gone: " +
                $"[{string.Join("; ", removed)}]. Why it matters: moving a type to its module changes its namespace and nothing else, and every " +
                "controller and test double calls these members, so a member added, removed or re-typed is a behaviour change hidden inside a move. " +
                "How to fix: undo the member change; the move only changes the namespace line.");
        }

        private static void ThenTheRuleReportsOnlyTheOffender(KernelRuleVerdict verdict)
        {
            var offender = typeof(AMadeUpAccessRuleReadingApiKeys).FullName!;
            var compliantTwin = typeof(AMadeUpAccessRuleReachingOnlyTheKernel).FullName!;
            var repository = typeof(global::Lighthouse.Backend.Services.Interfaces.Repositories.IApiKeyRepository).FullName!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    verdict.Examined,
                    Does.Contain(compliantTwin).And.Contain(offender),
                    "The Access Domain rule did not look at both made-up Access Domain types. Why it matters: a twin the rule never looked at " +
                    "proves nothing by passing, and a pattern that misses Modules.Access.Domain would pass the real code unseen. How to fix: " +
                    @"point the rule at ^Lighthouse\.Backend\.Modules\.Access\.Domain(\.|$).");

                Assert.That(
                    verdict.Offenders.Select(dependent => dependent.TypeName),
                    Is.EqualTo(new[] { offender }),
                    $"Expected exactly {offender} to be reported. Why it matters: an Access Domain type that reads a repository could answer " +
                    "differently for the same values, and a twin that reaches only its Domain, the Models and the base library must not be " +
                    "reported, or the rule flags everything. How to fix: the rule allows the Domain itself, the shared kernel, System.* and compiler-made types.");
            }

            var failure = Assert.Throws<AssertionException>(verdict.Check, $"The Access Domain rule passed with {offender} in it.");

            Assert.That(
                failure!.Message,
                Does.Contain(offender).And.Contain(repository),
                "The failure must name both the Access Domain type and the repository it reached, so the reader knows what to undo.");
        }

        internal interface IAMadeUpAdministrationAfterAChangedMove
        {
            string KeptAsync(string value);

            long CountAsync(int value);

            bool IsNewlyAddedAsync();
        }
    }
}

namespace Lighthouse.Backend.Modules.Access.Domain.LayoutFixtures.ReachingARepository
{
    internal sealed class AMadeUpAccessRuleReadingApiKeys(global::Lighthouse.Backend.Services.Interfaces.Repositories.IApiKeyRepository apiKeys)
    {
        public object Keys() => apiKeys;
    }
}

namespace Lighthouse.Backend.Modules.Access.Domain.LayoutFixtures.Clean
{
    /// <summary>
    /// Reaches only what a Domain may: a sibling in its own Domain, a Models type and the base library. Its iterator also
    /// makes the compiler write a helper type, so a rule that mistook that for a forbidden type would report it.
    /// </summary>
    internal static class AMadeUpAccessRuleReachingOnlyTheKernel
    {
        public static bool MayWrite(global::Lighthouse.Backend.Models.Authorization.UserRole role)
            => role != ASiblingInTheSameDomain.TheReadOnlyRole;

        public static IEnumerable<string> Shown(IEnumerable<global::Lighthouse.Backend.Models.Authorization.UserRole> roles)
        {
            foreach (var role in roles)
            {
                yield return role.ToString();
            }
        }
    }

    internal static class ASiblingInTheSameDomain
    {
        public const global::Lighthouse.Backend.Models.Authorization.UserRole TheReadOnlyRole = default;
    }
}
