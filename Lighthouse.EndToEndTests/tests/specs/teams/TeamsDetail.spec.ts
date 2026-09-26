import {
	expect,
	test,
	testWithDemoData,
} from "../../fixutres/LighthouseFixture";

const WHEN_WILL_IT_BE_DONE_SCENARIO_ID = 0;
const testWithTeam = testWithDemoData(WHEN_WILL_IT_BE_DONE_SCENARIO_ID);

// The three forecast surfaces on this tab used to be three specs. Each re-seeded
// the whole demo scenario and re-walked overview -> team -> Forecasts to reach the
// same page, then exercised one panel on it. They are steps of one visit now; the
// assertions are unchanged.
testWithTeam(
	"should show manual, new-work-item, backtesting, and reality check forecasts on the team Forecasts tab",
	async ({ testData, overviewPage }) => {
		const team = testData.teams[0];

		const teamDetailPage = await overviewPage.goToTeam(team.name);

		await expect(teamDetailPage.updateTeamDataButton).toBeEnabled();

		await teamDetailPage.goToForecasts();

		await test.step("Manual When and How Many forecast", async () => {
			const howMany = 20;
			const when = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000);
			const likelihood = await teamDetailPage.forecast(howMany, when);

			expect(likelihood).toBeGreaterThan(0);
		});

		await test.step("New work item creation forecast", async () => {
			await teamDetailPage.forecastNewWorkItems(["Bug"]);

			await expect(
				teamDetailPage.page.getByText("How many Bug Work Items will"),
			).toBeVisible();
			await expect(
				teamDetailPage.page
					.locator(".MuiTypography-root > .MuiSvgIcon-root")
					.first(),
			).toBeVisible();
		});

		await test.step("Forecast backtesting results", async () => {
			await expect(teamDetailPage.backtestForecastingSection).toBeVisible();

			await teamDetailPage.runBacktest();

			await expect(teamDetailPage.backtestResultsSection).toBeVisible();

			await expect(
				teamDetailPage.page.getByText(/Forecast Percentiles:/),
			).toBeVisible();
			await expect(
				teamDetailPage.page.getByText(/Actual Throughput:/),
			).toBeVisible();
		});

		// @walking_skeleton @driving_port @us-04 @slice-04
		await test.step("Forecast reality check opens in a dialog", async () => {
			await teamDetailPage.runRealityCheck();

			await expect(teamDetailPage.realityCheckDialog).toBeVisible();
			await expect(teamDetailPage.realityCheckLevelLine(85)).toBeVisible();
			await expect(teamDetailPage.realityCheckDialogDenominator).toBeVisible();
			await expect(teamDetailPage.realityCheckTable).toBeVisible();
			expect(
				await teamDetailPage.realityCheckWindowRows.count(),
			).toBeGreaterThanOrEqual(16);

			await teamDetailPage.closeRealityCheckWithEscape();

			await expect(teamDetailPage.realityCheckDialog).toBeHidden();
			await expect(teamDetailPage.runRealityCheckButton).toBeFocused();
		});
	},
);
