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

/** The icons the Metrics tab's Cycle Time percentiles carry for the 95th, 85th, 70th and 50th. */
const CERTAINTY_OF_EVERY_PERCENTILE = [
	"CheckCircleIcon",
	"CheckCircleOutlineOutlinedIcon",
	"QueryBuilderIcon",
	"ErrorOutlineOutlinedIcon",
];

/** The average, median, min and max of WIP and Total Work Item Age, in that order. */
const SPREAD_ICONS = [
	"DragHandleIcon",
	"VerticalAlignCenterIcon",
	"VerticalAlignBottomIcon",
	"VerticalAlignTopIcon",
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

const iconsIn = (segment: HTMLElement) =>
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

type SummarySide = Side | "change";

const theSummary = (): HTMLElement | null =>
	document.querySelector<HTMLElement>("[data-report-summary]");

const readingOfTheSummary = (): string => {
	const found = theSummary();
	expect(found, "the report has no summary").not.toBeNull();
	return readingOf(found as HTMLElement);
};

/** One side of the summary as read aloud; its lines end in a space, as a panel's do, so they read apart. */
const readingOfSummary = (side: SummarySide): string => {
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
			[TERMINOLOGY_KEYS.WORK_ITEM_AGE]: "Ticket Age",
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

const theChangeGroups = (): HTMLElement[] =>
	Array.from(
		theSummary()?.querySelectorAll<HTMLElement>(
			'[data-summary-side="change"] [data-change-kind]',
		) ?? [],
	);

/** The kind of each group on the CHANGE side and how it reads, in the order they are shown. */
const kindsAndReadingsOfTheChange = () =>
	theChangeGroups().map((group) => [
		group.dataset.changeKind,
		readingOf(group),
	]);

/** The colour the mark of a group is drawn in: the innermost element whose own text holds the mark. */
const colourOfTheMark = (group: HTMLElement, mark: string): string => {
	const holders = [group, ...group.querySelectorAll<HTMLElement>("*")].filter(
		(element) =>
			Array.from(element.childNodes).some(
				(node) =>
					node.nodeType === Node.TEXT_NODE && node.textContent?.includes(mark),
			),
	);
	expect(holders, `no element draws the mark ${mark}`).not.toHaveLength(0);
	const [innermost] = holders.slice(-1);
	return getComputedStyle(innermost).color;
};

/** Then's and Now's 95th, 85th, 70th and 50th, in days, over the same number of finished Work Items. */
const percentiles = (
	[p95, p85, p70, p50]: readonly [number, number, number, number],
	sampleSize: number,
): ReportMetricSide => ({
	p95: shown(p95, sampleSize),
	p85: shown(p85, sampleSize),
	p70: shown(p70, sampleSize),
	p50: shown(p50, sampleSize),
});

const nothingFinishedOnAnyPercentile: ReportMetricSide = {
	p95: aDash("no-finished-items"),
	p85: aDash("no-finished-items"),
	p70: aDash("no-finished-items"),
	p50: aDash("no-finished-items"),
};

const showing = (
	report: IReport,
	shownMetricKeys: IReport["shownMetricKeys"],
): IReport => ({ ...report, shownMetricKeys });

const withThroughput = (
	report: IReport,
	[thenTotal, thenPerDay]: readonly [number, number],
	[nowTotal, nowPerDay]: readonly [number, number],
): IReport => {
	report.thenAndNow.metrics.throughput = {
		atThen: { total: shown(thenTotal, 90), perDay: shown(thenPerDay, 90) },
		atNow: { total: shown(nowTotal, 90), perDay: shown(nowPerDay, 90) },
	};
	return report;
};

/** Every WIP line in Now as it was in Then. */
const withWipUnmoved = (report: IReport): IReport => {
	const wip = report.thenAndNow.metrics.wip as IReportMetric;
	report.thenAndNow.metrics.wip = { atThen: wip.atThen, atNow: wip.atThen };
	return report;
};

const SUCCESS_COLOUR = "rgb(46, 125, 50)";
const ERROR_COLOUR = "rgb(211, 47, 47)";
const SECONDARY_TEXT_COLOUR = "rgba(0, 0, 0, 0.6)";

// The header names, per shown metric, which way it moved by the marks its own panel shows, so a reader knows what
// changed before reaching the first panel, and a metric whose slow end worsened while its fast end improved is not
// passed off as an improvement.
describe("The summary's CHANGE side", () => {
	beforeEach(beforeEachReportTest);

	// @us-05b @slice-05b @driving_port @contract-shape:pure-function
	it("reads Then, Now and Change side by side, like the panels", async () => {
		openTheReport();

		await theReportIsReady();

		const sides = Array.from(
			theSummary()?.querySelectorAll<HTMLElement>("[data-summary-side]") ?? [],
		).map((side) => side.dataset.summarySide);
		expect(sides).toEqual(["then", "now", "change"]);
		expect(readingOfSummary("change")).toMatch(/^CHANGE /i);
	});

	// @us-05b @slice-05b @walking_skeleton @driving_port @contract-shape:pure-function
	// Every Cycle Time line, Throughput per day and every WIP line are better in Now; every Total Work Item Age
	// line is worse. Throughput's total carries no change mark and does not count.
	it("names the metrics that improved and the one that got worse", async () => {
		openTheReport();

		await theReportIsReady();

		expect(readingOfSummary("change")).toMatch(
			/^CHANGE ✔ Improved: Cycle Time, Throughput, WIP ✘ Got worse: Total Work Item Age$/i,
		);
		expect(kindsAndReadingsOfTheChange()).toEqual([
			["improved", "✔ Improved: Cycle Time, Throughput, WIP"],
			["worse", "✘ Got worse: Total Work Item Age"],
		]);
	});

	// @us-05b @slice-05b @contract-shape:pure-function
	// The spread widened: a majority rule would hide either that the typical Work Item got faster or the slow tail.
	it("reads Mixed for a metric whose slow end got worse while its fast end improved", async () => {
		openTheReport(
			withCycleTime({
				atThen: percentiles([25, 14, 9, 5], 30),
				atNow: percentiles([31, 17, 9, 4], 33),
			}),
		);

		await theReportIsReady();

		expect(readingOfSummary("change")).toMatch(
			/^CHANGE ✔ Improved: Throughput, WIP ✘ Got worse: Total Work Item Age ± Mixed: Cycle Time$/i,
		);
	});

	// @us-05b @slice-05b @boundary @contract-shape:pure-function
	// No threshold: a one-day move counts, and a metric none of whose lines moved is said to be unchanged.
	it("counts a one-day move, and reads Unchanged for a metric that did not move", async () => {
		const report = withCycleTime({
			atThen: percentiles([12, 10, 7, 5], 30),
			atNow: percentiles([12, 10, 8, 5], 30),
		});
		openTheReport(showing(withWipUnmoved(report), ["cycle-time", "wip"]));

		await theReportIsReady();

		expect(readingOfSummary("change")).toMatch(
			/^CHANGE ✘ Got worse: Cycle Time – Unchanged: WIP$/i,
		);
	});

	// @us-05b @slice-05b @boundary @contract-shape:pure-function
	// The header judges a line as its panel shows it: a move the panel cannot show is no move, the smallest it can is one.
	it.each<[string, number, number, string]>([
		[
			"a hundredth of a Work Item more per day",
			0.58,
			0.59,
			"✔ Improved: Throughput",
		],
		[
			"a move too small for the panel to show",
			0.584,
			0.576,
			"– Unchanged: Throughput",
		],
	])(
		"judges %s as the panel shows it",
		async (_, thenPerDay, nowPerDay, group) => {
			openTheReport(
				showing(
					withThroughput(
						theEngagementStartReport(),
						[52, thenPerDay],
						[53, nowPerDay],
					),
					["throughput"],
				),
			);

			await theReportIsReady();

			expect(readingOfSummary("change")).toMatch(
				new RegExp(`^CHANGE ${group}$`, "i"),
			);
		},
	);

	// @us-05b @slice-05b @error @contract-shape:pure-function
	// Team Pulsar finished nothing in its Now, so every Cycle Time line reads "—" there and cannot be compared.
	it("leaves out a metric none of whose lines can be compared", async () => {
		const report = withCycleTime({
			atThen: percentiles([20, 14, 9, 5], 3),
			atNow: nothingFinishedOnAnyPercentile,
		});
		openTheReport(
			showing(withThroughput(report, [3, 0.1], [0, 0]), [
				"cycle-time",
				"throughput",
			]),
		);

		await theReportIsReady();

		expect(readingOfSummary("change")).toMatch(
			/^CHANGE ✘ Got worse: Throughput$/i,
		);
	});

	// @us-05b @slice-05b @error @contract-shape:pure-function
	// "—" is the report's sign for "cannot be compared"; an empty side would read like "nothing changed".
	it("reads a dash when no shown metric can be compared at all", async () => {
		openTheReport(
			showing(
				withCycleTime({
					atThen: percentiles([20, 14, 9, 5], 3),
					atNow: nothingFinishedOnAnyPercentile,
				}),
				["cycle-time"],
			),
		);

		await theReportIsReady();

		expect(readingOfSummary("change")).toMatch(/^CHANGE —$/i);
		expect(theChangeGroups()).toHaveLength(0);
	});

	// @us-05b @slice-05b @error @contract-shape:pure-function
	// WIP rose since Then, but the report does not show it, so the summary does not judge it either.
	it("summarises only the metrics the report shows", async () => {
		const report = theEngagementStartReport();
		const wip = report.thenAndNow.metrics.wip as IReportMetric;
		report.thenAndNow.metrics.wip = { atThen: wip.atNow, atNow: wip.atThen };
		openTheReport(showing(report, ["cycle-time", "throughput"]));

		await theReportIsReady();

		expect(readingOfSummary("change")).toMatch(
			/^CHANGE ✔ Improved: Cycle Time, Throughput$/i,
		);
	});

	// @us-05b @slice-05b @error @contract-shape:pure-function
	it.each<[string, (report: IReport) => IReport, string]>([
		[
			"a metric this version has no panel for",
			(report) => {
				report.thenAndNow.metrics[A_LATER_METRIC] = {
					atThen: { p85: shown(30, 12) },
					atNow: { p85: shown(20, 12) },
				};
				return showing(report, ["cycle-time", A_LATER_METRIC]);
			},
			"✔ Improved: Cycle Time",
		],
		[
			"a shown metric the report never captured",
			(report) => {
				report.thenAndNow.metrics.wip = undefined;
				report.thenAndNow.metrics["work-item-age"] = null;
				return report;
			},
			"✔ Improved: Cycle Time, Throughput",
		],
	])("lists %s in no group", async (_, shape, group) => {
		openTheReport(shape(theEngagementStartReport()));

		await theReportIsReady();

		expect(readingOfSummary("change")).toMatch(
			new RegExp(`^CHANGE ${group}$`, "i"),
		);
	});

	// @us-05b @slice-05b @error @contract-shape:unbounded-preservation
	// The report "Then & Now — 30 days to 1 Sep 2026" was frozen before the 95th was captured. Its other lines did not
	// move: the 95th, which cannot be compared, neither makes Cycle Time a change nor leaves it out.
	it("judges a report frozen before the 95th was captured on its other percentiles alone", async () => {
		const report = withCycleTime({
			atThen: { p85: shown(21, 38), p70: shown(10, 38), p50: shown(5, 38) },
			atNow: percentiles([19, 21, 10, 5], 41),
		});
		openTheReport(
			showing({ ...report, name: "Then & Now — 30 days to 1 Sep 2026" }, [
				"cycle-time",
			]),
		);

		const panel = await thePanel(CYCLE_TIME_PANEL);

		expect(readingOfSegment(panel, "then")).toMatch(
			new RegExp(`^THEN 95th — 85th 21 days .*${NOT_CAPTURED}$`, "i"),
		);
		expect(readingOfSegment(panel, "change")).toMatch(
			/^CHANGE 95th: — 85th: /i,
		);
		expect(readingOfSummary("change")).toMatch(
			/^CHANGE – Unchanged: Cycle Time$/i,
		);
	});

	// @us-05b @slice-05b @boundary @contract-shape:pure-function
	// A rise from nothing has no percentage, but it is a rise all the same.
	it("counts a change from a Then of zero", async () => {
		openTheReport(
			showing(withThroughput(theEngagementStartReport(), [0, 0], [24, 0.27]), [
				"throughput",
			]),
		);

		await theReportIsReady();

		expect(readingOfSummary("change")).toMatch(
			/^CHANGE ✔ Improved: Throughput$/i,
		);
	});

	// @us-05b @slice-05b @boundary @contract-shape:pure-function
	// A 30-day Now finishes fewer Work Items than a 90-day Then at a higher rate; only the rate carries a change mark.
	it("judges Throughput by its rate per day, never by its total", async () => {
		const report = withThroughput(
			theEngagementStartReport(),
			[52, 0.58],
			[22, 0.73],
		);
		report.thenAndNow.nowWindow = {
			start: "2026-09-03",
			end: "2026-10-02",
			lengthDays: 30,
		};
		openTheReport(showing(report, ["throughput"]));

		await theReportIsReady();

		expect(readingOfSummary("change")).toMatch(
			/^CHANGE ✔ Improved: Throughput$/i,
		);
	});

	// @us-05b @slice-05b @contract-shape:pure-function
	it("names the metrics in the instance's words, as their panels are titled", async () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.CYCLE_TIME]: "Lead Time",
			[TERMINOLOGY_KEYS.THROUGHPUT]: "Delivery Rate",
			[TERMINOLOGY_KEYS.WIP]: "Work in Progress",
			[TERMINOLOGY_KEYS.WORK_ITEM_AGE]: "Ticket Age",
		};
		openTheReport();

		await theReportIsReady();

		expect(readingOfSummary("change")).toMatch(
			/^CHANGE ✔ Improved: Lead Time, Delivery Rate, Work in Progress ✘ Got worse: Total Ticket Age$/i,
		);
	});

	// @us-05b @slice-05b @contract-shape:pure-function
	// Colour backs the marks; the mark and the word carry the meaning for anyone who cannot see it.
	it("shows every kind in its order, each led by its mark and word, improved in green and got worse in red", async () => {
		openTheReport(
			withWipUnmoved(
				withCycleTime({
					atThen: percentiles([25, 14, 9, 5], 30),
					atNow: percentiles([31, 17, 9, 4], 33),
				}),
			),
		);

		await theReportIsReady();

		expect(kindsAndReadingsOfTheChange()).toEqual([
			["improved", "✔ Improved: Throughput"],
			["worse", "✘ Got worse: Total Work Item Age"],
			["mixed", "± Mixed: Cycle Time"],
			["unchanged", "– Unchanged: WIP"],
		]);
		const [improved, worse, mixed, unchanged] = theChangeGroups();
		expect(colourOfTheMark(improved, "✔")).toBe(SUCCESS_COLOUR);
		expect(colourOfTheMark(worse, "✘")).toBe(ERROR_COLOUR);
		expect(colourOfTheMark(mixed, "±")).toBe(SECONDARY_TEXT_COLOUR);
		expect(colourOfTheMark(unchanged, "–")).toBe(SECONDARY_TEXT_COLOUR);
	});

	// @us-05b @slice-05b @error @ac-1.8 @contract-shape:pure-function
	// The CHANGE side lives inside the summary, so it shares its loading and failure looks; after Retry it is fresh.
	it("shows the CHANGE of the report read after Retry, never one from before", async () => {
		const getReport = vi
			.fn()
			.mockRejectedValueOnce(new Error("the network dropped"))
			.mockResolvedValue(theEngagementStartReport());
		renderWithReports(
			<ReportView owner={LIGHTSPEED} reportId={ENGAGEMENT_START_ID} />,
			{ reportService: aReportService({ getReport }) },
		);

		expect(await screen.findByText(COULD_NOT_LOAD)).toBeVisible();
		expect(theSummary()).toBeNull();

		await userEvent.click(screen.getByRole("button", { name: "Retry" }));
		await theReportIsReady();

		expect(readingOfSummary("change")).toMatch(
			/^CHANGE ✔ Improved: Cycle Time, Throughput, WIP ✘ Got worse: Total Work Item Age$/i,
		);
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

		expect(then).toMatch(/^THEN /i);
		expect(then).toMatch(/ 85th 21 days.*70th 10 days.*50th 5 days$/);
		expect(now).toMatch(/^NOW /i);
		expect(now).toMatch(/ 85th 12 days.*70th 7 days.*50th 4 days$/);
		expect(readingOf(panel)).not.toContain("Work Items");
		expect(readingOfSegment(panel, "change")).toMatch(/^CHANGE /i);
	});

	// @us-01 @us-05b @slice-01 @slice-05b @contract-shape:pure-function
	// The same icons as the Metrics tab's Cycle Time percentiles, so a reader knows the 50th is a coin toss.
	it("marks every percentile on both sides with the certainty icon the Metrics tab shows for it", async () => {
		openTheReport();

		const panel = await thePanel(CYCLE_TIME_PANEL);

		expect(iconsIn(theSegment(panel, "then"))).toEqual(
			CERTAINTY_OF_EVERY_PERCENTILE,
		);
		expect(iconsIn(theSegment(panel, "now"))).toEqual(
			CERTAINTY_OF_EVERY_PERCENTILE,
		);
	});

	// @us-05b @slice-05b @driving_port @contract-shape:pure-function
	// Forecasts at the highest certainty lean on the 95th, so it opens the panel, above the 85th.
	it("opens the Cycle Time panel with the 95th for Then and for Now, and its change", async () => {
		openTheReport();

		const panel = await thePanel(CYCLE_TIME_PANEL);
		const [first] = changesIn(theSegment(panel, "change"));

		expect(readingOfSegment(panel, "then")).toMatch(
			/^THEN 95th 34 days 85th /i,
		);
		expect(readingOfSegment(panel, "now")).toMatch(/^NOW 95th 19 days 85th /i);
		expect(readingOf(first)).toBe("▼ 15 days (−44 %)");
		expect(first.dataset.change).toBe("better");
		expect(iconsIn(theSegment(panel, "now"))[0]).toBe("CheckCircleIcon");
	});

	// @us-01 @us-05b @slice-01 @slice-05b @contract-shape:pure-function
	// Colour alone tells nobody who cannot see it; the sign and the arrow say the same.
	it("tells a shorter Cycle Time as better on every percentile by arrow and sign as well as colour", async () => {
		openTheReport();

		const panel = await thePanel(CYCLE_TIME_PANEL);
		const changes = changesIn(theSegment(panel, "change"));

		expect(changes.map(readingOf)).toEqual([
			"▼ 15 days (−44 %)",
			"▼ 9 days (−43 %)",
			"▼ 3 days (−30 %)",
			"▼ 1 day (−20 %)",
		]);
		expect(changes.map((change) => change.dataset.change)).toEqual([
			"better",
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
			/ 85th: ▼ 3 days \(−20 %\) 70th: – No change 50th: ▲ 1 day \(\+20 %\)$/,
		);
		expect(worse.map(readingOf)).toEqual(["▲ 1 day (+20 %)"]);
	});

	// @us-01 @slice-01 @boundary @contract-shape:pure-function
	// The dash stands where an arrow would, so the column lines up; the words already say it, so it is not read out.
	it("leads No change with a dash in the arrow's place, hidden from a screen reader", async () => {
		openTheReport(
			withCycleTime({
				atThen: { p85: shown(15, 181), p70: shown(9, 181), p50: shown(5, 181) },
				atNow: { p85: shown(12, 203), p70: shown(9, 203), p50: shown(6, 203) },
			}),
		);

		const panel = await thePanel(CYCLE_TIME_PANEL);
		const [fell, unchanged, rose] = changesIn(theSegment(panel, "change"));

		expect(readingOf(unchanged)).toBe("– No change");
		expect(unchanged.dataset.change).toBe("neutral");
		expect(within(unchanged).getByText("–")).toHaveAttribute(
			"aria-hidden",
			"true",
		);
		expect(within(fell).getByText("▼")).not.toHaveAttribute("aria-hidden");
		expect(within(rose).getByText("▲")).not.toHaveAttribute("aria-hidden");
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
	it("shows WIP as its average, median, min and max, each with its own change, lower reading as better", async () => {
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
	it("tells a growing Total Work Item Age as worse on every line by sign and arrow as well as colour", async () => {
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

	// @us-05 @slice-05 @contract-shape:pure-function
	it.each([WIP_PANEL, AGE_PANEL])(
		"leads the average, median, min and max of %s with the same icons on both sides",
		async (title) => {
			openTheReport();

			const panel = await thePanel(title);

			expect(iconsIn(theSegment(panel, "then"))).toEqual(SPREAD_ICONS);
			expect(iconsIn(theSegment(panel, "now"))).toEqual(SPREAD_ICONS);
		},
	);

	// @us-04 @slice-04 @contract-shape:pure-function
	it("leads Throughput's total and per-day lines with their icons on both sides", async () => {
		openTheReport();

		const panel = await thePanel(THROUGHPUT_PANEL);

		for (const side of ["then", "now"] as const) {
			expect(iconsIn(theSegment(panel, side))).toEqual([
				"FunctionsIcon",
				"CalendarTodayIcon",
			]);
		}
	});

	// @us-04 @us-05 @slice-05 @contract-shape:pure-function
	// A screen reader reaches the change column apart from the labels beside it, so each change names its line.
	it.each([
		[
			WIP_PANEL,
			/^CHANGE Average: ▼ 2 \(−22 %\) Median: ▼ 2 \(−22 %\) Min: ▼ 1 \(−17 %\) Max: ▼ 3 \(−25 %\)$/i,
		],
		[
			AGE_PANEL,
			/^CHANGE Average: ▲ 270 days \(\+44 %\) Median: ▲ 270 days \(\+45 %\) Min: ▲ 220 days \(\+52 %\) Max: ▲ 230 days \(\+30 %\)$/i,
		],
		[THROUGHPUT_PANEL, /^CHANGE per day: ▲ 0\.14 per day \(\+24 %\)$/i],
	])(
		"names the line of every change on the %s panel",
		async (title, reading) => {
			openTheReport();

			const panel = await thePanel(title);

			expect(readingOfSegment(panel, "change")).toMatch(reading);
		},
	);

	// @us-01 @us-05b @slice-01 @slice-05b @contract-shape:pure-function
	it("names the line of every change on the Cycle Time panel, the 95th first", async () => {
		openTheReport();

		const panel = await thePanel(CYCLE_TIME_PANEL);

		expect(readingOfSegment(panel, "change")).toMatch(
			/^CHANGE 95th: ▼ 15 days \(−44 %\) 85th: ▼ 9 days \(−43 %\) 70th: ▼ 3 days \(−30 %\) 50th: ▼ 1 day \(−20 %\)$/i,
		);
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
	it("shows every WIP and Total Work Item Age line as a real 0 when Then had nothing in progress", async () => {
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
	it("says WIP and Total Work Item Age were not captured for a report made before they existed", async () => {
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
			/ 85th 21 days /,
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

		expect(readingOfSegment(panel, "then")).toMatch(/ 85th 9 days /);
		expect(readingOfSegment(panel, "now")).toMatch(/ 85th 5 days /);
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

	// @us-04 @slice-04 @error @contract-shape:pure-function
	// The note stands where the panel would; a metric from a later version has no word here, so it goes by its key.
	it("heads the note in place of a later version's metric with that metric's key", async () => {
		openTheReport(
			theEngagementStartReport({
				shownMetricKeys: ["cycle-time", A_LATER_METRIC],
			}),
		);
		await theReportIsReady();

		expect(thePanelTitles()).toEqual([CYCLE_TIME_PANEL, A_LATER_METRIC]);
		expect(
			within(await thePanel(A_LATER_METRIC)).getByText(
				METRIC_FROM_A_NEWER_VERSION,
			),
		).toBeVisible();
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
