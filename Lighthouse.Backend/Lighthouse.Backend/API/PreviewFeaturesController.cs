using Lighthouse.Backend.Models.Preview;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreviewFeaturesController : ControllerBase
    {
        private readonly IRepository<PreviewFeature> repository;

        public PreviewFeaturesController(IRepository<PreviewFeature> repository)
        {
            this.repository = repository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<PreviewFeature>> GetAll()
        {
            var previewFeatures = repository.GetAll();
            return Ok(previewFeatures);
        }

        [HttpGet("{featureKey}")]
        public ActionResult<PreviewFeature> GetPreviewFeatureByKey(string featureKey)
        {
            var feature = repository.GetByPredicate(f => f.Key == featureKey);

            if (feature == null)
            {
                return NotFound();
            }

            return Ok(feature);
        }

        [HttpPost("{id}")]
        public async Task<ActionResult<PreviewFeature>> UpdatePreviewFeature(int id, [FromBody] PreviewFeature updatedFeature)
        {
            return await this.GetEntityByIdAnExecuteAction(repository, id, async feature =>
            {
                feature.Enabled = updatedFeature.Enabled;
                repository.Update(feature);
                await repository.Save();

                return feature;
            });
        }
    }
}
