using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class BlackoutPeriodService(IRepository<BlackoutPeriod> repository, IRepository<RecurringBlackoutRule> recurringRuleRepository, IDomainEventDispatcher domainEventDispatcher) : IBlackoutPeriodService
    {
        private readonly IRepository<BlackoutPeriod> repository = repository ?? throw new ArgumentNullException(nameof(repository));

        private readonly IRepository<RecurringBlackoutRule> recurringRuleRepository = recurringRuleRepository ?? throw new ArgumentNullException(nameof(recurringRuleRepository));

        private readonly IDomainEventDispatcher domainEventDispatcher = domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));

        public IEnumerable<BlackoutPeriod> GetAll()
        {
            return repository.GetAll()
                .OrderBy(bp => bp.Start)
                .ThenBy(bp => bp.End);
        }

        public IReadOnlyList<BlackoutPeriod> GetEffectiveBlackoutDays(DateTime windowStart, DateTime windowEnd)
        {
            var oneOffPeriods = repository.GetAll();

            var windowStartDay = DateOnly.FromDateTime(windowStart);
            var windowEndDay = DateOnly.FromDateTime(windowEnd);

            var materializedRecurringDays = recurringRuleRepository.GetAll()
                .SelectMany(rule => rule.ExpandToBlackoutDays(windowStartDay, windowEndDay));

            return oneOffPeriods.Concat(materializedRecurringDays).ToList();
        }

        public BlackoutPeriod? GetById(int id)
        {
            return repository.GetById(id);
        }

        public async Task<BlackoutPeriod> Create(BlackoutPeriodDto dto)
        {
            ValidateDateRange(dto.Start, dto.End);

            var blackoutPeriod = new BlackoutPeriod
            {
                Start = dto.Start,
                End = dto.End,
                Description = dto.Description,
            };

            repository.Add(blackoutPeriod);
            await repository.Save();

            await domainEventDispatcher.PublishAsync(new BlackoutConfigurationChanged());

            return blackoutPeriod;
        }

        public async Task<BlackoutPeriod> Update(int id, BlackoutPeriodDto dto)
        {
            ValidateDateRange(dto.Start, dto.End);

            var existing = repository.GetById(id)
                ?? throw new KeyNotFoundException($"Blackout period with id {id} not found.");

            existing.Start = dto.Start;
            existing.End = dto.End;
            existing.Description = dto.Description;

            await repository.Save();

            await domainEventDispatcher.PublishAsync(new BlackoutConfigurationChanged());

            return existing;
        }

        public async Task Delete(int id)
        {
            if (!repository.Exists(id))
            {
                throw new KeyNotFoundException($"Blackout period with id {id} not found.");
            }

            repository.Remove(id);
            await repository.Save();

            await domainEventDispatcher.PublishAsync(new BlackoutConfigurationChanged());
        }

        private static void ValidateDateRange(DateOnly start, DateOnly end)
        {
            if (start > end)
            {
                throw new ArgumentException("Start date must be on or before end date.");
            }
        }
    }
}
