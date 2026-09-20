using System.Text.Json;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// ASP.NET Core takes its listening addresses either from an address source - ASPNETCORE_URLS,
    /// the ASPNETCORE_*_PORTS variables, or a launch profile's applicationUrl - or from
    /// Kestrel:Endpoints. Declaring both is not rejected: Kestrel:Endpoints wins and the framework
    /// logs the address it discarded at Warning level. Every warning reaches the Task Manager, so a
    /// surface that declares an address twice shows up to users as a problem with their instance on
    /// every single start. That is how this was noticed.
    ///
    /// A surface can collide without anything in this repository saying so, which is what makes a
    /// guard worth having: the aspnet base image sets ASPNETCORE_HTTP_PORTS itself, and it stays in
    /// force in our image unless the Dockerfile clears it.
    ///
    /// The surfaces below are a closed list. render.yaml is knowingly absent from it: it still
    /// names an address the image's endpoints override, so that service binds the image's ports
    /// rather than the one the file asks for, and whether the deployment still exists is unsettled.
    /// Adding it here would turn that open question into a red suite.
    /// </summary>
    [TestFixture]
    [Category("story-6044-kestrel-address-override")]
    public class StartupAddressSourceCollisionTest
    {
        private const string DockerfilePath = "Dockerfile";
        private const string LaunchSettingsPath = "Lighthouse.Backend/Lighthouse.Backend/Properties/launchSettings.json";
        private const string DevelopmentSettingsPath = "Lighthouse.Backend/Lighthouse.Backend/appsettings.Development.json";
        private const string HelmConfigMapPath = "chart/templates/configmap.yaml";

        private const string KestrelEndpointPrefix = "Kestrel__Endpoints__";

        /// <summary>
        /// The one the base image sets on our behalf. Clearing it is the whole of the container-side
        /// fix, so the guard says which variable it means rather than checking a family.
        /// </summary>
        private const string PortVariableTheBaseImageSets = "ASPNETCORE_HTTP_PORTS";

        private static readonly string[] AddressVariables =
        [
            "ASPNETCORE_URLS",
            "ASPNETCORE_HTTP_PORTS",
            "ASPNETCORE_HTTPS_PORTS",
        ];

        /// <summary>
        /// Declaring the variable empty is not the same as leaving it out. Leaving it out keeps the
        /// base image's value, which is precisely the case that produces the warning.
        /// </summary>
        [Test]
        public void TheImage_LeavesNoInheritedAddressVariableInForce()
        {
            var dockerfile = ShippedFile(DockerfilePath);

            if (!dockerfile.Contains(KestrelEndpointPrefix, StringComparison.Ordinal))
            {
                Assert.Pass("The image no longer defines Kestrel endpoints, so there is no address source left for them to override.");
            }

            var inherited = EnvironmentValueIn(dockerfile, PortVariableTheBaseImageSets);
            var assignedElsewhere = AddressVariables
                .Where(variable => EnvironmentValueIn(dockerfile, variable) is { Length: > 0 })
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inherited, Is.Not.Null,
                    $"the Dockerfile defines Kestrel endpoints but never clears {PortVariableTheBaseImageSets}, "
                    + "which the aspnet base image sets. The endpoints win, and the framework reports the "
                    + "port it threw away as a startup Warning that the Task Manager shows every user as a "
                    + $"problem. Add ENV {PortVariableTheBaseImageSets}=\"\" beside the endpoint keys.");

                Assert.That(inherited, Is.Empty,
                    $"the Dockerfile sets {PortVariableTheBaseImageSets} to a value while also defining "
                    + "Kestrel endpoints. The endpoints win and that port never binds, so the value is "
                    + "discarded and announced as a startup Warning. Set the ports through the Kestrel "
                    + "endpoint keys, and leave this variable empty.");

                Assert.That(assignedElsewhere, Is.Empty,
                    "the Dockerfile gives an address variable a value while also defining Kestrel "
                    + $"endpoints, which override it: {string.Join(", ", assignedElsewhere)}. Whatever it "
                    + "asks for will not bind, and the mismatch is logged as a Warning on every start.");
            }
        }

        /// <summary>
        /// The development settings define both endpoints, so a profile naming the same ports again
        /// adds nothing and costs a warning on every dev run.
        /// </summary>
        [Test]
        public void NoLaunchProfile_NamesAnAddressTheDevelopmentEndpointsDiscard()
        {
            if (!DevelopmentSettingsDefineKestrelEndpoints())
            {
                Assert.Pass("appsettings.Development.json no longer defines Kestrel endpoints, so a launch profile's address is the only one and binds.");
            }

            var offenders = ProfilesNamingAnApplicationUrl();

            Assert.That(offenders, Is.Empty,
                "these launch profiles set applicationUrl while appsettings.Development.json defines "
                + $"Kestrel endpoints: {string.Join(", ", offenders)}. The endpoints win, so the profile's "
                + "address is discarded and reported as a Warning on every dev run. The ports are already "
                + "in the settings file; drop applicationUrl from the profile.");
        }

        /// <summary>
        /// The chart overrides the endpoint keys because the image binds privileged ports. It must
        /// not reintroduce the collision it inherits a fix for.
        /// </summary>
        [Test]
        public void TheHelmConfigMap_NamesNoAddressVariableBesideItsEndpointKeys()
        {
            var configMap = ShippedFile(HelmConfigMapPath);

            if (!configMap.Contains(KestrelEndpointPrefix, StringComparison.Ordinal))
            {
                Assert.Pass("The ConfigMap no longer overrides the Kestrel endpoints, so there is nothing for an address variable to collide with.");
            }

            var offenders = AddressVariables
                .Where(variable => configMap.Contains(variable, StringComparison.Ordinal))
                .ToList();

            Assert.That(offenders, Is.Empty,
                "the ConfigMap sets an address variable alongside the Kestrel endpoint keys it "
                + $"overrides: {string.Join(", ", offenders)}. The endpoint keys win, so the variable "
                + "cannot move the port and only buys a startup Warning in every pod.");
        }

        /// <summary>
        /// Null when the Dockerfile never mentions the variable - which leaves the base image's
        /// value standing - and the assigned text otherwise, unquoted and possibly empty.
        /// </summary>
        private static string? EnvironmentValueIn(string dockerfile, string variable)
        {
            var declaration = $"{variable}=";

            foreach (var line in dockerfile.Split('\n'))
            {
                var text = line.Trim();
                if (!text.StartsWith("ENV ", StringComparison.Ordinal))
                {
                    continue;
                }

                var assignment = text[4..].TrimStart();
                if (!assignment.StartsWith(declaration, StringComparison.Ordinal))
                {
                    continue;
                }

                return assignment[declaration.Length..].Trim().Trim('"').Trim('\'');
            }

            return null;
        }

        private static bool DevelopmentSettingsDefineKestrelEndpoints()
        {
            using var settings = JsonDocument.Parse(ShippedFile(DevelopmentSettingsPath));

            return settings.RootElement.TryGetProperty("Kestrel", out var kestrel)
                && kestrel.TryGetProperty("Endpoints", out var endpoints)
                && endpoints.EnumerateObject().Any();
        }

        private static List<string> ProfilesNamingAnApplicationUrl()
        {
            using var launchSettings = JsonDocument.Parse(ShippedFile(LaunchSettingsPath));

            if (!launchSettings.RootElement.TryGetProperty("profiles", out var profiles))
            {
                return [];
            }

            return [.. profiles.EnumerateObject()
                .Where(profile => profile.Value.TryGetProperty("applicationUrl", out _))
                .Select(profile => profile.Name)];
        }

        private static string ShippedFile(string relativePath)
        {
            var file = Path.Combine(
                RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.That(File.Exists(file), Is.True,
                $"{relativePath} was moved or deleted, so this guard is no longer reading the file it "
                + "was written about and would hold whatever replaced it.");

            return File.ReadAllText(file);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Lighthouse.sln could not be found to anchor the read of the shipped files.");

            return Directory.GetParent(directory!.FullName)!.FullName;
        }
    }
}
