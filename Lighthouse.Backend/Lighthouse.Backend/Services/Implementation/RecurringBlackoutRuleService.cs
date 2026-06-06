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
            var rule = dto.ToEntity();

            repository.Add(rule);
            await repository.Save();

            return new RecurringBlackoutRuleDto(rule);
        }
    }
}
