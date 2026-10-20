import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";
import { ReportPrintPage } from "../../models/reports/ReportPrintPage";

// Premium scenario: it seeds Team Lightspeed with its history, and a premium instance has no cap on
// reports. CI uploads a licence to the instance before Playwright runs.
const TEAM_LIGHTSPEED_HISTORY_SCENARIO_ID = 11;
const testWithDemo = testWithDemoData(TEAM_LIGHTSPEED_HISTORY_SCENARIO_ID);

const LIGHTSPEED = "Team Lightspeed";

const pendingReviewFix = "Pending: slice 02 review fix (epic 6298)";
/** The light theme's divider colour, the one the page prints in. */
const THE_LIGHT_DIVIDER = "rgb(221, 221, 221)";

// The print dialog of a real browser cannot be driven, so it is stood in for: the page asks for it once, and
// closing it brings the reader back to the report.
// @walking_skeleton @driving_port @us-01 @slice-01 @kpi-OUT-6298-K1-pdf-export-used @contract-shape:bounded-change
testWithDemo(
	"a coach exports a Team's Then & Now report as a PDF: the page names the report and its owner, prints once and returns to the report",
	async ({ testData, overviewPage, page }) => {
		expect(testData.teams.map((team) => team.name)).toContain(LIGHTSPEED);

		const lightspeed = await overviewPage.goToTeam(LIGHTSPEED);
		const reports = await lightspeed.goToReports();
		const report = await reports.createTheDefaultReport();
		const name = await report.readName();
		await ReportPrintPage.standInForThePrintDialog(page);

		const printed = await report.exportPdf();

		await expect(printed.owner(LIGHTSPEED)).toBeVisible();
		await expect(printed.title).toHaveText(name);
		expect(await printed.reachesPastThePageEdgeInPrint()).toBe(false);
		await expect.poll(() => printed.timesThePrintDialogOpened()).toBe(1);

		await printed.closeThePrintDialog();

		await expect(report.title).toHaveText(name);
	},
);

// Only the typeface the app carries may reach the PDF: a machine font in it means the PDF looks different on
// the next machine.
// @us-02 @slice-02 @real-io @kpi-OUT-6298-G-pdf-matches-screen @contract-shape:unbounded-preservation
testWithDemo(
	"the PDF of a report embeds the Lighthouse typeface and no other",
	async ({ testData, overviewPage, page }) => {
		expect(testData.teams.map((team) => team.name)).toContain(LIGHTSPEED);

		const lightspeed = await overviewPage.goToTeam(LIGHTSPEED);
		const reports = await lightspeed.goToReports();
		const report = await reports.createTheDefaultReport();
		await ReportPrintPage.standInForThePrintDialog(page);
		const printed = await report.exportPdf();

		const fonts = await printed.fontsEmbeddedInThePdf();

		expect(fonts.length).toBeGreaterThan(0);
		for (const font of fonts) {
			expect(font).toMatch(/^Quicksand/);
		}
	},
);

// A name in Central European letters needs a part of the typeface the page fetches only once it meets them; on a
// slow connection that part can still be on its way when the page says it is ready to print.
// @us-02 @slice-02 @real-io @error @contract-shape:bounded-change
testWithDemo.fixme(
	`a report named in Central European letters is ready to print only once the Lighthouse typeface can draw them, on a slow connection (${pendingReviewFix})`,
	async ({ testData, overviewPage, page }) => {
		expect(testData.teams.map((team) => team.name)).toContain(LIGHTSPEED);
		const name = "Zespół Łódź Q3";

		const lightspeed = await overviewPage.goToTeam(LIGHTSPEED);
		const reports = await lightspeed.goToReports();
		const report = await reports.createReport({ name });
		await ReportPrintPage.holdBackTheTypefacesCentralEuropeanLetters(page);
		await ReportPrintPage.watchTheTypefaceWhenThePageSaysReady(page);

		const printed = await ReportPrintPage.openByItsAddress(
			page,
			report.address(),
		);

		await expect(printed.title).toHaveText(name);
		expect(await printed.couldTheTypefaceDrawEveryLetterWhenReady()).toBe(true);
	},
);

// A line in the text's own colour prints as a heavy black rule between the columns rather than a quiet divider.
// @us-02 @slice-02 @real-io @contract-shape:bounded-change
testWithDemo.fixme(
	`the PDF of a report sets each panel's columns apart with the light divider line (${pendingReviewFix})`,
	async ({ testData, overviewPage, page }) => {
		expect(testData.teams.map((team) => team.name)).toContain(LIGHTSPEED);

		const lightspeed = await overviewPage.goToTeam(LIGHTSPEED);
		const reports = await lightspeed.goToReports();
		const report = await reports.createTheDefaultReport();
		await ReportPrintPage.standInForThePrintDialog(page);
		const printed = await report.exportPdf();

		expect(await printed.colourOfTheLineBeforeNowInPrint()).toBe(
			THE_LIGHT_DIVIDER,
		);
	},
);
