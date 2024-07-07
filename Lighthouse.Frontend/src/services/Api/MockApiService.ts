import { Forecast } from '../../models/Forecast';
import { Project } from '../../models/Project';
import { Team } from '../../models/Team';
import { IApiService } from './IApiService';

export class MockApiService implements IApiService {
    private useDelay: boolean;
    private throwError: boolean;

    private binaryBlazers = new Team("Binary Blazers", 1, 20, 2, 4);
    private mavericks = new Team("Mavericks", 2, 5, 1, 1);
    private cyberSultans = new Team("Cyber Sultans", 3, 35, 3, 2);
    private techEagles = new Team("Tech Eagles", 4, 17, 1, 3);

    constructor(useDelay: boolean, throwError: boolean = false) {
        this.useDelay = useDelay;
        this.throwError = throwError;
    }

    async getTeams(): Promise<Team[]> {
        await this.delay();

        return [
            this.binaryBlazers,
            this.mavericks,
            this.cyberSultans,
            this.techEagles,
        ];
    }

    async deleteTeam(id: number): Promise<void> {
        console.log(`'Deleting' Team with id ${id}`)
        await this.delay();
    }

    async getVersion(): Promise<string> {
        await this.delay()
        return "v1.33.7";
    }

    async getProjectOverviewData(): Promise<Project[]> {
        await this.delay();

        const lastUpdated = new Date("06/23/2024 12:41");

        const projects: Project[] = [
            new Project("Release 1.33.7", 1, 35, [this.binaryBlazers], [new Forecast(50, new Date("07/31/2024")), new Forecast(70, new Date("08/05/2024")), new Forecast(85, new Date("08/09/2024")), new Forecast(95, new Date("08/14/2024"))], lastUpdated),
            new Project("Release 42", 2, 28, [this.mavericks, this.cyberSultans], [new Forecast(50, new Date("07/09/2024")), new Forecast(70, new Date("07/11/2024")), new Forecast(85, new Date("07/14/2024")), new Forecast(95, new Date("07/17/2024"))], lastUpdated),
            new Project("Release Codename Daniel", 3, 33, [this.binaryBlazers, this.techEagles, this.mavericks, this.cyberSultans], [new Forecast(50, new Date("07/07/2024")), new Forecast(70, new Date("07/09/2024")), new Forecast(85, new Date("07/12/2024")), new Forecast(95, new Date("07/16/2024"))], lastUpdated)
        ]

        return projects;
    }

    delay() {
        if (this.throwError){
            throw new Error('Simulated Error');
        }

        if (this.useDelay) {
            const randomDelay: number = Math.random() * 1000;
            return new Promise(resolve => setTimeout(resolve, randomDelay));
        }

        return Promise.resolve();
    }
}