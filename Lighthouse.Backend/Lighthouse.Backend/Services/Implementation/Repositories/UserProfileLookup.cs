using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class UserProfileLookup(LighthouseAppContext context) : IUserProfileLookup
    {
        // UserProfile.Subject carries a unique index, so this is a single indexed row.
        public Task<UserProfile?> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
        {
            return context.UserProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(profile => profile.Subject == subject, cancellationToken);
        }
    }
}
