namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    public static class RealityCheckVerdictPolicy
    {
        public static bool Held(int actualCompleted, int forecastValue) => actualCompleted >= forecastValue;
    }
}
