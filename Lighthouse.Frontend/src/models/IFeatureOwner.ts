import type { IFeature } from "./Feature";
import type { IProgressable } from "./IProgressable";

export interface IFeatureOwner extends IProgressable {
	name: string;
	id: number;
	lastUpdated: Date;
	features: IFeature[];
	remainingFeatures: number;
	tags: string[];
	serviceLevelExpectationProbability: number;
	serviceLevelExpectationRange: number;
	systemWIPLimit: number;
}
