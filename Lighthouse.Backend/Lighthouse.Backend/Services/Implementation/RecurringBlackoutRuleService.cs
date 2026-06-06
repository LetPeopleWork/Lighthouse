using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class RecurringBlackoutRuleService(IRepository<RecurringBlackoutRule> repository) : IRecurringBlackoutRuleService
    {
        private readonly IRepository<RecurringBlackoutRule> repository = repository ?? throw new ArgumentNullException(nameof(repository));

        public IEnumerable<RecurringBlackoutRuleDto> GetAll()
        {
            return repository.GetAll()
                .OrderBy(rule => rule.Start)
                .ThenBy(rule => rule.Id)
                .Select(rule => new RecurringBlackoutRuleDto(rule));
        }

        public async Task<RecurringBlackoutRuleDto> Create(RecurringBlackoutRuleDto dto)
        {
            Validate(dto);

            var rule = dto.ToEntity();

            repository.Add(rule);
            await repository.Save();

            return new RecurringBlackoutRuleDto(rule);
        }

        public async Task<RecurringBlackoutRuleDto> Update(int id, RecurringBlackoutRuleDto dto)
        {
            Validate(dto);

            var existing = repository.GetById(id)
                ?? throw new KeyNotFoundException($"Recurring blackout rule with id {id} not found.");

            existing.Weekdays = [.. dto.Weekdays];
            existing.IntervalWeeks = dto.IntervalWeeks;
            existing.Start = dto.Start;
            existing.End = dto.End;
            existing.Description = dto.Description;

            await repository.Save();

            return new RecurringBlackoutRuleDto(existing);
        }

        public async Task Delete(int id)
        {
            if (!repository.Exists(id))
            {
                throw new KeyNotFoundException($"Recurring blackout rule with id {id} not found.");
            }

            repository.Remove(id);
            await repository.Save();
        }

        private static void Validate(RecurringBlackoutRuleDto dto)
        {
            if (dto.Weekdays.Count == 0)
            {
                throw new ArgumentException("Select at least one weekday for the rule to repeat on.");
            }

            if (dto.IntervalWeeks < 1)
            {
                throw new ArgumentException("Repeat interval must be at least 1 week.");
            }

            if (dto.End is not null && dto.End < dto.Start)
            {
                throw new ArgumentException("End date must be on or after the start date.");
            }
        }
    }
}
