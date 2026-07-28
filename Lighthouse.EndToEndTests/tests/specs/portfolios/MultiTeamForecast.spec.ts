import {
	expect,
	test,
	testWithDemoData,
} from "../../fixutres/LighthouseFixture";

// Premium scenario. CI uploads a licence to the instance before Playwright runs.
const DEPENDENCIES_SCENARIO_ID = 12;
const testWithDependencies = testWithDemoData(DEPENDENCIES_SCENARIO_ID);

const PORTFOLIO_NAME = "Project Ocean Explorer";
const FEATURE_WITHOUT_FORECAST = "Deep Sea Mapping Initiative";
const FEATURE_WITH_FORECAST = "Coral Reef Restoration Program";

testWithDependencies(
	"should report the forecast state of a multi-team feature",
	async ({ testData, overviewPage }) => {
		const portfolio = testData.portfolios.find(
			(candidate) => candidate.name === PORTFOLIO_NAME,
		);

		if (!portfolio) {
			throw new Error(`Demo scenario did not seed ${PORTFOLIO_NAME}`);
		}

		const portfolioDetailPage = await overviewPage.goToPortfolio(portfolio.name);

		await test.step("Feature whose team has no throughput cannot be forecast", async () => {
			// Team Meridian contributes to this Epic and has closed nothing, so no honest
			// completion distribution exists for the Feature as a whole.
			const forecastCell = portfolioDetailPage.getFeatureForecastCell(
				FEATURE_WITHOUT_FORECAST,
			);

			await expect(forecastCell).toContainText("Cannot forecast", {
				timeout: 30_000,
			});
		});

		// This step proves the forecast still renders, NOT that the joint maths is right - dates would
		// appear under the old worst-team code too. The discrimination lives in
		// MultiTeamJointForecastDeliveryIntegrationTest, which asserts 25 % against the same HTTP
		// endpoint where the worst-team answer would be 50 %.
		await test.step("Feature whose teams all have throughput still shows dates", async () => {
			const forecastCell = portfolioDetailPage.getFeatureForecastCell(
				FEATURE_WITH_FORECAST,
			);

			await expect(forecastCell).not.toContainText("Cannot forecast");
			await expect(forecastCell).toContainText(/\d/);
		});
	},
);
