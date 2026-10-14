import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IReport,
	IReportMetric,
	IReportWindow,
	ReportMetricSide,
} from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { deferred } from "../../../tests/HeldMetricsService";
import {
	A_LATER_METRIC,
	aDash,
	aRefusal,
	aReportService,
	CANNOT_BE_REBUILT,
	COULD_NOT_LOAD,
	defaultTerms,
	ENGAGEMENT_START,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	LOADING,
	MADE_BY_A_NEWER_VERSION,
	METRIC_FROM_A_NEWER_VERSION,
	NO_LONGER_EXISTS,
	NOT_CAPTURED,
	readingOf,
	renderWithReports,
	shown,
	theEngagementStartReport,
	thePanel,
	thePanelTitles,
} from "../../../tests/ReportsTestKit";
import ReportView from "./ReportView";

/**
 * One report as a reader opens it: one line "Reports / <name>", a summary naming each side's window, its
 * length and how many Work Items closed in it, and one panel per shown metric in the catalog's order. A panel
 * is three segments, Then | Now | Change, headed by the side alone; every percentile carries the certainty icon the
 * Metrics tab shows for it, and a value that cannot be shown is a dash with its reason. A change is told by
 * its sign and arrow as well as its colour.
 *
 * The report waits for its read before drawing anything that could pass for numbers, says so when the read
 * fails or the report is gone, and never lets a late answer for another report land.
 */

const { reportUsage, terms, mockNavigate } = vi.hoisted(() => ({
	reportUsage: vi.fn(),
	terms: { current: {} as Record<string, string> },
	mockNavigate: vi.fn(),
}));

vi.mock(
	"../../../services/UsageData/usageDataReporter",
	async (importOriginal) => ({
		...(await importOriginal<
			typeof import("../../../services/UsageData/usageDataReporter")
		>()),
		useUsageDataReporter: () => reportUsage,
	}),
);

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("react-router", async () => ({
	...(await vi.importActual("react-router")),
	useNavigate: () => mockNavigate,
}));

const CYCLE_TIME_PANEL = "Cycle Time";
const THROUGHPUT_PANEL = "Throughput";
const WIP_PANEL = "WIP";
const AGE_PANEL = "Total Work Item Age";
const READY = "ready";

/** The icons the Metrics tab's Cycle Time percentiles carry for the 85th, 70th and 50th. */
const CERTAINTY_OF_85TH_70TH_50TH = [
	"CheckCircleOutlineOutlinedIcon",
	"QueryBuilderIcon",
	"ErrorOutlineOutlinedIcon",
];

const openTheReport = (
	report: IReport = theEngagementStartReport(),
	viewer: "admin" | "reader" = "reader",
) => {
	const reportService = aReportService({
		getReport: vi.fn().mockResolvedValue(report),
	});
	renderWithReports(
		<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
		{ reportService, viewer },
	);
	return reportService;
};

const theReportIsReady = async () =>
	waitFor(() =>
		expect(
			document.querySelector(`[data-report-status="${READY}"]`),
		).not.toBeNull(),
	);

const changesIn = (panel: HTMLElement) =>
	Array.from(panel.querySelectorAll<HTMLElement>("[data-change]"));

type Segment = "then" | "now" | "change";

const theSegment = (panel: HTMLElement, segment: Segment): HTMLElement => {
	const found = panel.querySelector<HTMLElement>(`[data-segment="${segment}"]`);
	expect(found, `the panel has no ${segment} segment`).not.toBeNull();
	return found as HTMLElement;
};

const readingOfSegment = (panel: HTMLElement, segment: Segment) =>
	readingOf(theSegment(panel, segment));

const certaintyIconsIn = (segment: HTMLElement) =>
	Array.from(segment.querySelectorAll<SVGElement>("svg[data-testid]")).map(
		(icon) => icon.dataset.testid,
	);

const withCycleTime = (
	metric: NonNullable<IReport["thenAndNow"]["metrics"]["cycle-time"]>,
): IReport => {
	const report = theEngagementStartReport();
	report.thenAndNow.metrics["cycle-time"] = metric;
	return report;
};

type Side = "then" | "now";

const SIDES: Side[] = ["then", "now"];

const theSummary = (): HTMLElement | null =>
	document.querySelector<HTMLElement>("[data-report-summary]");

const readingOfTheSummary = (): string => {
	const found = theSummary();
	expect(found, "the report has no summary").not.toBeNull();
	return readingOf(found as HTMLElement);
};

/** One side of the summary as read aloud; its lines end in a space, as a panel's do, so they read apart. */
const readingOfSummary = (side: Side): string => {
	const found = theSummary()?.querySelector<HTMLElement>(
		`[data-summary-side="${side}"]`,
	);
	expect(found, `the summary has no ${side} side`).toBeTruthy();
	return readingOf(found as HTMLElement);
};

const aWindow = (
	start: string,
	end: string,
	lengthDays: number,
): IReportWindow => ({ start, end, lengthDays });

const withWindows = (
	thenWindow: IReportWindow,
	nowWindow: IReportWindow,
): IReport => {
	const report = theEngagementStartReport();
	report.thenAndNow.thenWindow = thenWindow;
	report.thenAndNow.nowWindow = nowWindow;
	return report;
};

const withThenCycleTime = (atThen: ReportMetricSide): IReport =>
	withCycleTime({
		atThen,
		atNow: { p85: shown(12, 41), p70: shown(7, 41), p50: shown(4, 41) },
	});

