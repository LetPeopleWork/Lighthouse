import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import type {
	IReport,
	IReportMetric,
	ReportMetricKey,
} from "../../../models/Reports/Report";
import {
	aPrintDialog,
	forgetTheBrowser,
	printPathFor,
	renderTheExportScene,
	reportPathFor,
	theFontsHaveLoaded,
	thePrintPageIsReady,
} from "../../../tests/ReportPrintTestKit";
import {
	aDash,
	aReportService,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	readingOf,
	theEngagementStartReport,
} from "../../../tests/ReportsTestKit";

/**
 * The marks that say which way a report moved are drawn, not typed, so they look the same on every machine and in
 * every PDF, whatever fonts the machine has. The words beside them carry the meaning and stay as they are; the marks
 * are pictures a screen reader skips, coloured green for better and red for worse as today. The dash of a missing
 * value and the minus of a fall stay text: they are punctuation, not marks.
 */

const pendingEpic6298 =
	"Pending: the report's marks are drawn (epic 6298, slice 02)";

const here = dirname(fileURLToPath(import.meta.url));

/** One metric of a report, which the example changes in place; a report without it is a broken example. */
const theMetric = (report: IReport, key: ReportMetricKey): IReportMetric => {
	const metric = report.thenAndNow?.metrics[key];
	expect(metric, `the example has no ${key}`).toBeTruthy();
	return metric as IReportMetric;
};

const THE_MARK_CHARACTERS = /[✔✘±▲▼]/;
const NO_CHANGE_CHARACTER = "–";
const SUCCESS_COLOUR = "rgb(46, 125, 50)";
const ERROR_COLOUR = "rgb(211, 47, 47)";

type Where = "on the report" | "in the print";

const showingTheReport = async (
	where: Where,
	report = theEngagementStartReport(),
) => {
	const reportService = aReportService({
		getReport: async () => report,
	});
	if (where === "on the report") {
		renderTheExportScene({
			reportService,
			startAt: reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
			withPrintPages: false,
		});
		await waitFor(() => {
			expect(document.querySelector("[data-change-kind]")).not.toBeNull();
			expect(document.querySelector("[data-change]")).not.toBeNull();
		});
		return;
	}
	renderTheExportScene({
		reportService,
		startAt: printPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
	});
	await thePrintPageIsReady();
};

const theSummaryGroups = () =>
	Array.from(document.querySelectorAll<HTMLElement>("[data-change-kind]"));

const theLineChanges = () =>
	Array.from(document.querySelectorAll<HTMLElement>("[data-change]"));

/** The one drawn mark a summary group or a line change carries. */
const theDrawnMarkOf = (holder: HTMLElement): SVGSVGElement => {
	const drawn = holder.querySelectorAll("svg");
	expect(
		drawn,
		`"${readingOf(holder)}" carries no single drawn mark`,
	).toHaveLength(1);
	return drawn[0];
};

const theGroup = (kind: string) =>
	document.querySelector<HTMLElement>(
		`[data-change-kind="${kind}"]`,
	) as HTMLElement;

