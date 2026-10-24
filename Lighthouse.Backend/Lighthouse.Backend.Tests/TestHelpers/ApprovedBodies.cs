using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Holds a response body against the copy approved when it was first recorded, so a change that is
    /// meant to make a read cheaper can be shown to leave every byte a client parses where it was.
    ///
    /// The approved copies live next to the tests, under source control. When a body differs, or no copy has
    /// been approved yet, the body that came back is written beside it as <c>*.received.json</c> and the test
    /// fails; approving a change is renaming that file over the approved one, in a commit a reviewer sees.
    /// </summary>
    public static class ApprovedBodies
    {
        private const string ReceivedSuffix = ".received.json";

        private const string ApprovedSuffix = ".approved.json";

        private const string AValueThatVaries = "<differs on every run>";

        private static readonly JsonSerializerOptions Indented = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <param name="propertiesThatVary">
        /// Properties whose value is fresh on every run by design (a concurrency token, a generated key). Their
        /// presence is still held; only the value is set aside.
        /// </param>
        public static void Verify(string body, string folderUnderTheTestProject, string name, params string[] propertiesThatVary)
        {
            var folder = Path.Combine(TestProjectDirectory(), folderUnderTheTestProject.Replace('/', Path.DirectorySeparatorChar));
            var approvedPath = Path.Combine(folder, name + ApprovedSuffix);
            var receivedPath = Path.Combine(folder, name + ReceivedSuffix);
            var received = Normalised(body, propertiesThatVary);

            var approved = File.Exists(approvedPath) ? Normalised(File.ReadAllText(approvedPath), propertiesThatVary) : null;

            if (string.Equals(approved, received, StringComparison.Ordinal))
            {
                File.Delete(receivedPath);
                return;
            }

            Directory.CreateDirectory(folder);
            File.WriteAllText(receivedPath, received);

            Assert.Fail(approved is null
                ? $"No approved body for '{name}' yet. The body that came back is in {receivedPath}; read it, and rename it to {name}{ApprovedSuffix} to approve it."
                : $"The body of '{name}' is not the approved one. What came back is in {receivedPath}; compare it with {approvedPath}.");
        }

        private static string Normalised(string json, string[] propertiesThatVary)
        {
            var root = JsonNode.Parse(json);
            SetAside(root, propertiesThatVary);

            return (root?.ToJsonString(Indented) ?? "null").ReplaceLineEndings("\n") + "\n";
        }

        private static void SetAside(JsonNode? node, string[] propertiesThatVary)
        {
            switch (node)
            {
                case JsonObject anObject:
                    foreach (var property in anObject.Select(pair => pair.Key).ToList())
                    {
                        if (propertiesThatVary.Contains(property, StringComparer.Ordinal))
                        {
                            anObject[property] = AValueThatVaries;
                        }
                        else
                        {
                            SetAside(anObject[property], propertiesThatVary);
                        }
                    }

                    break;
                case JsonArray anArray:
                    foreach (var item in anArray)
                    {
                        SetAside(item, propertiesThatVary);
                    }

                    break;
            }
        }

        private static string TestProjectDirectory()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend.Tests.csproj")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "The test project folder could not be found from the test output folder.");

            return directory!.FullName;
        }
    }
}
