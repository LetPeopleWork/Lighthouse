using Lighthouse.Backend.Platform.Seeding;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Startup
{
    public sealed class OrganisationStructureMigrator : ISeeder
    {
        public Task Seed() => Task.CompletedTask;
    }
}
