import { expect, testWithDemoData } from "../../fixutres/LighthouseFixture";
import { ReportPrintPage } from "../../models/reports/ReportPrintPage";

// Premium scenario: it seeds Team Lightspeed with its history, and a premium instance has no cap on
// reports. CI uploads a licence to the instance before Playwright runs.
const TEAM_LIGHTSPEED_HISTORY_SCENARIO_ID = 11;
const testWithDemo = testWithDemoData(TEAM_LIGHTSPEED_HISTORY_SCENARIO_ID);

const LIGHTSPEED = "Team Lightspeed";

/** The light theme's divider colour, the one the page prints in. */
const THE_LIGHT_DIVIDER = "rgb(221, 221, 221)";

const pendingFramePrint = "Pending: Export PDF prints from a frame (epic 6298)";

// The print dialog of a real browser cannot be driven, so it is stood in for on the tab's page and on every frame
// in it: only the report's own frame may ask for it, and the coach never leaves the report.
// @walking_skeleton @driving_port @us-01 @rev-3 @kpi-OUT-6298-K1-pdf-export-used @contract-shape:bounded-change
testWithDemo.fixme(
	`a coach exports a Team's Then & Now report as a PDF from the report: its own print frame asks for the dialog once, and the coach is still on the report with the same history (${pendingFramePrint})`,
	async ({ testData, overviewPage, page }) => {
		expect(testData.teams.map((team) => team.name)).toContain(LIGHTSPEED);

		const lightspeed = await overviewPage.goToTeam(LIGHTSPEED);
		const reports = await lightspeed.goToReports();
		const report = await reports.createTheDefaultReport();
		const name = await report.readName();
		const address = report.address();
		const history = await report.historyLength();
		await ReportPrintPage.standInForThePrintDialog(page);

		await report.exportPdf();

		await expect
			.poll(() => report.timesThePrintFrameAskedForTheDialog())
			.toBe(1);
		expect(await report.timesThePageAskedForTheDialog()).toBe(0);
		await report.closeThePrintDialog();
		expect(report.address()).toBe(address);
		expect(await report.historyLength()).toBe(history);
		await expect(report.title).toHaveText(name);
	},
);

// The page a pasted link opens is the one a later server renderer will load: it names the report and its owner,
// fits the paper, and asks for no print dialog of its own.
// @us-01 @slice-01 @real-io @error @contract-shape:unbounded-preservation
testWithDemo(
	"a report's print page opened by its address names the report and its owner, fits A4 and never asks for the print dialog",
	async ({ testData, overviewPage, page }) => {
		expect(testData.teams.map((team) => team.name)).toContain(LIGHTSPEED);

		const lightspeed = await overviewPage.goToTeam(LIGHTSPEED);
		const reports = await lightspeed.goToReports();
		const report = await reports.createTheDefaultReport();
		const name = await report.readName();
		await ReportPrintPage.standInForThePrintDialog(page);

		const printed = await ReportPrintPage.openByItsAddress(
			page,
			report.address(),
		);

		await expect(printed.owner(LIGHTSPEED)).toBeVisible();
		await expect(printed.title).toHaveText(name);
		expect(await printed.reachesPastThePageEdgeInPrint()).toBe(false);
		expect(await printed.timesThePrintDialogOpened()).toBe(0);
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
		const printed = await ReportPrintPage.openByItsAddress(
			page,
			report.address(),
		);

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
testWithDemo(
	`a report named in Central European letters is ready to print only once the Lighthouse typeface can draw them, on a slow connection`,
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
testWithDemo(
	`the PDF of a report sets each panel's columns apart with the light divider line`,
	async ({ testData, overviewPage, page }) => {
		expect(testData.teams.map((team) => team.name)).toContain(LIGHTSPEED);

		const lightspeed = await overviewPage.goToTeam(LIGHTSPEED);
		const reports = await lightspeed.goToReports();
		const report = await reports.createTheDefaultReport();
		const printed = await ReportPrintPage.openByItsAddress(
			page,
			report.address(),
		);

		expect(await printed.colourOfTheLineBeforeNowInPrint()).toBe(
			THE_LIGHT_DIVIDER,
		);
	},
);
