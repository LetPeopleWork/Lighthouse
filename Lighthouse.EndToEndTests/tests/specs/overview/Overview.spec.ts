import { expect, test, testWithData } from "../../fixutres/LighthouseFixture";

testWithData(
	"should show all portfolios on dashboard",
	async ({ testData, overviewPage }) => {
		const [portfolio1, portfolio2] = testData.portfolios;

		await expect(await overviewPage.getPortfolioLink(portfolio1)).toBeVisible();
		await expect(await overviewPage.getPortfolioLink(portfolio2)).toBeVisible();
	},
);

testWithData(
	"should filter portfolios on dashboard",
	async ({ testData, overviewPage }) => {
		const [portfolio1, portfolio2] = testData.portfolios;

		await test.step(`Search for Portfolio ${portfolio1.name}`, async () => {
			await overviewPage.search(portfolio1.name);

			const portfolioLink = await overviewPage.getPortfolioLink(portfolio1);

			await expect(portfolioLink).toBeVisible();
		});

		await test.step(`Search for Portfolio ${portfolio2.name}`, async () => {
			await overviewPage.search(portfolio2.name);

			const portfolioLink = await overviewPage.getPortfolioLink(portfolio2);
			await expect(portfolioLink).toBeVisible();
		});

		await test.step("Search for not existing Portfolio", async () => {
			await overviewPage.search("Jambalaya");

			const portfolioLink1 = await overviewPage.getPortfolioLink(portfolio1);
			const portfolioLink2 = await overviewPage.getPortfolioLink(portfolio2);

			await expect(portfolioLink1).not.toBeVisible();
			await expect(portfolioLink2).not.toBeVisible();
		});

		await test.step("Clear Search", async () => {
			await overviewPage.search("");

			const portfolioLink1 = await overviewPage.getPortfolioLink(portfolio1);
			const portfolioLink2 = await overviewPage.getPortfolioLink(portfolio2);

			await expect(portfolioLink1).toBeVisible();
			await expect(portfolioLink2).toBeVisible();
		});
	},
);
