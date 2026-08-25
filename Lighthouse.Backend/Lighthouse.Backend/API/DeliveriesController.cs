using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.DTO.Archived;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
#pragma warning disable S107 // Bug #5567 adds the clock as the named seam for "which calendar day is it?", Story #5640 adds the projector as the one place a Delivery's numbers are read, and Epic #5565 adds the source resolver as the one place a remote Release is asked about; folding any of them into an aggregate with the unrelated repositories would hide it.
    public class DeliveriesController(
        IDeliveryRepository deliveryRepository,
        IRepository<Portfolio> portfolioRepository,
        ILicenseService licenseService,
        IDeliveryRuleService deliveryRuleService,
        IRbacAdministrationService rbacAdministrationService,
        IDeliveryMetricSnapshotRepository deliveryMetricSnapshotRepository,
        IBlackoutPeriodService blackoutPeriodService,
        DeliveryMetricValuesProjector deliveryMetricValuesProjector,
        ILighthouseClock clock,
        IDeliverySourceResolver deliverySourceResolver)
#pragma warning restore S107
        : ControllerBase
    {
        [HttpGet("portfolio/{portfolioId:int}")]
        [RbacGuard(RbacGuardRequirement.PortfolioRead, ScopeIdRouteKey = "portfolioId")]
        [ProducesResponseType<PortfolioDeliveriesDto>(StatusCodes.Status200OK)]
        public IActionResult GetByPortfolio(int portfolioId)
        {
            var deliveries = deliveryRepository.GetByPortfolioAsync(portfolioId).ToList();
            var active = deliveries.Where(delivery => delivery.ArchivedOn == null).ToList();
            var archived = deliveries.Where(delivery => delivery.ArchivedOn != null).ToList();

            return Ok(new PortfolioDeliveriesDto
            {
                Active = ActiveRows(active),
                Archived = ArchivedRows(archived),
            });
        }

        /// <summary>
        /// The forecast window is measured over the Deliveries still running. A retired Delivery is
        /// not forecast at all, so letting its target date stretch the window would have the screen
        /// and the day's recorded history read against two different sets of non-working days for a
        /// Delivery nobody touched.
        /// </summary>
        private List<DeliveryWithLikelihoodDto> ActiveRows(List<Delivery> deliveries)
        {
            var forecastWindowStart = clock.TodayAsUtcMidnight;
            var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                forecastWindowStart, ForecastWindowEnd(deliveries, forecastWindowStart));
            var deliveryDtos = deliveries
                .Select(delivery => DeliveryWithLikelihoodDto.FromDelivery(delivery, clock.Today, blackoutPeriods))
                .ToList();

            var snapshotCounts = deliveryMetricSnapshotRepository.GetSnapshotCountsByDelivery(deliveryDtos.Select(d => d.Id));
            foreach (var deliveryDto in deliveryDtos)
            {
                deliveryDto.MetricSnapshotCount = DaysOfHistoryBehind(snapshotCounts, deliveryDto.Id);
            }

            return [.. deliveryDtos.OrderBy(d => d.Date)];
        }

        /// <summary>
        /// A retired Delivery without a closure record is left out rather than shown with today's
        /// numbers. Retiring one always writes the record, so the only way here is a row that
        /// pre-dates this feature or one whose record was removed by hand - and in both cases a row
        /// of blanks says less than no row at all.
        /// </summary>
        private List<ArchivedDeliveryDto> ArchivedRows(List<Delivery> deliveries)
        {
            var deliveryIds = deliveries.Select(delivery => delivery.Id).ToList();
            var closureRecords = deliveryRepository.GetClosureRecordsByDelivery(deliveryIds);
            var snapshotCounts = deliveryMetricSnapshotRepository.GetSnapshotCountsByDelivery(deliveryIds);

            return [.. deliveries
                .Where(delivery => closureRecords.ContainsKey(delivery.Id))
                .Select(delivery => ArchivedDeliveryProjection.ToDto(
                    IdentityOf(delivery, DaysOfHistoryBehind(snapshotCounts, delivery.Id)),
                    closureRecords[delivery.Id]))
                .OrderByDescending(archivedDelivery => archivedDelivery.ArchivedOn)];
        }

        private static int DaysOfHistoryBehind(IReadOnlyDictionary<int, int> snapshotCounts, int deliveryId)
        {
            return snapshotCounts.TryGetValue(deliveryId, out var count) ? count : 0;
        }

        private static ArchivedDeliveryIdentity IdentityOf(Delivery delivery, int metricSnapshotCount)
        {
            return new ArchivedDeliveryIdentity(
                delivery.Id, delivery.Name, delivery.Date, delivery.PortfolioId, delivery.ConcurrencyToken, metricSnapshotCount);
        }

        [HttpGet("{deliveryId:int}/metrics-history")]
        [ProducesResponseType<DeliveryMetricsHistoryDto>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMetricsHistory(int deliveryId)
        {
            var delivery = deliveryRepository.GetById(deliveryId);
            if (delivery == null)
            {
                return NotFound();
            }

            if (!await rbacAdministrationService.CanSatisfyRequirementAsync(
                    User,
                    RbacGuardRequirement.PortfolioRead,
                    delivery.PortfolioId,
                    HttpContext?.RequestAborted ?? default))
            {
                return Forbid();
            }

            var snapshots = deliveryMetricSnapshotRepository.GetByDelivery(deliveryId);
            return Ok(DeliveryMetricsHistoryDto.From(delivery.Date, snapshots));
        }

        [HttpPost("portfolio/{portfolioId:int}")]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "portfolioId")]
        public async Task<IActionResult> CreateDelivery(
            int portfolioId,
            [FromBody] UpdateDeliveryRequest request)
        {
            var clientSuppliedFieldsError = VerifyClientSuppliedFields(request);
            if (clientSuppliedFieldsError != null)
            {
                return clientSuppliedFieldsError;
            }

            var deliveryRequestErrorCode = VerifyDeliveryRequest(portfolioId, request);
            if (deliveryRequestErrorCode != null)
            {
                return deliveryRequestErrorCode;
            }

            var portfolio = portfolioRepository.GetById(portfolioId);
            if (portfolio == null)
            {
                return NotFound($"Portfolio with ID {portfolioId} not found");
            }

            var (sourceError, sourcePreview) = await ResolveBoundSource(portfolio, request);
            if (sourceError != null)
            {
                return sourceError;
            }

            var delivery = NewDelivery(request, portfolioId, sourcePreview);

            var selectionError = ApplyFeatureSelection(request, delivery, sourcePreview);
            if (selectionError != null)
            {
                return selectionError;
            }

            deliveryRepository.Add(delivery);
            await deliveryRepository.Save();

            return Ok();
        }

        [HttpPut("{deliveryId:int}")]
        public async Task<IActionResult> UpdateDelivery(
            int deliveryId,
            [FromBody] UpdateDeliveryRequest request)
        {
            var existingDelivery = deliveryRepository.GetByIdForUpdate(deliveryId);
            if (existingDelivery == null)
            {
                return NotFound($"Delivery with ID {deliveryId} not found");
            }

            if (!await rbacAdministrationService.CanSatisfyRequirementAsync(
                    User,
                    RbacGuardRequirement.PortfolioWrite,
                    existingDelivery.PortfolioId,
                    HttpContext?.RequestAborted ?? default))
            {
                return Forbid();
            }

            var releasingFromASource = existingDelivery.SelectionMode == DeliverySelectionMode.SourceBound
                && request.SelectionMode != DeliverySelectionMode.SourceBound;

            var requestError = VerifyUpdateRequest(request, releasingFromASource);
            if (requestError != null)
            {
                return requestError;
            }

            var (sourceError, sourcePreview) = await ResolveSourceForUpdate(existingDelivery, request);
            if (sourceError != null)
            {
                return sourceError;
            }

            var selectionError = ApplyModeTransition(request, existingDelivery, sourcePreview, releasingFromASource);
            if (selectionError != null)
            {
                return selectionError;
            }

            if (request.ConcurrencyToken.HasValue)
            {
                deliveryRepository.ApplyConcurrencyTokenForEdit(existingDelivery, request.ConcurrencyToken.Value);
            }

            await deliveryRepository.Save();

            return Ok();
        }

        /// <summary>
        /// Which Release a Delivery follows is decided before anything the Delivery says is touched. A
        /// bound Delivery refuses every rename, reschedule and Feature write, so an update that edited
        /// first - including the one asking to stop following the Release - would be refused before the
        /// mode was ever read. Moving the release below back down under the edits breaks that silently:
        /// the tests that catch it are the ones that start from a bound Delivery.
        /// </summary>
        private IActionResult? ApplyModeTransition(
            UpdateDeliveryRequest request,
            Delivery delivery,
            PortfolioSourcePreview? sourcePreview,
            bool releasingFromASource)
        {
            if (delivery.SelectionMode == DeliverySelectionMode.SourceBound)
            {
                delivery.Unbind();
            }

            // Released from its Release, a Delivery keeps the name, the date and the Features the
            // Release last gave it. They are why somebody stops following one rather than deleting the
            // Delivery, and the payload carrying them is a rendering of what is already there.
            if (releasingFromASource && request.SelectionMode == DeliverySelectionMode.Manual)
            {
                return null;
            }

            if (!releasingFromASource && request.SelectionMode != DeliverySelectionMode.SourceBound)
            {
                delivery.Rename(request.Name);
                delivery.Reschedule(UtcDateOf(request));
            }

            return ApplyFeatureSelection(request, delivery, sourcePreview);
        }

        /// <summary>
        /// A Delivery on its way out of a Release has neither its name nor its date read from the
        /// payload, so neither is checked either. One released from a Release that shipped last quarter
        /// carries that past date on screen, and checking it would demand a future date before the
        /// Delivery could be released at all.
        /// </summary>
        private IActionResult? VerifyUpdateRequest(UpdateDeliveryRequest request, bool releasingFromASource)
        {
            // A mode nobody implements is refused before the Delivery is touched at all. Read later, the
            // refusal would already have released a bound Delivery from its Release and moved the version
            // an open browser is holding, on a request that is then answered with an error.
            if (!Enum.IsDefined(request.SelectionMode))
            {
                return BadRequest(NoSuchSelectionMode(request.SelectionMode));
            }

            if (releasingFromASource)
            {
                return CheckSelectionModePrerequisites(request);
            }

            return VerifyClientSuppliedFields(request) ?? CheckSelectionModePrerequisites(request);
        }

        private async Task<(IActionResult? Error, PortfolioSourcePreview? Preview)> ResolveSourceForUpdate(
            Delivery existingDelivery, UpdateDeliveryRequest request)
        {
            if (request.SelectionMode != DeliverySelectionMode.SourceBound)
            {
                return (null, null);
            }

            var portfolio = portfolioRepository.GetById(existingDelivery.PortfolioId);
            if (portfolio == null)
            {
                return (NotFound($"Portfolio with ID {existingDelivery.PortfolioId} not found"), null);
            }

            return await ResolveBoundSource(portfolio, request);
        }

        [HttpDelete("{deliveryId:int}")]
        public async Task<IActionResult> DeleteDelivery(int deliveryId)
        {
            // Archiving is the alternative to deleting, not protection from it: an archived Delivery is
            // deleted exactly as an active one is, and its pinned record and history go with it.
            var scopeCheck = await CheckScopeAsync(deliveryId, RbacGuardRequirement.PortfolioWrite);
            if (scopeCheck != null)
            {
                return scopeCheck;
            }

            deliveryRepository.Remove(deliveryId);
            await deliveryRepository.Save();

            return NoContent();
        }

        [HttpPost("{deliveryId:int}/archive")]
        public async Task<IActionResult> ArchiveDelivery(int deliveryId, [FromBody] ArchiveDeliveryRequest? request)
        {
            var scopeCheck = await CheckScopeAsync(deliveryId, RbacGuardRequirement.PortfolioWrite);
            if (scopeCheck != null)
            {
                return scopeCheck;
            }

            if (!licenseService.CanUsePremiumFeatures())
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Archiving a delivery requires a premium license");
            }

            var delivery = deliveryRepository.GetById(deliveryId);
            if (delivery == null)
            {
                return NotFound($"Delivery with ID {deliveryId} not found");
            }

            delivery.Archive(clock.TodayAsUtcMidnight);
            PinClosureRecord(delivery);

            ApplyClientToken(delivery, request);
            await deliveryRepository.Save();

            return Ok();
        }

        [HttpPost("{deliveryId:int}/unarchive")]
        public async Task<IActionResult> UnarchiveDelivery(int deliveryId, [FromBody] ArchiveDeliveryRequest? request)
        {
            var scopeCheck = await CheckScopeAsync(deliveryId, RbacGuardRequirement.PortfolioWrite);
            if (scopeCheck != null)
            {
                return scopeCheck;
            }

            var delivery = deliveryRepository.GetByIdForUpdate(deliveryId);
            if (delivery == null)
            {
                return NotFound($"Delivery with ID {deliveryId} not found");
            }

            // The pinned record stays where it is. A Delivery that comes back and closes again
            // overwrites it, and one that never closes again keeps a record nobody reads.
            delivery.Unarchive();

            ApplyClientToken(delivery, request);
            await deliveryRepository.Save();

            return Ok();
        }

        /// <summary>
        /// Computed here rather than looked up, so a Delivery created and closed on the same afternoon
        /// - one the daily recorder has never seen - still keeps a complete record of how it read.
        /// </summary>
        private void PinClosureRecord(Delivery delivery)
        {
            var values = deliveryMetricValuesProjector.Project(delivery, clock.Today);
            var closureRecord = deliveryRepository.GetOrCreateClosureRecord(delivery.Id);

            closureRecord.ArchivedOn = delivery.ArchivedOn!.Value;
            closureRecord.TargetDateAtClosure = values.TargetDate;
            closureRecord.TotalWork = values.TotalWork;
            closureRecord.DoneWork = values.DoneWork;
            closureRecord.RemainingWork = values.RemainingWork;
            closureRecord.EstimatedItemCount = values.EstimatedItemCount;
            closureRecord.LikelihoodPercentage = values.LikelihoodPercentage;
            closureRecord.WhenDistributionJson = values.WhenDistributionJson;
            closureRecord.FeatureBreakdownJson = values.FeatureBreakdownJson;
            closureRecord.HasSufficientData = values.HasSufficientData;
            closureRecord.TeamsWithoutForecastJson = values.TeamsWithoutForecastJson;
            closureRecord.SelectionMode = values.SelectionMode;
            closureRecord.RuleDefinitionJson = values.RuleDefinitionJson;
            closureRecord.RuleSchemaVersion = values.RuleSchemaVersion;
        }

        private void ApplyClientToken(Delivery delivery, ArchiveDeliveryRequest? request)
        {
            if (request?.ConcurrencyToken != null)
            {
                deliveryRepository.ApplyConcurrencyTokenForEdit(delivery, request.ConcurrencyToken.Value);
            }
        }

        /// <summary>
        /// A route that names only the Delivery carries neither a Portfolio nor a Team id, so the
        /// declarative guard has no scope to resolve and the check has to happen here instead.
        /// </summary>
        private async Task<ActionResult?> CheckScopeAsync(int deliveryId, RbacGuardRequirement requirement)
        {
            var portfolioId = deliveryRepository.GetPortfolioId(deliveryId);
            if (!portfolioId.HasValue)
            {
                return NotFound();
            }

            if (!await rbacAdministrationService.CanSatisfyRequirementAsync(
                    User,
                    requirement,
                    portfolioId.Value,
                    HttpContext?.RequestAborted ?? default))
            {
                return Forbid();
            }

            return null;
        }

        /// <summary>
        /// A Delivery that follows a Release elsewhere is told what it contains by the Release, so the
        /// name, the date and the Feature ids in the payload are not read at all on that path - not
        /// compared with the Release and not refused for differing. The client keeps sending them
        /// because one payload shape serves all three modes.
        /// </summary>
        private IActionResult? ApplyFeatureSelection(
            UpdateDeliveryRequest request, Delivery delivery, PortfolioSourcePreview? sourcePreview)
        {
            return request.SelectionMode switch
            {
                DeliverySelectionMode.RuleBased => CreateRuleBasedDelivery(request, delivery),
                DeliverySelectionMode.Manual => CreateManualFeatureSelectionDelivery(request, delivery),
                DeliverySelectionMode.SourceBound => BindDeliveryToSource(request, delivery, sourcePreview),
                // The enum converter accepts any number, so a caller can name a mode that does not
                // exist. That is a malformed request rather than something broken on our side.
                _ => BadRequest(NoSuchSelectionMode(request.SelectionMode)),
            };
        }

        private static string NoSuchSelectionMode(DeliverySelectionMode selectionMode)
        {
            return $"Delivery Mode {selectionMode} is not supported";
        }

        /// <summary>
        /// Everything the Delivery says now comes from the Release, so a rule it was choosing its
        /// Features by is dropped rather than carried along: left behind, it would be written into the
        /// Delivery's closure record as though it were still what picked the Features.
        /// </summary>
        private BadRequestObjectResult? BindDeliveryToSource(
            UpdateDeliveryRequest request, Delivery delivery, PortfolioSourcePreview? sourcePreview)
        {
            if (sourcePreview?.Resolution is not DeliverySourceResolution.Resolved resolved)
            {
                return BadRequest("A delivery can only follow a source that resolved");
            }

            delivery.SelectFeaturesByHand();
            delivery.BindToSource(request.SourceKey!, request.SourceReference!);

            // The same write the refresh makes, rather than three hand-writes that happen to set the
            // same fields. Binding read the source successfully, so the Delivery has heard from it -
            // recorded here, a Delivery bound today and not yet refreshed can say when it last heard
            // rather than claiming it never has.
            delivery.SyncFromSource(
                resolved.Snapshot.Name,
                resolved.Snapshot.Date,
                sourcePreview.TrackedFeatures,
                clock.TodayAsUtcMidnight);

            return null;
        }

        /// <summary>
        /// A remote that could not be asked is answered apart from a Release that is gone: reporting a
        /// network blip as a deleted Release sends somebody off to re-create a Delivery whose Release
        /// never moved.
        ///
        /// Whether the connection offers the named source at all is settled first, and separately. A
        /// connection that does not know the name - because it was mistyped, or because this kind of
        /// connection cannot read Releases in the first place - is a permanent state of affairs, and
        /// asking the remote about it either throws or comes back looking exactly like an outage.
        /// </summary>
        private async Task<(IActionResult? Error, PortfolioSourcePreview? Preview)> ResolveBoundSource(
            Portfolio portfolio, UpdateDeliveryRequest request)
        {
            if (request.SelectionMode != DeliverySelectionMode.SourceBound)
            {
                return (null, null);
            }

            if (!deliverySourceResolver.OffersSource(portfolio, request.SourceKey!))
            {
                return (NotFound($"Portfolio with ID {portfolio.Id} offers no delivery source called '{request.SourceKey}'"), null);
            }

            var previews = await deliverySourceResolver.ResolveForPortfolio(
                portfolio, request.SourceKey!, [request.SourceReference!]);

            if (!previews.TryGetValue(request.SourceReference!, out var preview))
            {
                return (StatusCode(StatusCodes.Status503ServiceUnavailable, SourceCouldNotBeAsked), null);
            }

            return preview.Resolution switch
            {
                DeliverySourceResolution.Resolved => (null, preview),
                DeliverySourceResolution.NotFound => (NotFound($"Source {request.SourceReference} does not exist"), null),
                DeliverySourceResolution.NoDate => (BadRequest($"Source {request.SourceReference} carries no date"), null),
                _ => (StatusCode(StatusCodes.Status503ServiceUnavailable, SourceCouldNotBeAsked), null),
            };
        }

        private BadRequestObjectResult? VerifyClientSuppliedFields(UpdateDeliveryRequest request)
        {
            if (request.SelectionMode == DeliverySelectionMode.SourceBound)
            {
                return string.IsNullOrEmpty(request.SourceKey) || string.IsNullOrEmpty(request.SourceReference)
                    ? BadRequest("A source-bound delivery must name the source it follows")
                    : null;
            }

            if (string.IsNullOrEmpty(request.Name))
            {
                return BadRequest("Name is required");
            }

            return DateOnly.FromDateTime(request.Date) <= clock.Today
                ? BadRequest("Delivery date must be in the future")
                : null;
        }

        private NotFoundObjectResult? CreateManualFeatureSelectionDelivery(UpdateDeliveryRequest request, Delivery delivery)
        {
            var featureList = deliveryRepository.GetFeaturesByIds(request.FeatureIds);

            var missingIds = request.FeatureIds
                .Except(featureList.Select(f => f.Id))
                .ToList();

            if (missingIds.Count != 0)
            {
                return NotFound($"Feature with ID {missingIds[0]} does not exist");
            }

            delivery.SelectFeaturesByHand();
            delivery.ReplaceFeatures(featureList);
            return null;
        }

        private IActionResult? CreateRuleBasedDelivery(UpdateDeliveryRequest request, Delivery delivery)
        {
            var mode = string.Equals(request.Mode, WorkItemRuleSet.ModeOr, StringComparison.OrdinalIgnoreCase)
                ? WorkItemRuleSet.ModeOr
                : WorkItemRuleSet.ModeAnd;
            var ruleSet = new WorkItemRuleSet
            {
                Version = WorkItemRuleSet.SchemaVersion,
                Mode = mode,
                Conditions = request.Rules!.Select(r => new WorkItemRuleCondition
                {
                    FieldKey = r.FieldKey,
                    Operator = r.Operator,
                    Value = r.Value
                }).ToList()
            };
            delivery.SelectFeaturesByRule(WorkItemRuleSetJson.Serialize(ruleSet), WorkItemRuleSet.SchemaVersion);

            var portfolioFeatures = GetFeaturesForPortfolio(delivery.PortfolioId);
            var matchingFeatures = deliveryRuleService.GetMatchingFeaturesForRuleset(ruleSet, portfolioFeatures);
            delivery.ReplaceFeatures(matchingFeatures);
            return null;
        }

        private List<Feature> GetFeaturesForPortfolio(int portfolioId)
        {
            var portfolio = portfolioRepository.GetById(portfolioId);

            return portfolio == null ? [] : portfolio.Features;
        }

        private IActionResult? VerifyDeliveryRequest(int portfolioId, UpdateDeliveryRequest request)
        {
            if (request.SelectionMode == DeliverySelectionMode.RuleBased)
            {
                return CheckRuleBasedDeliveryPrerequisites(request);
            }

            if (request.SelectionMode == DeliverySelectionMode.SourceBound)
            {
                return CheckSourceBoundDeliveryPrerequisites();
            }

            if (licenseService.CanUsePremiumFeatures())
            {
                return null;
            }

            var existingDeliveries = deliveryRepository.GetByPortfolioAsync(portfolioId);
            if (existingDeliveries.Any())
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Free users can only have 1 delivery per portfolio");
            }

            return null;
        }

        private IActionResult? CheckSelectionModePrerequisites(UpdateDeliveryRequest request)
        {
            if (request.SelectionMode == DeliverySelectionMode.RuleBased)
            {
                return CheckRuleBasedDeliveryPrerequisites(request);
            }

            return request.SelectionMode == DeliverySelectionMode.SourceBound
                ? CheckSourceBoundDeliveryPrerequisites()
                : null;
        }

        /// <summary>
        /// Following a Release is premium in its own right rather than by counting Deliveries: the
        /// free-tier cap counts what a Portfolio already holds, so it only bites on the second one and
        /// would hand every free-tier user their first bound Delivery for nothing. An update creates no
        /// Delivery at all, so on that path the cap would never have been consulted.
        /// </summary>
        private ObjectResult? CheckSourceBoundDeliveryPrerequisites()
        {
            return licenseService.CanUsePremiumFeatures()
                ? null
                : StatusCode(StatusCodes.Status403Forbidden, "Following a delivery source requires a premium license");
        }

        private IActionResult? CheckRuleBasedDeliveryPrerequisites(UpdateDeliveryRequest request)
        {
            if (!licenseService.CanUsePremiumFeatures())
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Rule-based delivery selection requires a premium license");
            }

            if (request.Rules == null || request.Rules.Count == 0)
            {
                return BadRequest("At least one rule condition is required for rule-based selection");
            }

            return null;
        }

        /// <summary>
        /// The Release the Delivery follows owns its name and its date, so a source-bound create reads
        /// neither from the payload. The two are deliberately not compared with what the client sent
        /// either: with no comparison there is no difference to refuse, which is what keeps a browser
        /// rendering the day one off from ever looking like somebody trying to edit the date.
        /// </summary>
        private static Delivery NewDelivery(UpdateDeliveryRequest request, int portfolioId, PortfolioSourcePreview? sourcePreview)
        {
            if (sourcePreview?.Resolution is DeliverySourceResolution.Resolved resolved)
            {
                return new Delivery(resolved.Snapshot.Name, resolved.Snapshot.Date, portfolioId);
            }

            return new Delivery(request.Name, UtcDateOf(request), portfolioId);
        }

        /// <summary>
        /// A date the browser left without a zone is taken as UTC rather than as the server's local
        /// day, which is the same reading the rest of the API gives one.
        /// </summary>
        private static DateTime UtcDateOf(UpdateDeliveryRequest request)
        {
            return request.Date.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.Date, DateTimeKind.Utc)
                : request.Date.ToUniversalTime();
        }

        private const string SourceCouldNotBeAsked = "The system holding the source could not be reached";

        private static DateTime ForecastWindowEnd(List<Delivery> deliveries, DateTime today)
        {
            const int CalendarHeadroomDays = 14;

            var latestDeliveryDate = deliveries.Count == 0
                ? today
                : deliveries.Max(delivery => delivery.Date.Date);

            var horizon = latestDeliveryDate > today ? latestDeliveryDate : today;

            return horizon.AddDays(CalendarHeadroomDays);
        }
    }
}