using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Modules.Access.Application;
using Lighthouse.Backend.Modules.Reports.Application;
using Lighthouse.Backend.Modules.Reports.Domain;
using Lighthouse.Backend.Platform.Host;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    public abstract class ReportNotesControllerBase(
        IReportNoteQueries queries,
        IReportNoteCommands commands,
        ICurrentUserProfileService profiles,
        IAuthModeResolver authModes,
        ILighthouseClock clock) : ControllerBase
    {
        protected IReportNoteQueries Queries { get; } = queries;

        protected IReportNoteCommands Commands { get; } = commands;

        protected ICurrentUserProfileService Profiles { get; } = profiles;

        protected IAuthModeResolver AuthModes { get; } = authModes;

        protected ILighthouseClock Clock { get; } = clock;

        public static ReportNoteCaller CallerOf(AuthMode mode, UserProfile? profile)
        {
            throw new NotImplementedException();
        }
    }
}
