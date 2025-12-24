using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateController(ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses)
        : ControllerBase
    {
        [HttpGet("status")]
        [ProducesResponseType(typeof(UpdateStatusResponse), StatusCodes.Status200OK)]
        public ActionResult<UpdateStatusResponse> GetUpdateStatus()
        {
            var activeUpdates = updateStatuses.Values
                .Where(status => status.Status is UpdateProgress.Queued or UpdateProgress.InProgress)
                .ToList();

            var response = new UpdateStatusResponse(activeUpdates.Any(), activeUpdates.Count);

            return Ok(response);
        }

        public sealed record UpdateStatusResponse(bool HasActiveUpdates, int ActiveCount);
    }
}