namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The OAuth token service keeps tokens for the connectors and lives in Modules/TrackerIntegration. The repository
    /// rules tell a repository from a platform service by namespace, so the token places under the module must count as the
    /// persistence platform, while a namespace that only shares their prefix must not.
    /// </summary>
    [TestFixture]
    public class TrackerIntegrationRulesSeeTheModuleArchUnitTest
    {
        [Test]
        public void TheOAuthTokenPlaceUnderTheModule_CountsAsThePersistencePlatform_WhileANamesakeDoesNot()
        {
            var tokenPlaces = new[]
            {
                "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth",
                "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.OAuth",
                "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.OAuth.Providers",
                "Lighthouse.Backend.Services.Implementation.OAuth",
                "Lighthouse.Backend.Services.Interfaces.OAuth",
            };
            var namesakes = new[]
            {
                "Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Application.OAuth",
                "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuthFixtures",
            };

            var recognised = WhenThePersistencePlatformIsRecognisedAmong(tokenPlaces.Concat(namesakes));

            ThenExactlyTheTokenPlacesArePersistencePlatform(recognised, tokenPlaces, namesakes);
        }

        private static List<string> WhenThePersistencePlatformIsRecognisedAmong(IEnumerable<string> namespaceNames)
            => [.. namespaceNames.Where(RepositoryRecognition.IsPlatformPersistence)];

        private static void ThenExactlyTheTokenPlacesArePersistencePlatform(List<string> recognised, string[] tokenPlaces, string[] namesakes)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var tokenPlace in tokenPlaces)
                {
                    Assert.That(
                        recognised,
                        Does.Contain(tokenPlace),
                        $"{tokenPlace} is not recognised as the persistence platform. Why it matters: the OAuth token service keeps tokens " +
                        "for the connectors and moves into Modules/TrackerIntegration; if it is no longer seen as the platform, the platform " +
                        "map loses it and the rules that tell a repository from a platform service read it as neither. How to fix: add " +
                        @"Modules\.TrackerIntegration\.(Application|Adapters)\.OAuth to PlatformPersistencePattern in RepositoryRecognition.");
                }

                foreach (var namesake in namesakes)
                {
                    Assert.That(
                        recognised,
                        Does.Not.Contain(namesake),
                        $"{namesake} is recognised as the persistence platform, but only shares a prefix with the OAuth token place. Why " +
                        "it matters: a pattern without its end anchor makes anything that starts alike part of the platform. How to fix: " +
                        @"end the OAuth alternative with ($|\..*), as the other alternatives do.");
                }
            }
        }
    }
}
