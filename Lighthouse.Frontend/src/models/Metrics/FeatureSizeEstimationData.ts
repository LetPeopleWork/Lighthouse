export type FeatureSizeEstimationStatus = "NotConfigured" | "NoData" | "Ready";

export interface IFeatureEstimationDataPoint {
	featureId: number;
	estimationNumericValue: number;
	estimationDisplayValue: string;
}

export interface IFeatureSizeEstimationResponse {
	status: FeatureSizeEstimationStatus;
	estimationUnit: string | null;
	useNonNumericEstimation: boolean;
	categoryValues: string[];
	featureEstimations: IFeatureEstimationDataPoint[];
}
