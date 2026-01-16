using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
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