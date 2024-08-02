import { WhenForecast } from '../../models/Forecasts/WhenForecast';
import { Project } from '../../models/Project/Project';
import { Team } from '../../models/Team/Team';
import { IApiService } from './IApiService';
import { Feature } from '../../models/Feature';
import { Throughput } from '../../models/Forecasts/Throughput';
import { ManualForecast } from '../../models/Forecasts/ManualForecast';
import { HowManyForecast } from '../../models/Forecasts/HowManyForecast';
import dayjs from 'dayjs';
import { Milestone } from '../../models/Project/Milestone';
import { IWorkTrackingSystemConnection, WorkTrackingSystemConnection } from '../../models/WorkTracking/WorkTrackingSystemConnection';
import { WorkTrackingSystemOption } from '../../models/WorkTracking/WorkTrackingSystemOption';
import { ITeamSettings, TeamSettings } from '../../models/Team/TeamSettings';
import { IProjectSettings, ProjectSettings } from '../../models/Project/ProjectSettings';
import { LoremIpsum } from "lorem-ipsum";
import { IRefreshSettings, RefreshSettings } from '../../models/AppSettings/RefreshSettings';

export class MockApiService implements IApiService {
    private useDelay: boolean;
    private throwError: boolean;

    private lastUpdated = new Date("06/23/2024 12:41");

    private dayMultiplier: number = 24 * 60 * 60 * 1000;
    private today: number = Date.now();

    private feature1 = new Feature('Feature 1', 1, "https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/1", new Date(), { 1: "Release 1.33.7" }, { 1: 10 }, {}, [new WhenForecast(50, new Date(this.today + 5 * this.dayMultiplier)), new WhenForecast(70, new Date(this.today + 10 * this.dayMultiplier)), new WhenForecast(85, new Date(this.today + 17 * this.dayMultiplier)), new WhenForecast(95, new Date(this.today + 25 * this.dayMultiplier))]);
    private feature2 = new Feature('Feature 2', 2, "https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/2", new Date(), { 2: "Release 42" }, { 2: 5 }, { 0: 89.3 }, [new WhenForecast(50, new Date(this.today + 15 * this.dayMultiplier)), new WhenForecast(70, new Date(this.today + 28 * this.dayMultiplier)), new WhenForecast(85, new Date(this.today + 35 * this.dayMultiplier)), new WhenForecast(95, new Date(this.today + 45 * this.dayMultiplier))]);
    private feature3 = new Feature('Feature 3', 3, "https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/3", new Date(), { 3: "Release Codename Daniel" }, { 3: 7, 2: 15 }, { 1: 78.9, 2: 65.0 }, [new WhenForecast(50, new Date(this.today + 7 * this.dayMultiplier)), new WhenForecast(70, new Date(this.today + 12 * this.dayMultiplier)), new WhenForecast(85, new Date(this.today + 14 * this.dayMultiplier)), new WhenForecast(95, new Date(this.today + 16 * this.dayMultiplier))]);
    private feature4 = new Feature('Feature 4', 4, "https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/4", new Date(), { 3: "Release Codename Daniel", 1: "Release 1.33.7" }, { 1: 3, 4: 9 }, { 1: 45.5, 2: 78.0 }, [new WhenForecast(50, new Date(this.today + 21 * this.dayMultiplier)), new WhenForecast(70, new Date(this.today + 37 * this.dayMultiplier)), new WhenForecast(85, new Date(this.today + 55 * this.dayMultiplier)), new WhenForecast(95, new Date(this.today + 71 * this.dayMultiplier))]);

    private binaryBlazers = new Team("Binary Blazers", 1, [], [this.feature1, this.feature4], 1);
    private mavericks = new Team("Mavericks", 2, [], [this.feature2, this.feature3], 2);
    private cyberSultans = new Team("Cyber Sultans", 3, [], [this.feature3], 1);
    private techEagles = new Team("Tech Eagles", 4, [], [this.feature4], 2);

    private milestone1 = new Milestone(0, "Milestone 1", new Date(this.today + 14 * this.dayMultiplier));
    private milestone2 = new Milestone(1, "Milestone 2", new Date(this.today + 28 * this.dayMultiplier));
    private milestone3 = new Milestone(2, "Milestone 3", new Date(this.today + 90 * this.dayMultiplier));

    private release_1337 = new Project("Release 1.33.7", 1, [this.binaryBlazers], [this.feature1], [], this.lastUpdated);
    private release_42 = new Project("Release 42", 2, [this.mavericks], [this.feature2], [this.milestone1], this.lastUpdated);
    private release_codename_daniel = new Project("Release Codename Daniel", 3, [this.binaryBlazers, this.techEagles, this.mavericks, this.cyberSultans], [this.feature3, this.feature4], [this.milestone2, this.milestone3], this.lastUpdated);


