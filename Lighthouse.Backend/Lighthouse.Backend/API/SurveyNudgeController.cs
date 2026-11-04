using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    public record SurveyNudgeState(DateTimeOffset? NextEligibleAt);

    public class SurveyNudgeActionRequest
    {
        public SurveyNudgeAction? Action { get; set; }
    }

    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [Authorize]
    public class SurveyNudgeController(IAppSettingService appSettingService) : ControllerBase
    {
        [SignedInOnly("Returns only the date from which the survey prompt may appear again, which is not specific to any Team or Portfolio.")]
        [HttpGet]
        [ProducesResponseType<SurveyNudgeState>(StatusCodes.Status200OK)]
        public ActionResult<SurveyNudgeState> GetState()
        {
            return Ok(new SurveyNudgeState(appSettingService.GetSurveyNudgeNextEligibleAt()));
        }

        [SignedInOnly("Anybody signed in may answer the survey prompt, and one answer snoozes it for everybody on the instance.")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RecordAction([FromBody] SurveyNudgeActionRequest request)
        {
            if (request.Action is null)
            {
                return BadRequest();
            }

            await appSettingService.RecordSurveyNudgeAction(request.Action.Value);
            return NoContent();
        }
    }
}
