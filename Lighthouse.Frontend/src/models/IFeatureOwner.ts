import { IFeature } from "./Feature";
import { IProgressable } from "./IProgressable";

export interface IFeatureOwner extends IProgressable {
    name: string;
    id : number;    
    features: IFeature[];
    remainingFeatures: number;
}