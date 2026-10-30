import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";

// Premium scenario: it seeds Team Lightspeed with its history, and a premium instance has no cap on
// reports. CI uploads a licence to the instance before Playwright runs.
const TEAM_LIGHTSPEED_HISTORY_SCENARIO_ID = 11;
const testWithDemo = testWithDemoData(TEAM_LIGHTSPEED_HISTORY_SCENARIO_ID);

const LIGHTSPEED = "Team Lightspeed";

testWithDemo(
	"a coach creates a Then & Now report on a Team and it opens with Then frozen and the Cycle Time panel",
	async ({ testData, overviewPage }) => {
		expect(testData.teams.map((team) => team.name)).toContain(LIGHTSPEED);

		const lightspeed = await overviewPage.goToTeam(LIGHTSPEED);
		const reports = await lightspeed.goToReports();
		const report = await reports.createTheDefaultReport();

		await expect(report.title).toHaveText(
			/^Then & Now — 90 days to \d{1,2} \w{3,4} \d{4}$/,
		);
		await expect(report.panel("Cycle Time")).toBeVisible();
	},
);

testWithDemo(
	"a report a coach created shows beside its Team on the Overview and opens from there",
	async ({ testData, overviewPage }) => {
		expect(testData.teams.map((team) => team.name)).toContain(LIGHTSPEED);

		const lightspeed = await overviewPage.goToTeam(LIGHTSPEED);
		const reports = await lightspeed.goToReports();
		const created = await reports.createTheDefaultReport();
		const name = await created.readName();

		const overview = await overviewPage.lighthousePage.goToOverview();
		const report = await overview.openTeamReport(LIGHTSPEED, name);

		await expect(report.title).toHaveText(name);
	},
);
