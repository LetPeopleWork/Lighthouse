using System.Globalization;

namespace Lighthouse.Backend.Tests
{
    /// <summary>
    /// Pins the test run to US English, the way lighthouse.runsettings pins the timezone.
    /// Numbers and dates in user-facing messages are formatted for whoever is reading them, so a
    /// machine set to Swiss German renders "16'384" where a test expects "16,384" and the suite
    /// fails on formatting alone. An environment variable cannot do this: .NET takes its culture
    /// from the operating system on Windows and ignores LANG and LC_ALL.
    /// </summary>
    [SetUpFixture]
    public class InstanceCulturePin
    {
        internal const string InstanceCultureName = "en-US";

        [OneTimeSetUp]
        public void PinCulture()
        {
            var culture = CultureInfo.GetCultureInfo(InstanceCultureName);

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
    }
}
