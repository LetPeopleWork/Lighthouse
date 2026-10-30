import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type {
	IReportMetric,
	IReportValue,
	ReportMetricKey,
} from "../../../../models/Reports/Report";
import { readingOf } from "../../../../tests/ReportsTestKit";
import { reportPanelRegistry } from "./reportPanelRegistry";
import { DASH, lineComparisonsOf } from "./thenNowComparison";

const aValue = (value: number): IReportValue => ({
	value,
	sampleSize: 90,
	absentReason: null,
});

/**
 * One read that gives every panel a better, a worse, an unmoved and a dash line where it has that many lines. Then
 * never captured the 95th or the max; Throughput's per day differs only below what the panel shows, so it is unmoved.
 */
const MIXED: IReportMetric = {
	atThen: {
		p85: aValue(21),
		p70: aValue(7),
		p50: aValue(5),
		total: aValue(52),
		perDay: aValue(0.584),
		average: aValue(8.62),
		median: aValue(9),
		min: aValue(6),
	},
	atNow: {
		p95: aValue(19),
		p85: aValue(12),
		p70: aValue(8),
		p50: aValue(5),
		total: aValue(65),
		perDay: aValue(0.576),
		average: aValue(7.38),
		median: aValue(10),
		min: aValue(6),
		max: aValue(9),
	},
};

type Mark = "better" | "worse" | "neutral" | "dash";

const changeSegment = () =>
	document.querySelector<HTMLElement>('[data-segment="change"]') as HTMLElement;

/**
 * The summary header tallies what lineComparisonsOf returns, and the reader sees what the panel draws. A panel that
 * drew a line its definition does not list, or rounded one differently, would let the two disagree.
 */
describe("every panel in the registry", () => {
	it.each<[ReportMetricKey, Mark[]]>([
		["cycle-time", ["dash", "better", "worse", "neutral"]],
		["throughput", ["neutral"]],
		["wip", ["better", "worse", "neutral", "dash"]],
		["work-item-age", ["better", "worse", "neutral", "dash"]],
	])(
		"draws on the %s panel the change marks the summary tallies",
		(key, marks) => {
			const definition = reportPanelRegistry[key];
			const Panel = definition.component;
			render(
				<Panel
					title="A panel"
					metric={MIXED}
					workItem="Work Item"
					workItems="Work Items"
					directionOfGood={definition.directionOfGood}
				/>,
			);

			const tallied = lineComparisonsOf(MIXED, definition).map(
				(line): Mark => line.change?.verdict ?? "dash",
			);
			const drawn = Array.from(
				changeSegment().querySelectorAll<HTMLElement>("[data-change]"),
			).map((change) => change.dataset.change);
			const drawnDashes = readingOf(changeSegment()).split(DASH).length - 1;

			expect(tallied).toEqual(marks);
			expect(drawn).toEqual(marks.filter((mark) => mark !== "dash"));
			expect(drawnDashes).toBe(marks.filter((mark) => mark === "dash").length);
		},
	);
});
