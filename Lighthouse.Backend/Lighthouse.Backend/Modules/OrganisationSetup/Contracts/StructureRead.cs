namespace Lighthouse.Backend.Modules.OrganisationSetup.Contracts
{
    public abstract record StructureRead
    {
        private StructureRead()
        {
        }

        public sealed record NotMigrated : StructureRead;

        public sealed record Migrated(OrganisationStructureView Structure) : StructureRead;
    }
}
