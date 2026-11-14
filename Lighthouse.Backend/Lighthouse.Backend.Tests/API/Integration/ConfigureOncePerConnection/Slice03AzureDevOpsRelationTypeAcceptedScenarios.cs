using System.Net;
using System.Reflection;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;

namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// Tomás Ortega's PMO records waits with the custom relation type <c>Contoso.DependsOn</c> on Azure DevOps
    /// Server 2022. Today an Additional Field referencing <c>Contoso.DependsOn-Forward</c> fails Validate as a
    /// missing field, so there is no way to name it. From this slice it validates; a typo still fails and lists
    /// the relation types the organisation defines. Only the Dependency Field ever reads a relation type: naming one in any other setting reads as if
    /// nothing were named, and says so.
    ///
    /// Validate is posted to the validate route the connection page's Validate button posts to, and the answer
    /// is read as that button reads it; the production Azure DevOps connector answers it over a recorded client.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    public class Slice03AzureDevOpsRelationTypeAcceptedScenarios : NamedDependencyLinkAcceptanceTest
    {
        private const int Item4711 = 4711;

        private const int Item4702 = 4702;

        private const string StoreOperations = "Store Operations";

        private const string AcceptedAsTheButtonReadsIt = "The Validate button is answered with OK.";

        private const string RefusedAsTheButtonReadsIt = "A refused connection is answered as a bad request, as the Validate button reads it.";

        private const string TheNativeParentStands = "The native parent stands.";

        private static readonly string[] Nothing = [];

        [SetUp]
        public void ContosoListsItsCustomRelationType()
        {
            var contoso = TheOrganisationAt(ContosoOrganisation);
            contoso.RelationTypesItLists.AddRange([ContosoDependsOnForward, ContosoDependsOnReverse]);
            contoso.FieldsItDefines.Add("Custom.RemoteParent");
        }

        [Test]
        [Category("us-03")]
        [Category("ac-3.1")]
        [Category("driving_port")]
        [Category("contract-shape:pure-function")]
        public async Task A_relation_type_reference_with_its_end_validates_where_a_field_name_goes()
        {
            var answer = await TheAdministratorValidatesAConnectionTo(ContosoOrganisation, ContosoDependsOnForward);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Status, Is.EqualTo(HttpStatusCode.OK), AcceptedAsTheButtonReadsIt);
                Assert.That(answer.IsValid, Is.True, $"Validate refused it: {answer.Message}");
            }
        }
    }
}
