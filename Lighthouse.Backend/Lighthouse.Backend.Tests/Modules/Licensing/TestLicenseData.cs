namespace Lighthouse.Backend.Tests.Modules.Licensing
{
    public static class TestLicenseData
    {
        public static string InvalidLicense => File.ReadAllText("Modules/Licensing/invalid_license.json");
        
        public static string ValidExpiredLicense => File.ReadAllText("Modules/Licensing/valid_expired_license.json");
        
        public static string ValidLicense => File.ReadAllText("Modules/Licensing/valid_not_expired_license.json");
    }
}