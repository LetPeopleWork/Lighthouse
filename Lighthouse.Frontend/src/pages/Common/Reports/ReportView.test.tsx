import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IReport } from "../../../models/Reports/Report";
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
 * One report as a reader opens it: one line "Reports / <name>", the header naming both windows, and one
 * panel per shown metric in the catalog's order. A panel is three segments, Then | Now | Change; each side's
 * heading says how many Work Items or days it rests on, every percentile carries the certainty icon the
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
const AGE_PANEL = "Work Item Age";
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

const beforeEachReportTest = () => {
	reportUsage.mockClear();
	mockNavigate.mockClear();
	terms.current = { ...defaultTerms };
};

describe("The report header", () => {
	beforeEach(beforeEachReportTest);

	// @us-01 @us-02 @us-07 @slice-07 @driving_port @contract-shape:pure-function
	it.skip("names Then as frozen, Now as the last days ending today, and how long ago Then ended", async () => {
		openTheReport();

		await theReportIsReady();
		const page = readingOf(document.body);

		expect(page).toContain("Then: 90 days to 31 Jul 2026 — frozen");
		expect(page).toContain("Now: last 90 days, 5 Jul – 2 Oct 2026");
		expect(page).toContain("63 days since Then ended");
		expect(page).not.toMatch(/baseline/i);
	});

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

describe("The panels of a report", () => {
	beforeEach(beforeEachReportTest);

	// @us-01 @slice-01 @driving_port @contract-shape:pure-function
	it("shows the Cycle Time panel as Then, Now and Change, each side's Work Items named once in its heading", async () => {
		openTheReport();

		const panel = await thePanel(CYCLE_TIME_PANEL);
		const then = readingOfSegment(panel, "then");
		const now = readingOfSegment(panel, "now");

		expect(then).toMatch(/^THEN · 38 Work Items /i);
		expect(then).toMatch(/85th 21 days.*70th 10 days.*50th 5 days$/);
		expect(now).toMatch(/^NOW · 41 Work Items /i);
		expect(now).toMatch(/85th 12 days.*70th 7 days.*50th 4 days$/);
		expect(then.match(/Work Items/g)).toHaveLength(1);
		expect(now.match(/Work Items/g)).toHaveLength(1);
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
				beyondThenLimits: null,
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

	// @us-03 @slice-03 @contract-shape:pure-function
	it.skip("counts Now's Work Items beyond Then's limits, and says why there is no count below", async () => {
		openTheReport();

		const panel = readingOf(await thePanel(CYCLE_TIME_PANEL));

		expect(panel).toContain("Work Items beyond Then's limits: 1 above");
		expect(panel).toContain("— below");
		expect(panel).toContain("Then's lower limit is 0");
	});

	// @us-04 @slice-04 @contract-shape:pure-function
	it.skip("shows Throughput as a total and a per-day average, compared per day", async () => {
		openTheReport();

		const panel = await thePanel(THROUGHPUT_PANEL);
		const then = readingOfSegment(panel, "then");

		expect(then).toMatch(/^THEN · 90 days /i);
		expect(then).toContain("Total 52 Work Items");
		expect(then).toContain("0.58 per day");
		expect(then.match(/90 days/g)).toHaveLength(1);
		expect(readingOf(panel)).toContain("Days beyond Then's limits: 2 above");
		expect(readingOf(changesIn(panel)[0])).toBe("▲ 0.14 per day (+24 %)");
		expect(changesIn(panel)[0].dataset.change).toBe("better");
	});

	// @us-05 @slice-05 @contract-shape:pure-function
	it.skip("shows WIP as an average with its range", async () => {
		openTheReport();

		const panel = await thePanel(WIP_PANEL);

		expect(readingOfSegment(panel, "then")).toMatch(
			/^THEN · 90 days Average 9 \(6–12\)$/i,
		);
		expect(readingOfSegment(panel, "now")).toMatch(
			/^NOW · 90 days Average 7 \(5–9\)$/i,
		);
		expect(readingOf(changesIn(panel)[0])).toMatch(/^▼ .*\(−22 %\)$/);
	});

	// @us-05 @slice-05 @contract-shape:pure-function
	it.skip("tells a growing Work Item Age as worse by sign and arrow as well as colour", async () => {
		openTheReport();

		const panel = await thePanel(AGE_PANEL);

		expect(readingOf(changesIn(panel)[0])).toBe("▲ 270 days (+44 %)");
		expect(changesIn(panel)[0].dataset.change).toBe("worse");
	});

	// @us-04 @slice-04 @contract-shape:pure-function
	it.skip("stacks only the shown metrics, in the catalog's order whatever order they were ticked in", async () => {
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
			[TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets",
		};
		openTheReport();

		const panel = await thePanel("Lead Time");

		expect(readingOfSegment(panel, "then")).toMatch(/^THEN · 38 Tickets /i);
		expect(readingOfSegment(panel, "now")).toMatch(/^NOW · 41 Tickets /i);
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
				beyondThenLimits: null,
			}),
		);

		const panel = await thePanel(CYCLE_TIME_PANEL);
		const then = readingOfSegment(panel, "then");

		expect(then).toContain("No finished Work Items in Then");
		expect(then).toMatch(/85th —.*70th —.*50th —/);
		expect(then).not.toMatch(/\d+ days/);
		expect(changesIn(panel)).toHaveLength(0);
	});

	// @us-03 @slice-03 @error @contract-shape:pure-function
	it.skip("shows a dash for the counts beyond Then's limits when Then had too few points to have any", async () => {
		openTheReport(
			withCycleTime({
				atThen: { p85: shown(21, 4), p70: shown(10, 4), p50: shown(5, 4) },
				atNow: { p85: shown(12, 41), p70: shown(7, 41), p50: shown(4, 41) },
				beyondThenLimits: {
					above: aDash("too-few-points"),
					below: aDash("too-few-points"),
				},
			}),
		);

		const panel = await thePanel(CYCLE_TIME_PANEL);

		expect(readingOf(panel)).toContain("Then has too few points for limits");
		expect(readingOfSegment(panel, "then")).toMatch(
			/^THEN · 4 Work Items 85th 21 days/i,
		);
	});

	// @us-04 @slice-04 @error @contract-shape:pure-function
	// A rise from nothing has no percentage; the difference itself still shows.
	it.skip("shows no percentage for a change from a Then of zero", async () => {
		const report = theEngagementStartReport();
		report.thenAndNow.metrics.throughput = {
			atThen: { total: shown(0, 90), perDay: shown(0, 90) },
			atNow: { total: shown(24, 90), perDay: shown(0.27, 90) },
			beyondThenLimits: {
				above: aDash("no-process"),
				below: aDash("no-process"),
			},
		};
		openTheReport(report);

		const panel = readingOf(await thePanel(THROUGHPUT_PANEL));

		expect(panel).toContain("Total 0 Work Items");
		expect(panel).toContain("Then was 0");
		expect(panel).toContain("Then never varied from 0");
		expect(panel).not.toMatch(/%|Infinity|NaN/);
	});

	// @us-05 @slice-05 @error @contract-shape:pure-function
	// Nothing in progress is a real WIP of 0, but an average age over no Work Items does not exist.
	it.skip("shows WIP as a real 0 and the average Work Item Age as a dash when Then had nothing in progress", async () => {
		const report = theEngagementStartReport();
		report.thenAndNow.metrics.wip = {
			atThen: { average: shown(0, 90), min: shown(0, 90), max: shown(0, 90) },
			atNow: { average: shown(7, 90), min: shown(5, 90), max: shown(9, 90) },
			beyondThenLimits: null,
		};
		report.thenAndNow.metrics["work-item-age"] = {
			atThen: {
				totalWindowAverage: shown(0, 90),
				totalLastDay: shown(0, 90),
				averageWindowAverage: aDash("no-wip"),
				averageLastDay: aDash("no-wip"),
			},
			atNow: {
				totalWindowAverage: shown(880, 10),
				totalLastDay: shown(910, 10),
				averageWindowAverage: shown(88, 10),
				averageLastDay: shown(91, 10),
			},
			beyondThenLimits: null,
		};
		openTheReport(report);

		const wip = readingOf(await thePanel(WIP_PANEL));
		const age = await thePanel(AGE_PANEL);

		expect(wip).toMatch(/THEN · 90 days Average 0 \(0–0\)/i);
		expect(wip).not.toContain("No Work Items in progress");
		expect(readingOf(age)).toContain("No Work Items in progress");
	});

	// @us-03 @slice-03 @boundary @contract-shape:pure-function
	// A report created before the counts existed never froze Then's limits, so it cannot count against them.
	it.skip("says the counts beyond Then's limits were not captured for a report made before they existed", async () => {
		openTheReport(
			withCycleTime({
				atThen: { p85: shown(21, 38), p70: shown(10, 38), p50: shown(5, 38) },
				atNow: { p85: shown(12, 41), p70: shown(7, 41), p50: shown(4, 41) },
			}),
		);

		const panel = await thePanel(CYCLE_TIME_PANEL);

		expect(readingOf(panel)).toContain(NOT_CAPTURED);
		expect(readingOfSegment(panel, "then")).toMatch(
			/^THEN · 38 Work Items 85th 21 days/i,
		);
	});

	// @us-01 @slice-01 @boundary @contract-shape:pure-function
	// Thin data is shown, not hidden: the number of Work Items behind it lets the reader judge.
	it("shows a value backed by very few Work Items with that number beside it", async () => {
		openTheReport(
			withCycleTime({
				atThen: { p85: shown(9, 2), p70: shown(9, 2), p50: shown(4, 2) },
				atNow: { p85: shown(5, 3), p70: shown(5, 3), p50: shown(3, 3) },
				beyondThenLimits: null,
			}),
		);

		const panel = await thePanel(CYCLE_TIME_PANEL);

		expect(readingOfSegment(panel, "then")).toMatch(
			/^THEN · 2 Work Items 85th 9 days/i,
		);
		expect(readingOfSegment(panel, "now")).toMatch(
			/^NOW · 3 Work Items 85th 5 days/i,
		);
	});

	// @us-04 @slice-04 @error @contract-shape:unbounded-preservation
	// A later version may add a metric this one has no panel for; the rest of the report still reads.
	it.skip("puts a plain note in place of a metric this version cannot show, and shows the others", async () => {
		const report = theEngagementStartReport({
			shownMetricKeys: ["cycle-time", A_LATER_METRIC],
		});
		report.thenAndNow.metrics[A_LATER_METRIC] = {
			atThen: { p85: shown(30, 12) },
			atNow: { p85: shown(20, 12) },
			beyondThenLimits: null,
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