describe("The marks of a report are drawn", () => {
	beforeEach(() => {
		theFontsHaveLoaded();
		aPrintDialog();
	});

	afterEach(forgetTheBrowser);

	// @us-02 @slice-02 @driving_port @contract-shape:unbounded-preservation
	it.skip.each<Where>(["on the report", "in the print"])(
		`draws every summary and line mark %s, and types none (${pendingEpic6298})`,
		async (where) => {
			await showingTheReport(where);

			const holders = [...theSummaryGroups(), ...theLineChanges()];
			expect(holders.length).toBeGreaterThan(0);
			for (const holder of holders) {
				expect(theDrawnMarkOf(holder)).toHaveAttribute("aria-hidden", "true");
				expect(readingOf(holder)).not.toMatch(THE_MARK_CHARACTERS);
			}
		},
	);

	// @us-02 @slice-02 @driving_port @contract-shape:bounded-change
	it.skip.each<Where>(["on the report", "in the print"])(
		`keeps every word beside the marks %s as it was (${pendingEpic6298})`,
		async (where) => {
			await showingTheReport(where);

			expect(theSummaryGroups().map(readingOf)).toEqual([
				"Improved: Cycle Time, Throughput, WIP",
				"Got worse: Total Work Item Age",
			]);
			expect(theLineChanges().map(readingOf).slice(0, 4)).toEqual([
				"15 days (−44 %)",
				"9 days (−43 %)",
				"3 days (−30 %)",
				"1 day (−20 %)",
			]);
		},
	);

	// @us-02 @slice-02 @contract-shape:bounded-change
	it.skip.each<Where>(["on the report", "in the print"])(
		`draws better and worse apart, green and red, %s (${pendingEpic6298})`,
		async (where) => {
			await showingTheReport(where);

			const improved = theDrawnMarkOf(theGroup("improved"));
			const worse = theDrawnMarkOf(theGroup("worse"));
			expect(improved.innerHTML).not.toBe(worse.innerHTML);
			expect(getComputedStyle(improved).color).toBe(SUCCESS_COLOUR);
			expect(getComputedStyle(worse).color).toBe(ERROR_COLOUR);
		},
	);

	// @us-02 @slice-02 @edge @contract-shape:bounded-change
	it.skip(`draws a rise and a fall as different marks (${pendingEpic6298})`, async () => {
		await showingTheReport("on the report");

		const drawings = new Set(
			theLineChanges().map((change) => theDrawnMarkOf(change).innerHTML),
		);
		expect(drawings.size).toBeGreaterThanOrEqual(2);
	});

	// @us-02 @slice-02 @edge @contract-shape:bounded-change
	it.skip(`draws the mark of a line that did not change, and keeps its words (${pendingEpic6298})`, async () => {
		const report = theEngagementStartReport();
		const cycleTime = theMetric(report, "cycle-time");
		cycleTime.atNow.p70 = cycleTime.atThen.p70;
		expect(cycleTime.atThen.p70).toBeTruthy();
		expect(cycleTime.atNow.p70).toBe(cycleTime.atThen.p70);
		await showingTheReport("on the report", report);

		const unchanged = theLineChanges().find((change) =>
			readingOf(change).includes("No change"),
		) as HTMLElement;
		expect(readingOf(unchanged)).toBe("No change");
		expect(theDrawnMarkOf(unchanged)).toHaveAttribute("aria-hidden", "true");
		expect(readingOf(unchanged)).not.toContain(NO_CHANGE_CHARACTER);
	});
});

describe("Punctuation stays text", () => {
	beforeEach(() => {
		theFontsHaveLoaded();
		aPrintDialog();
	});

	afterEach(forgetTheBrowser);

	// @us-02 @slice-02 @edge @contract-shape:unbounded-preservation
	// Guards the screen today and after the marks are drawn: only the marks become pictures.
	it("keeps the minus of a fall and the dash of a missing value as text on the report", async () => {
		const report = theEngagementStartReport();
		const throughput = theMetric(report, "throughput");
		throughput.atNow.perDay = aDash("no-finished-items");
		expect(throughput.atNow.perDay?.value).toBeNull();
		renderTheExportScene({
			reportService: aReportService({ getReport: async () => report }),
			startAt: reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
			withPrintPages: false,
		});

		const throughputPanel = await screen.findByRole("region", {
			name: "Throughput",
		});
		await waitFor(() => expect(theLineChanges().length).toBeGreaterThan(0));

		expect(readingOf(theLineChanges()[0])).toContain("−44 %");
		expect(readingOf(throughputPanel)).toContain("—");
	});
});

const productionSourcesUnder = (directory: string): string[] =>
	readdirSync(directory).flatMap((entry) => {
		const path = join(directory, entry);
		if (statSync(path).isDirectory()) return productionSourcesUnder(path);
		return /\.(ts|tsx)$/.test(entry) && !/\.test\.tsx?$/.test(entry)
			? [path]
			: [];
	});

describe("No mark is typed anywhere in Reports", () => {
	// @us-02 @slice-02 @error @contract-shape:unbounded-preservation
	it.skip(`leaves no mark character in the Reports code (${pendingEpic6298})`, () => {
		const typed = productionSourcesUnder(here)
			.filter((file) => THE_MARK_CHARACTERS.test(readFileSync(file, "utf8")))
			.map((file) => relative(here, file).replaceAll("\\", "/"));

		expect(productionSourcesUnder(here).length).toBeGreaterThan(0);
		expect(typed).toEqual([]);
	});
});
