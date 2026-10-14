import type { ReportMetricKey } from "../../../../models/Reports/Report";
import type { LineComparison } from "./thenNowComparison";

export type ChangeKind = "improved" | "worse" | "mixed" | "unchanged";

/** A shown metric and how its panel judged each of its change lines. */
export interface ShownMetricLines {
	readonly key: ReportMetricKey;
	readonly lines: readonly LineComparison[];
}

/** The metrics that moved the same way, in the order their panels stack. */
export interface ChangeGroup {
	readonly kind: ChangeKind;
	readonly keys: readonly ReportMetricKey[];
}

const KIND_ORDER: readonly ChangeKind[] = [
	"improved",
	"worse",
	"mixed",
	"unchanged",
];

/** Null for a metric with no line that can be compared, which says nothing about how it moved. */
const kindOf = (lines: readonly LineComparison[]): ChangeKind | null => {
	const verdicts = lines.flatMap(({ change }) =>
		change === null ? [] : [change.verdict],
	);
	if (verdicts.length === 0) return null;
	const better = verdicts.includes("better");
	const worse = verdicts.includes("worse");
	if (better && worse) return "mixed";
	if (better) return "improved";
	if (worse) return "worse";
	return "unchanged";
};

/** Which way each shown metric moved, grouped as improved, got worse, mixed and unchanged, in that order. */
export const summariseChange = (
	shown: readonly ShownMetricLines[],
): readonly ChangeGroup[] => {
	const placed = shown.map(({ key, lines }) => ({ key, kind: kindOf(lines) }));
	return KIND_ORDER.map((kind) => ({
		kind,
		keys: placed.filter((metric) => metric.kind === kind).map(({ key }) => key),
	})).filter((group) => group.keys.length > 0);
};
