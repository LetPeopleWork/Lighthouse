using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class WorkTrackingSystemConnectionDto
    {
        public WorkTrackingSystemConnectionDto()
        {
        }


        public WorkTrackingSystemConnectionDto(WorkTrackingSystemConnection workTrackingSystemConnection)
            : this(workTrackingSystemConnection, oAuthCredential: null, secretReader: null)
        {
        }

        public WorkTrackingSystemConnectionDto(
            WorkTrackingSystemConnection workTrackingSystemConnection,
            OAuthCredential? oAuthCredential)
            : this(workTrackingSystemConnection, oAuthCredential, secretReader: null)
        {
        }

        public WorkTrackingSystemConnectionDto(
            WorkTrackingSystemConnection workTrackingSystemConnection,
            OAuthCredential? oAuthCredential,
            ICryptoService? secretReader)
        {
            Id = workTrackingSystemConnection.Id;
            Name = workTrackingSystemConnection.Name;
            WorkTrackingSystem = workTrackingSystemConnection.WorkTrackingSystem;
            AuthenticationMethodKey = workTrackingSystemConnection.AuthenticationMethodKey;
            AuthenticationMethodDisplayName = AuthenticationMethodSchema.GetDisplayName(
                workTrackingSystemConnection.WorkTrackingSystem,
                workTrackingSystemConnection.AuthenticationMethodKey);
            AvailableAuthenticationMethods = AuthenticationMethodSchema
                .GetMethodsForSystem(workTrackingSystemConnection.WorkTrackingSystem)
                .Select(AuthenticationMethodDto.FromSchema)
                .ToList();
            Options.AddRange(workTrackingSystemConnection.Options.Select(o => new WorkTrackingSystemConnectionOptionDto(o, secretReader)));
            AdditionalFieldDefinitions.AddRange(
                workTrackingSystemConnection.AdditionalFieldDefinitions.Select(f => new AdditionalFieldDefinitionDto(f)));
            WriteBackMappingDefinitions.AddRange(
                workTrackingSystemConnection.WriteBackMappingDefinitions.Select(m => new WriteBackMappingDefinitionDto(m)));

            RequiresReconnect = oAuthCredential is
            {
                Status: OAuthCredentialStatus.RefreshFailed or OAuthCredentialStatus.Disconnected
            };

            ConcurrencyToken = workTrackingSystemConnection.ConcurrencyToken;
            Defaults = new ConnectionDefaultsDto(workTrackingSystemConnection);
            SupportedDefaults = [.. SupportedConnectionDefaults.For(workTrackingSystemConnection.WorkTrackingSystem).Select(NameOf)];
        }

        [JsonRequired]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        [JsonRequired]
        public WorkTrackingSystems WorkTrackingSystem { get; set; }

        [JsonRequired]
        public string AuthenticationMethodKey { get; set; } = string.Empty;

        public string AuthenticationMethodDisplayName { get; set; } = string.Empty;

        public List<AuthenticationMethodDto> AvailableAuthenticationMethods { get; set; } = [];

        public List<WorkTrackingSystemConnectionOptionDto> Options { get; set; } = [];

        public List<AdditionalFieldDefinitionDto> AdditionalFieldDefinitions { get; set; } = [];

        public List<WriteBackMappingDefinitionDto> WriteBackMappingDefinitions { get; set; } = [];

        public bool? RequiresReconnect { get; set; }

        public Guid? ConcurrencyToken { get; set; }

        // Nested so that a save leaving it out keeps every default, while a null inside it clears one.
        public ConnectionDefaultsDto? Defaults { get; set; }

        // The setter is private so a saved body cannot claim what its tracker offers.
        public List<string> SupportedDefaults { get; private set; } = [];

        public static string NameOf(ConnectionDefaultSetting setting) => JsonNamingPolicy.CamelCase.ConvertName(setting.ToString());

        // Only the list of connections says who uses each one. The setter is private so a saved body that
        // still carries the counts it was read with cannot bind them back.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ConnectionUsageDto? UsedBy { get; private set; }

        public WorkTrackingSystemConnectionDto WithUsage(ConnectionUsage usage, ConnectionFollowers followers, IEnumerable<FollowerGroup> followerGroups)
        {
            UsedBy = new ConnectionUsageDto(usage.Teams, usage.Portfolios, followers, [.. followerGroups.Select(FollowerGroupDto.From)]);
            return this;
        }
    }
}