/**
 * A report made before Throughput was captured. Its summary counts the closed Work Items from its Cycle Time;
 * one that captured Throughput counts them from Throughput's total, which takes in every finished Work Item.
 */
const madeBeforeThroughput = (
	report: IReport = theEngagementStartReport(),
): IReport => {
	report.thenAndNow.metrics.throughput = undefined;
	return report;
};

const noneFinished = {
	value: null,
	sampleSize: 0,
	absentReason: "no-finished-items" as const,
};

const beforeEachReportTest = () => {
	reportUsage.mockClear();
	mockNavigate.mockClear();
	terms.current = { ...defaultTerms };
};

describe("The report header", () => {
	beforeEach(beforeEachReportTest);

	// @us-07 @slice-07 @driving_port @contract-shape:pure-function
	// Where and in what words the summary says how long ago Then ended waits for a sketch the maintainer has
	// seen; the lines naming Then as frozen and Now as the last days were dropped and are pinned as absent below.
	it.todo(
		"says how long ago Then ended, in the words of the sketch the maintainer approves",
	);

	// @us-01 @slice-01 @contract-shape:pure-function
	it("names the report on one line after Reports, which leads back to the list, with no large heading", async () => {
		openTheReport();

		await theReportIsReady();

		expect(
			readingOf(screen.getByRole("navigation", { name: "breadcrumb" })),
		).toMatch(new RegExp(`^Reports ?/ ?${ENGAGEMENT_START}$`));
		expect(screen.getByRole("link", { name: "Reports" })).toHaveAttribute(
			"href",
			`/teams/${LIGHTSPEED.id}/reports`,
		);
		expect(screen.queryByRole("heading", { level: 1 })).toBeNull();
	});
});

