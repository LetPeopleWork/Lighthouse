import { Feature } from './Feature';
import { IFeatureOwner } from './IFeatureOwner';
import { ITeam, Team } from './Team'

export interface IProject extends IFeatureOwner {
    name: string;
    id: number;
    involvedTeams: ITeam[];
    lastUpdated: Date;
}

export class Project implements IProject {
    name!: string;
    id!: number;

    features!: Feature[];
    involvedTeams!: Team[];

    lastUpdated!: Date;

    constructor(name: string, id: number, involvedTeams: Team[], features: Feature[], lastUpdated: Date) {
        this.name = name;
        this.id = id;
        this.involvedTeams = involvedTeams;
        this.lastUpdated = lastUpdated;
        this.features = features || [];
    }

    get remainingWork(): number {
        const totalRemainingWork = this.features.reduce((acc, feature) => acc + feature.getAllRemainingWork(), 0);
    
        return totalRemainingWork;
    }

    get remainingFeatures(): number {
        return this.features.length;
    }
}