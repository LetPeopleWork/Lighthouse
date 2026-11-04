using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [Authorize]
    public class LicenseController : ControllerBase
    {
        private readonly ILicenseService licenseService;
        private readonly IRbacAdministrationService rbac;
        private readonly ILogger<LicenseController> logger;

        public LicenseController(
            ILicenseService licenseService,
            IRbacAdministrationService rbac,
            ILogger<LicenseController> logger)
        {
            this.licenseService = licenseService;
            this.rbac = rbac;
            this.logger = logger;
        }

        // Asks for a System Admin and not for the bootstrap rule the import route accepts: while an
        // instance has no System Admin yet, that rule lets everybody through, and so would these details.
        [HttpGet]
        [ProducesResponseType<LicenseStatusDto>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLicenseStatus(CancellationToken cancellationToken = default)
        {
            var (licenseInfo, isValid) = licenseService.GetLicenseData();
            var canUsePremiumFeatures = licenseService.CanUsePremiumFeatures();

            var licenseStatus = new LicenseStatusDto(licenseInfo, isValid, canUsePremiumFeatures);

            return Ok(await rbac.ConfirmsSystemAdminAsync(User, logger, cancellationToken)
                ? licenseStatus
                : licenseStatus.WithoutWhatOnlyAnAdministratorMaySee());
        }

        [HttpPost("import")]
        [RbacGuard(RbacGuardRequirement.SystemAdminOrBootstrap)]
        public async Task<IActionResult> ImportLicense(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided");
            }

            if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("File must be a JSON file");
            }

            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var content = await reader.ReadToEndAsync(cancellationToken);

                var licenseInfo = await licenseService.ImportLicense(content);

                if (licenseInfo == null)
                {
                    return BadRequest("Invalid license file");
                }

                return await GetLicenseStatus(cancellationToken);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing license file: {ex.Message}");
            }
        }

        [HttpDelete]
        [RbacGuard(RbacGuardRequirement.SystemAdminOrBootstrap)]
        public async Task<IActionResult> ClearLicense()
        {
            try
            {
                await licenseService.ClearLicense();
                return Ok(new { message = "License cleared successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error clearing license: {ex.Message}");
            }
        }
    }
}
