using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API.Helpers
{
    /// <summary>The refusals a Team's Parent Override can earn, whether the save creates the Team or changes it.</summary>
    public static class TeamParentOverrideRefusals
    {
        private const string NothingWasSaved = "Nothing was saved.";

        public static ObjectResult? RefusalOfTheParentOverride(this ControllerBase controller, Team? team, TeamSettingDto teamSetting)
        {
            if (teamSetting.ParentOverrideSource is { } source && !Enum.IsDefined(source))
            {
                return controller.RefusedWithCode(
                    StatusCodes.Status400BadRequest,
                    "team-setting-source-unknown",
                    title: "The Team names a source this version of Lighthouse does not know.",
                    detail: $"The Parent Override Field source must be one of: {string.Join(", ", Enum.GetNames<SettingSource>())}. {NothingWasSaved}");
            }

            if (team?.NamesAFieldItsConnectionLacks(teamSetting) == true)
            {
                return controller.RefusedWithCode(
                    StatusCodes.Status400BadRequest,
                    "team-field-not-on-connection",
                    title: "The Team names a field its connection does not have.",
                    detail: $"The Parent Override Field is not an Additional Field of this Team's connection. {NothingWasSaved}");
            }

            return null;
        }
    }
}
