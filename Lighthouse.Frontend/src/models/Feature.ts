import { IWhenForecast } from "./Forecasts/WhenForecast";

export interface IFeature {
    name: string;
    id: number;
    lastUpdated: Date;
    remainingWork: { [key: number]: number };
    milestoneLikelihood: { [key: number]: number };
    projectId: number;
    projectName: string;
    forecasts: IWhenForecast[];
}


export interface DictionaryObject{
    readonly [key: number]: number
}

export class Feature implements IFeature {
    name!: string;
    id!: number;
    lastUpdated!: Date;
    projectId: number;
    projectName: string;
    remainingWork!: DictionaryObject;
    milestoneLikelihood! : DictionaryObject;
    forecasts!: IWhenForecast[];

    constructor(name: string, id: number, lastUpdated: Date, projectId: number, projectName: string, remainingWork: DictionaryObject, milestoneLikelihood: DictionaryObject, forecasts: IWhenForecast[]) {
        this.name = name;
        this.id = id;
        this.lastUpdated = lastUpdated;
        this.projectId = projectId;
        this.projectName = projectName;
        this.remainingWork = remainingWork;
        this.milestoneLikelihood = milestoneLikelihood;
        this.forecasts = forecasts;
    }

    getRemainingWorkForTeam(id: number): number {
        return this.remainingWork[id] ?? 0;
    }

    getAllRemainingWork(): number {
        let totalRemainingWork = 0;
        const values = Object.values(this.remainingWork);

        for (const work of values) {
            totalRemainingWork += work;
        }

        return totalRemainingWork;
    }

    getMilestoneLikelihood(milestoneId: number){
        return this.milestoneLikelihood[milestoneId] ?? 0;
    }
}