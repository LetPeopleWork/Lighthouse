using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces;
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
        IPortfolioRepository portfolioRepository,
        ILighthouseClock clock)
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
                    work.Status == UpdateProgress.Queued ? laneHolderName : null,
                    ElapsedOn(work)))
                .ToList();

            return Ok(tasks);
        }

        /// <summary>
        /// How long the work has been in the state it is in, measured here rather than in the browser. A
        /// reader's clock is wrong by whatever their machine is wrong by, so letting the browser subtract
        /// would give two people looking at one instance different answers to the same question.
        ///
        /// Running counts from when it started and waiting from when it was admitted, which is what lets
        /// one number read as "running for" or "queued for" without the caller knowing which moment it has.
        /// </summary>
        private long? ElapsedOn(UpdateStatus work)
        {
            var since = work.Status switch
            {
                UpdateProgress.InProgress => work.StartedAt,
                UpdateProgress.Queued => work.QueuedAt,

                // Work that has finished is neither running nor waiting, and it can still be on this list -
                // briefly as it ends, or for good if the replica that was running it died in that window.
                // Time since admission under a "Completed" label reads as time since it completed, which is a
                // different number and usually a much smaller one.
                _ => null,
            };

            if (since is null)
            {
                // Nothing recorded it - an entry admitted by a replica still on an older build, or one that
                // started before this instance began keeping the moment. There is no honest number, and any
                // stand-in is a duration a reader would believe.
                return null;
            }

            var elapsed = clock.Now - since.Value;

            // The moment is written by whichever replica handled the transition and read by whichever
            // answers this call, and their clocks do not agree to the millisecond. Something that started
            // fractionally in the future has just started.
            return elapsed < TimeSpan.Zero ? 0 : (long)elapsed.TotalMilliseconds;
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
            string? WaitingBehind,
            long? ElapsedMs);
    }
}
