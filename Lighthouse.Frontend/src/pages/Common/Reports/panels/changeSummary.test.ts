import fc from "fast-check";
import { describe, expect, it } from "vitest";
import type {
	IReportMetric,
	IReportValue,
	ReportMetricKey,
} from "../../../../models/Reports/Report";
import {
	type ChangeGroup,
	type ShownMetricLines,
	summariseChange,
} from "./changeSummary";
import { reportPanelRegistry } from "./reportPanelRegistry";
import {
	type ChangeVerdict,
	type LineComparison,
	lineComparisonsOf,
	type ReportMetricSideKey,
	type ThenNowChange,
} from "./thenNowComparison";

/**
 * The summary header's CHANGE side places each shown metric by the marks its own panel shows: improved when at
 * least one line is better and none worse, got worse the other way round, mixed when it moved both ways, and
 * unchanged when no line moved. A line that cannot be compared does not count; nothing is weighted.
 */

const CHANGES: Record<ChangeVerdict, ThenNowChange> = {
	better: { delta: -1, percent: -5, mark: "down", verdict: "better" },
	worse: { delta: 1, percent: 5, mark: "up", verdict: "worse" },
	neutral: { delta: 0, percent: 0, mark: "none", verdict: "neutral" },
};

const LINE_KEYS: ReportMetricSideKey[] = ["p95", "p85", "p70", "p50"];

type Line = ChangeVerdict | "dash";

const metric = (
	key: ReportMetricKey,
	...lines: readonly Line[]
): ShownMetricLines => ({
	key,
	lines: lines.map(
		(line, index): LineComparison => ({
			key: LINE_KEYS[index],
			change: line === "dash" ? null : CHANGES[line],
		}),
	),
});

describe("summariseChange", () => {
	it.each<[string, ChangeGroup["kind"], readonly Line[]]>([
		[
			"better on one line and unmoved on the rest",
			"improved",
			["better", "neutral", "neutral", "neutral"],
		],
		[
			"better on every line",
			"improved",
			["better", "better", "better", "better"],
		],
		[
			"worse on one line and unmoved on the rest",
			"worse",
			["neutral", "neutral", "worse", "neutral"],
		],
		["worse on every line", "worse", ["worse", "worse", "worse", "worse"]],
		[
			"worse at the slow end and better at the fast end",
			"mixed",
			["worse", "worse", "neutral", "better"],
		],
		[
			"better on three lines and worse on one",
			"mixed",
			["better", "better", "better", "worse"],
		],
		[
			"unmoved on every line",
			"unchanged",
			["neutral", "neutral", "neutral", "neutral"],
		],
	])("places a metric %s as %s", (_, kind, lines) => {
		expect(summariseChange([metric("cycle-time", ...lines)])).toEqual([
			{ kind, keys: ["cycle-time"] },
		]);
	});

	// A dash is a line that cannot be compared, including one never captured; it neither moves a metric nor holds it.
	it.each<[string, ChangeGroup["kind"], readonly Line[]]>([
		[
			"a dash beside a better line",
			"improved",
			["dash", "better", "neutral", "neutral"],
		],
		[
			"a dash beside a worse line",
			"worse",
			["dash", "neutral", "worse", "neutral"],
		],
		[
			"a dash beside unmoved lines",
			"unchanged",
			["dash", "neutral", "neutral", "neutral"],
		],
		[
			"dashes beside a better and a worse line",
			"mixed",
			["dash", "better", "dash", "worse"],
		],
	])("ignores %s, placing the metric as %s", (_, kind, lines) => {
		expect(summariseChange([metric("cycle-time", ...lines)])).toEqual([
			{ kind, keys: ["cycle-time"] },
		]);
	});

	it("lists no metric whose every line is a dash, nor one with no change line at all", () => {
		expect(
			summariseChange([
				metric("cycle-time", "dash", "dash", "dash", "dash"),
				metric("throughput"),
				metric("wip", "worse"),
			]),
		).toEqual([{ kind: "worse", keys: ["wip"] }]);
	});

	it("gives the groups as improved, got worse, mixed and unchanged, whatever order the metrics come in", () => {
		expect(
			summariseChange([
				metric("cycle-time", "neutral"),
				metric("throughput", "better", "worse"),
				metric("wip", "worse"),
				metric("work-item-age", "better"),
			]),
		).toEqual([
			{ kind: "improved", keys: ["work-item-age"] },
			{ kind: "worse", keys: ["wip"] },
			{ kind: "mixed", keys: ["throughput"] },
			{ kind: "unchanged", keys: ["cycle-time"] },
		]);
	});

	it("keeps the metrics of one group in the order they came in, and leaves out a group with none", () => {
		expect(
			summariseChange([
				metric("cycle-time", "better"),
				metric("throughput", "better"),
				metric("wip", "better", "neutral"),
				metric("work-item-age", "worse"),
			]),
		).toEqual([
			{ kind: "improved", keys: ["cycle-time", "throughput", "wip"] },
			{ kind: "worse", keys: ["work-item-age"] },
		]);
	});

	it.each<[string, readonly ShownMetricLines[]]>([
		["no metric is shown", []],
		[
			"no shown metric has a line that can be compared",
			[metric("cycle-time", "dash", "dash"), metric("throughput", "dash")],
		],
	])("gives no group when %s", (_, shown) => {
		expect(summariseChange(shown)).toEqual([]);
	});
});

