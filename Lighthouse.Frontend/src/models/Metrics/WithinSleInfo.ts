import type { IInfoWidgetComparison } from "./InfoWidgetData";

export type WithinSleVerdict = "NoSle" | "TooFew" | "Measured";

interface WithinSleInfoCommon {
	closedCount: number;
	withinCount: number;
	comparison: IInfoWidgetComparison;
}

export interface IWithinSleMeasured extends WithinSleInfoCommon {
	verdict: "Measured";
	slePercentile: number;
	sleDays: number;
	sharePercent: number;
}

export interface IWithinSleTooFew extends WithinSleInfoCommon {
	verdict: "TooFew";
	slePercentile: number;
	sleDays: number;
	sharePercent?: null;
}

export interface IWithinSleNoSle extends WithinSleInfoCommon {
	verdict: "NoSle";
	slePercentile?: null;
	sleDays?: null;
	sharePercent?: null;
}

export type IWithinSleInfo =
	| IWithinSleMeasured
	| IWithinSleTooFew
	| IWithinSleNoSle;
