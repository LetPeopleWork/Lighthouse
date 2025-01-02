import type { IFeature } from "./Feature";
import type { IProgressable } from "./IProgressable";

export interface IFeatureOwner extends IProgressable {
	name: string;
	id: number;
	features: IFeature[];
	remainingFeatures: number;
}
