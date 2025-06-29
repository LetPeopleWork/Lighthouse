using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("parent")]
        public ActionResult<List<FeatureDto>> GetParentFeaturesById([FromQuery] List<string> parentFeatureReferenceIds)
        {
            var parentFeatures = new List<FeatureDto>();

            var features = featureRepository.GetAllByPredicate(f => parentFeatureReferenceIds.Contains(f.ReferenceId)).ToList();

            foreach (var feature in features.OrderBy(f => f, new FeatureComparer()))
            {
                parentFeatures.Add(new FeatureDto(feature));
            }

            return Ok(parentFeatures);
        }
    }
}