// Two windows of different lengths can end on the same day; without their lengths, 203 against 27 closed
// Work Items reads as a collapse rather than a shorter window.
describe("The report's summary header", () => {
	beforeEach(beforeEachReportTest);

	// @us-01 @slice-03 @driving_port @contract-shape:pure-function
	// A report made before Throughput was captured counts from its Cycle Time; the count from Throughput is slice 04's.
	it("opens with a summary of each side's window and how many Work Items closed in it", async () => {
		openTheReport(madeBeforeThroughput());

		await theReportIsReady();

		expect(readingOfSummary("then")).toMatch(
			/^THEN 3 May – 31 Jul 2026 · 90 days 38 Work Items closed$/i,
		);
		expect(readingOfSummary("now")).toMatch(
			/^NOW 5 Jul – 2 Oct 2026 · 90 days 41 Work Items closed$/i,
		);
	});

	// @us-01 @slice-03 @boundary @contract-shape:pure-function
	it.each([
		[
			"a Then and a Now within one year",
			aWindow("2026-07-16", "2026-10-13", 90),
			aWindow("2026-10-04", "2026-10-13", 10),
			"16 Jul – 13 Oct 2026 · 90 days",
			"4 Oct – 13 Oct 2026 · 10 days",
		],
		[
			"a Then reaching back into the year before",
			aWindow("2025-12-20", "2026-01-13", 25),
			aWindow("2026-01-01", "2026-01-13", 13),
			"20 Dec 2025 – 13 Jan 2026 · 25 days",
			"1 Jan – 13 Jan 2026 · 13 days",
		],
		[
			"a Then wholly in the year before Now",
			aWindow("2025-10-02", "2025-12-30", 90),
			aWindow("2025-12-31", "2026-01-13", 14),
			"2 Oct – 30 Dec 2025 · 90 days",
			"31 Dec 2025 – 13 Jan 2026 · 14 days",
		],
		[
			"a Then whose first day the server names apart from its length",
			aWindow("2026-06-01", "2026-07-31", 90),
			aWindow("2026-07-05", "2026-10-02", 90),
			"1 Jun – 31 Jul 2026 · 90 days",
			"5 Jul – 2 Oct 2026 · 90 days",
		],
	])(
		"names both windows and their lengths for %s, a year written once where both ends share it",
		async (_, thenWindow, nowWindow, thenLine, nowLine) => {
			openTheReport(withWindows(thenWindow, nowWindow));

			await theReportIsReady();

			expect(readingOfSummary("then")).toMatch(
				new RegExp(`^THEN ${thenLine} `, "i"),
			);
			expect(readingOfSummary("now")).toMatch(
				new RegExp(`^NOW ${nowLine} `, "i"),
			);
		},
	);

	// @us-01 @slice-03 @boundary @contract-shape:pure-function
	it("says one day and one Work Item in the singular", async () => {
		const report = withCycleTime({
			atThen: { p85: shown(21, 38), p70: shown(10, 38), p50: shown(5, 38) },
			atNow: { p85: shown(3, 1), p70: shown(3, 1), p50: shown(3, 1) },
		});
		report.thenAndNow.nowWindow = aWindow("2026-10-02", "2026-10-02", 1);
		openTheReport(madeBeforeThroughput(report));

		await theReportIsReady();

		expect(readingOfSummary("now")).toMatch(/ · 1 day 1 Work Item closed$/);
		expect(readingOfSummary("now")).not.toMatch(/days|Items/);
	});

	// @us-01 @slice-03 @error @contract-shape:pure-function
	it.each([
		[
			"the Work Items behind the first percentile that has any, the 85th recorded as never captured",
			{ p85: null, p70: shown(10, 4), p50: shown(5, 4) },
			"4 Work Items closed",
		],
		[
			"the Work Items of a side whose 85th was never kept",
			{ p70: shown(10, 38), p50: shown(5, 38) },
			"38 Work Items closed",
		],
		[
			"none for a side with no finished Work Items",
			{
				p85: aDash("no-finished-items"),
				p70: aDash("no-finished-items"),
				p50: aDash("no-finished-items"),
			},
			"0 Work Items closed",
		],
		[
			"none for a side whose percentiles rest on no Work Items",
			{ p85: noneFinished, p70: noneFinished, p50: noneFinished },
			"0 Work Items closed",
		],
	])("counts as closed in Then %s", async (_, atThen, countLine) => {
		openTheReport(madeBeforeThroughput(withThenCycleTime(atThen)));

		await theReportIsReady();

		expect(readingOfSummary("then")).toMatch(
			new RegExp(`^THEN 3 May – 31 Jul 2026 · 90 days ${countLine}$`, "i"),
		);
		expect(readingOfSummary("now")).toMatch(/ · 90 days 41 Work Items closed$/);
	});

	// @us-01 @slice-03 @error @contract-shape:pure-function
	it.each<[string, IReportMetric | null | undefined]>([
		["missing from the report", undefined],
		["recorded as never captured", null],
	])(
		"names both windows but no closed count when neither Throughput nor Cycle Time was captured, Cycle Time %s",
		async (_, cycleTime) => {
			const report = theEngagementStartReport({
				shownMetricKeys: ["throughput"],
			});
			report.thenAndNow.metrics["cycle-time"] = cycleTime;
			openTheReport(madeBeforeThroughput(report));

			await theReportIsReady();

			expect(readingOfSummary("then")).toMatch(
				/^THEN 3 May – 31 Jul 2026 · 90 days$/i,
			);
			expect(readingOfSummary("now")).toMatch(
				/^NOW 5 Jul – 2 Oct 2026 · 90 days$/i,
			);
			expect(readingOfTheSummary()).not.toContain("closed");
		},
	);

	// @us-01 @slice-03 @boundary @contract-shape:pure-function
	// Every report keeps its Cycle Time, so the count is there even when its panel is not.
	it("counts the closed Work Items even when the report does not show its Cycle Time panel", async () => {
		openTheReport(
			madeBeforeThroughput(
				theEngagementStartReport({ shownMetricKeys: ["throughput"] }),
			),
		);

		await theReportIsReady();

		expect(readingOfSummary("then")).toMatch(/ 38 Work Items closed$/);
		expect(readingOfSummary("now")).toMatch(/ 41 Work Items closed$/);
	});

	// @us-03 @us-04 @slice-04 @driving_port @contract-shape:pure-function
	// Throughput counts every finished Work Item, Cycle Time only those whose cycle time it can measure. Once a
	// report captured Throughput, the summary reads its total, so the summary and the Throughput panel agree.
	it.each<[string, (report: IReport) => IReport, string, string]>([
		[
			"a report that captured both",
			(report) => report,
			"52 Work Items closed",
			"65 Work Items closed",
		],
		[
			"a report that does not show its Throughput panel",
			(report) => ({ ...report, shownMetricKeys: ["cycle-time"] }),
			"52 Work Items closed",
			"65 Work Items closed",
		],
		[
			"a report whose Cycle Time was never captured",
			(report) => {
				report.thenAndNow.metrics["cycle-time"] = null;
				return report;
			},
			"52 Work Items closed",
			"65 Work Items closed",
		],
		[
			"a Then where nothing finished",
			(report) => {
				report.thenAndNow.metrics.throughput = {
					atThen: { total: shown(0, 90), perDay: shown(0, 90) },
					atNow: { total: shown(65, 90), perDay: shown(0.72, 90) },
				};
				report.thenAndNow.metrics["cycle-time"] = {
					atThen: {
						p85: aDash("no-finished-items"),
						p70: aDash("no-finished-items"),
						p50: aDash("no-finished-items"),
					},
					atNow: { p85: shown(12, 41), p70: shown(7, 41), p50: shown(4, 41) },
				};
				return report;
			},
			"0 Work Items closed",
			"65 Work Items closed",
		],
	])(
		"counts as closed the Work Items Throughput finished, for %s",
		async (_, shape, thenLine, nowLine) => {
			openTheReport(shape(theEngagementStartReport()));

			await theReportIsReady();

			expect(readingOfSummary("then")).toMatch(
				new RegExp(`^THEN 3 May – 31 Jul 2026 · 90 days ${thenLine}$`, "i"),
			);
			expect(readingOfSummary("now")).toMatch(
				new RegExp(`^NOW 5 Jul – 2 Oct 2026 · 90 days ${nowLine}$`, "i"),
			);
		},
	);

	// @us-01 @slice-03 @contract-shape:pure-function
	it("names the closed Work Items in the instance's words", async () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.WORK_ITEM]: "Ticket",
			[TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets",
		};
		const report = theEngagementStartReport();
		report.thenAndNow.metrics["cycle-time"] = {
			atThen: { p85: shown(21, 38), p70: shown(10, 38), p50: shown(5, 38) },
			atNow: { p85: shown(3, 1), p70: shown(3, 1), p50: shown(3, 1) },
		};
		openTheReport(madeBeforeThroughput(report));

		await theReportIsReady();

		expect(readingOfSummary("then")).toMatch(/ · 90 days 38 Tickets closed$/);
		expect(readingOfSummary("now")).toMatch(/ · 90 days 1 Ticket closed$/);
		expect(readingOfTheSummary()).not.toContain("Work Item");
	});

	// @us-01 @slice-03 @contract-shape:pure-function
	it("says nothing of Then being frozen or of Now always being the last days", async () => {
		openTheReport();

		await theReportIsReady();

		expect(readingOfTheSummary()).not.toMatch(/frozen|always|last \d+ days/i);
	});

	// @us-01 @slice-03 @contract-shape:pure-function
	it("sits between the line naming the report and the first panel, inside the report's loading look", async () => {
		openTheReport();

		const firstPanel = await thePanel(CYCLE_TIME_PANEL);
		const breadcrumb = screen.getByRole("navigation", { name: "breadcrumb" });
		const summary = theSummary();

		expect(summary).not.toBeNull();
		expect(
			breadcrumb.compareDocumentPosition(summary as HTMLElement) &
				Node.DOCUMENT_POSITION_FOLLOWING,
		).toBeTruthy();
		expect(
			(summary as HTMLElement).compareDocumentPosition(firstPanel) &
				Node.DOCUMENT_POSITION_FOLLOWING,
		).toBeTruthy();
		expect(screen.getByTestId("widget-shell-body-report")).toContainElement(
			summary,
		);
	});

	// @us-01 @slice-03 @boundary @ac-1.8 @contract-shape:pure-function
	it("shows no summary while the report is on its way, and shows it once the report is here", async () => {
		const held = deferred<IReport>();
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{
				reportService: aReportService({
					getReport: vi.fn().mockReturnValue(held.promise),
				}),
			},
		);

		await screen.findByText(LOADING);

		expect(theSummary()).toBeNull();
		expect(readingOf(document.body)).not.toMatch(/closed|\d+ days/);

		await act(async () => held.resolve(theEngagementStartReport()));
		await theReportIsReady();

		for (const side of SIDES) {
			expect(readingOfSummary(side)).toMatch(
				/ · 90 days \d+ Work Items closed$/,
			);
		}
	});

	// @us-01 @slice-03 @error @ac-1.8 @contract-shape:pure-function
	it.each([
		["could not be loaded", new Error("the network dropped"), COULD_NOT_LOAD],
		[
			"was made by a newer version",
			aRefusal(422, "template-not-supported"),
			MADE_BY_A_NEWER_VERSION,
		],
	])("shows no summary when the report %s", async (_, failure, message) => {
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{
				reportService: aReportService({
					getReport: vi.fn().mockRejectedValue(failure),
				}),
			},
		);

		expect(await screen.findByText(message)).toBeVisible();
		expect(theSummary()).toBeNull();
		expect(readingOf(document.body)).not.toMatch(/closed|\d+ days/);
	});
});

