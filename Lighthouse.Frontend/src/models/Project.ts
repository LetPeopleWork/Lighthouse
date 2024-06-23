import { Forecast } from './Forecast';
import { Team } from './Team'

export class Project {
    name!: string;
    id! : number;
    remainingWork!: number;
    involvedTeams!: Team[];
    forecasts!: Forecast[];
    lastUpdated!: Date;

    constructor(name: string, id: number, remainingWork: number, involvedTeams: Team[], forecasts: Forecast[], lastUpdated: Date) {
        this.name = name;
        this.id = id;
        this.remainingWork = remainingWork;
        this.involvedTeams = involvedTeams;
        this.forecasts = forecasts;
        this.lastUpdated = lastUpdated;
    }
}