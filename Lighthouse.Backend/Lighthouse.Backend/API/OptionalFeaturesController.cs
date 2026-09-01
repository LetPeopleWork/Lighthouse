using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    public class OptionalFeaturesController(
        IRepository<OptionalFeature> repository,
        ILicenseService licenseService,
        OptionalFeatureApplierRegistry applierRegistry) : ControllerBase
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

        [HttpPost("{featureKey}")]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<ActionResult<OptionalFeature>> UpdateOptionalFeature(string featureKey, [FromBody] OptionalFeature updatedFeature)
        {
            // By key, not by Id: the store keys these rows by their key, so nothing generates the number
            // and every row carries zero. Choosing a row by it matches all of them the moment there is
            // more than one.
            var feature = repository.GetByPredicate(f => f.Key == featureKey);

            if (feature == null)
            {
                return NotFound();
            }

            if (feature.IsPremium && !licenseService.CanUsePremiumFeatures())
            {
                // Deliberately the same words the licence attribute answers with. This check needs the row
                // being written, which an attribute cannot see, and a caller must not meet two different
                // refusals for the one setting.
                return StatusCode(StatusCodes.Status403Forbidden, "Access Denied: Premium Features Required");
            }

            await applierRegistry.ApplierFor(feature.Key).ApplyAsync(feature, updatedFeature.Enabled);

            return Ok(feature);
        }
    }
}
