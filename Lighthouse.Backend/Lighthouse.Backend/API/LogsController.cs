using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Lighthouse.Backend.API
{
    // The log file is instance-wide: every team and portfolio name, every work-tracking URL, every
    // connector error, whatever the current level captures. Until 2026-08-06 this controller carried
    // no guard at all and was admitted by the fallback policy, which asks only that the caller be
    // authenticated — so any account on the instance could download it, and after ADR-137 that set
    // includes every viewer who reaches the Jira frame.
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.SystemAdmin)]
    public class LogsController : ControllerBase
    {
        private readonly ILogConfiguration logConfiguration;
        private readonly ILogger<LogsController> logger;

        public LogsController(ILogConfiguration logConfiguration, ILogger<LogsController> logger)
        {
            this.logConfiguration = logConfiguration;
            this.logger = logger;
        }

        [HttpGet("level/supported")]
        public ActionResult<string[]> GetSupportedLogLevels()
        {
            return Ok(logConfiguration.SupportedLogLevels);
        }

        [HttpGet("level")]
        public ActionResult<string> GetLogLevel()
        {
            return Ok(logConfiguration.CurrentLogLevel);
        }

        [HttpPost("level")]
        public ActionResult SetLogLevel([FromBody] LogLevelDto logLevel)
        {
            try
            {
                logConfiguration.SetLogLevel(logLevel.Level);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error when setting log level to {Level}", logLevel.Level);
            }

            return Ok();
        }

        [HttpGet]
        public ActionResult<string> GetLogs()
        {
            var logs = logConfiguration.GetLogs();
            return Ok(logs);
        }

        [HttpGet("download")]
        public IActionResult DownloadLogs()
        {
            var logsContent = logConfiguration.GetLogs();
            var fileBytes = Encoding.UTF8.GetBytes(logsContent);
            var fileName = $"Lighthouse_Log_{DateTime.UtcNow:yyyy.MM.dd}.txt";
            return File(fileBytes, "text/plain", fileName);
        }

        public class LogLevelDto
        {
            public string Level { get; set; }
        }
    }
}