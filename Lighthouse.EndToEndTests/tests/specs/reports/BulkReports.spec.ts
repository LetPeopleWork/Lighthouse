import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";
import type { ReportOwnerName } from "../../models/reports/AllReportsPage";

// The dependencies scenario seeds Teams Zenith and Gravity with history and is Premium; CI uploads a licence to the
// instance before Playwright runs, and creating for several owners is Premium.
const DEPENDENCIES_SCENARIO_ID = 12;
const testWithDemo = testWithDemoData(DEPENDENCIES_SCENARIO_ID);

// The demo data names its Teams "Team …"; the kind is the instance's seeded word for a Team.
const ZENITH: ReportOwnerName = { name: "Team Zenith", kind: "Team" };
const GRAVITY: ReportOwnerName = { name: "Team Gravity", kind: "Team" };
const REPORT_NAME = "Quarter against the last";

// Pending until the Reports page and creating for several owners are built.
// @walking_skeleton @driving_port @us-02a @slice-02a @kpi-OUT-6350-bulk-creation-used @contract-shape:bounded-change
testWithDemo.fixme(
	"a coach creates one comparison for two Teams from the Reports page, finds both listed and opens one",
	async ({ testData, overviewPage }) => {
		const teams = testData.teams.map((team) => team.name);
		expect(teams).toContain(ZENITH.name);
		expect(teams).toContain(GRAVITY.name);

		const reports = await overviewPage.lighthousePage.goToReports();
		await reports.createForEach([ZENITH, GRAVITY], REPORT_NAME);

		await expect(reports.rowOf(REPORT_NAME, ZENITH)).toBeVisible();
		await expect(reports.rowOf(REPORT_NAME, GRAVITY)).toBeVisible();
		const report = await reports.open(REPORT_NAME, GRAVITY);
		await expect(report.title).toHaveText(REPORT_NAME);
	},
);
