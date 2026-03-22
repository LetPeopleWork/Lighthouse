using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IBlackoutPeriodService
    {
        IEnumerable<BlackoutPeriod> GetAll();

        BlackoutPeriod? GetById(int id);

        Task<BlackoutPeriod> Create(BlackoutPeriodDto dto);

        Task<BlackoutPeriod> Update(int id, BlackoutPeriodDto dto);

        Task Delete(int id);
    }
}