    constructor(useDelay: boolean, throwError: boolean = false) {
        this.useDelay = useDelay;
        this.throwError = throwError;
    }

    async updateThroughput(teamId: number): Promise<void> {
        console.log(`Updating Throughput for Team ${teamId}`);

        await this.delay();
    }

    async getThroughput(teamId: number): Promise<Throughput> {
        console.log(`Getting Throughput for Team ${teamId}`);

        await this.delay();

        const randomThroughput = this.generateThroughput();
        return new Throughput(randomThroughput);
    }

    async updateForecast(teamId: number): Promise<void> {
        console.log(`Updating Forecast for Team ${teamId}`);

        await this.delay();
    }

    async runManualForecast(teamId: number, remainingItems: number, targetDate: Date): Promise<ManualForecast> {
        console.log(`Updating Forecast for Team ${teamId}: How Many: ${remainingItems} - When: ${targetDate}`);
        await this.delay();

        const howManyForecasts = [
            new HowManyForecast(50, 42), new HowManyForecast(70, 31), new HowManyForecast(85, 12), new HowManyForecast(95, 7)
        ]

        const whenForecasts = [
            new WhenForecast(50, dayjs().add(2, 'days').toDate()), new WhenForecast(70, dayjs().add(5, 'days').toDate()), new WhenForecast(85, dayjs().add(9, 'days').toDate()), new WhenForecast(95, dayjs().add(12, 'days').toDate())
        ]

        const likelihood = Math.round(Math.random() * 10000) / 100;

        return new ManualForecast(remainingItems, targetDate, whenForecasts, howManyForecasts, likelihood);
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

    async getTeam(id: number): Promise<Team | null> {
        console.log(`Getting Team with id ${id}`)
        const teams = await this.getTeams();
        const team = teams.find(team => team.id === id);
        return team || null;
    }

    async deleteTeam(id: number): Promise<void> {
        console.log(`'Deleting' Team with id ${id}`)
        await this.delay();
    }

    async getTeamSettings(id: number): Promise<ITeamSettings> {
        console.log(`Getting Settings for team ${id}`);

        await this.delay();

        return new TeamSettings(1, "My Team", 30, 1, "[System.TeamProject] = \"My Team\"", ["User Story", "Bug"], 12, "");
    }

    async updateTeam(teamSettings: ITeamSettings): Promise<ITeamSettings> {
        console.log(`Updating Team ${teamSettings.name}`);

        await this.delay();
        return teamSettings;
    }

    async createTeam(teamSettings: ITeamSettings): Promise<ITeamSettings> {
        console.log(`Creating Team ${teamSettings.name}`);

        await this.delay();
        return teamSettings;
    }

    async getProjectSettings(id: number): Promise<IProjectSettings> {
        console.log(`Getting Settings for Project ${id}`);

        await this.delay();

        return new ProjectSettings(1, "My Project", ["Feature", "Epic"], [new Milestone(1, "Target Date", new Date(this.today + 14 * this.dayMultiplier))], "[System.TeamProject] = \"My Team\"", "[System.TeamProject] = \"My Team\"", 15, 2);
    }

    async updateProject(projectSettings: IProjectSettings): Promise<IProjectSettings> {
        console.log(`Updating Project ${projectSettings.name}`);

        await this.delay();
        return projectSettings;
    }

    async createProject(projectSettings: IProjectSettings): Promise<IProjectSettings> {
        console.log(`Creating Project ${projectSettings.name}`);

        await this.delay();
        return projectSettings;
    }

    async getProject(id: number): Promise<Project | null> {
        console.log(`Getting Project with id ${id}`)
        const projects = await this.getProjects();
        const project = projects.find(project => project.id === id);
        return project || null;
    }

    async refreshFeaturesForProject(id: number): Promise<Project | null> {
        console.log(`Refreshing Project with id ${id}`)
        await this.delay();
        const projects = await this.getProjects();
        const project = projects.find(project => project.id === id);
        await this.delay();
        return project || null;
    }

    async refreshForecastsForProject(id: number): Promise<Project | null> {
        console.log(`Refreshing Project with id ${id}`)
        await this.delay();
        const projects = await this.getProjects();
        const project = projects.find(project => project.id === id);
        await this.delay();
        return project || null;
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

    async getWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]> {
        await this.delay();

        return [
            new WorkTrackingSystemConnection(null, "New Azure DevOps Connection", "AzureDevOps", [new WorkTrackingSystemOption("Azure DevOps Url", "", false), new WorkTrackingSystemOption("Personal Access Token", "", true)]),
            new WorkTrackingSystemConnection(null, "New Jira Connection", "Jira", [new WorkTrackingSystemOption("Jira Url", "", false), new WorkTrackingSystemOption("Username", "", false), new WorkTrackingSystemOption("Api Token", "", true)])];
    }

    async getConfiguredWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]> {
        await this.delay();

        return [
            new WorkTrackingSystemConnection(12, "My ADO Connection", "AzureDevOps", [new WorkTrackingSystemOption("Azure DevOps Url", "https://dev.azure.com/letpeoplework", false), new WorkTrackingSystemOption("Personal Access Token", "", true)]),
            new WorkTrackingSystemConnection(42, "My Jira Connection", "Jira", [new WorkTrackingSystemOption("Jira Url", "https://letpeoplework.atlassian.com", false), new WorkTrackingSystemOption("Username", "superuser@letpeople.work", false), new WorkTrackingSystemOption("Api Token", "", true)])]
    }

