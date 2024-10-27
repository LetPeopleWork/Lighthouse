import { Feature } from "../Feature";
import { IFeatureOwner } from "../IFeatureOwner";
import { IProject, Project } from "../Project/Project";


export interface ITeam extends IFeatureOwner {
    name: string;
    id: number;
    projects: IProject[];
    featureWip: number;
}

export class Team implements ITeam {
    name!: string;
    id!: number;
    projects!: Project[];
    features!: Feature[];
    featureWip: number;

    constructor(name: string, id: number, projects: Project[], features: Feature[], featureWip: number) {
        this.name = name;
        this.id = id;
        this.projects = projects;
        this.features = features;
        this.featureWip = featureWip;
    }

    get remainingWork(): number {
        return this.features.reduce((acc, feature) => acc + feature.getRemainingWorkForTeam(this.id), 0);
    }

    get totalWork(): number {
        return this.features.reduce((acc, feature) => acc + feature.getTotalWorkForTeam(this.id), 0);
    }

    get remainingFeatures(): number {
        return this.features.length;
    }
}