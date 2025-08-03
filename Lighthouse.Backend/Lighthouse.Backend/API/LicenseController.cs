using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class LicenseController : ControllerBase
    {
        private readonly ILicenseService licenseService;

        public LicenseController(ILicenseService licenseService)
        {
            this.licenseService = licenseService;
        }

        [HttpGet]
        public IActionResult GetLicenseStatus()
        {
            var (licenseInfo, isValid) = licenseService.GetLicenseData();

            var licenseStatus = new LicenseStatusDto(licenseInfo, isValid);

            return Ok(licenseStatus);
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportLicense(IFormFile file)
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
                var content = await reader.ReadToEndAsync();

                var licenseInfo = licenseService.ImportLicense(content);

                if (licenseInfo == null)
                {
                    return BadRequest("Invalid license file");
                }

                return GetLicenseStatus();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing license file: {ex.Message}");
            }
        }
    }
}
