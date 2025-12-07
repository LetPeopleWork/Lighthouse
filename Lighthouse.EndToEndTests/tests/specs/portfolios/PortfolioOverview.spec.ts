import { expect, test, testWithData } from "../../fixutres/LighthouseFixture";

testWithData(
	"should show all Portfolios on Portfolios Overview",
	async ({ testData, overviewPage }) => {
		const [portfolio1, portfolio2, portfolio3] = testData.portfolios;

		const portfolioLink1 = await overviewPage.getPortfolioLink(portfolio1);
		const portfolioLink2 = await overviewPage.getPortfolioLink(portfolio2);
		const portfolioLink3 = await overviewPage.getPortfolioLink(portfolio3);

		await expect(portfolioLink1).toBeVisible();
		await expect(portfolioLink2).toBeVisible();
		await expect(portfolioLink3).toBeVisible();
	},
);

testWithData(
	"should filter portfolios on portfolio Overview",
	async ({ testData, overviewPage }) => {
		const [portfolio1, portfolio2] = testData.portfolios;

		await test.step(`Search for portfolio ${portfolio1.name}`, async () => {
			await overviewPage.search(portfolio1.name);

			const portfolioLink1 = await overviewPage.getPortfolioLink(portfolio1);
			const portfolioLink2 = await overviewPage.getPortfolioLink(portfolio2);

			await expect(portfolioLink1).toBeVisible();
			await expect(portfolioLink2).not.toBeVisible();
		});

		await test.step(`Search for portfolio ${portfolio2.name}`, async () => {
			await overviewPage.search(portfolio2.name);

			const portfolioLink1 = await overviewPage.getPortfolioLink(portfolio1);
			const portfolioLink2 = await overviewPage.getPortfolioLink(portfolio2);

			await expect(portfolioLink1).not.toBeVisible();
			await expect(portfolioLink2).toBeVisible();
		});

		await test.step("Search for not existing portfolio", async () => {
			await overviewPage.search("Jambalaya");

			const portfolioLink1 = await overviewPage.getPortfolioLink(portfolio1);
			const portfolioLink2 = await overviewPage.getPortfolioLink(portfolio2);

			await expect(portfolioLink1).not.toBeVisible();
			await expect(portfolioLink2).not.toBeVisible();
		});
	},
);

testWithData(
	"should open portfolio Info when clicking on portfolio",
	async ({ testData, overviewPage }) => {
		const [portfolio] = testData.portfolios;

		const portfolioDetailPage = await overviewPage.goToPortfolio(portfolio);
		expect(portfolioDetailPage.page.url()).toContain(
			`/portfolios/${portfolio.id}`,
		);
	},
);

testWithData(
	"should open portfolio Edit Page when clicking on Edit Icon",
	async ({ testData, overviewPage }) => {
		const [portfolio] = testData.portfolios;

		const portfolioEditPage = await overviewPage.editPortfolio(portfolio);
		expect(portfolioEditPage.page.url()).toContain(
			`/portfolios/edit/${portfolio.id}`,
		);
	},
);

testWithData(
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
			const portfolioLink = await overviewPage.getPortfolioLink(portfolio);

			await expect(portfolioLink).not.toBeVisible();
		});
	},
);

testWithData(
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

			const portfolioLink = await overviewPage.getPortfolioLink(portfolio);
			await expect(portfolioLink).toBeVisible();
		});
	},
);

testWithData(
	"should clone project when clicking on Clone icon",
	async ({ testData, overviewPage }) => {
		const [project1] = testData.portfolios;

		await test.step(`Clone Project ${project1.name}`, async () => {
			const projectEditPage = await overviewPage.clonePortfolio(project1.name);

			// Verify we're on the new project page with cloneFrom parameter
			expect(projectEditPage.page.url()).toContain("/portfolios/new");
			expect(projectEditPage.page.url()).toContain(`cloneFrom=${project1.id}`);

			// Verify the project name is prefixed with "Copy of"
			const nameField = await projectEditPage.getName();
			expect(nameField).toBe(`Copy of ${project1.name}`);
		});
	},
);
