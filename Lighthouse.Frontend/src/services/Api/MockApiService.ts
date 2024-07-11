import { WhenForecast } from '../../models/WhenForecast';
import { Project } from '../../models/Project';
import { Team } from '../../models/Team';
import { IApiService } from './IApiService';
import { Feature } from '../../models/Feature';

export class MockApiService implements IApiService {
    private useDelay: boolean;
    private throwError: boolean;    

    private lastUpdated = new Date("06/23/2024 12:41");
    
    private feature1 = new Feature('Feature 1', 1, new Date(), { 1: 10 }, [new WhenForecast(50, new Date("07/31/2024")), new WhenForecast(70, new Date("08/05/2024")), new WhenForecast(85, new Date("08/09/2024")), new WhenForecast(95, new Date("08/14/2024"))]);
    private feature2 = new Feature('Feature 2', 2, new Date(), { 2: 5 }, [new WhenForecast(50, new Date("07/09/2024")), new WhenForecast(70, new Date("07/11/2024")), new WhenForecast(85, new Date("07/14/2024")), new WhenForecast(95, new Date("07/17/2024"))]);
    private feature3 = new Feature('Feature 3', 3, new Date(), { 3: 7, 2: 15 }, [new WhenForecast(50, new Date("07/07/2024")), new WhenForecast(70, new Date("07/09/2024")), new WhenForecast(85, new Date("07/12/2024")), new WhenForecast(95, new Date("07/16/2024"))]);
    private feature4 = new Feature('Feature 4', 4, new Date(), { 1: 3, 4: 9 }, [new WhenForecast(50, new Date("07/31/2024")), new WhenForecast(70, new Date("08/05/2024")), new WhenForecast(85, new Date("08/09/2024")), new WhenForecast(95, new Date("08/14/2024"))]);

    private binaryBlazers = new Team("Binary Blazers", 1, [], [this.feature1, this.feature4]);
    private mavericks = new Team("Mavericks", 2, [], [this.feature2, this.feature3]);
    private cyberSultans = new Team("Cyber Sultans", 3, [], [this.feature3]);
    private techEagles = new Team("Tech Eagles", 4, [], [this.feature4]);

    private release_1337 = new Project("Release 1.33.7", 1, [this.binaryBlazers], [this.feature1], this.lastUpdated);
    private release_42 = new Project("Release 42", 2, [this.mavericks], [this.feature2], this.lastUpdated);
    private release_codename_daniel = new Project("Release Codename Daniel", 3, [this.binaryBlazers, this.techEagles, this.mavericks, this.cyberSultans], [this.feature3, this.feature4], this.lastUpdated);


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

    async deleteProject(id: number): Promise<void> {
        console.log(`'Deleting' Project with id ${id}`)
        await this.delay();

    }

    async getVersion(): Promise<string> {
        await this.delay()
        return "v1.33.7";
    }

    async getProjects(): Promise<Project[]> {
        await this.delay();
        
        return [this.release_1337, this.release_42, this.release_codename_daniel];
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