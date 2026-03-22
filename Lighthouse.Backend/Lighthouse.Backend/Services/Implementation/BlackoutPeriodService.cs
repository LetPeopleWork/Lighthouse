using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class BlackoutPeriodService(IRepository<BlackoutPeriod> repository) : IBlackoutPeriodService
    {
        private readonly IRepository<BlackoutPeriod> repository = repository ?? throw new ArgumentNullException(nameof(repository));

        public IEnumerable<BlackoutPeriod> GetAll()
        {
            return repository.GetAll()
                .OrderBy(bp => bp.Start)
                .ThenBy(bp => bp.End);
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
