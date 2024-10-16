﻿using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.History;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class FeatureHistoryRepository : RepositoryBase<FeatureHistoryEntry>
    {
        public FeatureHistoryRepository(LighthouseAppContext context, ILogger<FeatureHistoryRepository> logger) : base(context, (context) => context.FeatureHistory, logger)
        {
        }

        public override IEnumerable<FeatureHistoryEntry> GetAll()
        {
            return Context.FeatureHistory
                .Include(f => f.FeatureWork)
                .Include(f => f.Forecasts).ThenInclude(f => f.SimulationResults);
        }

        public override IEnumerable<FeatureHistoryEntry> GetAllByPredicate(Func<FeatureHistoryEntry, bool> predicate)
        {
            return Context.FeatureHistory
                .Include(f => f.FeatureWork)
                .Include(f => f.Forecasts).ThenInclude(f => f.SimulationResults)
                .Where(predicate);
        }
    }
}
