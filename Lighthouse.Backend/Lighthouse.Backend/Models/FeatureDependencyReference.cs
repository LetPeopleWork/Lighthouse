using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    // The reference is kept as the tracker's own id string rather than as a foreign key to the
    // Feature it names. A refresh can meet a link before it has imported the Feature on the other
    // end of it, and a foreign key cannot be written at all in that moment - the edge would be
    // dropped and never come back. A string is writable either way and resolves on the next read.
    public class FeatureDependencyReference : IEntity
    {
        public FeatureDependencyReference(int featureId, string referenceId, DependencySource source)
        {
            FeatureId = featureId;
            ReferenceId = referenceId;
            Source = source;
        }

        public int Id { get; }

        public int FeatureId { get; }

        public string ReferenceId { get; }

        public DependencySource Source { get; }
    }
}
