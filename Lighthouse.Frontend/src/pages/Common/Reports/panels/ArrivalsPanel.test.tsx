import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import fc from "fast-check";
import { afterEach, describe, expect, it } from "vitest";
import type {
	IReport,
	IReportMetric,
	ReportMetricKey,
} from "../../../../models/Reports/Report";
import {
	forgetTheBrowser,
	printPathFor,
	renderTheExportScene,
	reportPathFor,
	theFontsHaveLoaded,
	thePrintPageIsReady,
} from "../../../../tests/ReportPrintTestKit";
import {
	aReportService,
	ENGAGEMENT_START_ID,
	LIGHTSPEED,
	NOT_CAPTURED,
	readingOf,
	shown,
	theEngagementStartReport,
} from "../../../../tests/ReportsTestKit";
import { gapSentenceOf } from "./ArrivalsPanel";
import { lineComparisonsOf, roundForDisplay } from "./thenNowComparison";

type Where = "on the report" | "in the print";
type GapSentenceKind = "more" | "fewer" | "balanced";

const WHERE: readonly Where[] = ["on the report", "in the print"];
const ARRIVALS = "Arrivals";
const ARRIVALS_KEY = "arrivals" as ReportMetricKey;

/** The four values one side of the Arrivals metric carries, as the report read sends them. */
const aSideOf = (
	total: number,
	perDay: number,
	throughputPerDay: number,
	gapPerDay: number,
	days = 90,
): IReportMetric["atThen"] => ({
	total: shown(total, days),
	perDay: shown(perDay, days),
	throughputPerDay: shown(throughputPerDay, days),
	gapPerDay: shown(gapPerDay, days),
});

/** A Team report showing only Arrivals; `undefined` is a report that never froze it. */
const aReportShowingArrivals = (
	arrivals: IReportMetric | undefined,
): IReport => {
	const base = theEngagementStartReport();
	return theEngagementStartReport({
		shownMetricKeys: [ARRIVALS_KEY],
		thenAndNow: {
			...base.thenAndNow,
			metrics: { ...base.thenAndNow.metrics, [ARRIVALS_KEY]: arrivals },
		},
	});
};

const THEN_STARTED_MORE = aSideOf(99, 1.1, 0.8, 0.3);
const NOW_STARTED_FEWER = aSideOf(81, 0.9, 1.0, -0.1);

afterEach(() => {
	forgetTheBrowser();
});

const openingTheReport = async (where: Where, report: IReport) => {
	const reportService = aReportService({ getReport: async () => report });
	if (where === "on the report") {
		renderTheExportScene({
			reportService,
			startAt: reportPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
			withPrintPages: false,
		});
	} else {
		theFontsHaveLoaded();
		renderTheExportScene({
			reportService,
			startAt: printPathFor(LIGHTSPEED, ENGAGEMENT_START_ID),
		});
		await thePrintPageIsReady();
	}
	return screen.findByRole("region", { name: ARRIVALS });
};

const segment = (panel: HTMLElement, name: "then" | "now" | "change") =>
	readingOf(
		panel.querySelector<HTMLElement>(`[data-segment="${name}"]`) as HTMLElement,
	);

const theChangeMark = (panel: HTMLElement) =>
	panel.querySelector<HTMLElement>("[data-change]")?.dataset.change;

const theSummaryOfChange = () =>
	readingOf(
		document.querySelector<HTMLElement>(
			'[data-summary-side="change"]',
		) as HTMLElement,
	);

