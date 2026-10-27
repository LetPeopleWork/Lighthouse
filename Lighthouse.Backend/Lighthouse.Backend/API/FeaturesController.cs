using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Dependencies;
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
        private readonly FeatureDependencyRendering dependencyRendering;

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
            IDependencyDecision dependencyDecision)
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
            dependencyRendering = new FeatureDependencyRendering(featureRepository, rbacAdministrationService, dependencyDecision);
        }

        /// <summary>
        /// Every Feature the caller may read, across every Portfolio, in the order the forecast draws from.
        /// Free on every instance - this is the list every screen reads, not the paid re-ordering page.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<FeatureDto>>> GetAllFeatures()
        {
            var features = await GetFeaturesByPredicate(_ => true, everyFeatureIsLoaded: true);

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
                .Select(w => new WorkItemDto(w, clock, w.Team != null && blockedItemService.IsBlocked(w, w.Team)) { Estimate = w.Team != null ? WorkItemEstimateDto.For(w.Team, w) : null })
                .ToList();

            return Ok(items);
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

        /// <param name="everyFeatureIsLoaded">
        /// True when the predicate leaves nothing out, so the caller's own list can answer the dependency
        /// question without loading the Features a second time. It is an optimisation and nothing else:
        /// a request for a handful gets the same verdicts, worked out over the same whole graph.
        /// </param>
        private async Task<List<FeatureDto>> GetFeaturesByPredicate(
            Expression<Func<Feature, bool>> predicate, bool everyFeatureIsLoaded = false)
        {
            var features = featureRepository.GetAllByPredicate(predicate).ToList();
            var readablePortfolioIdSet = await GetReadablePortfolioIds(features.SelectMany(f => f.Portfolios).Select(p => p.Id));
            var positions = await featurePositionMap.GetAsync(RequestAborted);
            var forecastWindowStart = clock.TodayAsUtcMidnight;
            var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                forecastWindowStart, FeatureForecastWindow.EndFor(forecastWindowStart, features));

            var readable = features.Where(f => IsReadableBy(f, readablePortfolioIdSet)).ToList();
            var verdicts = await featureMoveAuthorization.GetVerdictsAsync(User, readable, readablePortfolioIdSet, RequestAborted);
            var dependencies = await dependencyRendering.WhatTheseFeaturesWaitOn(User, features, positions, everyFeatureIsLoaded, RequestAborted);

            return readable
                .Select(f => BuildFeatureDto(f, blackoutPeriods, readablePortfolioIdSet, positions, verdicts[f.Id], dependencies))
                .ToList();
        }

        private FeatureDto BuildFeatureDto(
            Feature feature,
            IReadOnlyList<BlackoutPeriod> blackoutPeriods,
            HashSet<int> readablePortfolioIds,
            IReadOnlyDictionary<int, int> positions,
            FeatureMoveVerdict verdict,
            DependenciesAsRead dependencies)
        {
            var isBlocked = feature.Portfolios.Any(p => blockedItemService.IsBlocked(feature, p));

            var dto = new FeatureDto(feature, clock, blackoutPeriods, isBlocked, null, readablePortfolioIds)
            {
                // Null only if the Feature was deleted between the row read and the position read.
                Position = positions.TryGetValue(feature.Id, out var position) ? position : null,
                CanMove = verdict.CanMove,
                MoveBlockReason = verdict.BlockReason,
            };

            dto.DependsOn.AddRange(dependencies.Of(feature));
            dto.BlockingPortfolios.AddRange(verdict.BlockingPortfolios.Select(p => new EntityReferenceDto(p.Id, p.Name)));

            return dto;
        }

        private async Task<IReadOnlyDictionary<int, FeatureMoveVerdict>> GetMoveVerdicts(IReadOnlyCollection<Feature> features)
        {
            var readablePortfolioIdSet = await GetReadablePortfolioIds(features.SelectMany(f => f.Portfolios).Select(p => p.Id));

            return await featureMoveAuthorization.GetVerdictsAsync(User, features, readablePortfolioIdSet, RequestAborted);
        }

        private static bool IsReadableBy(Feature feature, HashSet<int> readablePortfolioIds)
            => FeatureReadability.IsReadableBy(feature, readablePortfolioIds);

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
