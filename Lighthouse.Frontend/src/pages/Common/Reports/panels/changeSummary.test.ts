import { describe, expect, it } from "vitest";
import type { ReportMetricKey } from "../../../../models/Reports/Report";
import {
	type ChangeGroup,
	type ShownMetricLines,
	summariseChange,
} from "./changeSummary";
import type {
	ChangeVerdict,
	LineComparison,
	ReportMetricSideKey,
	ThenNowChange,
} from "./thenNowComparison";

/**
 * The summary header's CHANGE side places each shown metric by the marks its own panel shows: improved when at
 * least one line is better and none worse, got worse the other way round, mixed when it moved both ways, and
 * unchanged when no line moved. A line that cannot be compared does not count; nothing is weighted.
 */

const CHANGES: Record<ChangeVerdict, ThenNowChange> = {
	better: { delta: -1, percent: -5, mark: "▼", verdict: "better" },
	worse: { delta: 1, percent: 5, mark: "▲", verdict: "worse" },
	neutral: { delta: 0, percent: 0, mark: "–", verdict: "neutral" },
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
	// @us-05b @slice-05b @contract-shape:pure-function
	it.skip.each<[string, ChangeGroup["kind"], readonly Line[]]>([
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

	// @us-05b @slice-05b @error @contract-shape:pure-function
	// A dash is a line that cannot be compared, including one never captured; it neither moves a metric nor holds it.
	it.skip.each<[string, ChangeGroup["kind"], readonly Line[]]>([
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

	// @us-05b @slice-05b @error @contract-shape:pure-function
	it.skip("lists no metric whose every line is a dash, nor one with no change line at all", () => {
		expect(
			summariseChange([
				metric("cycle-time", "dash", "dash", "dash", "dash"),
				metric("throughput"),
				metric("wip", "worse"),
			]),
		).toEqual([{ kind: "worse", keys: ["wip"] }]);
	});

	// @us-05b @slice-05b @contract-shape:pure-function
	it.skip("gives the groups as improved, got worse, mixed and unchanged, whatever order the metrics come in", () => {
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

	// @us-05b @slice-05b @contract-shape:pure-function
	it.skip("keeps the metrics of one group in the order they came in, and leaves out a group with none", () => {
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

	// @us-05b @slice-05b @boundary @contract-shape:pure-function
	it.skip.each<[string, readonly ShownMetricLines[]]>([
		["no metric is shown", []],
		[
			"no shown metric has a line that can be compared",
			[metric("cycle-time", "dash", "dash"), metric("throughput", "dash")],
		],
	])("gives no group when %s", (_, shown) => {
		expect(summariseChange(shown)).toEqual([]);
	});
});
