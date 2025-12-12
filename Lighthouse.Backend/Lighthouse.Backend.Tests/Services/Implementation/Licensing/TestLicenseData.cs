namespace Lighthouse.Backend.Tests.Services.Implementation.Licensing
{
    public static class TestLicenseData
    {
        public static string InvalidLicense => File.ReadAllText("Services/Implementation/Licensing/invalid_license.json");
        
        public static string ValidExpiredLicense => File.ReadAllText("Services/Implementation/Licensing/valid_expired_license.json");
        
        public static string ValidLicense => File.ReadAllText("Services/Implementation/Licensing/valid_not_expired_license.json");
    }
}