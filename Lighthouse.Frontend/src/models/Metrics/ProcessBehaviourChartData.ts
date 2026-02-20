export type SpecialCauseType =
	| "None"
	| "LargeChange"
	| "ModerateChange"
	| "ModerateShift"
	| "SmallShift";

export type BaselineStatus =
	| "BaselineMissing"
	| "BaselineInvalid"
	| "InsufficientData"
	| "Ready";

export type XAxisKind = "Date" | "DateTime";

export type ProcessBehaviourChartDataPoint = {
	readonly xValue: string;
	readonly yValue: number;
	readonly specialCauses: SpecialCauseType[];
	readonly workItemIds: number[];
};

export type ProcessBehaviourChartData = {
	readonly status: BaselineStatus;
	readonly statusReason: string;
	readonly xAxisKind: XAxisKind;
	readonly average: number;
	readonly upperNaturalProcessLimit: number;
	readonly lowerNaturalProcessLimit: number;
	readonly baselineConfigured: boolean;
	readonly dataPoints: ProcessBehaviourChartDataPoint[];
};
