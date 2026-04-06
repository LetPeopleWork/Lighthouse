import { expect, testWithData } from "../../fixutres/LighthouseFixture";

testWithData(
	"should show Manual When and How Many Forecast for team",
	async ({ testData, overviewPage }) => {
		const team = testData.teams[0];

		const teamDetailPage = await overviewPage.goToTeam(team.name);

		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

		await teamDetailPage.updateTeamData();
		await expect(teamDetailPage.updateTeamDataButton).toBeDisabled();

		// Wait for update to be done
		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

		await teamDetailPage.goToForecasts();

		const howMany = 20;
		const when = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000);
		const likelihood = await teamDetailPage.forecast(howMany, when);

		expect(likelihood).toBeGreaterThan(0);
	},
);

testWithData(
	"should show new work item creation forecast for team",
	async ({ testData, overviewPage }) => {
		const team = testData.teams[0];

		const teamDetailPage = await overviewPage.goToTeam(team.name);

		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

		await teamDetailPage.updateTeamData();
		await expect(teamDetailPage.updateTeamDataButton).toBeDisabled();

		// Wait for update to be done
		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

		await teamDetailPage.goToForecasts();

		await teamDetailPage.forecastNewWorkItems(["Bug"]);

		await expect(
			teamDetailPage.page.getByText("How many Bug Work Items will"),
		).toBeVisible();
		await expect(
			teamDetailPage.page
				.locator(".MuiTypography-root > .MuiSvgIcon-root")
				.first(),
		).toBeVisible();
	},
);

testWithData(
	"should show forecast backtesting results for team",
	async ({ testData, overviewPage }) => {
		const team = testData.teams[0];

		const teamDetailPage = await overviewPage.goToTeam(team.name);

		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

		await teamDetailPage.updateTeamData();
		await expect(teamDetailPage.updateTeamDataButton).toBeDisabled();

		// Wait for update to be done
		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

		await teamDetailPage.goToForecasts();

		// Verify the Forecast Backtesting section is visible
		await expect(teamDetailPage.backtestForecastingSection).toBeVisible();

		// Run the backtest with default dates
		await teamDetailPage.runBacktest();

		// Verify backtest results appear
		await expect(teamDetailPage.backtestResultsSection).toBeVisible();

		// Verify the percentile list and actual throughput are displayed
		await expect(
			teamDetailPage.page.getByText(/Forecast Percentiles:/),
		).toBeVisible();
		await expect(
			teamDetailPage.page.getByText(/Actual Throughput:/),
		).toBeVisible();
	},
);
