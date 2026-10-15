namespace Lighthouse.Backend.Tests.API.Integration.ConnectionFieldChange
{
    /// <summary>
    /// Story #6307, slice 02: before Priya saves a field change, the connection page asks her whether to
    /// update the Teams and Portfolios on the connection, and says how many there are. The page reads its
    /// connection from the list of connections, so that list says, for each connection, how many Teams
    /// and how many Portfolios read through it. The page fetches nothing more to ask the question.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6307-refresh-on-additional-fields-change")]
    [Category("slice-02")]
    public partial class Slice02ConnectionsSayWhoUsesThemTest
    {
        [TestCase(NordwindJiraDc, 4, 2)]
        [TestCase(NordwindAzureDevOps, 1, 0)]
        [TestCase(NordwindJiraSandbox, 0, 0)]
        [TestCase(NordwindJiraRoadmaps, 0, 2)]
        [Category("us-02")]
        [Category("driving_port")]
        [Category("contract-shape:pure-function")]
        [Ignore(Pending)]
        public async Task The_connection_list_says_how_many_Teams_and_Portfolios_use_each_connection(string connection, int teams, int portfolios)
        {
            await GivenNordwindsConnectionsTeamsAndPortfolios();
            await GivenAConnectionOnlyPortfoliosReadFrom();

            var read = await WhenPriyaOpensTheConnectionPage(connection);

            ThenTheConnectionIsUsedBy(read, teams, portfolios);
        }
    }
}