describe("The panels of a report", () => {
	beforeEach(beforeEachReportTest);

	// @us-01 @slice-01 @driving_port @contract-shape:pure-function
	// The closed Work Items are counted once, in the summary above, not again on each panel.
	it("shows the Cycle Time panel as Then, Now and Change, each side headed by its name alone", async () => {
		openTheReport();

		const panel = await thePanel(CYCLE_TIME_PANEL);
		const then = readingOfSegment(panel, "then");
		const now = readingOfSegment(panel, "now");

		expect(then).toMatch(/^THEN 85th /i);
		expect(then).toMatch(/85th 21 days.*70th 10 days.*50th 5 days$/);
		expect(now).toMatch(/^NOW 85th /i);
		expect(now).toMatch(/85th 12 days.*70th 7 days.*50th 4 days$/);
		expect(readingOf(panel)).not.toContain("Work Items");
		expect(readingOfSegment(panel, "change")).toMatch(/^CHANGE /i);
	});

	// @us-01 @slice-01 @contract-shape:pure-function
	// The same icons as the Metrics tab's Cycle Time percentiles, so a reader knows the 50th is a coin toss.
	it("marks every percentile on both sides with the certainty icon the Metrics tab shows for it", async () => {
		openTheReport();

		const panel = await thePanel(CYCLE_TIME_PANEL);

		expect(certaintyIconsIn(theSegment(panel, "then"))).toEqual(
			CERTAINTY_OF_85TH_70TH_50TH,
		);
		expect(certaintyIconsIn(theSegment(panel, "now"))).toEqual(
			CERTAINTY_OF_85TH_70TH_50TH,
		);
	});

	// @us-01 @slice-01 @contract-shape:pure-function
	// Colour alone tells nobody who cannot see it; the sign and the arrow say the same.
	it("tells a shorter Cycle Time as better on every percentile by arrow and sign as well as colour", async () => {
		openTheReport();

		const panel = await thePanel(CYCLE_TIME_PANEL);
		const changes = changesIn(theSegment(panel, "change"));

		expect(changes.map(readingOf)).toEqual([
			"▼ 9 days (−43 %)",
			"▼ 3 days (−30 %)",
			"▼ 1 day (−20 %)",
		]);
		expect(changes.map((change) => change.dataset.change)).toEqual([
			"better",
			"better",
			"better",
		]);
	});

	// @us-01 @slice-01 @boundary @contract-shape:pure-function
	it("reads a percentile that did not move as No change, and a longer one as worse", async () => {
		openTheReport(
			withCycleTime({
				atThen: { p85: shown(15, 181), p70: shown(9, 181), p50: shown(5, 181) },
				atNow: { p85: shown(12, 203), p70: shown(9, 203), p50: shown(6, 203) },
			}),
		);

		const panel = await thePanel(CYCLE_TIME_PANEL);
		const worse = changesIn(theSegment(panel, "change")).filter(
			(change) => change.dataset.change === "worse",
		);

		expect(readingOfSegment(panel, "change")).toMatch(
			/^CHANGE ▼ 3 days \(−20 %\) No change ▲ 1 day \(\+20 %\)$/i,
		);
		expect(worse.map(readingOf)).toEqual(["▲ 1 day (+20 %)"]);
	});

	// @us-04 @slice-04 @contract-shape:pure-function
	it("shows Throughput as a total and a per-day average, compared per day", async () => {
		openTheReport();

		const panel = await thePanel(THROUGHPUT_PANEL);
		const then = readingOfSegment(panel, "then");

		expect(then).toMatch(/^THEN Total 52 Work Items /i);
		expect(then).toMatch(/ 0\.58 per day$/);
		expect(readingOfSegment(panel, "now")).toMatch(
			/^NOW Total 65 Work Items .*0\.72 per day$/i,
		);
		expect(readingOf(panel)).not.toMatch(/\d+ days|limit|beyond/i);
		expect(changesIn(panel)).toHaveLength(1);
		expect(readingOf(changesIn(panel)[0])).toBe("▲ 0.14 per day (+24 %)");
		expect(changesIn(panel)[0].dataset.change).toBe("better");
	});

	// @us-05 @slice-05 @contract-shape:pure-function
	// The average 8.62 shows as 9 and 7.38 as 7, and the change is worked out from what is shown.
	it.skip("shows WIP as its average, median, min and max, each with its own change, lower reading as better", async () => {
		openTheReport();

		const panel = await thePanel(WIP_PANEL);
		const changes = changesIn(theSegment(panel, "change"));

		expect(readingOfSegment(panel, "then")).toMatch(
			/^THEN Average 9 Median 9 Min 6 Max 12$/i,
		);
		expect(readingOfSegment(panel, "now")).toMatch(
			/^NOW Average 7 Median 7 Min 5 Max 9$/i,
		);
		expect(changes.map(readingOf)).toEqual([
			"▼ 2 (−22 %)",
			"▼ 2 (−22 %)",
			"▼ 1 (−17 %)",
			"▼ 3 (−25 %)",
		]);
		expect(changes.map((change) => change.dataset.change)).toEqual([
			"better",
			"better",
			"better",
			"better",
		]);
	});

	// @us-05 @slice-05 @contract-shape:pure-function
	// Colour alone tells nobody who cannot see it; the sign and the arrow say the same.
	it.skip("tells a growing Total Work Item Age as worse on every line by sign and arrow as well as colour", async () => {
		openTheReport();

		const panel = await thePanel(AGE_PANEL);
		const changes = changesIn(theSegment(panel, "change"));

		expect(readingOfSegment(panel, "then")).toMatch(
			/^THEN Average 610 days Median 600 days Min 420 days Max 760 days$/i,
		);
		expect(readingOfSegment(panel, "now")).toMatch(
			/^NOW Average 880 days Median 870 days Min 640 days Max 990 days$/i,
		);
		expect(changes.map(readingOf)).toEqual([
			"▲ 270 days (+44 %)",
			"▲ 270 days (+45 %)",
			"▲ 220 days (+52 %)",
			"▲ 230 days (+30 %)",
		]);
		expect(changes.map((change) => change.dataset.change)).toEqual([
			"worse",
			"worse",
			"worse",
			"worse",
		]);
	});

	// @us-04 @slice-04 @contract-shape:pure-function
	it("stacks only the shown metrics, in the catalog's order whatever order they were ticked in", async () => {
		openTheReport(
			theEngagementStartReport({
				shownMetricKeys: ["throughput", "cycle-time"],
			}),
		);

		await theReportIsReady();

		expect(thePanelTitles()).toEqual([CYCLE_TIME_PANEL, THROUGHPUT_PANEL]);
	});

	// @us-01 @slice-01 @contract-shape:pure-function
	it("names every metric and the Work Items in the instance's words", async () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.CYCLE_TIME]: "Lead Time",
			[TERMINOLOGY_KEYS.WORK_ITEM]: "Ticket",
			[TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets",
		};
		openTheReport();

		await thePanel("Lead Time");

		for (const side of SIDES) {
			expect(readingOfSummary(side)).toMatch(/ \d+ Tickets closed$/);
		}
		expect(readingOf(document.body)).not.toContain("Cycle Time");
		expect(readingOf(document.body)).not.toContain("Work Items");
	});
});

