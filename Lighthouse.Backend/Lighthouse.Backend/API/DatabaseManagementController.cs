using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/database-management")]
    [ApiController]
    public class DatabaseManagementController(IDatabaseManagementService databaseManagementService) : ControllerBase
    {
        private readonly IDatabaseManagementService databaseManagementService = databaseManagementService ?? throw new ArgumentNullException(nameof(databaseManagementService));

        [HttpGet("status")]
        public ActionResult<DatabaseCapabilityStatus> GetStatus()
        {
            var status = databaseManagementService.GetCapabilityStatus();
            return Ok(status);
        }

        [HttpPost("backup")]
        public async Task<IActionResult> CreateBackup([FromBody] BackupRequest request)
        {
            if (string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Password is required for backup.");
            }

            try
            {
                var status = await databaseManagementService.CreateBackup(request.Password);
                return Accepted(status);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("backup/{operationId}")]
        public IActionResult GetBackupArtifact(string operationId)
        {
            try
            {
                var stream = databaseManagementService.GetBackupArtifact(operationId);
                var fileName = $"Lighthouse_Backup_{DateTime.UtcNow:yyyy.MM.dd_HH.mm.ss}.zip";
                return File(stream, "application/zip", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("restore")]
        [RequestSizeLimit(512 * 1024 * 1024)]
        public async Task<IActionResult> RestoreBackup(IFormFile file, [FromForm] string password)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("A backup file is required.");
            }

            if (string.IsNullOrEmpty(password))
            {
                return BadRequest("Password is required for restore.");
            }

            try
            {
                var stream = file.OpenReadStream();
                var status = await databaseManagementService.RestoreBackup(stream, password);
                return Accepted(status);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("clear")]
        public async Task<IActionResult> ClearDatabase()
        {
            try
            {
                var status = await databaseManagementService.ClearDatabase();
                return Accepted(status);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("operations/{operationId}")]
        public ActionResult<DatabaseOperationStatus> GetOperationStatus(string operationId)
        {
            var status = databaseManagementService.GetOperationStatus(operationId);

            if (status == null)
            {
                return NotFound();
            }

            return Ok(status);
        }

        public sealed record BackupRequest(string Password);
    }
}
