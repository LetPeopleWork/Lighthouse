using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API.Helpers
{
    /// <summary>How a refusal names the kind of owner whose Parent Override it refuses.</summary>
    public sealed record ParentOverrideOwnerKind(string Code)
    {
        public static readonly ParentOverrideOwnerKind Team = new("team");

        public static readonly ParentOverrideOwnerKind Portfolio = new("portfolio");

        public string NounIn(OwnerTerms terms) => this == Team ? terms.Team : terms.Portfolio;
    }

    /// <summary>The refusals a Team's or Portfolio's Parent Override can earn, whether the save creates the owner or changes it.</summary>
    public class ParentOverrideRefusals(IRepository<WorkTrackingSystemConnection> connectionRepository, ITerminologyService terminologyService)
    {
        private const string NothingWasSaved = "Nothing was saved.";

        public ObjectResult? RefusalOf(ControllerBase controller, ParentOverrideOwnerKind kind, WorkTrackingSystemOptionsOwner? owner, SettingsOwnerDtoBase setting)
        {
            if (setting.ParentOverrideSource is { } source && !Enum.IsDefined(source))
            {
                return controller.RefusedWithCode(
                    StatusCodes.Status400BadRequest,
                    $"{kind.Code}-setting-source-unknown",
                    title: $"The {NounFor(kind)} names a source this version of Lighthouse does not know.",
                    detail: $"The Parent Override Field source must be one of: {string.Join(", ", Enum.GetNames<SettingSource>())}. {NothingWasSaved}");
            }

            if (owner is not null && NamesAFieldItsConnectionLacks(owner, setting))
            {
                return controller.RefusedWithCode(
                    StatusCodes.Status400BadRequest,
                    $"{kind.Code}-field-not-on-connection",
                    title: $"The {NounFor(kind)} names a field its connection does not have.",
                    detail: $"The Parent Override Field is not an Additional Field of this {NounFor(kind)}'s connection. {NothingWasSaved}");
            }

            return null;
        }

        private string NounFor(ParentOverrideOwnerKind kind) => kind.NounIn(OwnerTerms.From(terminologyService.GetAll()));

        // Only a field the owner will hold as its own is judged. One sent along with following is not what the
        // owner reads, such as the old default's field in a copy read before the default moved. It is judged
        // against the connection the save leaves the owner on, which a save may also change.
        private bool NamesAFieldItsConnectionLacks(WorkTrackingSystemOptionsOwner owner, SettingsOwnerDtoBase setting)
            => owner.ParentOverrideOwnedOnceTaken(setting.ParentOverrideSource, setting.ParentOverrideAdditionalFieldDefinitionId) is { } field
                && TheConnectionTheSaveLeavesItOn(owner, setting) is { } connection
                && !connection.HoldsStoredField(field);

        private WorkTrackingSystemConnection? TheConnectionTheSaveLeavesItOn(WorkTrackingSystemOptionsOwner owner, SettingsOwnerDtoBase setting)
            => owner.WorkTrackingSystemConnection?.Id == setting.WorkTrackingSystemConnectionId
                ? owner.WorkTrackingSystemConnection
                : connectionRepository.GetById(setting.WorkTrackingSystemConnectionId);
    }
}