describe("Values a report cannot show", () => {
	beforeEach(beforeEachReportTest);

	// @us-01 @slice-01 @error @contract-shape:pure-function
	it("shows a dash and says Then had no finished Work Items", async () => {
		openTheReport(
			withCycleTime({
				atThen: {
					p85: aDash("no-finished-items"),
					p70: aDash("no-finished-items"),
					p50: aDash("no-finished-items"),
				},
				atNow: { p85: shown(12, 41), p70: shown(7, 41), p50: shown(4, 41) },
			}),
		);

		const panel = await thePanel(CYCLE_TIME_PANEL);
		const then = readingOfSegment(panel, "then");

		expect(then).toContain("No finished Work Items in Then");
		expect(then).toMatch(/85th —.*70th —.*50th —/);
		expect(then).not.toMatch(/\d+ days/);
		expect(changesIn(panel)).toHaveLength(0);
	});

	// @us-04 @slice-04 @error @contract-shape:pure-function
	// A rise from nothing has no percentage; the difference itself still shows.
	it("shows no percentage for a change from a Then of zero", async () => {
		const report = theEngagementStartReport();
		report.thenAndNow.metrics.throughput = {
			atThen: { total: shown(0, 90), perDay: shown(0, 90) },
			atNow: { total: shown(24, 90), perDay: shown(0.27, 90) },
		};
		openTheReport(report);

		const panel = readingOf(await thePanel(THROUGHPUT_PANEL));

		expect(panel).toContain("Total 0 Work Items");
		expect(panel).toContain("▲ 0.27 per day (Then was 0)");
		expect(panel).not.toMatch(/%|Infinity|NaN|—/);
	});

	// @us-04 @slice-04 @contract-shape:pure-function
	it("names a single finished Work Item in the singular", async () => {
		const report = theEngagementStartReport();
		report.thenAndNow.metrics.throughput = {
			atThen: { total: shown(1, 90), perDay: shown(0.01, 90) },
			atNow: { total: shown(2, 90), perDay: shown(0.02, 90) },
		};
		openTheReport(report);

		const panel = await thePanel(THROUGHPUT_PANEL);

		expect(readingOfSegment(panel, "then")).toMatch(
			/^THEN Total 1 Work Item /i,
		);
		expect(readingOfSegment(panel, "now")).toMatch(/^NOW Total 2 Work Items /i);
	});

	// @us-05 @slice-05 @error @contract-shape:pure-function
	// A day with nothing in progress is a real 0 for both metrics; only the percentage from a Then of 0 is missing.
	it.skip("shows every WIP and Total Work Item Age line as a real 0 when Then had nothing in progress", async () => {
		const zero = shown(0, 90);
		const report = theEngagementStartReport();
		report.thenAndNow.metrics.wip = {
			atThen: { average: zero, median: zero, min: zero, max: zero },
			atNow: {
				average: shown(7, 90),
				median: shown(7, 90),
				min: shown(5, 90),
				max: shown(9, 90),
			},
		};
		report.thenAndNow.metrics["work-item-age"] = {
			atThen: { average: zero, median: zero, min: zero, max: zero },
			atNow: {
				average: shown(880, 90),
				median: shown(870, 90),
				min: shown(640, 90),
				max: shown(990, 90),
			},
		};
		openTheReport(report);

		const wip = await thePanel(WIP_PANEL);
		const age = await thePanel(AGE_PANEL);

		expect(readingOfSegment(wip, "then")).toMatch(
			/^THEN Average 0 Median 0 Min 0 Max 0$/i,
		);
		expect(readingOfSegment(age, "then")).toMatch(
			/^THEN Average 0 days Median 0 days Min 0 days Max 0 days$/i,
		);
		for (const panel of [wip, age]) {
			const changes = changesIn(theSegment(panel, "change")).map(readingOf);
			expect(changes).toHaveLength(4);
			for (const change of changes) {
				expect(change).toMatch(/^▲ .*\(Then was 0\)$/);
			}
			expect(readingOf(panel)).not.toMatch(/%|—|in progress/);
		}
	});

	// @us-05 @slice-05 @boundary @contract-shape:unbounded-preservation
	// A report made before WIP and Work Item Age were captured has nothing frozen for them.
	it.skip("says WIP and Total Work Item Age were not captured for a report made before they existed", async () => {
		const report = theEngagementStartReport();
		report.thenAndNow.metrics.wip = undefined;
		report.thenAndNow.metrics["work-item-age"] = undefined;
		openTheReport(report);

		for (const title of [WIP_PANEL, AGE_PANEL]) {
			const panel = await thePanel(title);
			expect(readingOf(panel)).toContain(NOT_CAPTURED);
			expect(readingOf(panel)).not.toMatch(/Average \d/);
		}
		expect(readingOfSegment(await thePanel(CYCLE_TIME_PANEL), "then")).toMatch(
			/^THEN 85th 21 days/i,
		);
	});

	// @us-01 @slice-01 @boundary @contract-shape:pure-function
	// Thin data is shown, not hidden: the number of Work Items behind it, in the summary above, lets the reader judge.
	it("shows a value backed by very few Work Items, with that number in the summary above", async () => {
		openTheReport(
			madeBeforeThroughput(
				withCycleTime({
					atThen: { p85: shown(9, 2), p70: shown(9, 2), p50: shown(4, 2) },
					atNow: { p85: shown(5, 3), p70: shown(5, 3), p50: shown(3, 3) },
				}),
			),
		);

		const panel = await thePanel(CYCLE_TIME_PANEL);

		expect(readingOfSegment(panel, "then")).toMatch(/^THEN 85th 9 days/i);
		expect(readingOfSegment(panel, "now")).toMatch(/^NOW 85th 5 days/i);
		expect(readingOfSummary("then")).toMatch(/ 2 Work Items closed$/);
		expect(readingOfSummary("now")).toMatch(/ 3 Work Items closed$/);
	});

	// @us-04 @slice-04 @error @contract-shape:unbounded-preservation
	// A later version may add a metric this one has no panel for; the rest of the report still reads.
	it("puts a plain note in place of a metric this version cannot show, and shows the others", async () => {
		const report = theEngagementStartReport({
			shownMetricKeys: ["cycle-time", A_LATER_METRIC],
		});
		report.thenAndNow.metrics[A_LATER_METRIC] = {
			atThen: { p85: shown(30, 12) },
			atNow: { p85: shown(20, 12) },
		};
		openTheReport(report);
		await theReportIsReady();

		expect(await thePanel(CYCLE_TIME_PANEL)).toBeVisible();
		expect(screen.getByText(METRIC_FROM_A_NEWER_VERSION)).toBeVisible();
		expect(screen.queryByText(COULD_NOT_LOAD)).toBeNull();
	});
});

