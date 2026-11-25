using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application
{
    public interface IRecurringBlackoutRuleService
    {
        IEnumerable<RecurringBlackoutRuleDto> GetAll();

        Task<RecurringBlackoutRuleDto> Create(RecurringBlackoutRuleDto dto);

        Task<RecurringBlackoutRuleDto> Update(int id, RecurringBlackoutRuleDto dto);

        Task Delete(int id);
    }
}
