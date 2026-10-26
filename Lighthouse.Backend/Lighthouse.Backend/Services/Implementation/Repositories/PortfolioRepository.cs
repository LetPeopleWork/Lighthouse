﻿using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class PortfolioRepository(LighthouseAppContext context, ILogger<PortfolioRepository> logger)
        : RepositoryBase<Portfolio>(context, (context) => context.Portfolios, logger), IPortfolioRepository
    {
        private const string ForgottenTokenPrefix = "forgotten:";

        private const string RunningMarkPrefix = "computing:";

        // The stored value has to still be the one this run read, or a forget landed in between and must win.
        // EF compares a null read as IS NULL, so a first record only succeeds while nothing is recorded yet.
        public async Task<bool> TryRecordForecastInputs(int portfolioId, string? readAtGather, string fresh, CancellationToken cancellationToken)
        {
            var recorded = await Context.Portfolios
                .Where(portfolio => portfolio.Id == portfolioId && portfolio.ForecastInputFingerprint == readAtGather)
                .ExecuteUpdateAsync(setters => setters.SetProperty(portfolio => portfolio.ForecastInputFingerprint, fresh), cancellationToken);

            return recorded == 1;
        }

        public async Task<string?> TryMarkForecastRunning(int portfolioId, string? readAtGather, CancellationToken cancellationToken)
        {
            var mark = RunningMarkPrefix + Guid.NewGuid().ToString("N");

            return await TryRecordForecastInputs(portfolioId, readAtGather, mark, cancellationToken) ? mark : null;
        }

        public Task ForgetForecastInputs(IReadOnlyCollection<int> portfolioIds, CancellationToken cancellationToken)
        {
            return Forget(Context.Portfolios.Where(portfolio => portfolioIds.Contains(portfolio.Id)), cancellationToken);
        }

        public Task ForgetAllForecastInputs(CancellationToken cancellationToken)
        {
            return Forget(Context.Portfolios, cancellationToken);
        }

        // A fresh token rather than null: a forecast that read an earlier forget, or read nothing at all, would
        // otherwise still find the value it read and record over this forget as though nobody had asked.
        private static Task<int> Forget(IQueryable<Portfolio> portfolios, CancellationToken cancellationToken)
        {
            var token = ForgottenTokenPrefix + Guid.NewGuid().ToString("N");

            return portfolios
                .ExecuteUpdateAsync(setters => setters.SetProperty(portfolio => portfolio.ForecastInputFingerprint, token), cancellationToken);
        }

        public IReadOnlyCollection<int> GetPortfolioIdsForTeam(int teamId)
        {
            return GetPortfolioIdsForTeams([teamId]);
        }

        public IReadOnlyCollection<int> GetPortfolioIdsForTeams(IReadOnlyCollection<int> teamIds)
        {
            return Context.Features
                .Where(feature => feature.FeatureWork.Any(work => teamIds.Contains(work.TeamId)))
                .SelectMany(feature => feature.Portfolios)
                .Select(portfolio => portfolio.Id)
                .Distinct()
                .ToList();
        }

        public override IEnumerable<Portfolio> GetAll()
        {
            return GetAllProjectsWithIncludes()
                .ToList();
        }

        public override Portfolio? GetById(int id)
        {
            logger.LogDebug("Get Project by Id. Id: {Id}", id);

            return GetAllProjectsWithIncludes()
                .SingleOrDefault(x => x.Id == id);
        }

        public override void Remove(int id)
        {
            logger.LogInformation("Removing Project with {Id}", id);
            var itemToRemove = Context.Portfolios
                .Include(p => p.Features)
                    .ThenInclude(f => f.Portfolios)
                .SingleOrDefault(p => p.Id == id);

            if (itemToRemove != null)
            {
                RemoveOrphanedFeatures(id, itemToRemove);

                Context.Portfolios.Remove(itemToRemove);
            }
        }

        private void RemoveOrphanedFeatures(int id, Portfolio? itemToRemove)
        {
            var orphanedFeatures = new List<Feature>();
            foreach (var feature in itemToRemove.Features)
            {
                feature.Portfolios.Remove(itemToRemove);
                if (feature.Portfolios.Count == 0)
                {
                    logger.LogInformation("Feature {Feature} ({Id}) is not related to any portfolio - removing.", feature.Name, id);
                    orphanedFeatures.Add(feature);
                }
            }

            Context.Features.RemoveRange(orphanedFeatures);
        }

        private IQueryable<Portfolio> GetAllProjectsWithIncludes()
        {
            // Split queries are configured globally for every relational provider (DatabaseConfigurator), so S8733's Cartesian explosion cannot occur.
#pragma warning disable S8733
            return Context.Portfolios
                .Include(r => r.Features).ThenInclude(f => f.FeatureWork).ThenInclude(rw => rw.Team.WorkTrackingSystemConnection.Options)
                .Include(f => f.Features).ThenInclude(f => f.Forecasts).ThenInclude(f => f.SimulationResults)
                .Include(p => p.WorkTrackingSystemConnection.Options)
                .Include(p => p.WorkTrackingSystemConnection.AdditionalFieldDefinitions)
                .Include(p => p.WorkTrackingSystemConnection.WriteBackMappingDefinitions);
#pragma warning restore S8733
        }
    }
}
