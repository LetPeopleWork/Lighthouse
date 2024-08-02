import { IWhenForecast } from "./Forecasts/WhenForecast";

export interface IFeature {
    name: string;
    id: number;
    url: string | null;
    lastUpdated: Date;
    remainingWork: { [key: number]: number };
    milestoneLikelihood: { [key: number]: number };
    projects: {[key: number]: string };
    forecasts: IWhenForecast[];
}


export interface DictionaryObject<TValue>{
    readonly [key: number]: TValue
}

export class Feature implements IFeature {
    name!: string;
    id!: number;
    url: string | null;
    lastUpdated!: Date;
    projects: DictionaryObject<string>;
    remainingWork: DictionaryObject<number>;
    milestoneLikelihood: DictionaryObject<number>;
    forecasts!: IWhenForecast[];

    constructor(name: string, id: number, url: string | null, lastUpdated: Date, projects: DictionaryObject<string>, remainingWork: DictionaryObject<number>, milestoneLikelihood: DictionaryObject<number>, forecasts: IWhenForecast[]) {
        this.name = name;
        this.id = id;
        this.url = url;
        this.lastUpdated = lastUpdated;
        this.projects = projects;
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