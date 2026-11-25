using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application
{
    public interface IBlackoutPeriodService
    {
        IEnumerable<BlackoutPeriod> GetAll();

        IReadOnlyList<BlackoutPeriod> GetEffectiveBlackoutDays(DateTime windowStart, DateTime windowEnd);

        BlackoutPeriod? GetById(int id);

        Task<BlackoutPeriod> Create(BlackoutPeriodDto dto);

        Task<BlackoutPeriod> Update(int id, BlackoutPeriodDto dto);

        Task Delete(int id);
    }
}
