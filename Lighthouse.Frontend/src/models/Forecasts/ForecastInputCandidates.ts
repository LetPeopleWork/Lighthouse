export interface IFeatureCandidate {
	id: number;
	name: string;
	remainingWork: number;
}

export interface IForecastInputCandidates {
	currentWipCount: number;
	backlogCount: number;
	features: IFeatureCandidate[];
}
