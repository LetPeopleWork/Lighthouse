using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>The owner's mutators in a shape an object initializer can end with.</summary>
    public static class OwnerSettings
    {
        public static TOwner OwningParentOverride<TOwner>(this TOwner owner, int? additionalFieldDefinitionId)
            where TOwner : WorkTrackingSystemOptionsOwner
        {
            owner.OwnParentOverride(additionalFieldDefinitionId);
            return owner;
        }

        public static TOwner FollowingParentOverrideDefault<TOwner>(this TOwner owner)
            where TOwner : WorkTrackingSystemOptionsOwner
        {
            owner.FollowParentOverrideDefault();
            return owner;
        }
    }
}
