import { IWhenForecast } from "./Forecasts/WhenForecast";

export interface IFeature {
    name: string;
    id: number;
    url: string | null;
    lastUpdated: Date;
    remainingWork: { [key: number]: number };
    totalWork: { [key: number]: number };
    milestoneLikelihood: { [key: number]: number };
    projects: { [key: number]: string };
    forecasts: IWhenForecast[];
}


export interface DictionaryObject<TValue> {
    readonly [key: number]: TValue
}

export class Feature implements IFeature {
    name!: string;
    id!: number;
    url: string | null;
    lastUpdated!: Date;
    projects: DictionaryObject<string>;
    remainingWork: DictionaryObject<number>;
    totalWork: { [key: number]: number };
    milestoneLikelihood: DictionaryObject<number>;
    forecasts!: IWhenForecast[];

    constructor(name: string, id: number, url: string | null, lastUpdated: Date, projects: DictionaryObject<string>, remainingWork: DictionaryObject<number>, totalWork: { [key: number]: number }, milestoneLikelihood: DictionaryObject<number>, forecasts: IWhenForecast[]) {
        this.name = name;
        this.id = id;
        this.url = url;
        this.lastUpdated = lastUpdated;
        this.projects = projects;
        this.remainingWork = remainingWork;
        this.totalWork = totalWork;
        this.milestoneLikelihood = milestoneLikelihood;
        this.forecasts = forecasts;
    }

    getRemainingWorkForTeam(id: number): number {
        return this.getWorkForTeam(id, this.remainingWork);
    }

    getTotalWorkForTeam(id: number): number {
        return this.getWorkForTeam(id, this.totalWork);
    }

    getCompletionPercentageForTeam(id: number) : number {
        return parseFloat(((100 / this.getTotalWorkForTeam(id)) * this.getRemainingWorkForTeam(id)).toFixed(2))
    }

    getRemainingWorkForFeature(): number {
        return this.getAllWork(this.remainingWork);
    }
    
    getTotalWorkForFeature(): number {
        return this.getAllWork(this.totalWork);
    }

    getCompletionPercentageForFeature() : number {
        return parseFloat(((100 / this.getTotalWorkForFeature()) * this.getRemainingWorkForFeature()).toFixed(2))
    }

    getMilestoneLikelihood(milestoneId: number) {
        return this.milestoneLikelihood[milestoneId] ?? 0;
    }

    getAllWork(work: { [key: number]: number }): number {
        let totalWork = 0;
        const values = Object.values(work);

        for (const work of values) {
            totalWork += work;
        }

        return totalWork;
    }

    getWorkForTeam(id: number, work: { [key: number]: number }): number {
        return work[id] ?? 0;
    }
}