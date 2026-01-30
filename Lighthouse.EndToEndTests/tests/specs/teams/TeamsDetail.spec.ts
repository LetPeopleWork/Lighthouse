import { expect, testWithData } from "../../fixutres/LighthouseFixture";
import { expectDateToBeRecent } from "../../helpers/dates";

const testData = [
	{ name: "Azure DevOps", index: 1 },
	{ name: "Jira", index: 2 },
];

for (const { index, name } of testData) {
	testWithData(
		`should update Team Data for ${name} team on click`,
		async ({ testData, overviewPage }) => {
			const team = testData.teams[index];

			const teamDetailPage = await overviewPage.goToTeam(team.name);

			await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

			await teamDetailPage.updateTeamData();
			await expect(teamDetailPage.updateTeamDataButton).toBeDisabled();

			// Wait for update to be done
			await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

			const lastUpdatedDate = await teamDetailPage.getLastUpdatedDate();
			expectDateToBeRecent(lastUpdatedDate);

			// Check metrics
			await teamDetailPage.goToMetrics();

			// Add a small delay to give metrics time to load
			await teamDetailPage.page.waitForTimeout(300);

			await expect(teamDetailPage.workItemsInProgressWidget).toBeVisible();
			await expect(teamDetailPage.cycleTimePercentileWidget).toBeVisible();
			await expect(teamDetailPage.startedVsClosedWidget).toBeVisible();
			await expect(teamDetailPage.throughputRunChartWidget).toBeVisible();
			await expect(teamDetailPage.cycleTimeScatterplotWidget).toBeVisible();
			await expect(teamDetailPage.workItemAgingChart).toBeVisible();
			await expect(teamDetailPage.wipOverTimeWidget).toBeVisible();
			await expect(teamDetailPage.simplifiedCfdWidget).toBeVisible();
		},
	);
}

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
		const likelihood = await teamDetailPage.forecast(howMany);

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
		await expect(teamDetailPage.page.getByText(/Forecast Percentiles:/)).toBeVisible();
		await expect(teamDetailPage.page.getByText(/Actual Throughput:/)).toBeVisible();
	},
);