describe("the Arrivals panel of a Then & Now report", () => {
	it.each(WHERE)(
		'reads "Total started 99 Work Items", "Arrivals 1.10 per day", "Throughput 0.80 per day" and "0.30 more started than finished per day" for Then, "0.10 fewer started than finished per day" for Now, and "Change gap per day: 0.40 per day" marked better with no percent %s',
		async (where) => {
			const panel = await openingTheReport(
				where,
				aReportShowingArrivals({
					atThen: THEN_STARTED_MORE,
					atNow: NOW_STARTED_FEWER,
				}),
			);

			expect(segment(panel, "then")).toMatch(
				/^Then Total started 99 Work Items Arrivals 1\.10 per day Throughput 0\.80 per day 0\.30 more started than finished per day$/,
			);
			expect(segment(panel, "now")).toMatch(
				/^Now Total started 81 Work Items Arrivals 0\.90 per day Throughput 1\.00 per day 0\.10 fewer started than finished per day$/,
			);
			expect(segment(panel, "change")).toMatch(
				/^Change gap per day: 0\.40 per day$/,
			);
			expect(theChangeMark(panel)).toBe("better");
		},
	);

	it('explains in its info text "Arrivals are Work Items started. The gap is started minus finished, per day. Lower is better."', async () => {
		const panel = await openingTheReport(
			"on the report",
			aReportShowingArrivals({
				atThen: THEN_STARTED_MORE,
				atNow: NOW_STARTED_FEWER,
			}),
		);

		await userEvent.click(within(panel).getByRole("button"));

		expect(
			await screen.findByText(
				"Arrivals are Work Items started. The gap is started minus finished, per day. Lower is better.",
			),
		).toBeVisible();
	});

	it("lists Arrivals under Improved in the header when the gap got smaller", async () => {
		await openingTheReport(
			"on the report",
			aReportShowingArrivals({
				atThen: THEN_STARTED_MORE,
				atNow: NOW_STARTED_FEWER,
			}),
		);

		await waitFor(() =>
			expect(theSummaryOfChange()).toMatch(/^CHANGE Improved: Arrivals$/i),
		);
	});

	it.each(WHERE)(
		'reads a gap of 0.004 as "as many started as finished per day" and 0.006 as "0.01 more started than finished per day", rounding before it compares, so the change reads "Change gap per day: 0.01 per day" %s',
		async (where) => {
			const panel = await openingTheReport(
				where,
				aReportShowingArrivals({
					atThen: aSideOf(90, 1.004, 1.0, 0.004),
					atNow: aSideOf(90, 1.006, 1.0, 0.006),
				}),
			);

			expect(segment(panel, "then")).toMatch(
				/ as many started as finished per day$/,
			);
			expect(segment(panel, "then")).not.toMatch(/0\.00 (more|fewer)/);
			expect(segment(panel, "now")).toMatch(
				/ 0\.01 more started than finished per day$/,
			);
			expect(segment(panel, "change")).toMatch(
				/^Change gap per day: 0\.01 per day$/,
			);
			expect(theChangeMark(panel)).toBe("worse");
		},
	);

	it.each(WHERE)(
		'reads "Not captured for this report" on both sides and puts Arrivals in no header group when the report never froze it %s',
		async (where) => {
			const panel = await openingTheReport(
				where,
				aReportShowingArrivals(undefined),
			);

			expect(segment(panel, "then")).toContain(NOT_CAPTURED);
			expect(segment(panel, "now")).toContain(NOT_CAPTURED);
			expect(segment(panel, "then")).not.toMatch(/\d/);
			expect(segment(panel, "change")).toMatch(/—/);
			expect(theSummaryOfChange()).not.toContain(ARRIVALS);
		},
	);
});

/** What the gap line must say for a rounded gap, written out from the approved rule. */
const expectedSentenceKind = (rounded: number): GapSentenceKind => {
	if (rounded > 0) return "more";
	if (rounded < 0) return "fewer";
	return "balanced";
};

describe("the gap sentence follows the number the reader sees", () => {
	it("reads balanced exactly when the gap rounds to zero at two decimals, and its magnitude is the shown number", () => {
		const aGap = fc.oneof(
			fc.double({ min: -50, max: 50, noNaN: true }),
			fc.double({ min: -0.012, max: 0.012, noNaN: true }),
			fc.integer({ min: -2000, max: 2000 }).map((steps) => steps / 200),
		);

		fc.assert(
			fc.property(aGap, (gap) => {
				const rounded = roundForDisplay(gap, 2);
				const sentence = gapSentenceOf(rounded);
				expect(sentence.kind, `gap ${gap}`).toBe(expectedSentenceKind(rounded));
				expect(sentence.magnitude, `gap ${gap}`).toBe(Math.abs(rounded));
			}),
		);
		for (const [gap, kind, magnitude] of [
			[0, "balanced", 0],
			[0.004, "balanced", 0],
			[-0.004, "balanced", 0],
			[0.006, "more", 0.01],
		] as const) {
			expect(gapSentenceOf(roundForDisplay(gap, 2)), `gap ${gap}`).toEqual({
				kind,
				magnitude,
			});
		}
	});

	it("keeps the change as the difference of the shown gaps, better exactly when it fell", () => {
		const wholeThousandths = fc
			.integer({ min: -5000, max: 5000 })
			.filter((value) => Math.abs(value) % 10 !== 5);
		const hundredthsShown = (thousandths: number) =>
			Math.sign(thousandths) * Math.round(Math.abs(thousandths) / 10);
		const changeOf = (then: number, now: number) =>
			lineComparisonsOf(
				{
					atThen: { gapPerDay: shown(then) },
					atNow: { gapPerDay: shown(now) },
				} as IReportMetric,
				{
					changeLines: [
						{ key: "gapPerDay", label: "gap per day", decimals: 2 },
					] as never,
					directionOfGood: "lower",
				},
			)[0].change;

		fc.assert(
			fc.property(wholeThousandths, wholeThousandths, (then, now) => {
				const change = changeOf(then / 1000, now / 1000);
				const delta = (hundredthsShown(now) - hundredthsShown(then)) / 100;
				const verdict = (): string => {
					if (delta === 0) return "neutral";
					return delta < 0 ? "better" : "worse";
				};
				expect(change?.delta, `then ${then} now ${now}`).toBeCloseTo(delta, 9);
				expect(change?.verdict, `then ${then} now ${now}`).toBe(verdict());
			}),
		);
		expect(changeOf(0.3, -0.1)?.delta).toBeCloseTo(-0.4, 9);
		expect(changeOf(0, 0.35)?.delta).toBeCloseTo(0.35, 9);
	});
});
