using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ManualSorting
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic 5375 slice 01 — the Features view read path.
    /// Backend-observable contract: one read port lists every Feature the caller may see, in the order
    /// the forecast draws from, each row carrying its position across the whole instance (ADR-135 — the
    /// ordinal is numbered over the whole table BEFORE the result set is filtered) and naming every
    /// Portfolio it belongs to.
    /// </summary>
    public partial class Slice01FeaturesViewTest : ManualSortingAcceptanceTest
    {
        private readonly record struct ListedFeature(int Id, string Name, int Position, string[] Portfolios);

        // --- Given ---

        private int GivenAPortfolio(string name) => SeedPortfolio(name);

        private int GivenAFeatureTheTrackerRanked(string name, string sourceOrder, params int[] portfolioIds)
            => SeedFeature(name, sourceOrder, StateCategories.ToDo, portfolioIds);

        private int GivenAFinishedFeature(string name, string sourceOrder, params int[] portfolioIds)
            => SeedFeature(name, sourceOrder, StateCategories.Done, portfolioIds);

        private int GivenAFeatureTheTrackerNeverRanked(string name, params int[] portfolioIds)
            => SeedFeature(name, string.Empty, StateCategories.ToDo, portfolioIds);

        /// <summary>
        /// AC-1.5's fixture: a run of consecutively-ranked Features spread across Portfolios so that the
        /// two the reader may see sit at non-adjacent places in the global order.
        /// </summary>
        private void GivenTheInstanceIsRankedFromOneTo(int lastRank, Func<int, int[]> portfoliosForRank)
            => SeedRankedFeatures(lastRank, portfoliosForRank);

        private void GivenTheCallerMayReadOnly(params int[] portfolioIds) => TheCallerCanReadPortfolios(portfolioIds);

        private void GivenTheCallerAdministersTheInstance() => TheCallerAdministersTheWholeInstance();

        private void GivenTheInstanceHasNoPremiumLicence() => TheInstanceIsNotLicensedForPremium();

        // --- Given (the writable batch — OQ-1; each Given puts the service in exactly one early-return branch) ---

        private static RbacAdministrationService GivenAccessControlIsSwitchedOff(LighthouseAppContext store)
            => BuildRealRbacService(store, LicensedInstance(), new Mock<ICurrentUserProfileService>(), rbacEnabled: false);

        /// <summary>
        /// Access control is on but nobody administers it, so the enforcement gate is unsatisfied.
        /// </summary>
        private static RbacAdministrationService GivenAccessControlIsOnButUnusable(LighthouseAppContext store)
            => BuildRealRbacService(store, LicensedInstance(), new Mock<ICurrentUserProfileService>(), rbacEnabled: true);

        private static async Task<RbacAdministrationService> GivenAccessControlIsOnWithAnAdministrator(LighthouseAppContext store, string subject)
        {
            var administrator = await AddProfile(store, id: 1, subject);
            store.UserPermissions.Add(new UserPermission
            {
                UserProfileId = administrator.Id,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
            });
            await store.SaveChangesAsync();

            return BuildRealRbacService(store, LicensedInstance(), ResolvingTo(administrator), rbacEnabled: true);
        }

        private static async Task<RbacAdministrationService> GivenAccessControlIsOnAndTheCallerIsUnrecognised(LighthouseAppContext store)
        {
            await AddStandingAdministrator(store);

            var unresolvable = new Mock<ICurrentUserProfileService>();
            unresolvable
                .Setup(s => s.GetOrCreateFromPrincipalAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);

            return BuildRealRbacService(store, LicensedInstance(), unresolvable, rbacEnabled: true);
        }

        private static async Task<RbacAdministrationService> GivenAccessControlIsOnWithAReaderOf(LighthouseAppContext store, string subject, int portfolioId)
        {
            await AddStandingAdministrator(store);

            var reader = await AddProfile(store, id: 2, subject);
            store.UserPermissions.Add(new UserPermission
            {
                UserProfileId = reader.Id,
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = portfolioId,
            });
            await store.SaveChangesAsync();

            return BuildRealRbacService(store, LicensedInstance(), ResolvingTo(reader), rbacEnabled: true);
        }

        private static async Task AddStandingAdministrator(LighthouseAppContext store)
        {
            var administrator = await AddProfile(store, id: 99, "the-standing-administrator");
            store.UserPermissions.Add(new UserPermission
            {
                UserProfileId = administrator.Id,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
            });
            await store.SaveChangesAsync();
        }

        private static async Task<UserProfile> AddProfile(LighthouseAppContext store, int id, string subject)
        {
            var profile = new UserProfile { Id = id, Subject = subject, SubjectClaimType = "sub", DisplayName = subject };
            store.UserProfiles.Add(profile);
            await store.SaveChangesAsync();
            return profile;
        }

        private static Mock<ILicenseService> LicensedInstance()
        {
            var licence = new Mock<ILicenseService>();
            licence.Setup(l => l.CanUsePremiumFeatures()).Returns(true);
            return licence;
        }

        private static Mock<ICurrentUserProfileService> ResolvingTo(UserProfile profile)
        {
            var profileService = new Mock<ICurrentUserProfileService>();
            profileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            return profileService;
        }

        // --- When ---

        private Task<(HttpStatusCode Status, string Body)> WhenTheProductOwnerOpensTheFeaturesView() => GetAllFeatures();

        // --- Then ---

        private static void ThenExactlyTheseFeaturesAreListed((HttpStatusCode Status, string Body) response, string[] expectedNames)
        {
            var listed = ParseListedFeatures(response);

            Assert.That(listed.Select(f => f.Name).ToArray(), Is.EqualTo(expectedNames),
                $"The view must list every {nameof(Feature)} from a readable Portfolio and nothing else, in forecast order. Body: {response.Body}");
        }

        private static void ThenTheListedPositionsAre((HttpStatusCode Status, string Body) response, int[] expectedPositions)
        {
            var listed = ParseListedFeatures(response);

            Assert.That(listed.Select(f => f.Position).ToArray(), Is.EqualTo(expectedPositions),
                $"The position is the rank across the whole instance, never the index of the visible row. Body: {response.Body}");
        }

        private static void ThenTheFeatureIsListedOnceNaming((HttpStatusCode Status, string Body) response, string featureName, string[] expectedPortfolios)
        {
            var listed = ParseListedFeatures(response);
            var matching = listed.Where(f => f.Name == featureName).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(matching, Has.Count.EqualTo(1),
                    $"A {nameof(Feature)} belonging to several Portfolios must appear exactly once. Body: {response.Body}");
                Assert.That(matching.Single().Portfolios.Order().ToArray(), Is.EqualTo(expectedPortfolios.Order().ToArray()),
                    $"The row must name every Portfolio the caller may read it through. Body: {response.Body}");
            }
        }

        private static void ThenEveryListedFeatureReportsAPosition((HttpStatusCode Status, string Body) response)
        {
            var listed = ParseListedFeatures(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(listed, Is.Not.Empty, $"The fixture must produce rows to judge. Body: {response.Body}");
                Assert.That(listed.Select(f => f.Position), Has.All.GreaterThan(0),
                    $"Every row reports a place in the order — no blank, no zero. Body: {response.Body}");
                Assert.That(listed.Select(f => f.Position).Distinct().Count(), Is.EqualTo(listed.Count),
                    $"Two Features may not claim the same place in the order. Body: {response.Body}");
            }
        }

        private static void ThenThisManyFeaturesAreListed((HttpStatusCode Status, string Body) response, int expectedCount)
        {
            var listed = ParseListedFeatures(response);

            Assert.That(listed, Has.Count.EqualTo(expectedCount),
                $"The view must answer for the whole instance. Body length: {response.Body.Length}");
        }

        private static void ThenTheViewOpened((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The Features view is not premium and stays open on an unlicensed instance (D12). Body: {response.Body}");
        }

        // --- Then (the writable batch — OQ-1) ---

        private static void ThenEveryPortfolioIsWritable(IReadOnlyList<int> writable, int[] requested)
        {
            Assert.That(writable.Order().ToArray(), Is.EqualTo(requested.Order().ToArray()),
                "This branch must hand back every requested Portfolio — diverging from the read path here is silent over- or under-permission.");
        }

        private static void ThenNoPortfolioIsWritable(IReadOnlyList<int> writable)
        {
            Assert.That(writable, Is.Empty,
                "This branch must fail closed — a half-configured or unrecognised caller may write nothing.");
        }

        // --- Parsing ---

        private static List<ListedFeature> ParseListedFeatures((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The Features view read port must answer. Body: {Excerpt(response.Body)}");
            Assert.That(response.Body.TrimStart(), Does.StartWith("["),
                $"The read port must return a JSON array, not HTML/other — the endpoint appears unimplemented. Body starts: {Excerpt(response.Body)}");

            using var document = JsonDocument.Parse(response.Body);

            return document.RootElement
                .EnumerateArray()
                .Select(element => new ListedFeature(
                    element.GetProperty("id").GetInt32(),
                    element.GetProperty("name").GetString() ?? string.Empty,
                    ReadPosition(element),
                    element.GetProperty("projects").EnumerateArray().Select(p => p.GetProperty("name").GetString() ?? string.Empty).ToArray()))
                .ToList();
        }

        private static int ReadPosition(JsonElement element)
        {
            Assert.That(element.TryGetProperty("position", out var position), Is.True,
                $"Every row must carry its place in the order. Row: {Excerpt(element.ToString())}");
            Assert.That(position.ValueKind, Is.EqualTo(JsonValueKind.Number),
                $"The position must be a number — never null, never a blank string. Row: {Excerpt(element.ToString())}");

            return position.GetInt32();
        }

        private static string Excerpt(string body) => body[..Math.Min(120, body.Length)];
    }
}
