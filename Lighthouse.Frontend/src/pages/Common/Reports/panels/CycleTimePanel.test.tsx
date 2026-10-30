import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type {
	IReportMetric,
	IReportValue,
	ReportAbsentReason,
} from "../../../../models/Reports/Report";
import CycleTimePanel from "./CycleTimePanel";

const shown = (value: number, sampleSize: number): IReportValue => ({
	value,
	sampleSize,
	absentReason: null,
});

const aDash = (absentReason: ReportAbsentReason): IReportValue => ({
	value: null,
	sampleSize: null,
	absentReason,
});

const NOW_IN_FULL = {
	p95: shown(19, 41),
	p85: shown(12, 41),
	p70: shown(7, 41),
	p50: shown(4, 41),
};

const THEN_IN_FULL = {
	p95: shown(34, 38),
	p85: shown(21, 38),
	p70: shown(10, 38),
	p50: shown(5, 38),
};

/** What a report frozen before the 95th was captured holds for Then: the other three percentiles only. */
const THEN_BEFORE_THE_95TH = {
	p85: shown(21, 38),
	p70: shown(10, 38),
	p50: shown(5, 38),
};

/** The icons the Metrics tab's Cycle Time percentiles carry for the 95th, 85th, 70th and 50th. */
const CERTAINTY_OF_95TH_85TH_70TH_50TH = [
	"CheckCircleIcon",
	"CheckCircleOutlineOutlinedIcon",
	"QueryBuilderIcon",
	"ErrorOutlineOutlinedIcon",
];

const renderPanel = (metric: IReportMetric) =>
	render(
		<CycleTimePanel
			title="Cycle Time"
			metric={metric}
			workItem="Work Item"
			workItems="Work Items"
			directionOfGood="lower"
		/>,
	);

const segment = (name: "then" | "now" | "change") =>
	document.querySelector(`[data-segment="${name}"]`) as HTMLElement;

const headingOf = (name: "then" | "now") =>
	segment(name).firstElementChild?.textContent;

const iconsIn = (name: "then" | "now") =>
	Array.from(
		segment(name).querySelectorAll<SVGElement>("svg[data-testid]"),
	).map((icon) => icon.dataset.testid);

const theChanges = () =>
	Array.from(segment("change").querySelectorAll<HTMLElement>("[data-change]"));

describe("the Cycle Time panel", () => {
	// Read from the 85th down: the 95th's row above it is pinned by its own cases below.
	it("says a percentile a report never kept was not captured, and shows no change for it", () => {
		renderPanel({ atThen: { p85: shown(21, 38) }, atNow: NOW_IN_FULL });

		expect(segment("then")).toHaveTextContent("Not captured for this report");
		expect(headingOf("then")).toBe("Then");
		expect(segment("change")).toHaveTextContent(
			/ 85th: 9 days \(−43 %\) 70th: — 50th: —$/,
		);
	});

	// The report's summary counts the closed Work Items once; the panel does not repeat it.
	it("heads each side by its name alone", () => {
		renderPanel({ atThen: NOW_IN_FULL, atNow: NOW_IN_FULL });

		expect(headingOf("then")).toBe("Then");
		expect(headingOf("now")).toBe("Now");
	});

	it("shows a dash for a value absent for a reason this version does not know, and says it cannot be shown", () => {
		renderPanel({
			atThen: { ...NOW_IN_FULL, p85: aDash("unknown") },
			atNow: NOW_IN_FULL,
		});

		expect(segment("then")).toHaveTextContent(
			/ 85th — 70th 7 days 50th 4 days Can't be shown in this version of Lighthouse$/,
		);
	});

	it("shows no change where Now has no value", () => {
		renderPanel({
			atThen: THEN_IN_FULL,
			atNow: { ...NOW_IN_FULL, p70: aDash("no-finished-items") },
		});

		expect(segment("change")).toHaveTextContent(
			/ 85th: 9 days \(−43 %\) 70th: — 50th: 1 day \(−20 %\)$/,
		);
	});

	// Forecasts at the highest certainty lean on the 95th, so it opens the panel, above the 85th.
	it("shows the 95th above the 85th on both sides, with the certainty icon the Metrics tab shows for it", () => {
		renderPanel({ atThen: THEN_IN_FULL, atNow: NOW_IN_FULL });

		expect(segment("then")).toHaveTextContent(
			/^Then 95th 34 days 85th 21 days 70th 10 days 50th 5 days$/,
		);
		expect(segment("now")).toHaveTextContent(
			/^Now 95th 19 days 85th 12 days 70th 7 days 50th 4 days$/,
		);
		expect(iconsIn("then")).toEqual(CERTAINTY_OF_95TH_85TH_70TH_50TH);
		expect(iconsIn("now")).toEqual(CERTAINTY_OF_95TH_85TH_70TH_50TH);
	});

	it("shows the 95th's own change first, a shorter one reading as better", () => {
		renderPanel({ atThen: THEN_IN_FULL, atNow: NOW_IN_FULL });

		expect(segment("change")).toHaveTextContent(
			/^Change 95th: 15 days \(−44 %\) 85th: 9 days \(−43 %\) 70th: 3 days \(−30 %\) 50th: 1 day \(−20 %\)$/,
		);
		expect(theChanges()[0].dataset.change).toBe("better");
	});

	// A report frozen before the 95th was captured has no Then 95th at all: not a zero, not a dash with a reason.
	it("says a Then 95th was not captured for a report frozen before it was, shows no change for it, and still shows Now's", () => {
		renderPanel({ atThen: THEN_BEFORE_THE_95TH, atNow: NOW_IN_FULL });

		expect(segment("then")).toHaveTextContent(
			/^Then 95th — 85th 21 days 70th 10 days 50th 5 days Not captured for this report$/,
		);
		expect(segment("now")).toHaveTextContent(
			/^Now 95th 19 days 85th 12 days 70th 7 days 50th 4 days$/,
		);
		expect(segment("change")).toHaveTextContent(
			/^Change 95th: — 85th: 9 days \(−43 %\) 70th: 3 days \(−30 %\) 50th: 1 day \(−20 %\)$/,
		);
		expect(theChanges()).toHaveLength(3);
	});
});
