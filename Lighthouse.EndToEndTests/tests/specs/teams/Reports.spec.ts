import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";

// Premium scenario: it seeds Team Lightspeed with its history, and a premium instance has no cap on
// reports. CI uploads a licence to the instance before Playwright runs.
const TEAM_LIGHTSPEED_HISTORY_SCENARIO_ID = 11;
const testWithDemo = testWithDemoData(TEAM_LIGHTSPEED_HISTORY_SCENARIO_ID);

const LIGHTSPEED = "Team Lightspeed";

// @walking_skeleton @driving_port @us-01 @slice-01 @kpi-OUT-5878-K1-report-created @contract-shape:bounded-change
// Pending until slice 01 ships the Reports tab, the create wizard and the report page.
testWithDemo.fixme(
	"a coach creates a Then & Now report on a Team and it opens with Then frozen and the Cycle Time panel",
	async ({ testData, overviewPage }) => {
		expect(testData.teams.map((team) => team.name)).toContain(LIGHTSPEED);

		const lightspeed = await overviewPage.goToTeam(LIGHTSPEED);
		const reports = await lightspeed.goToReports();
		const report = await reports.createTheDefaultReport();

		await expect(report.thenWindow).toBeVisible();
		await expect(report.panel("Cycle Time: Then & Now")).toBeVisible();
	},
);