describe("A report on a slow or failing connection", () => {
	beforeEach(beforeEachReportTest);

	// @us-01 @slice-01 @boundary @ac-1.8 @contract-shape:pure-function
	it("shows only the way back and a spinner while the report is on its way", async () => {
		const held = deferred<IReport>();
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{
				reportService: aReportService({
					getReport: vi.fn().mockReturnValue(held.promise),
				}),
			},
		);

		expect(await screen.findByText(LOADING)).toBeVisible();
		expect(screen.getByRole("progressbar")).toBeVisible();
		expect(screen.getByRole("link", { name: "Reports" })).toBeVisible();
		expect(screen.queryByRole("region")).toBeNull();
		expect(document.querySelector('[data-report-status="ready"]')).toBeNull();
	});

	// @us-01 @slice-01 @error @ac-1.8 @contract-shape:pure-function
	it("says the report could not be loaded and offers Retry, showing no numbers", async () => {
		const getReport = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockResolvedValue(theEngagementStartReport());
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{ reportService: aReportService({ getReport }) },
		);

		expect(await screen.findByText(COULD_NOT_LOAD)).toBeVisible();
		expect(screen.queryByRole("region")).toBeNull();

		await userEvent.click(screen.getByRole("button", { name: "Retry" }));

		expect(await thePanel(CYCLE_TIME_PANEL)).toBeVisible();
	});

	// @us-08 @slice-08 @error @ac-1.8 @contract-shape:pure-function
	// Deleted, or another owner's: either way there is nothing here to show.
	it.skip("says a report that is gone no longer exists and leads back to the list", async () => {
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{
				reportService: aReportService({
					getReport: vi.fn().mockRejectedValue(aRefusal(404, "not-found")),
				}),
			},
		);

		expect(await screen.findByText(NO_LONGER_EXISTS)).toBeVisible();
		expect(
			screen.getByRole("link", { name: "Back to Reports" }),
		).toHaveAttribute("href", `/teams/${LIGHTSPEED.id}/reports`);
		expect(screen.queryByText(COULD_NOT_LOAD)).toBeNull();
	});

	// @us-01 @slice-01 @error @contract-shape:pure-function
	// An instance rolled back from a later version still holds that version's reports.
	it("says a report made by a newer version cannot be shown here and leads back to the list", async () => {
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{
				reportService: aReportService({
					getReport: vi
						.fn()
						.mockRejectedValue(aRefusal(422, "template-not-supported")),
				}),
			},
		);

		expect(await screen.findByText(MADE_BY_A_NEWER_VERSION)).toBeVisible();
		expect(
			screen.getByRole("link", { name: "Back to Reports" }),
		).toHaveAttribute("href", `/teams/${LIGHTSPEED.id}/reports`);
		expect(screen.queryByText(COULD_NOT_LOAD)).toBeNull();
		expect(screen.queryByRole("button", { name: "Retry" })).toBeNull();
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it("never lets a late answer for the report opened before land on the one opened now", async () => {
		const first = deferred<IReport>();
		const getReport = vi
			.fn()
			.mockImplementation((_owner, reportId: number) =>
				reportId === ENGAGEMENT_START_ID
					? first.promise
					: Promise.resolve(
							theEngagementStartReport({ id: reportId, name: "Since August" }),
						),
			);
		const { rerenderWith } = renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{ reportService: aReportService({ getReport }) },
		);

		rerenderWith(<ReportView owner={LIGHTSPEED} reportId={43} />);
		await theReportIsReady();
		await act(async () => first.resolve(theEngagementStartReport()));

		expect(screen.getByText("Since August")).toBeVisible();
		expect(screen.queryByText(ENGAGEMENT_START)).toBeNull();
	});

	it("answers a report made by a newer version at once, asking only once", async () => {
		const getReport = vi
			.fn()
			.mockRejectedValue(aRefusal(422, "template-not-supported"));
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{ reportService: aReportService({ getReport }), retry: 2 },
		);

		expect(await screen.findByText(MADE_BY_A_NEWER_VERSION)).toBeVisible();
		expect(getReport).toHaveBeenCalledTimes(1);
	});

	it("answers a report that is gone at once, asking only once", async () => {
		const getReport = vi.fn().mockRejectedValue(aRefusal(404, "not-found"));
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{ reportService: aReportService({ getReport }), retry: 2 },
		);

		expect(await screen.findByText(COULD_NOT_LOAD)).toBeVisible();
		expect(getReport).toHaveBeenCalledTimes(1);
	});

	it("asks again when a read fails for any other reason", async () => {
		const getReport = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockResolvedValue(theEngagementStartReport());
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{ reportService: aReportService({ getReport }), retry: 2 },
		);

		expect(
			await screen.findByText(ENGAGEMENT_START, undefined, { timeout: 3000 }),
		).toBeVisible();
		expect(getReport).toHaveBeenCalledTimes(2);
	});

	it("asks again when the server fails to answer", async () => {
		const getReport = vi
			.fn()
			.mockRejectedValueOnce(aRefusal(500, "unexpected"))
			.mockResolvedValue(theEngagementStartReport());
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{ reportService: aReportService({ getReport }), retry: 2 },
		);

		expect(
			await screen.findByText(ENGAGEMENT_START, undefined, { timeout: 3000 }),
		).toBeVisible();
		expect(getReport).toHaveBeenCalledTimes(2);
	});

	it("asks no more often than the instance's usual retries before saying the report could not be loaded", async () => {
		const getReport = vi
			.fn()
			.mockRejectedValue(new Error("the network dropped"));
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{ reportService: aReportService({ getReport }), retry: 1 },
		);

		expect(
			await screen.findByText(COULD_NOT_LOAD, undefined, { timeout: 3000 }),
		).toBeVisible();
		expect(getReport).toHaveBeenCalledTimes(2);
	});

	it.each([
		["a 422 for another reason", aRefusal(422, "then-too-short")],
		[
			"another status naming the newer Type",
			aRefusal(400, "template-not-supported"),
		],
	])(
		"offers Retry, not the way back, when the report fails with %s",
		async (_, refusal) => {
			renderWithReports(
				<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
				{
					reportService: aReportService({
						getReport: vi.fn().mockRejectedValue(refusal),
					}),
				},
			);

			expect(await screen.findByText(COULD_NOT_LOAD)).toBeVisible();
			expect(screen.getByRole("button", { name: "Retry" })).toBeVisible();
			expect(
				screen.queryByRole("link", { name: "Back to Reports" }),
			).toBeNull();
		},
	);

	it("says it is busy while the report is on its way, and no longer once it is shown", async () => {
		const held = deferred<IReport>();
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{
				reportService: aReportService({
					getReport: vi.fn().mockReturnValue(held.promise),
				}),
			},
		);

		await screen.findByText(LOADING);
		expect(document.querySelector('[aria-busy="true"]')).not.toBeNull();
		expect(
			within(
				screen.getByRole("navigation", { name: "breadcrumb" }),
			).getAllByRole("listitem"),
		).toHaveLength(1);

		await act(async () => held.resolve(theEngagementStartReport()));
		await theReportIsReady();

		expect(document.querySelector("[aria-busy]")).toBeNull();
	});
});

