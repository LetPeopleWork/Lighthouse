import type { IEntityReference } from "./EntityReference";

export interface IFeatureOwner {
	name: string;
	id: number;
	lastUpdated: Date;
	features: IEntityReference[];
	remainingFeatures: number;
	tags: string[];
	serviceLevelExpectationProbability: number;
	serviceLevelExpectationRange: number;
	systemWIPLimit: number;
}
