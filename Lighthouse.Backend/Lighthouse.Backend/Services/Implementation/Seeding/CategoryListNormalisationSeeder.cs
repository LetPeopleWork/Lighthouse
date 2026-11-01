using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;

namespace Lighthouse.Backend.Services.Implementation.Seeding
{
    // SCAFFOLD: true
    public class CategoryListNormalisationSeeder(
        IRepository<Team> teamRepository,
        IRepository<Portfolio> portfolioRepository,
        ILogger<CategoryListNormalisationSeeder> logger) : ISeeder
    {
        private readonly IRepository<Team> teams = teamRepository;

        private readonly IRepository<Portfolio> portfolios = portfolioRepository;

        private readonly ILogger<CategoryListNormalisationSeeder> log = logger;

        public Task Seed()
            => throw new InvalidOperationException($"Not yet implemented -- RED scaffold ({teams.GetType().Name}, {portfolios.GetType().Name}, {log.GetType().Name})");
    }
}
