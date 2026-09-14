using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [Authorize]
    public class UpdateController(
        IUpdateStatusStore updateStatusStore,
        IRepository<Team> teamRepository,
        IPortfolioRepository portfolioRepository)
        : ControllerBase
    {
        [HttpGet("status")]
        [ProducesResponseType(typeof(UpdateStatusResponse), StatusCodes.Status200OK)]
        public ActionResult<UpdateStatusResponse> GetUpdateStatus()
        {
            var activeUpdates = updateStatusStore.GetAdmittedWork()
                .Where(status => status.Status is UpdateProgress.Queued or UpdateProgress.InProgress)
                .ToList();

            var response = new UpdateStatusResponse(activeUpdates.Count > 0, activeUpdates.Count);

            return Ok(response);
        }

        /// <summary>
        /// What the instance is refreshing and what is waiting, for an operator who would otherwise have to
        /// read a log to find out. System-Administrator-guarded because it names every entity on the
        /// instance, which is the same reason the refresh history is.
        /// </summary>
        [HttpGet("tasks")]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        [ProducesResponseType(typeof(IEnumerable<UpdateTaskResponse>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<UpdateTaskResponse>> GetTasks()
        {
            var admitted = updateStatusStore.GetAdmittedWork();

            // The queue runs one thing at a time, so whatever is running is what everything queued is
            // waiting for. That is the whole claim this field makes - not a position, not an estimate.
            var holdingTheLane = admitted.FirstOrDefault(work => work.Status == UpdateProgress.InProgress);
            var laneHolderName = holdingTheLane is null ? null : NameOf(holdingTheLane);

            var tasks = admitted
                .Select(work => new UpdateTaskResponse(
                    work.UpdateType,
                    work.Id,
                    NameOf(work),
                    work.Status,
                    work.Status == UpdateProgress.Queued ? laneHolderName : null))
                .ToList();

            return Ok(tasks);
        }

        /// <summary>
        /// Resolved as the list is read rather than stored alongside the status, so a rename shows up
        /// immediately and the update path stays ignorant of anything a screen needs. An entity that has
        /// gone - deleted while its own refresh was in flight - still has a type and an id, and saying
        /// those is more use to an operator than a blank row.
        /// </summary>
        private string NameOf(UpdateStatus work)
        {
            var name = work.UpdateType switch
            {
                UpdateType.Team or UpdateType.TeamDelete => teamRepository.GetById(work.Id)?.Name,
                _ => portfolioRepository.GetById(work.Id)?.Name,
            };

            return string.IsNullOrWhiteSpace(name) ? $"{work.UpdateType} {work.Id}" : name;
        }

        public sealed record UpdateStatusResponse(bool HasActiveUpdates, int ActiveCount);

        public sealed record UpdateTaskResponse(
            UpdateType UpdateType,
            int Id,
            string Name,
            UpdateProgress Status,
            string? WaitingBehind);
    }
}
