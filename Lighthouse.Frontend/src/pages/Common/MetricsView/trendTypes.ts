export type TrendDirection = "up" | "down" | "flat" | "none";

export type TrendDetailRow = {
	readonly label: string;
	readonly currentValue: string;
	readonly previousValue: string;
};

export type TrendPayload = {
	readonly direction: TrendDirection;
	readonly metricLabel: string;
	readonly currentLabel?: string;
	readonly currentValue?: string;
	readonly previousLabel?: string;
	readonly previousValue?: string;
	readonly percentageDelta?: string;
	readonly detailRows?: readonly TrendDetailRow[];
};
