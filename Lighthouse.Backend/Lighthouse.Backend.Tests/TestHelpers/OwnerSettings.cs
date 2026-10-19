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

        public static Portfolio OwningDependencyField(this Portfolio portfolio, int? additionalFieldDefinitionId)
        {
            portfolio.OwnDependencyField(additionalFieldDefinitionId);
            return portfolio;
        }

        public static Portfolio OwningSizeEstimateField(this Portfolio portfolio, int? additionalFieldDefinitionId)
        {
            portfolio.OwnSizeEstimateField(additionalFieldDefinitionId);
            return portfolio;
        }

        public static Portfolio OwningFeatureOwnerField(this Portfolio portfolio, int? additionalFieldDefinitionId)
        {
            portfolio.OwnFeatureOwnerField(additionalFieldDefinitionId);
            return portfolio;
        }

        // Each of these owns the whole bundle, keeping the parts of the owner's own bundle it does not name.
        public static TOwner OwningEstimationField<TOwner>(this TOwner owner, int? additionalFieldDefinitionId)
            where TOwner : WorkTrackingSystemOptionsOwner
            => owner.OwningEstimation(new EstimationBundle(additionalFieldDefinitionId, owner.OwnEstimationUnit, owner.OwnUseNonNumericEstimation, owner.OwnEstimationCategoryValues));

        public static TOwner OwningEstimationUnit<TOwner>(this TOwner owner, string? unit)
            where TOwner : WorkTrackingSystemOptionsOwner
            => owner.OwningEstimation(new EstimationBundle(owner.OwnEstimationAdditionalFieldDefinitionId, unit, owner.OwnUseNonNumericEstimation, owner.OwnEstimationCategoryValues));

        public static TOwner OwningNonNumericEstimation<TOwner>(this TOwner owner, bool useNonNumericEstimation)
            where TOwner : WorkTrackingSystemOptionsOwner
            => owner.OwningEstimation(new EstimationBundle(owner.OwnEstimationAdditionalFieldDefinitionId, owner.OwnEstimationUnit, useNonNumericEstimation, owner.OwnEstimationCategoryValues));

        public static TOwner OwningEstimationCategories<TOwner>(this TOwner owner, List<string> categoryValues)
            where TOwner : WorkTrackingSystemOptionsOwner
            => owner.OwningEstimation(new EstimationBundle(owner.OwnEstimationAdditionalFieldDefinitionId, owner.OwnEstimationUnit, owner.OwnUseNonNumericEstimation, categoryValues));

        public static TOwner OwningEstimation<TOwner>(this TOwner owner, EstimationBundle bundle)
            where TOwner : WorkTrackingSystemOptionsOwner
        {
            owner.OwnEstimation(bundle);
            return owner;
        }
    }
}
