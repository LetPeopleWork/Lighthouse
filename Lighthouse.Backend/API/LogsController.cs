using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly ILogConfiguration logConfiguration;

        public LogsController(ILogConfiguration logConfiguration)
        {
            this.logConfiguration = logConfiguration;
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
            logConfiguration.SetLogLevel(logLevel.Level);
            return Ok();
        }

        [HttpGet]
        public ActionResult<string> GetLogs()
        {
            var logs = logConfiguration.GetLogs();
            return Ok(logs);
        }

        public class LogLevelDto
        {
            public string Level { get; set; }
        }

    }
}
