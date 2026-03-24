using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class OptionalFeaturesController(IRepository<OptionalFeature> repository, ILicenseService licenseService) : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<OptionalFeature>> GetAll()
        {
            var optionalFeatures = repository.GetAll();
            return Ok(optionalFeatures);
        }

        [HttpGet("{featureKey}")]
        public ActionResult<OptionalFeature> GetOptionalFeatureByKey(string featureKey)
        {
            var feature = repository.GetByPredicate(f => f.Key == featureKey);

            if (feature == null)
            {
                return NotFound();
            }

            return Ok(feature);
        }

        [HttpPost("{id}")]
        public async Task<ActionResult<OptionalFeature>> UpdateOptionalFeature(int id, [FromBody] OptionalFeature updatedFeature)
        {
            return await this.GetEntityByIdAnExecuteAction(repository, id, async feature =>
            {
                if (feature.IsPremium && !licenseService.CanUsePremiumFeatures())
                {
                    return feature;
                }
                
                feature.Enabled = updatedFeature.Enabled;
                repository.Update(feature);
                await repository.Save();

                return feature;
            });
        }
    }
}
