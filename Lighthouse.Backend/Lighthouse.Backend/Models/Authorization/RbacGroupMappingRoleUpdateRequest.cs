namespace Lighthouse.Backend.Models.Authorization
{
    public class RbacGroupMappingRoleUpdateRequest
    {
        public string Role { get; set; } = string.Empty;

        public Guid? ConcurrencyToken { get; set; }
    }
}
