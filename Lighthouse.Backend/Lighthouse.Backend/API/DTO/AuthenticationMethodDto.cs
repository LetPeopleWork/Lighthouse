using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.API.DTO
{
    public class AuthenticationMethodDto
    {
        public required string Key { get; init; }
        
        public required string DisplayName { get; init; }

        public required List<AuthenticationMethodOptionDto> Options { get; init; }

        public static AuthenticationMethodDto FromSchema(AuthenticationMethod method)
        {
            return new AuthenticationMethodDto
            {
                Key = method.Key,
                DisplayName = method.DisplayName,
                Options = method.Options.Select(AuthenticationMethodOptionDto.FromSchema).ToList()
            };
        }
    }

    public class AuthenticationMethodOptionDto
    {
        public required string Key { get; init; }
        
        public required string DisplayName { get; init; }
        
        public required bool IsSecret { get; init; }
        
        public bool IsOptional { get; init; }

        public static AuthenticationMethodOptionDto FromSchema(AuthenticationMethodOption option)
        {
            return new AuthenticationMethodOptionDto
            {
                Key = option.Key,
                DisplayName = option.DisplayName,
                IsSecret = option.IsSecret,
                IsOptional = option.IsOptional
            };
        }
    }
}