describe("Who may change a report from its page", () => {
	beforeEach(beforeEachReportTest);

	// @us-10 @us-08 @slice-08 @slice-10 @contract-shape:pure-function
	it.skip("offers an admin Edit report and Delete", async () => {
		openTheReport(theEngagementStartReport(), "admin");

		await theReportIsReady();

		expect(screen.getByRole("button", { name: "Edit report" })).toBeEnabled();
		expect(screen.getByRole("button", { name: "Delete" })).toBeEnabled();
	});

	// @us-10 @us-08 @slice-08 @slice-10 @error @contract-shape:unbounded-preservation
	it.skip("offers a reader neither", async () => {
		openTheReport(theEngagementStartReport(), "reader");

		await theReportIsReady();

		expect(screen.queryByRole("button", { name: "Edit report" })).toBeNull();
		expect(screen.queryByRole("button", { name: "Delete" })).toBeNull();
	});

	// @us-08 @slice-08 @driving_port @kpi-OUT-5878-guardrail-quick-deletes @contract-shape:bounded-change
	it.skip("deletes the report after asking, warns when its Then cannot be rebuilt, and goes back to the list", async () => {
		const reportService = openTheReport(
			theEngagementStartReport({
				thenAndNow: {
					...theEngagementStartReport().thenAndNow,
					thenRebuildable: false,
				},
			}),
			"admin",
		);
		await theReportIsReady();

		await userEvent.click(screen.getByRole("button", { name: "Delete" }));
		const dialog = await screen.findByRole("dialog");
		expect(dialog).toHaveTextContent(CANNOT_BE_REBUILT);
		await userEvent.click(
			within(dialog).getByRole("button", { name: "Delete" }),
		);

		await waitFor(() =>
			expect(mockNavigate).toHaveBeenCalledWith(
				`/teams/${LIGHTSPEED.id}/reports`,
			),
		);
		expect(reportService.deleteReport).toHaveBeenCalledWith(
			LIGHTSPEED,
			ENGAGEMENT_START_ID,
		);
		expect(reportUsage).toHaveBeenCalledWith({ name: "ReportDeleted" });
	});
});

