import { Forecast } from './Forecast';
import { IData } from './IData';
import { Team } from './Team'

export class Project implements IData {
    name!: string;
    id! : number;
    remainingWork!: number;
    involvedTeams!: Team[];
    forecasts!: Forecast[];
    lastUpdated!: Date;
    features!: number;

    constructor(name: string, id: number, remainingWork: number, involvedTeams: Team[], features: number, forecasts: Forecast[], lastUpdated: Date) {
        this.name = name;
        this.id = id;
        this.remainingWork = remainingWork;
        this.involvedTeams = involvedTeams;
        this.forecasts = forecasts;
        this.lastUpdated = lastUpdated;
        this.features = features;
    }
}