using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Framework;
using System.Linq.Expressions;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeaturesController : ControllerBase
    {
        private readonly IRepository<Feature> featureRepository;

        public FeaturesController(IRepository<Feature> featureRepository)
        {
            this.featureRepository = featureRepository;
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

        private List<FeatureDto> GetFeaturesByPredicate(Expression<Func<Feature, bool>> predicate)
        {
            var featureDtos = new List<FeatureDto>();

            var features = featureRepository.GetAllByPredicate(predicate).ToList().OrderBy(f => f, new FeatureComparer());

            return features.Select(f => new FeatureDto(f)).ToList();
        }
    }
}
