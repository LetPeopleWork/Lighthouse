import { IFeature } from "./Feature";

export interface IFeatureOwner{
    name: string;
    id : number;    
    features: IFeature[];
    remainingWork: number;
    remainingFeatures: number;
}