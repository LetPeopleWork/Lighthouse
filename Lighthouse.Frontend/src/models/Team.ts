import { Feature } from "./Feature";
import { IFeatureOwner } from "./IFeatureOwner";
import { IProject, Project } from "./Project";


export interface ITeam extends IFeatureOwner {
    name: string;
    id: number;
    projects: IProject[];
}

export class Team implements ITeam {
    name!: string;
    id!: number;
    projects!: Project[];
    features!: Feature[];

    constructor(name: string, id: number, projects: Project[], features: Feature[]) {
        this.name = name;
        this.id = id;
        this.projects = projects;
        this.features = features;
    }
    
    get remainingWork(): number {
        return this.features.reduce((acc, feature) => acc += feature.getRemainingWorkForTeam(this.id), 0);
    }

    get remainingFeatures(): number {
        return this.features.length;
    }
}