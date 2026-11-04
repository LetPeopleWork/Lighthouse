using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Claims;

namespace Lighthouse.Backend.API.Helpers
{
    public static class RbacAdministrationServiceExtensions
    {
        // For a route everybody may reach, where a System Admin is only told more than the others. The
        // interface needs such a route before it can offer anything, so anything short of a confirmed
        // yes withholds, and nothing here fails the request: that would take the interface down to keep
        // back a few fields.
        //
        // Nobody is not an administrator. Asking about a caller who never signed in would be a question
        // about a user that does not exist, and these routes let anonymous callers through in
        // deployments that have no authentication at all.
        public static async Task<bool> ConfirmsSystemAdminAsync(
            this IRbacAdministrationService rbac,
            ClaimsPrincipal? caller,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            if (caller?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            try
            {
                return await rbac.CanSatisfyRequirementAsync(
                    caller, RbacGuardRequirement.SystemAdmin, cancellationToken: cancellationToken);
            }
            // Said out loud on the way past. Withholding is the safe answer to a question that could not
            // be asked, but it is the same answer a genuine fault in the permission check would produce,
            // and an authorisation bug that only ever shows up as a missing row is one nobody reports.
            catch (Exception couldNotBeAsked) when (couldNotBeAsked is DbException or DbUpdateException or InvalidOperationException)
            {
                // Stryker disable once String: that this is said at warning level is the behaviour; the
                // sentence is for whoever reads the log and is not something a test should freeze.
                logger.LogWarning(
                    couldNotBeAsked,
                    "Could not work out whether this caller administers the instance, so the answer was given without what only an administrator may see.");

                return false;
            }
        }
    }
}
