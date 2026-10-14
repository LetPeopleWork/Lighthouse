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
	p85: shown(12, 41),
	p70: shown(7, 41),
	p50: shown(4, 41),
};

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

describe("the Cycle Time panel", () => {
	it("says a percentile a report never kept was not captured, and shows no change for it", () => {
		renderPanel({ atThen: { p85: shown(21, 38) }, atNow: NOW_IN_FULL });

		expect(segment("then")).toHaveTextContent("Not captured for this report");
		expect(headingOf("then")).toBe("Then");
		expect(segment("change")).toHaveTextContent(
			/^Change ▼ 9 days \(−43 %\) — —$/,
		);
	});

	// The report's summary counts the closed Work Items once; the panel does not repeat it.
	it("heads each side by its name alone", () => {
		renderPanel({ atThen: NOW_IN_FULL, atNow: NOW_IN_FULL });

		expect(headingOf("then")).toBe("Then");
		expect(headingOf("now")).toBe("Now");
	});

	it("shows no change where Now has no value", () => {
		renderPanel({
			atThen: { p85: shown(21, 38), p70: shown(10, 38), p50: shown(5, 38) },
			atNow: { ...NOW_IN_FULL, p70: aDash("no-finished-items") },
		});

		expect(segment("change")).toHaveTextContent(
			/^Change ▼ 9 days \(−43 %\) — ▼ 1 day \(−20 %\)$/,
		);
	});
});
