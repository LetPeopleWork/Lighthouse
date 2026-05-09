namespace Lighthouse.Backend.Models.Authorization
{
    public class RbacGroupMappingRequest
    {
        public string GroupValue { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string ScopeType { get; set; } = string.Empty;

        public int? ScopeId { get; set; }
    }
}
