using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeaturesController : ControllerBase
    {
        private readonly IRepository<Feature> featureRepository;
        private readonly IWorkItemRepository workItemRepository;

        public FeaturesController(IRepository<Feature> featureRepository, IWorkItemRepository workItemRepository)
        {
            this.featureRepository = featureRepository;
            this.workItemRepository = workItemRepository;
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
            return this.GetEntityByIdAnExecuteAction(featureRepository, featureId, feature =>
            {
                var items = workItemRepository.GetAllByPredicate(wi => wi.ParentReferenceId == feature.ReferenceId)
                    .Select(w => new WorkItemDto(w))
                    .ToList();

                return items;
            });
        }

        private List<FeatureDto> GetFeaturesByPredicate(Expression<Func<Feature, bool>> predicate)
        {
            var features = featureRepository.GetAllByPredicate(predicate).OrderBy(f => f, new FeatureComparer());

            return features.Select(f => new FeatureDto(f)).ToList();
        }
    }
}
