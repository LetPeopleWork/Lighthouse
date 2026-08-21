﻿using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class PortfolioRepository(LighthouseAppContext context, ILogger<PortfolioRepository> logger)
        : RepositoryBase<Portfolio>(context, (context) => context.Portfolios, logger)
    {
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

        private IEnumerable<Portfolio> GetAllProjectsWithIncludes()
        {
            // Split queries are configured globally for every relational provider (DatabaseConfigurator), so S8733's Cartesian explosion cannot occur.
#pragma warning disable S8733
            return Context.Portfolios
                .Include(r => r.Features).ThenInclude(f => f.FeatureWork).ThenInclude(rw => rw.Team.WorkTrackingSystemConnection.Options)
                .Include(f => f.Features).ThenInclude(f => f.Forecasts).ThenInclude(f => f.SimulationResults)
                .Include(p => p.WorkTrackingSystemConnection.Options)
                .Include(p => p.WorkTrackingSystemConnection.AdditionalFieldDefinitions)
                .Include(p => p.WorkTrackingSystemConnection.WriteBackMappingDefinitions)
                .Include(p => p.Teams);
#pragma warning restore S8733
        }
    }
}
