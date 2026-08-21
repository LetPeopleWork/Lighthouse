using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
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

            return Ok(notes.Select(ToDto).ToList());
        }

        [HttpPost]
        public async Task<ActionResult<DeliveryNoteDto>> AddNote(int deliveryId, [FromBody] DeliveryNoteRequest request)
        {
            var scopeCheck = await CheckScopeAsync(deliveryId, RbacGuardRequirement.PortfolioWrite);
            if (scopeCheck is not null)
            {
                return scopeCheck;
            }

            var text = request?.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return BadRequest("A note needs some text.");
            }

            var author = await currentUserProfileService.GetOrCreateFromPrincipalAsync(
                User, HttpContext?.RequestAborted ?? default);

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

            return Ok(ToDto(note));
        }

        private DeliveryNoteDto ToDto(DeliveryNote note)
        {
            return new DeliveryNoteDto(
                note,
                clock.ToInstanceDay(note.CreatedAt),
                note.LastEditedAt.HasValue ? clock.ToInstanceDay(note.LastEditedAt.Value) : null);
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
