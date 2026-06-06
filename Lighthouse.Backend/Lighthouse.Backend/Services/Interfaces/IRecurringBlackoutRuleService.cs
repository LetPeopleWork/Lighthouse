using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IRecurringBlackoutRuleService
    {
        IEnumerable<RecurringBlackoutRuleDto> GetAll();

        Task<RecurringBlackoutRuleDto> Create(RecurringBlackoutRuleDto dto);
    }
}
