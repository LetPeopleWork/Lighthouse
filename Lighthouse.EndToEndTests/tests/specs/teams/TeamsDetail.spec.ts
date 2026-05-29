import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";

const WHEN_WILL_IT_BE_DONE_SCENARIO_ID = 0;
const testWithTeam = testWithDemoData(WHEN_WILL_IT_BE_DONE_SCENARIO_ID);

testWithTeam(
	"should show Manual When and How Many Forecast for team",
	async ({ testData, overviewPage }) => {
		const team = testData.teams[0];

		const teamDetailPage = await overviewPage.goToTeam(team.name);

		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

		await teamDetailPage.goToForecasts();

		const howMany = 20;
		const when = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000);
		const likelihood = await teamDetailPage.forecast(howMany, when);

		expect(likelihood).toBeGreaterThan(0);
	},
);

testWithTeam(
	"should show new work item creation forecast for team",
	async ({ testData, overviewPage }) => {
		const team = testData.teams[0];

		const teamDetailPage = await overviewPage.goToTeam(team.name);

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

testWithTeam(
	"should show forecast backtesting results for team",
	async ({ testData, overviewPage }) => {
		const team = testData.teams[0];

		const teamDetailPage = await overviewPage.goToTeam(team.name);

		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

		await teamDetailPage.goToForecasts();

		await expect(teamDetailPage.backtestForecastingSection).toBeVisible();

		await teamDetailPage.runBacktest();

		await expect(teamDetailPage.backtestResultsSection).toBeVisible();

		await expect(
			teamDetailPage.page.getByText(/Forecast Percentiles:/),
		).toBeVisible();
		await expect(
			teamDetailPage.page.getByText(/Actual Throughput:/),
		).toBeVisible();
	},
);
