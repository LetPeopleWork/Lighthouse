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
            var itemToRemove = Context.Portfolios.Find(id);

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
            return Context.Portfolios
                .Include(r => r.Features).ThenInclude(f => f.FeatureWork).ThenInclude(rw => rw.Team).ThenInclude(t => t.WorkTrackingSystemConnection).ThenInclude(wtsc => wtsc.Options)
                .Include(f => f.Features).ThenInclude(f => f.Forecasts).ThenInclude(f => f.SimulationResults)
                .Include(p => p.WorkTrackingSystemConnection).ThenInclude(wtsc => wtsc.Options)
                .Include(p => p.WorkTrackingSystemConnection).ThenInclude(wtsc => wtsc.AdditionalFieldDefinitions)
                .Include(p => p.Teams);
        }
    }
}
