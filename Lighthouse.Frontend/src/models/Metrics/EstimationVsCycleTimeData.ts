export type EstimationVsCycleTimeStatus = "NotConfigured" | "NoData" | "Ready";

export interface IEstimationVsCycleTimeDiagnostics {
	totalCount: number;
	mappedCount: number;
	unmappedCount: number;
	invalidCount: number;
}

export interface IEstimationVsCycleTimeDataPoint {
	workItemIds: number[];
	estimationNumericValue: number;
	estimationDisplayValue: string;
	cycleTime: number;
}

export interface IEstimationVsCycleTimeResponse {
	status: EstimationVsCycleTimeStatus;
	diagnostics: IEstimationVsCycleTimeDiagnostics;
	estimationUnit: string | null;
	useNonNumericEstimation: boolean;
	categoryValues: string[];
	dataPoints: IEstimationVsCycleTimeDataPoint[];
}
