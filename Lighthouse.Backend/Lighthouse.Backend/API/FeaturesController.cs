using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [Authorize]
    public class FeaturesController : ControllerBase
    {
        private readonly IRepository<Feature> featureRepository;
        private readonly IWorkItemRepository workItemRepository;
        private readonly IRbacAdministrationService rbacAdministrationService;

        public FeaturesController(
            IRepository<Feature> featureRepository,
            IWorkItemRepository workItemRepository,
            IRbacAdministrationService rbacAdministrationService)
        {
            this.featureRepository = featureRepository;
            this.workItemRepository = workItemRepository;
            this.rbacAdministrationService = rbacAdministrationService;
        }

        [HttpGet("ids")]
        public ActionResult<List<FeatureDto>> GetFeatureDetailsById([FromQuery] List<int> featureIds)
        {
            if (featureIds.Count == 0)
            {
                return BadRequest();
            }

            var featureDetails = GetFeaturesByPredicate(f => featureIds.Contains(f.Id));

            return Ok(featureDetails);
        }

        [HttpGet("references")]
        public ActionResult<List<FeatureDto>> GetFeatureDetailsByReference([FromQuery] List<string> featureReferences)
        {
            if (featureReferences.Count == 0)
            {
                return BadRequest();
            }

            var featureDetails = GetFeaturesByPredicate(f => featureReferences.Contains(f.ReferenceId));

            return Ok(featureDetails);
        }

        [HttpGet("{featureId:int}/workitems")]
        public ActionResult<List<WorkItemDto>> GetFeatureWorkItems(int featureId)
        {
            var feature = featureRepository.GetById(featureId);
            if (feature is null)
            {
                return NotFound();
            }

            var readablePortfolioIdSet = GetReadablePortfolioIds(feature.Portfolios.Select(p => p.Id));
            if (feature.Portfolios.Any() && !feature.Portfolios.Any(p => readablePortfolioIdSet.Contains(p.Id)))
            {
                return NotFound();
            }

            var items = workItemRepository.GetAllByPredicate(wi => wi.ParentReferenceId == feature.ReferenceId)
                .Select(w => new WorkItemDto(w))
                .ToList();

            return Ok(items);
        }

        private List<FeatureDto> GetFeaturesByPredicate(Expression<Func<Feature, bool>> predicate)
        {
            var features = featureRepository.GetAllByPredicate(predicate).OrderBy(f => f, new FeatureComparer()).ToList();
            var readablePortfolioIdSet = GetReadablePortfolioIds(features.SelectMany(f => f.Portfolios).Select(p => p.Id));

            return features
                .Where(f => !f.Portfolios.Any() || f.Portfolios.Any(p => readablePortfolioIdSet.Contains(p.Id)))
                .Select(f => new FeatureDto(f, readablePortfolioIdSet))
                .ToList();
        }

        private HashSet<int> GetReadablePortfolioIds(IEnumerable<int> portfolioIds)
        {
            var requestedPortfolioIds = portfolioIds.Distinct().ToArray();
            return rbacAdministrationService
                .GetReadablePortfolioIdsAsync(User, requestedPortfolioIds, HttpContext?.RequestAborted ?? default)
                .GetAwaiter()
                .GetResult() is { } readablePortfolioIds
                    ? readablePortfolioIds.ToHashSet()
                    : requestedPortfolioIds.ToHashSet();
        }
    }
}
