namespace Lighthouse.Backend.Modules.Forecasting.Domain
{
    /// <summary>
    /// Hands a forecast the numbers it will draw from. Production starts every run from a fresh number, so
    /// each refresh stays an independent sample exactly as it always was; a test starts from a pinned one,
    /// which is the only way two runs can be compared number for number.
    /// </summary>
    public interface IDrawStreamFactory
    {
        IDrawStream ForOneRun();
    }
}
