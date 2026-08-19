using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Text.Json;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [Authorize]
    public class FeaturesController : ControllerBase
    {
        private readonly IFeatureRepository featureRepository;
        private readonly IWorkItemRepository workItemRepository;
        private readonly IBlackoutPeriodService blackoutPeriodService;
        private readonly IRbacAdministrationService rbacAdministrationService;
        private readonly IBlockedItemService blockedItemService;
        private readonly IFeaturePositionMap featurePositionMap;
        private readonly IFeatureMoveAuthorization featureMoveAuthorization;
        private readonly IFeatureRankingService featureRankingService;
        private readonly IFeatureOrderingPolicyProvider featureOrderingPolicyProvider;
        private readonly ILighthouseClock clock;
        private readonly IDependencyHonourPolicy dependencyHonourPolicy;

#pragma warning disable S107 // Every parameter is a distinct port this controller drives; bundling them into a parameter object would only hide the arity, not the coupling.
        public FeaturesController(
            IFeatureRepository featureRepository,
            IWorkItemRepository workItemRepository,
            IBlackoutPeriodService blackoutPeriodService,
            IRbacAdministrationService rbacAdministrationService,
            IBlockedItemService blockedItemService,
            IFeaturePositionMap featurePositionMap,
            IFeatureMoveAuthorization featureMoveAuthorization,
            IFeatureRankingService featureRankingService,
            IFeatureOrderingPolicyProvider featureOrderingPolicyProvider,
            ILighthouseClock clock,
            IDependencyHonourPolicy dependencyHonourPolicy)
#pragma warning restore S107
        {
            this.featureRepository = featureRepository;
            this.workItemRepository = workItemRepository;
            this.blackoutPeriodService = blackoutPeriodService;
            this.rbacAdministrationService = rbacAdministrationService;
            this.blockedItemService = blockedItemService;
            this.featurePositionMap = featurePositionMap;
            this.featureMoveAuthorization = featureMoveAuthorization;
            this.featureRankingService = featureRankingService;
            this.featureOrderingPolicyProvider = featureOrderingPolicyProvider;
            this.clock = clock;
            this.dependencyHonourPolicy = dependencyHonourPolicy;
        }

        /// <summary>
        /// Every Feature the caller may read, across every Portfolio, in the order the forecast draws from.
        /// Free on every instance - this is the list every screen reads, not the paid re-ordering page.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<FeatureDto>>> GetAllFeatures()
        {
            var features = await GetFeaturesByPredicate(_ => true);

            return Ok(features);
        }

        [HttpGet("ids")]
        public async Task<ActionResult<List<FeatureDto>>> GetFeatureDetailsById([FromQuery] List<int> featureIds)
        {
            if (featureIds.Count == 0)
            {
                return BadRequest();
            }

            var featureDetails = await GetFeaturesByPredicate(f => featureIds.Contains(f.Id));

            return Ok(featureDetails);
        }

        [HttpGet("references")]
        public async Task<ActionResult<List<FeatureDto>>> GetFeatureDetailsByReference([FromQuery] List<string> featureReferences)
        {
            if (featureReferences.Count == 0)
            {
                return BadRequest();
            }

            var featureDetails = await GetFeaturesByPredicate(f => featureReferences.Contains(f.ReferenceId));

            return Ok(featureDetails);
        }

        [HttpGet("{featureId:int}/workitems")]
        public async Task<ActionResult<List<WorkItemDto>>> GetFeatureWorkItems(int featureId)
        {
            var feature = featureRepository.GetById(featureId);
            if (feature is null)
            {
                return NotFound();
            }

            var readablePortfolioIdSet = await GetReadablePortfolioIds(feature.Portfolios.Select(p => p.Id));
            if (!IsReadableBy(feature, readablePortfolioIdSet))
            {
                return NotFound();
            }

            var items = workItemRepository.GetAllByPredicate(wi => wi.ParentReferenceId == feature.ReferenceId)
                .AsEnumerable()
                .Select(w => new WorkItemDto(w, clock, w.Team != null && blockedItemService.IsBlocked(w, w.Team)))
                .ToList();

            return Ok(items);
        }

        /// <summary>
        /// Which Features this one is waiting on, opened from the number on its row. Free on every
        /// instance, and read-only by construction: Lighthouse never records a dependency of its own, so
        /// there is no route anywhere that adds, removes or suppresses one.
        /// </summary>
        [HttpGet("{featureId:int}/dependencies")]
        public async Task<ActionResult<List<FeatureDependencyDto>>> GetFeatureDependencies(int featureId)
        {
            var featuresHeld = featureRepository.GetAll().ToList();

            var feature = featuresHeld.Find(candidate => candidate.Id == featureId);
            if (feature is null)
            {
                return NotFound();
            }

            var readablePortfolioIdSet = await GetReadablePortfolioIds(feature.Portfolios.Select(p => p.Id));
            if (!IsReadableBy(feature, readablePortfolioIdSet))
            {
                return NotFound();
            }

            var blockersHeld = FeaturesWaitedOnBy(feature, featuresHeld);
            var readableBlockerPortfolioIds = await GetReadablePortfolioIds(
                blockersHeld.Values.SelectMany(blocker => blocker.Portfolios).Select(p => p.Id));
            var reasons = await ReasonsAgainstWhat(feature, featuresHeld);

            var entries = feature.DependsOnReferences
                .Where(reference => blockersHeld.ContainsKey(reference.ReferenceId))
                .Select(reference => new FeatureDependencyDto(
                    blockersHeld[reference.ReferenceId],
                    reference.Source,
                    reasons.GetValueOrDefault(reference.ReferenceId),
                    readableBlockerPortfolioIds))
                .ToList();

            return Ok(entries);
        }

        /// <summary>
        /// The Features a Feature waits on that this Lighthouse actually holds. A reference is only ever
        /// an id string the tracker wrote, so one naming something not held yields nothing here - exactly
        /// as it counts for nothing on the row, which is what keeps the number and the list under it
        /// accountable to each other.
        /// </summary>
        private static Dictionary<string, Feature> FeaturesWaitedOnBy(Feature feature, IReadOnlyCollection<Feature> featuresHeld)
        {
            var waitedOn = feature.DependsOnReferences.Select(reference => reference.ReferenceId).ToHashSet(StringComparer.Ordinal);

            return featuresHeld
                .Where(candidate => waitedOn.Contains(candidate.ReferenceId))
                .GroupBy(candidate => candidate.ReferenceId, StringComparer.Ordinal)
                .ToDictionary(byReferenceId => byReferenceId.Key, byReferenceId => byReferenceId.First(), StringComparer.Ordinal);
        }

        /// <summary>
        /// Why Lighthouse will not act on each of the dependencies this Feature has, asked of the one
        /// component that decides it. Nothing here works a reason out for itself: a second opinion is how
        /// a warning ends up disagreeing with what a forecast actually did.
        /// </summary>
        private async Task<Dictionary<string, NotHonouredReason?>> ReasonsAgainstWhat(
            Feature feature, IReadOnlyCollection<Feature> featuresHeld)
        {
            var decided = dependencyHonourPolicy.Evaluate(await TheFactsAbout(featuresHeld));

            return decided.Verdicts
                .Where(verdict => verdict.DependentReferenceId == feature.ReferenceId)
                .GroupBy(verdict => verdict.BlockerReferenceId, StringComparer.Ordinal)
                .ToDictionary(byBlocker => byBlocker.Key, byBlocker => byBlocker.First().Reason, StringComparer.Ordinal);
        }

        /// <summary>
        /// Every Feature the decision is allowed to see, as the plain facts it reads. A Feature with no
        /// place of its own is put last, which is the one position that cannot make another Feature look
        /// as though it were waiting on something below it.
        /// </summary>
        /// <remarks>
        /// The licence answer is left false because nothing may read it yet: no dependency changes a
        /// forecast until that behaviour ships, and until it does an instance's licence has no bearing on
        /// anything decided here. Whoever turns it on has to hand the real answer in from here.
        /// </remarks>
        private async Task<DependencyHonourInput> TheFactsAbout(IReadOnlyCollection<Feature> featuresHeld)
        {
            var positions = await featurePositionMap.GetAsync(RequestAborted);

            var facts = featuresHeld
                .Select(held => new FeatureDependencyFacts(
                    held.ReferenceId,
                    held.Portfolios.Select(portfolio => portfolio.Id).ToList(),
                    positions.TryGetValue(held.Id, out var position) ? position : int.MaxValue,
                    held.CanBeForecast,
                    held.DependsOnReferences.Select(reference => reference.ReferenceId).ToList()))
                .ToList();

            return new DependencyHonourInput(facts, HasPremiumLicence: false);
        }

        /// <summary>
        /// Moves one Feature to the place another one holds. Every gesture in the UI — Top, Up, Down,
        /// Bottom, and "above/below a named Feature" — arrives here, because they differ only in which row
        /// the client names as the target.
        /// </summary>
        [HttpPatch("{featureId:int}/rank")]
        [LicenseGuard(RequirePremium = true)]
        public async Task<ActionResult> MoveFeature(int featureId, [FromBody] JsonElement move)
        {
            if (!TryReadTheTarget(move, out var targetFeatureId, out var placeBefore))
            {
                return BadRequest();
            }

            var feature = featureRepository.GetById(featureId);
            if (feature is null)
            {
                return NotFound();
            }

            // While the tracker owns the order, a place written here is one nobody reads. Accepting it would
            // leave the caller looking at an unmoved list with no way to tell why.
            if (featureOrderingPolicyProvider.GetPolicy() != FeatureOrderingPolicy.ManualOrder)
            {
                return Forbid();
            }

            var verdicts = await GetMoveVerdicts([feature]);
            if (!verdicts[feature.Id].CanMove)
            {
                return Forbid();
            }

            var placement = await featureRankingService.PlaceAsync(featureId, targetFeatureId, placeBefore, RequestAborted);

            return placement == FeatureMovePlacement.Placed ? Ok() : NotFound();
        }

        /// <summary>
        /// The command carries exactly one target. <c>beforeFeatureId: null</c> is the end of the
        /// order; naming both, or neither, is not a move and is not guessed at.
        /// </summary>
        private static bool TryReadTheTarget(JsonElement move, out int? targetFeatureId, out bool placeBefore)
        {
            targetFeatureId = null;
            placeBefore = true;

            if (move.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var namesBefore = move.TryGetProperty("beforeFeatureId", out var before);
            var namesAfter = move.TryGetProperty("afterFeatureId", out var after);

            if (namesBefore == namesAfter)
            {
                return false;
            }

            var named = namesBefore ? before : after;
            placeBefore = namesBefore;

            if (namesBefore && named.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            if (named.ValueKind != JsonValueKind.Number || !named.TryGetInt32(out var id))
            {
                return false;
            }

            targetFeatureId = id;
            return true;
        }

        private async Task<List<FeatureDto>> GetFeaturesByPredicate(Expression<Func<Feature, bool>> predicate)
        {
            var features = featureRepository.GetAllByPredicate(predicate).ToList();
            var readablePortfolioIdSet = await GetReadablePortfolioIds(features.SelectMany(f => f.Portfolios).Select(p => p.Id));
            var positions = await featurePositionMap.GetAsync(RequestAborted);
            var forecastWindowStart = clock.TodayAsUtcMidnight;
            var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                forecastWindowStart, FeatureForecastWindow.EndFor(forecastWindowStart, features));

            var readable = features.Where(f => IsReadableBy(f, readablePortfolioIdSet)).ToList();
            var verdicts = await featureMoveAuthorization.GetVerdictsAsync(User, readable, readablePortfolioIdSet, RequestAborted);
            var referenceIdsHeld = ReferenceIdsHeld();

            return readable
                .Select(f => BuildFeatureDto(f, blackoutPeriods, readablePortfolioIdSet, positions, verdicts[f.Id], referenceIdsHeld))
                .ToList();
        }

        /// <summary>
        /// Every Feature id this Lighthouse holds. A dependency is only ever an id string the tracker
        /// wrote, so nothing knows whether it names a Feature until it is matched against these — and a
        /// request for a handful of Features still has to match against all of them. Read as bare id
        /// strings on purpose: fetching whole Features to answer a question about ids would pull every
        /// Portfolio, team assignment, forecast and simulation result along with them, on a route that
        /// has already read all of that once.
        /// </summary>
        private HashSet<string> ReferenceIdsHeld()
            => featureRepository.GetAllReferenceIds().ToHashSet(StringComparer.Ordinal);

        private FeatureDto BuildFeatureDto(
            Feature feature,
            IReadOnlyList<BlackoutPeriod> blackoutPeriods,
            HashSet<int> readablePortfolioIds,
            IReadOnlyDictionary<int, int> positions,
            FeatureMoveVerdict verdict,
            HashSet<string> referenceIdsHeld)
        {
            var isBlocked = feature.Portfolios.Any(p => blockedItemService.IsBlocked(feature, p));

            var dto = new FeatureDto(feature, clock, blackoutPeriods, isBlocked, null, readablePortfolioIds)
            {
                // Null only if the Feature was deleted between the row read and the position read.
                Position = positions.TryGetValue(feature.Id, out var position) ? position : null,
                CanMove = verdict.CanMove,
                MoveBlockReason = verdict.BlockReason,
                DependsOnCount = feature.DependsOnReferences.Count(r => referenceIdsHeld.Contains(r.ReferenceId)),
            };

            dto.BlockingPortfolios.AddRange(verdict.BlockingPortfolios.Select(p => new EntityReferenceDto(p.Id, p.Name)));

            return dto;
        }

        private async Task<IReadOnlyDictionary<int, FeatureMoveVerdict>> GetMoveVerdicts(IReadOnlyCollection<Feature> features)
        {
            var readablePortfolioIdSet = await GetReadablePortfolioIds(features.SelectMany(f => f.Portfolios).Select(p => p.Id));

            return await featureMoveAuthorization.GetVerdictsAsync(User, features, readablePortfolioIdSet, RequestAborted);
        }

        // A Feature in no Portfolio is visible to everyone; otherwise one readable Portfolio is enough.
        private static bool IsReadableBy(Feature feature, HashSet<int> readablePortfolioIds)
        {
            return feature.Portfolios.Count == 0 || feature.Portfolios.Any(p => readablePortfolioIds.Contains(p.Id));
        }

        private async Task<HashSet<int>> GetReadablePortfolioIds(IEnumerable<int> portfolioIds)
        {
            var requestedPortfolioIds = portfolioIds.Distinct().ToArray();
            var readablePortfolioIds = await rbacAdministrationService
                .GetReadablePortfolioIdsAsync(User, requestedPortfolioIds, RequestAborted)
                .ConfigureAwait(false);

            return readablePortfolioIds is { } ? readablePortfolioIds.ToHashSet() : requestedPortfolioIds.ToHashSet();
        }

        private CancellationToken RequestAborted => HttpContext?.RequestAborted ?? default;
    }
}
