import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type {
	IReportMetric,
	IReportValue,
} from "../../../../models/Reports/Report";
import { readingOf } from "../../../../tests/ReportsTestKit";
import ThroughputPanel from "./ThroughputPanel";
import { DASH } from "./thenNowComparison";
import WipPanel from "./WipPanel";
import WorkItemAgePanel from "./WorkItemAgePanel";

const aValue = (value: number): IReportValue => ({
	value,
	sampleSize: 30,
	absentReason: null,
});

const aSide = (total: number, perDay: number): IReportMetric["atThen"] => ({
	total: aValue(total),
	perDay: aValue(perDay),
});

const changeOf = (panel: HTMLElement) =>
	panel.querySelector<HTMLElement>('[data-segment="change"]') as HTMLElement;

const aThroughputPanel = (metric: IReportMetric) =>
	render(
		<ThroughputPanel
			title="Throughput"
			metric={metric}
			workItem="Work Item"
			workItems="Work Items"
			directionOfGood="higher"
		/>,
	).container;

const theChangeOfThroughput = (metric: IReportMetric) =>
	changeOf(aThroughputPanel(metric));

const theNowOfThroughput = (container: HTMLElement) =>
	readingOf(
		container.querySelector<HTMLElement>('[data-segment="now"]') as HTMLElement,
	);

const verdictsDrawn = (change: HTMLElement) =>
	Array.from(change.querySelectorAll<HTMLElement>("[data-change]")).map(
		(line) => line.dataset.change,
	);

const marksDrawn = (change: HTMLElement) =>
	change.querySelectorAll("svg").length;

const risesDrawn = (change: HTMLElement) =>
	change.querySelectorAll('[role="img"][aria-label="rose"]').length;

const BOTH_ZERO = "No change (both 0)";

describe("the Throughput panel's change column", () => {
	it('shows the Total change "13 (+25 %)" with a rise mark beside "Total 65 Work Items", before per day "0.43 per day (+25 %)", both marked better, when Then finished 52 and Now 65 in 30 days', () => {
		const container = aThroughputPanel({
			atThen: aSide(52, 1.73),
			atNow: aSide(65, 2.16),
		});
		const change = changeOf(container);
		const [totalLine, perDayLine] = Array.from(
			change.querySelectorAll<HTMLElement>("[data-change]"),
		);

		expect(theNowOfThroughput(container)).toMatch(/^Now Total 65 Work Items/);
		expect(readingOf(totalLine)).toBe("13 (+25 %)");
		expect(risesDrawn(totalLine)).toBe(1);
		expect(readingOf(perDayLine)).toBe("0.43 per day (+25 %)");
		expect(verdictsDrawn(change)).toEqual(["better", "better"]);
		expect(risesDrawn(change)).toBe(2);
	});

	it('reads "No change (both 0)" on the Total line and the per day line, with no mark and no percent, when Throughput is 0 then and 0 now', () => {
		const change = theChangeOfThroughput({
			atThen: aSide(0, 0),
			atNow: aSide(0, 0),
		});
		const reading = readingOf(change);

		expect(reading.split(BOTH_ZERO)).toHaveLength(3);
		expect(reading).not.toMatch(/%|Then was 0/);
		expect(marksDrawn(change)).toBe(0);
		expect(verdictsDrawn(change)).toEqual(["neutral", "neutral"]);
	});

	it('reads "30 (Then was 0)" with a rise mark and no percent on the Total line when Then finished 0 and Now 30 in 14 days', () => {
		const change = theChangeOfThroughput({
			atThen: aSide(0, 0),
			atNow: aSide(30, 2.14),
		});
		const reading = readingOf(change);

		expect(reading).toMatch(/30 \(Then was 0\)/);
		expect(reading).not.toMatch(/%|both 0/);
		expect(verdictsDrawn(change)).toEqual(["better", "better"]);
		expect(risesDrawn(change)).toBe(2);
	});

	it('shows the grey dash mark and "No change", never "both 0", on both lines when Then and Now each finished 91', () => {
		const change = theChangeOfThroughput({
			atThen: aSide(91, 3.03),
			atNow: aSide(91, 3.03),
		});
		const reading = readingOf(change);

		expect(reading.split("No change")).toHaveLength(3);
		expect(reading).not.toMatch(/both 0|%/);
		expect(verdictsDrawn(change)).toEqual(["neutral", "neutral"]);
		expect(marksDrawn(change)).toBe(2);
		expect(risesDrawn(change)).toBe(0);
	});

	it('shows "\u2014" and no change mark on the Total and per day lines when Then never captured Throughput', () => {
		const change = theChangeOfThroughput({
			atThen: {},
			atNow: aSide(65, 2.17),
		});

		expect(readingOf(change).split(DASH)).toHaveLength(3);
		expect(readingOf(change)).not.toMatch(/%|No change/);
		expect(verdictsDrawn(change)).toEqual([]);
		expect(marksDrawn(change)).toBe(0);
	});
});

describe("the WIP and Total Work Item Age panels, which keep their look for a value of 0 on both sides", () => {
	const ZERO_SPREAD = {
		average: aValue(0),
		median: aValue(0),
		min: aValue(0),
		max: aValue(0),
	};

	it.each([
		["WIP", WipPanel],
		["Total Work Item Age", WorkItemAgePanel],
	])(
		'reads "No change" with a drawn mark on every line of the %s panel',
		(title, Panel) => {
			const { container } = render(
				<Panel
					title={title}
					metric={{ atThen: ZERO_SPREAD, atNow: ZERO_SPREAD }}
					workItem="Work Item"
					workItems="Work Items"
					directionOfGood="lower"
				/>,
			);
			const change = changeOf(container);

			expect(readingOf(change)).not.toMatch(/both 0/);
			expect(readingOf(change).split("No change")).toHaveLength(5);
			expect(verdictsDrawn(change)).toEqual([
				"neutral",
				"neutral",
				"neutral",
				"neutral",
			]);
		},
	);
});
