export type WithinSleHorizon = 30 | 60 | 90;

export type WithinSleDayOrigin = "Recorded" | "FilledIn";

export interface IWithinSleDay {
	recordedAt: string;
	horizon: number;
	closedCount: number;
	withinCount: number;
	slePercentile: number;
	sleDays: number;
	origin: WithinSleDayOrigin;
	verdict: "Measured" | "TooFew";
	sharePercent: number | null;
}

export interface IWithinSleOverTime {
	currentSlePercentile: number | null;
	currentSleDays: number | null;
	days: IWithinSleDay[];
}
