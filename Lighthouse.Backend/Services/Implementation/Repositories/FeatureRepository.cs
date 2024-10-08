﻿using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class FeatureRepository : RepositoryBase<Feature>
    {
        public FeatureRepository(LighthouseAppContext context, ILogger<FeatureRepository> logger) : base(context, (context) => context.Features, logger)
        {            
        }

        public override IEnumerable<Feature> GetAll()
        {
            var features = Context.Features
                .Include(f => f.Projects)
                    .ThenInclude(p => p.Milestones)
                .Include(f => f.FeatureWork).ThenInclude(rw => rw.Team)
                .Include(f => f.Forecasts).ThenInclude(f => f.SimulationResults)
                .ToList();

            return features.OrderBy(f => f, new FeatureComparer());
        }
    }
}
