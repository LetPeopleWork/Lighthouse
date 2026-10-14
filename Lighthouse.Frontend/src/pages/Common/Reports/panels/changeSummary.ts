import type { ReportMetricKey } from "../../../../models/Reports/Report";
import type { LineComparison } from "./thenNowComparison";

// RED scaffold for summariseChange, written with its tests; the change that builds it removes the marker.
export const __SCAFFOLD__ = true;

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

/** Which way each shown metric moved, grouped as improved, got worse, mixed and unchanged, in that order. */
export const summariseChange = (
	shown: readonly ShownMetricLines[],
): readonly ChangeGroup[] => {
	throw new Error(
		`Not yet implemented -- RED scaffold: the change of ${shown.length} shown metrics`,
	);
};
