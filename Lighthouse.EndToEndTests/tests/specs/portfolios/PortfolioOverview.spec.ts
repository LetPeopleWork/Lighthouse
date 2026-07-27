import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";

const WHEN_WILL_IT_BE_DONE_SCENARIO_ID = 0;
const testWithPortfolio = testWithDemoData(WHEN_WILL_IT_BE_DONE_SCENARIO_ID);

// This grid is the SAME DataOverviewTable the Teams Overview renders — the six specs
// that used to live here (list, open, edit, delete, cancel-delete, clone) were the
// team walk with `team` swapped for `portfolio`, each paying a full demo re-seed to
// re-prove one shared component against a second entity. What is left is the walking
// skeleton: the seeded portfolios really are listed, and clicking one really opens
// that portfolio.
//
// The rest sits a layer down and covers portfolios explicitly:
//   - grid filtering, alphabetical order, the clone URL, and the per-row
//     Edit/Clone/Delete predicates -> DataOverviewTable.test.tsx (which has its own
//     "with Portfolios data" and "shows Clone action for portfolios" cases)
//   - the confirm/cancel dialog -> DeleteConfirmationDialog.test.tsx
//   - create/read/delete on the wire -> PortfoliosControllerTest.cs and
//     PortfolioDeleteSerialisationTests.cs
testWithPortfolio(
	"should list the seeded portfolios and open one from the Portfolios Overview",
	async ({ testData, overviewPage }) => {
		expect(testData.portfolios.length).toBeGreaterThan(0);

		for (const portfolio of testData.portfolios) {
			const portfolioLink = await overviewPage.getPortfolioLink(portfolio.name);
			await expect(portfolioLink).toBeVisible();
		}

		const [portfolio] = testData.portfolios;
		const portfolioDetailPage = await overviewPage.goToPortfolio(
			portfolio.name,
		);
		expect(portfolioDetailPage.page.url()).toContain(
			`/portfolios/${portfolio.id}`,
		);
	},
);
