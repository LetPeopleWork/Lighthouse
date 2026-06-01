using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    public record SurveyNudgeState(DateTimeOffset? NextEligibleAt);

    public class SurveyNudgeActionRequest
    {
        public SurveyNudgeAction Action { get; set; }
    }

    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [Authorize]
    public class SurveyNudgeController(IAppSettingService appSettingService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<SurveyNudgeState>(StatusCodes.Status200OK)]
        public ActionResult<SurveyNudgeState> GetState()
        {
            return Ok(new SurveyNudgeState(appSettingService.GetSurveyNudgeNextEligibleAt()));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RecordAction([FromBody] SurveyNudgeActionRequest request)
        {
            await appSettingService.RecordSurveyNudgeAction(request.Action);
            return NoContent();
        }
    }
}
