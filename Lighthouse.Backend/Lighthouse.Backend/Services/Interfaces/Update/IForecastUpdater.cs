namespace Lighthouse.Backend.Services.Interfaces.Update
{
    public interface IForecastUpdater : IUpdateService
    {
        /// <summary>
        /// Forecasts this portfolio now, without waiting for anything else. Deliberately a second way in
        /// rather than a flag on <see cref="IUpdateService.TriggerUpdate"/>: the waiting that
        /// <see cref="IUpdateService.TriggerUpdate"/> does exists because a bulk refresh otherwise makes a
        /// delivery date settle and then move, and a flag would let something reacting to that same bulk
        /// refresh switch the waiting off without anyone reading the call site noticing. This one is for a
        /// person who asked for a forecast and is watching for the answer.
        /// </summary>
        void TriggerImmediateUpdate(int id);
    }
}