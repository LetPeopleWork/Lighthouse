using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    // S6960 counts the injected services and reads three of them as three jobs. They are one: this is the
    // HTTP surface of the update queue, and the three routes are "is anything running", "what exactly", and
    // "stop that one" - a question an operator asks in that order, about one subsystem. Splitting them puts
    // two controllers on one route prefix and gives the System-Administrator guard two places to drift apart.
#pragma warning disable S6960
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [Authorize]
    public class UpdateController(
        IUpdateStatusStore updateStatusStore,
        IUpdateTaskNaming naming,
        ILighthouseClock clock,
        IUpdateQueueService updateQueueService)
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
            // What is running and what is waiting, and nothing else. A key whose replica died before it
            // could be removed stays in the store with a terminal status and nothing reaps it, so without
            // this the popover reports a finished refresh for the life of the deployment while the badge
            // beside it counts one - and /update/status, which has always filtered, calls the same instance
            // idle.
            var admitted = updateStatusStore.GetAdmittedWork()
                .Where(work => work.Status is UpdateProgress.Queued or UpdateProgress.InProgress)
                .ToList();

            // The queue runs one thing at a time, so whatever is running is what everything queued is
            // waiting for. That is the whole claim this field makes - not a position, not an estimate.
            var holdingTheLane = admitted.FirstOrDefault(work => work.Status == UpdateProgress.InProgress);
            var laneHolderName = holdingTheLane is null ? null : naming.NameOf(holdingTheLane);

            var tasks = admitted
                .OrderBy(InTheOrderTheQueueWillReachIt)
                .Select(work => new UpdateTaskResponse(
                    work.UpdateType,
                    work.Id,
                    naming.NameOf(work),
                    work.Status,
                    work.Status == UpdateProgress.Queued ? laneHolderName : null,
                    ElapsedOn(work)))
                .ToList();

            return Ok(tasks);
        }

        /// <summary>
        /// The popover draws these rows as a sequence, so the sequence is a claim about which one the queue
        /// reaches next - and the store has no order to give. In process it hands back its dictionary's
        /// buckets and under Redis its hash's, neither of which is an order and both of which move between
        /// reads, so an operator would see the refresh that is actually running listed below three that are
        /// only waiting for it.
        ///
        /// Running first, because that is what everything else is waiting for. Then longest-waiting first,
        /// because that is the one the queue reaches next. A row whose moment nobody recorded goes to the
        /// end of its own group rather than the end of the list: it still is what it says it is, and a
        /// refresh under way during a rolling upgrade must not drop below work that has not begun. Ties are
        /// settled on what the row is and which entity it names, which is the only thing about it that
        /// cannot change between two glances.
        /// </summary>
        private static (int Group, int MomentIsMissing, DateTimeOffset Moment, UpdateType UpdateType, int Id) InTheOrderTheQueueWillReachIt(UpdateStatus work)
        {
            var moment = WhenItsCurrentStateBegan(work);

            return (
                work.Status == UpdateProgress.InProgress ? 0 : 1,
                moment.HasValue ? 0 : 1,
                moment ?? DateTimeOffset.MinValue,
                work.UpdateType,
                work.Id);
        }

        /// <summary>
        /// The moment the work entered the state it is now in: when it started running, or when it was
        /// admitted to wait. How long it has been there and where it sits in the list are two answers about
        /// that same moment, so both read it from here.
        ///
        /// Work that has finished is neither running nor waiting, and it can still be on this list - briefly
        /// as it ends, or for good if the replica running it died in that window. Time since admission under
        /// a "Completed" label reads as time since it completed, which is a different and usually much
        /// smaller number.
        ///
        /// Absent is an ordinary answer rather than a fault. A replica still on an older build admits work
        /// without recording anything.
        /// </summary>
        private static DateTimeOffset? WhenItsCurrentStateBegan(UpdateStatus work) => work.Status switch
        {
            UpdateProgress.InProgress => work.StartedAt,
            UpdateProgress.Queued => work.QueuedAt,
            _ => null,
        };

        /// <summary>
        /// Stops a queued or running refresh. Guarded like the list it is reached from, and for the same
        /// reason: it acts on every entity on the instance.
        ///
        /// Accepted whatever state the work is in, including gone. The row an operator clicked was drawn
        /// before they clicked it, so "it finished while you were reading" is the ordinary case rather than
        /// an error, and answering one would put a failure in front of somebody who did nothing wrong.
        /// </summary>
        [HttpPost("tasks/{updateType}/{id:int}/cancel")]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> CancelTask(UpdateType updateType, int id)
        {
            // A delete is not a refresh. Stopping one half-way is not "the data is a bit stale" - the
            // caller that asked for it is waiting on the answer and would be told the entity had gone
            // while its row is still in the database. Refusing is the honest answer to a control that
            // offers to stop refreshing something.
            if (updateType is UpdateType.TeamDelete or UpdateType.PortfolioDelete)
            {
                return BadRequest("A deletion cannot be cancelled once it has been asked for.");
            }

            await updateQueueService.CancelAsync(new UpdateKey(updateType, id));
            return NoContent();
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
            var since = WhenItsCurrentStateBegan(work);

            if (since is null)
            {
                // Nothing recorded it, so there is no honest number and any stand-in is a duration a reader
                // would believe.
                return null;
            }

            var elapsed = clock.Now - since.Value;

            // The moment is written by whichever replica handled the transition and read by whichever
            // answers this call, and their clocks do not agree to the millisecond. Something that started
            // fractionally in the future has just started.
            return elapsed < TimeSpan.Zero ? 0 : (long)elapsed.TotalMilliseconds;
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
#pragma warning restore S6960
}