    async addNewWorkTrackingSystemConnection(newWorkTrackingSystemConnection: IWorkTrackingSystemConnection): Promise<IWorkTrackingSystemConnection> {
        await this.delay();

        newWorkTrackingSystemConnection.id = 12;

        for (const option of newWorkTrackingSystemConnection.options){
            if (option.isSecret){
                option.value = "";
            }
        }

        return newWorkTrackingSystemConnection;
    }

    async updateWorkTrackingSystemConnection(modifiedConnection: IWorkTrackingSystemConnection): Promise<IWorkTrackingSystemConnection> {
        await this.delay();

        return modifiedConnection;
    }

    async deleteWorkTrackingSystemConnection(connectionId: number): Promise<void> {
        console.log(`Deleting Work Tracking Connection with id ${connectionId}`)
        await this.delay();
    }

    async validateWorkTrackingSystemConnection(connection: IWorkTrackingSystemConnection): Promise<boolean> {
        console.log(`Validating connection for ${connection.name}`);
        await this.delay();
        return true;
    }

    async getLogLevel(): Promise<string> {
        await this.delay();
        return "Information";
    }

    async getSupportedLogLevels(): Promise<string[]> {
        await this.delay();

        return ["Debug", "Information", "Warning", "Error"];
    }

    async setLogLevel(logLevel: string): Promise<void> {
        console.log(`Setting log level to ${logLevel}`);
        await this.delay();
    }

    async getLogs(): Promise<string> {
        await this.delay();

        const lorem = new LoremIpsum({
            sentencesPerParagraph: {
                max: 4,
                min: 2
            },
            wordsPerSentence: {
                max: 10,
                min: 2
            }
        });

        return lorem.generateParagraphs(7);
    }


    async getRefreshSettings(settingName: string): Promise<IRefreshSettings> {
        console.log(`Getting ${settingName} refresh settings`);

        await this.delay();

        return new RefreshSettings(10, 20, 30);
    }

    async updateRefreshSettings(settingName: string, refreshSettings: IRefreshSettings): Promise<void> {
        console.log(`Update ${settingName} refresh settings: ${refreshSettings}`);

        await this.delay();
    }

    async getDefaultTeamSettings(): Promise<ITeamSettings> {
        await this.delay();

        return new TeamSettings(1, "My Team", 30, 1, "[System.TeamProject] = \"My Team\"", ["User Story", "Bug"], 12, "");
    }

    async updateDefaultTeamSettings(teamSettings: ITeamSettings): Promise<void> {
        console.log(`Updating ${teamSettings.name} Team Settings`);
        await this.delay();
    }

    async getDefaultProjectSettings(): Promise<IProjectSettings> {
        await this.delay();

        return new ProjectSettings(1, "My Project", ["Feature", "Epic"], [new Milestone(1, "Target Date", new Date(this.today + 14 * this.dayMultiplier))], "[System.TeamProject] = \"My Team\"", "[System.TeamProject] = \"My Team\"", 15, 2);
    }

    async updateDefaultProjectSettings(projecSettings: IProjectSettings): Promise<void> {
        console.log(`Updating ${projecSettings.name} Team Settings`);
        await this.delay();
    }

    delay() {
        if (this.throwError) {
            throw new Error('Simulated Error');
        }

        if (this.useDelay) {
            const randomDelay: number = Math.random() * 1000;
            return new Promise(resolve => setTimeout(resolve, randomDelay));
        }

        return Promise.resolve();
    }

    generateThroughput(): number[] {
        const length = Math.floor(Math.random() * (90 - 15 + 1)) + 15;
        const randomArray: number[] = [];

        for (let i = 0; i < length; i++) {
            const randomNumber = Math.floor(Math.random() * 5);
            randomArray.push(randomNumber);
        }

        return randomArray;
    }
}