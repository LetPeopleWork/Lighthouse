using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Logging;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Lighthouse.Backend.API
{
    // The log file is instance-wide: every team and portfolio name, every work-tracking URL, every
    // connector error, whatever the current level captures. Until 2026-08-06 this controller carried
    // no guard at all and was admitted by the fallback policy, which asks only that the caller be
    // authenticated — so any account on the instance could download it, and that set now includes
    // every viewer who reaches the embedded Jira frame.
    // S6960 counts the injected services and reads two of them as two jobs. They are one: this is the
    // instance's own record of what it has been doing, and what has gone wrong lately is the part of it an
    // operator reads first. Splitting it puts a second controller on the same material and gives the
    // System-Administrator guard a second place to drift away from this one.
#pragma warning disable S6960
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.SystemAdmin)]
    public class LogsController : ControllerBase
    {
        private readonly ILogConfiguration logConfiguration;
        private readonly IRecentProblemsReport recentProblems;
        private readonly ILogger<LogsController> logger;

        public LogsController(ILogConfiguration logConfiguration, IRecentProblemsReport recentProblems, ILogger<LogsController> logger)
        {
            this.logConfiguration = logConfiguration;
            this.recentProblems = recentProblems;
            this.logger = logger;
        }

        /// <summary>
        /// The same material the log file carries, filtered to what has gone wrong and answered as records
        /// rather than as text — so it lives behind the guard this controller already carries rather than
        /// behind a second one that would have to argue the case again.
        /// </summary>
        [HttpGet("problems")]
        public ActionResult<IReadOnlyList<RecentProblem>> GetRecentProblems()
        {
            return Ok(recentProblems.MostRecentFirst());
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

        /// <summary>
        /// The whole newest log file, or — when a tail is asked for — roughly that many bytes from the
        /// end of it. The tail is what makes following the log affordable: the file is re-read in full
        /// on every ask, and a Debug-level instance writes one far too large to re-send every few
        /// seconds. Download still takes the whole thing.
        /// </summary>
        [HttpGet]
        public ActionResult<string> GetLogs([FromQuery] int? tailBytes = null)
        {
            var logs = logConfiguration.GetLogs(BoundedTail(tailBytes));
            return Ok(logs);
        }

        /// <summary>
        /// How much of the end of the log one ask may read. An administrator account is still an
        /// account and a number off a query string is still input, so the size of the read is decided
        /// here rather than by the caller.
        /// </summary>
        private const int MaxTailBytes = 1024 * 1024;

        private static int? BoundedTail(int? tailBytes)
            => tailBytes is null ? null : Math.Clamp(tailBytes.Value, 1, MaxTailBytes);

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
#pragma warning restore S6960
}
