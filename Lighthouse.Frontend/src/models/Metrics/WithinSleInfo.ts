import type { IInfoWidgetComparison } from "./InfoWidgetData";

export type WithinSleVerdict = "NoSle" | "TooFew" | "Measured";

export interface IWithinSleInfo {
	verdict: WithinSleVerdict;
	closedCount: number;
	withinCount: number;
	slePercentile?: number | null;
	sleDays?: number | null;
	sharePercent?: number | null;
	comparison: IInfoWidgetComparison;
}
