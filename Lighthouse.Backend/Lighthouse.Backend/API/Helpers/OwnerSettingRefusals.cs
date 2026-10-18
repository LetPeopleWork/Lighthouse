using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API.Helpers
{
    /// <summary>How a refusal names the kind of owner whose setting it refuses.</summary>
    public sealed record SettingsOwnerKind(string Code)
    {
        public static readonly SettingsOwnerKind Team = new("team");

        public static readonly SettingsOwnerKind Portfolio = new("portfolio");

        public string NounIn(OwnerTerms terms) => this == Team ? terms.Team : terms.Portfolio;
    }

    /// <summary>
    /// The refusals a Team's or Portfolio's settings that can follow a connection default can earn, whether the
    /// save creates the owner or changes it.
    /// </summary>
    public class OwnerSettingRefusals(IRepository<WorkTrackingSystemConnection> connectionRepository, ITerminologyService terminologyService)
    {
        private const string NothingWasSaved = "Nothing was saved.";

        public ObjectResult? RefusalOf(ControllerBase controller, SettingsOwnerKind kind, WorkTrackingSystemOptionsOwner? owner, SettingsOwnerDtoBase setting)
        {
            var sourced = SourcedSettingsSentFor(owner, setting, TheTermsThisInstanceUses());

            if (sourced.Find(each => each.Source is { } source && !Enum.IsDefined(source)) is { } unknown)
            {
                return controller.RefusedWithCode(
                    StatusCodes.Status400BadRequest,
                    $"{kind.Code}-setting-source-unknown",
                    title: $"The {NounFor(kind)} names a source this version of Lighthouse does not know.",
                    detail: $"The {unknown.Setting} source must be one of: {string.Join(", ", Enum.GetNames<SettingSource>())}. {NothingWasSaved}");
            }

            if (owner is not null && sourced.Find(each => NamesAFieldItsConnectionLacks(owner, setting, each.FieldOwnedOnceTaken())) is { } lacking)
            {
                return controller.RefusedWithCode(
                    StatusCodes.Status400BadRequest,
                    $"{kind.Code}-field-not-on-connection",
                    title: $"The {NounFor(kind)} names a field its connection does not have.",
                    detail: $"The {lacking.Setting} is not an Additional Field of this {NounFor(kind)}'s connection. {NothingWasSaved}");
            }

            return null;
        }

        private sealed record SourcedSetting(string Setting, SettingSource? Source, Func<int?> FieldOwnedOnceTaken);

        // What each owner would hold is worked out only once every sent source is known, since reading a
        // setting under a source this version does not know has no answer.
        private static List<SourcedSetting> SourcedSettingsSentFor(WorkTrackingSystemOptionsOwner? owner, SettingsOwnerDtoBase setting, OwnerTerms terms)
        {
            var sourced = new List<SourcedSetting>
            {
                new("Parent Override Field", setting.ParentOverrideSource,
                    () => owner?.ParentOverrideOwnedOnceTaken(setting.ParentOverrideSource, setting.ParentOverrideAdditionalFieldDefinitionId)),
            };

            if (setting is PortfolioSettingDto portfolioSetting)
            {
                var portfolio = owner as Portfolio;
                sourced.Add(new("Dependency Field", portfolioSetting.DependencyFieldSource,
                    () => portfolio?.DependencyFieldOwnedOnceTaken(portfolioSetting.DependencyFieldSource, portfolioSetting.DependencyOverrideAdditionalFieldDefinitionId)));
                sourced.Add(new("Size Estimate Field", portfolioSetting.SizeEstimateSource,
                    () => portfolio?.SizeEstimateFieldOwnedOnceTaken(portfolioSetting.SizeEstimateSource, portfolioSetting.SizeEstimateAdditionalFieldDefinitionId)));
                sourced.Add(new(ConnectionFieldDefaults.FeatureOwnerFieldIn(terms), portfolioSetting.FeatureOwnerSource,
                    () => portfolio?.FeatureOwnerFieldOwnedOnceTaken(portfolioSetting.FeatureOwnerSource, portfolioSetting.FeatureOwnerAdditionalFieldDefinitionId)));
            }

            return sourced;
        }

        private string NounFor(SettingsOwnerKind kind) => kind.NounIn(TheTermsThisInstanceUses());

        private OwnerTerms TheTermsThisInstanceUses() => OwnerTerms.From(terminologyService.GetAll());

        // Only a field the owner will hold as its own is judged. One sent along with following is not what the
        // owner reads, such as the old default's field in a copy read before the default moved. It is judged
        // against the connection the save leaves the owner on, which a save may also change.
        private bool NamesAFieldItsConnectionLacks(WorkTrackingSystemOptionsOwner owner, SettingsOwnerDtoBase setting, int? fieldOwnedOnceTaken)
            => fieldOwnedOnceTaken is { } field
                && TheConnectionTheSaveLeavesItOn(owner, setting) is { } connection
                && !connection.HoldsStoredField(field);

        private WorkTrackingSystemConnection? TheConnectionTheSaveLeavesItOn(WorkTrackingSystemOptionsOwner owner, SettingsOwnerDtoBase setting)
            => owner.WorkTrackingSystemConnection?.Id == setting.WorkTrackingSystemConnectionId
                ? owner.WorkTrackingSystemConnection
                : connectionRepository.GetById(setting.WorkTrackingSystemConnectionId);
    }
}