describe("the CHANGE summary of a Throughput read", () => {
	const aValue = (value: number): IReportValue => ({
		value,
		sampleSize: 30,
		absentReason: null,
	});
	const placedAs = (read: IReportMetric) =>
		summariseChange([
			{
				key: "throughput",
				lines: lineComparisonsOf(read, reportPanelRegistry.throughput),
			},
		]);

	it.each<[string, IReportMetric, readonly ChangeGroup[]]>([
		[
			"a Total of 52 then and 65 now with the same 1.73 per day improved, because its Total line is judged too",
			{
				atThen: { total: aValue(52), perDay: aValue(1.73) },
				atNow: { total: aValue(65), perDay: aValue(1.73) },
			},
			[{ kind: "improved", keys: ["throughput"] }],
		],
		[
			"a Total of 0 then and 0 now unchanged",
			{
				atThen: { total: aValue(0), perDay: aValue(0) },
				atNow: { total: aValue(0), perDay: aValue(0) },
			},
			[{ kind: "unchanged", keys: ["throughput"] }],
		],
		[
			"a Total of 91 then and 91 now unchanged",
			{
				atThen: { total: aValue(91), perDay: aValue(3.03) },
				atNow: { total: aValue(91), perDay: aValue(3.03) },
			},
			[{ kind: "unchanged", keys: ["throughput"] }],
		],
		[
			"a Total of 0 then and 30 now improved",
			{
				atThen: { total: aValue(0), perDay: aValue(0) },
				atNow: { total: aValue(30), perDay: aValue(2.14) },
			},
			[{ kind: "improved", keys: ["throughput"] }],
		],
		[
			"a Then that never captured Throughput not judged",
			{ atThen: {}, atNow: { total: aValue(65), perDay: aValue(2.17) } },
			[],
		],
	])("places %s", (_, read, groups) => {
		expect(placedAs(read)).toEqual(groups);
	});

	// With equal windows the per day line is the Total over the same number of days, so the two never disagree on direction.
	it("L3 never judges Throughput mixed, because its Total and per day change never point in opposite directions", () => {
		const placedOver = (days: number, then: number, now: number) =>
			summariseChange([
				{
					key: "throughput",
					lines: lineComparisonsOf(
						{
							atThen: { total: aValue(then), perDay: aValue(then / days) },
							atNow: { total: aValue(now), perDay: aValue(now / days) },
						},
						reportPanelRegistry.throughput,
					),
				},
			]);
		const comparedLines = (days: number, then: number, now: number) =>
			lineComparisonsOf(
				{
					atThen: { total: aValue(then), perDay: aValue(then / days) },
					atNow: { total: aValue(now), perDay: aValue(now / days) },
				},
				reportPanelRegistry.throughput,
			).map(({ key }) => key);

		fc.assert(
			fc.property(
				fc.integer({ min: 14, max: 400 }),
				fc.integer({ min: 0, max: 3000 }),
				fc.integer({ min: 0, max: 3000 }),
				(days, then, now) => {
					expect(comparedLines(days, then, now)).toEqual(["total", "perDay"]);
					expect(
						placedOver(days, then, now),
						`${then} then, ${now} now over ${days} days`,
					).not.toContainEqual(expect.objectContaining({ kind: "mixed" }));
				},
			),
		);
		// Totals differing by 1 over a long window move the Total and leave per day unmoved: improved, not mixed.
		expect(placedOver(365, 1000, 1001)).toEqual([
			{ kind: "improved", keys: ["throughput"] },
		]);
		// Both totals 0 move nothing.
		expect(placedOver(30, 0, 0)).toEqual([
			{ kind: "unchanged", keys: ["throughput"] },
		]);
	});
});
