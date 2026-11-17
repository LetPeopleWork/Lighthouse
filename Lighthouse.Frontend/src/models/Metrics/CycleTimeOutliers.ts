/** Whether the baseline of the Cycle Time process behaviour chart gave limits to judge outliers against. */
export type CycleTimeOutlierStatus = "Ready" | "NoLimits";

/** The backend's verdict on which closed Work Items of a date range are outliers. */
export interface ICycleTimeOutliers {
	readonly status: CycleTimeOutlierStatus;
	/** The unrounded upper natural process limit in days, or null without limits. */
	readonly upperNaturalProcessLimit: number | null;
	readonly outlierWorkItemIds: number[];
}
