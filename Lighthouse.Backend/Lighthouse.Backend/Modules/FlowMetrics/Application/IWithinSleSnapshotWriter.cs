using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    /// <summary>
    /// The one writer of the Within SLE record. Each call works out one day for every trailing window at once.
    /// Recording today saves the day it works out; filling in a past day saves through
    /// <see cref="SaveFilledDay"/>, so a day another copy of the application stored first is dropped on its own.
    /// </summary>
    public interface IWithinSleSnapshotWriter
    {
        Task RecordToday(int ownerId, OwnerType ownerType, WithinSleFamilyReader family);

        void FillDayIfAbsent(int ownerId, OwnerType ownerType, DateOnly day, WithinSleFamilyReader family);

        Task SaveFilledDay();
    }
}
