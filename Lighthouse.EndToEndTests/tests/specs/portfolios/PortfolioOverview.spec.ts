import {
	expect,
	test,
	testWithDemoData,
} from "../../fixutres/LighthouseFixture";

const WHEN_WILL_IT_BE_DONE_SCENARIO_ID = 0;
const testWithPortfolio = testWithDemoData(WHEN_WILL_IT_BE_DONE_SCENARIO_ID);

testWithPortfolio(
	"should show seeded Portfolios on Portfolios Overview",
	async ({ testData, overviewPage }) => {
		expect(testData.portfolios.length).toBeGreaterThan(0);

		for (const portfolio of testData.portfolios) {
			const portfolioLink = await overviewPage.getPortfolioLink(portfolio.name);
			await expect(portfolioLink).toBeVisible();
		}
	},
);

testWithPortfolio(
	"should open portfolio Info when clicking on portfolio",
	async ({ testData, overviewPage }) => {
		const [portfolio] = testData.portfolios;

		const portfolioDetailPage = await overviewPage.goToPortfolio(
			portfolio.name,
		);
		expect(portfolioDetailPage.page.url()).toContain(
			`/portfolios/${portfolio.id}`,
		);
	},
);

testWithPortfolio(
	"should open portfolio Edit Page when clicking on Edit Icon",
	async ({ testData, overviewPage }) => {
		const [portfolio] = testData.portfolios;

		const portfolioEditPage = await overviewPage.editPortfolio(portfolio);
		expect(portfolioEditPage.page.url()).toContain(
			`/portfolios/${portfolio.id}/settings`,
		);
	},
);

testWithPortfolio(
	"should delete portfolio when clicking on Delete Icon and confirming",
	async ({ testData, overviewPage }) => {
		const [portfolio] = testData.portfolios;

		await test.step(`Delete portfolio ${portfolio.name}`, async () => {
			const portfolioDeletionModal =
				await overviewPage.deletePortfolio(portfolio);
			await portfolioDeletionModal.delete();
		});

		await test.step(`Search for portfolio ${portfolio.name}`, async () => {
			await overviewPage.search(portfolio.name);
			const portfolioLink = await overviewPage.getPortfolioLink(portfolio.name);

			await expect(portfolioLink).not.toBeVisible();
		});
	},
);

testWithPortfolio(
	"should not delete portfolio when clicking on Delete Icon and cancelling",
	async ({ testData, overviewPage }) => {
		const [portfolio] = testData.portfolios;

		await test.step(`Delete portfolio ${portfolio.name}`, async () => {
			const portfolioDeletionDialog =
				await overviewPage.deletePortfolio(portfolio);
			await portfolioDeletionDialog.cancel();
		});

		await test.step(`Search for portfolio ${portfolio.name}`, async () => {
			await overviewPage.search(portfolio.name);

			const portfolioLink = await overviewPage.getPortfolioLink(portfolio.name);
			await expect(portfolioLink).toBeVisible();
		});
	},
);

testWithPortfolio(
	"should clone project when clicking on Clone icon",
	async ({ testData, overviewPage }) => {
		const [project1] = testData.portfolios;

		await test.step(`Clone Project ${project1.name}`, async () => {
			const projectEditPage = await overviewPage.clonePortfolio(project1.name);

			expect(projectEditPage.page.url()).toContain("/portfolios/new");
			expect(projectEditPage.page.url()).toContain(`cloneFrom=${project1.id}`);

			const nameField = await projectEditPage.getName();
			expect(nameField).toBe(`Copy of ${project1.name}`);
		});
	},
);
