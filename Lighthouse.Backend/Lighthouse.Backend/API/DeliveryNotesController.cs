using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/deliveries/{deliveryId:int}/notes")]
    [Route("api/latest/deliveries/{deliveryId:int}/notes")]
    [ApiController]
    public class DeliveryNotesController(
        LighthouseAppContext context,
        IDeliveryRepository deliveryRepository,
        IRbacAdministrationService rbacAdministrationService,
        ICurrentUserProfileService currentUserProfileService,
        ILighthouseClock clock)
        : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeliveryNoteDto>>> GetNotes(int deliveryId)
        {
            var scopeCheck = await CheckScopeAsync(deliveryId, RbacGuardRequirement.PortfolioRead);
            if (scopeCheck is not null)
            {
                return scopeCheck;
            }

            var notes = await context.DeliveryNotes
                .Where(note => note.DeliveryId == deliveryId)
                .OrderByDescending(note => note.CreatedAt)
                .ThenByDescending(note => note.Id)
                .ToListAsync(HttpContext?.RequestAborted ?? default);

            var currentAuthor = await CurrentAuthorAsync();

            return Ok(notes.Select(note => ToDto(note, currentAuthor)).ToList());
        }

        [HttpPost]
        public async Task<ActionResult<DeliveryNoteDto>> AddNote(int deliveryId, [FromBody] DeliveryNoteRequest request)
        {
            var scopeCheck = await CheckScopeAsync(deliveryId, RbacGuardRequirement.PortfolioWrite);
            if (scopeCheck is not null)
            {
                return scopeCheck;
            }

            RefuseWhenArchived(deliveryId);

            var text = request?.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return BadRequest("A note needs some text.");
            }

            var author = await CurrentAuthorAsync();

            var note = new DeliveryNote
            {
                DeliveryId = deliveryId,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                AuthorUserProfileId = author?.Id,
                AuthorDisplayName = author?.DisplayName,
            };

            context.DeliveryNotes.Add(note);
            await context.SaveChangesAsync(HttpContext?.RequestAborted ?? default);

            return Ok(ToDto(note, author));
        }

        [HttpPut("{noteId:int}")]
        public async Task<ActionResult<DeliveryNoteDto>> UpdateNote(int deliveryId, int noteId, [FromBody] DeliveryNoteRequest request)
        {
            var scopeCheck = await CheckScopeAsync(deliveryId, RbacGuardRequirement.PortfolioWrite);
            if (scopeCheck is not null)
            {
                return scopeCheck;
            }

            RefuseWhenArchived(deliveryId);

            var note = await FindNoteAsync(deliveryId, noteId);
            if (note is null)
            {
                return NotFound();
            }

            var author = await CurrentAuthorAsync();
            if (!MayModify(note, author))
            {
                return Forbid();
            }

            var text = request?.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return BadRequest("A note needs some text.");
            }

            note.Text = text;
            note.LastEditedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(HttpContext?.RequestAborted ?? default);

            return Ok(ToDto(note, author));
        }

        [HttpDelete("{noteId:int}")]
        public async Task<IActionResult> DeleteNote(int deliveryId, int noteId)
        {
            var scopeCheck = await CheckScopeAsync(deliveryId, RbacGuardRequirement.PortfolioWrite);
            if (scopeCheck is not null)
            {
                return scopeCheck;
            }

            RefuseWhenArchived(deliveryId);

            var note = await FindNoteAsync(deliveryId, noteId);
            if (note is null)
            {
                return NotFound();
            }

            if (!MayModify(note, await CurrentAuthorAsync()))
            {
                return Forbid();
            }

            context.DeliveryNotes.Remove(note);
            await context.SaveChangesAsync(HttpContext?.RequestAborted ?? default);

            return NoContent();
        }

        /// <summary>
        /// Notes are what a Delivery's story is written in, and archiving one is the moment that story
        /// stops. Reading stays open; writing does not, and the refusal says the Delivery is closed
        /// rather than that the caller lacks rights, because those send the reader to two very
        /// different places.
        /// </summary>
        private void RefuseWhenArchived(int deliveryId)
        {
            if (deliveryRepository.IsArchived(deliveryId))
            {
                throw DeliveryArchivedException.CannotBeChanged(deliveryId);
            }
        }

        /// <summary>
        /// Deliberately two branches rather than comparing the two ids directly. Comparing them
        /// treats a note nobody signed as belonging to nobody, so on an instance that ran without
        /// authentication and later switched it on, every note written in between becomes permanently
        /// uneditable - visible to everyone and fixable by no one.
        /// </summary>
        private static bool MayModify(DeliveryNote note, UserProfile? currentAuthor)
        {
            if (note.AuthorUserProfileId is null)
            {
                // Nobody signed it, so nobody owns it. Write access to the Portfolio is the whole rule.
                return true;
            }

            return currentAuthor is not null && note.AuthorUserProfileId == currentAuthor.Id;
        }

        private Task<UserProfile?> CurrentAuthorAsync()
        {
            return currentUserProfileService.GetOrCreateFromPrincipalAsync(
                User, HttpContext?.RequestAborted ?? default);
        }

        private Task<DeliveryNote?> FindNoteAsync(int deliveryId, int noteId)
        {
            // Scoped by Delivery as well as id: a note reached through a Delivery it does not belong
            // to is either a mistake or an attempt, and neither should find anything.
            return context.DeliveryNotes
                .SingleOrDefaultAsync(
                    note => note.Id == noteId && note.DeliveryId == deliveryId,
                    HttpContext?.RequestAborted ?? default);
        }

        private DeliveryNoteDto ToDto(DeliveryNote note, UserProfile? currentAuthor)
        {
            return new DeliveryNoteDto(
                note,
                clock.ToInstanceDay(note.CreatedAt),
                note.LastEditedAt.HasValue ? clock.ToInstanceDay(note.LastEditedAt.Value) : null,
                MayModify(note, currentAuthor));
        }

        private async Task<ActionResult?> CheckScopeAsync(int deliveryId, RbacGuardRequirement requirement)
        {
            var portfolioId = deliveryRepository.GetPortfolioId(deliveryId);
            if (!portfolioId.HasValue)
            {
                return NotFound();
            }

            if (!await rbacAdministrationService.CanSatisfyRequirementAsync(
                    User,
                    requirement,
                    portfolioId.Value,
                    HttpContext?.RequestAborted ?? default))
            {
                return Forbid();
            }

            return null;
        }
    }

    public class DeliveryNoteRequest
    {
        public string Text { get; set; } = string.Empty;
    }
}
