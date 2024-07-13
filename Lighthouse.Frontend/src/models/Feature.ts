import { IWhenForecast } from "./Forecasts/WhenForecast";

export interface IFeature {
    name: string;
    id: number;
    lastUpdated: Date;
    remainingWork: { [key: number]: number };
    forecasts: IWhenForecast[];
}


export interface DictionaryObject{
    readonly [key: number]: number
}

export class Feature implements IFeature {
    name!: string;
    id!: number;
    lastUpdated!: Date;
    remainingWork!: DictionaryObject;
    forecasts!: IWhenForecast[];

    constructor(name: string, id: number, lastUpdated: Date, remainingWork: DictionaryObject, forecasts: IWhenForecast[]) {
        this.name = name;
        this.id = id;
        this.lastUpdated = lastUpdated;
        this.remainingWork = remainingWork;
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
}