describe("What opening a report reports", () => {
	beforeEach(() => {
		beforeEachReportTest();
		vi.useFakeTimers({ shouldAdvanceTime: true });
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	const reportsOfOpening = () =>
		reportUsage.mock.calls.filter(([use]) => use.name === "ReportOpened");

	// @us-01 @slice-01 @kpi-OUT-5878-K2-reports-reopened @contract-shape:bounded-change
	it("reports one report opened, with its Type, once it has been open for five seconds", async () => {
		openTheReport();
		await theReportIsReady();

		await act(() => vi.advanceTimersByTimeAsync(4_900));
		expect(reportsOfOpening()).toHaveLength(0);

		await act(() => vi.advanceTimersByTimeAsync(200));
		expect(reportsOfOpening()).toEqual([
			[{ name: "ReportOpened", reportTemplate: "ThenAndNow" }],
		]);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	// A glance that leaves at once is not somebody reading the report.
	it("reports nothing for a report left within five seconds", async () => {
		const { unmount } = renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{ reportService: aReportService() },
		);
		await theReportIsReady();

		await act(() => vi.advanceTimersByTimeAsync(3_000));
		unmount();
		await act(() => vi.advanceTimersByTimeAsync(5_000));

		expect(reportsOfOpening()).toHaveLength(0);
	});

	// @us-01 @slice-01 @error @contract-shape:unbounded-preservation
	it("reports nothing for a report that could not be loaded", async () => {
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{
				reportService: aReportService({
					getReport: vi
						.fn()
						.mockRejectedValue(new Error("the network dropped")),
				}),
			},
		);
		await screen.findByText(COULD_NOT_LOAD);

		await act(() => vi.advanceTimersByTimeAsync(10_000));

		expect(reportsOfOpening()).toHaveLength(0);
	});
});
