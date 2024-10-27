import { WhenForecast } from '../../models/Forecasts/WhenForecast';
import { Project } from '../../models/Project/Project';
import { Team } from '../../models/Team/Team';
import { Feature } from '../../models/Feature';
import { Throughput } from '../../models/Forecasts/Throughput';
import { ManualForecast } from '../../models/Forecasts/ManualForecast';
import { HowManyForecast } from '../../models/Forecasts/HowManyForecast';
import dayjs from 'dayjs';
import { IMilestone, Milestone } from '../../models/Project/Milestone';
import { IWorkTrackingSystemConnection, WorkTrackingSystemConnection } from '../../models/WorkTracking/WorkTrackingSystemConnection';
import { WorkTrackingSystemOption } from '../../models/WorkTracking/WorkTrackingSystemOption';
import { ITeamSettings } from '../../models/Team/TeamSettings';
import { IProjectSettings, ProjectSettings } from '../../models/Project/ProjectSettings';
import { LoremIpsum } from "lorem-ipsum";
import { IRefreshSettings, RefreshSettings } from '../../models/AppSettings/RefreshSettings';
import { ILighthouseRelease, LighthouseRelease } from '../../models/LighthouseRelease/LighthouseRelease';
import { ILighthouseReleaseAsset, LighthouseReleaseAsset } from '../../models/LighthouseRelease/LighthouseReleaseAsset';
import { IWorkTrackingSystemService } from './WorkTrackingSystemService';
import { IForecastService } from './ForecastService';
import { ILogService } from './LogService';
import { IProjectService } from './ProjectService';
import { ISettingsService } from './SettingsService';
import { ITeamService } from './TeamService';
import { IVersionService } from './VersionService';
import { IChartService } from './ChartService';
import { BurndownEntry, ILighthouseChartData, ILighthouseChartFeatureData, LighthouseChartData, LighthouseChartFeatureData } from '../../models/Charts/LighthouseChartData';
import { IPreviewFeatureService } from './PreviewFeatureService';
import { PreviewFeature } from '../../models/Preview/PreviewFeature';

export class DemoApiService implements IForecastService, ILogService, IProjectService, ISettingsService, ITeamService, IVersionService, IWorkTrackingSystemService, IChartService, IPreviewFeatureService {
    private useDelay: boolean;
    private throwError: boolean;

    private lastUpdated = new Date("06/23/2024 12:41");

    private dayMultiplier: number = 24 * 60 * 60 * 1000;
    private today: number = Date.now();

    private milestones = [
        new Milestone(0, "Milestone 1", new Date(this.today + 14 * this.dayMultiplier)),
        new Milestone(1, "Milestone 2", new Date(this.today + 28 * this.dayMultiplier)),
        new Milestone(2, "Milestone 3", new Date(this.today + 90 * this.dayMultiplier))
    ];

    private features: Feature[] = [];
    private projects: Project[] = [];
    private teams: Team[] = [];

    private projectSettings = [
        new ProjectSettings(0, "Release 1.33.7", ["Feature", "Epic"], this.milestones, "[System.TeamProject] = \"My Team\"", "[System.TeamProject] = \"My Team\"", false, 15, 85, "", 2, "customfield_10037"),
        new ProjectSettings(1, "Release 42", ["Feature", "Epic"], this.milestones, "[System.TeamProject] = \"My Team\"", "[System.TeamProject] = \"My Team\"", true, 15, 85, "[System.TeamProject] = \"My Team\"", 2, "customfield_10037"),
        new ProjectSettings(2, "Release Codename Daniel", ["Feature", "Epic"], this.milestones, "[System.TeamProject] = \"My Team\"", "[System.TeamProject] = \"My Team\"", false, 15, 85, "", 2, "customfield_10037"),
    ];

    private readonly previewFeatures = [
        new PreviewFeature(0, "LighthouseChart", "Lighthouse Chart", "Shows Burndown Chart with Forecasts for each Feature in a Project", true),
        new PreviewFeature(1, "SomeOtherFeature", "Feature that is longer in Preview already", "Does something else but also somewhat new", false),
    ]

    constructor(useDelay: boolean, throwError: boolean = false) {
        this.useDelay = useDelay;
        this.throwError = throwError;

        this.recreateFeatures();
        this.recreateTeams();
        this.recreateProjects();
    }

    async getAllFeatures(): Promise<PreviewFeature[]> {
        await this.delay();

        return this.previewFeatures;
    }

    async getFeatureByKey(key: string): Promise<PreviewFeature | null> {
        await this.delay();

        const feature = this.previewFeatures.find(feature => feature.key === key);
        return feature || null;
    }

    async updateFeature(feature: PreviewFeature): Promise<void> {
        await this.delay();

        const featureIndex = this.previewFeatures.findIndex(f => f.key === feature.key);

        if (featureIndex >= 0){
            this.previewFeatures.splice(featureIndex, 1);
            this.previewFeatures.splice(featureIndex, 0, feature);
        }
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

        return this.teams;
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

        return { id: 1, name: 'My Team', throughputHistory: 30, featureWIP: 1, workItemQuery: "[System.TeamProject] = \"My Team\"", workItemTypes: ["User Story", "Bug"], workTrackingSystemConnectionId: 12, relationCustomField: '', toDoStates: ["New"], doingStates: ["Active"], doneStates: ["Done"] }
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

        const projectSettings = this.projectSettings.find(project => project.id === id);

        if (projectSettings) {
            return projectSettings;
        }

        return this.projectSettings[0];
    }

    async updateProject(projectSettings: IProjectSettings): Promise<IProjectSettings> {
        console.log(`Updating Project ${projectSettings.name}`);

        this.projectSettings[projectSettings.id] = projectSettings

        this.milestones = projectSettings.milestones;
        this.recreateFeatures();
        this.recreateProjects();
        this.recreateTeams();

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

    async getCurrentVersion(): Promise<string> {
        await this.delay()
        return "DEMO VERSION";
    }

    async isUpdateAvailable(): Promise<boolean> {
        await this.delay()
        return true;
    }

    async getNewReleases(): Promise<ILighthouseRelease[]> {
        await this.delay()

        const assets: ILighthouseReleaseAsset[] = [
            new LighthouseReleaseAsset("Lighthouse_v24.8.3.1040_linux-x64.zip", "https://github.com/LetPeopleWork/Lighthouse/releases/download/v24.8.3.1040/Lighthouse_v24.8.3.1040_linux-x64.zip"),
            new LighthouseReleaseAsset("Lighthouse_v24.8.3.1040_osx-x64.zip", "https://github.com/LetPeopleWork/Lighthouse/releases/download/v24.8.3.1040/Lighthouse_v24.8.3.1040_osx-x64.zip"),
            new LighthouseReleaseAsset("Lighthouse_v24.8.3.1040_win-x64.zip", "https://github.com/LetPeopleWork/Lighthouse/releases/download/v24.8.3.1040/Lighthouse_v24.8.3.1040_win-x64.zip")
        ]

        return [new LighthouseRelease(
            "Lighthouse v24.8.3.1040",
            "https://github.com/LetPeopleWork/Lighthouse/releases/tag/v24.8.3.1040",
            "# Highlights\r\n- This release adds interactive tutorials for various pages\r\n- Possibility to adjust milestones via the project view\r\n- Possibility to adjust Feature WIP of involved teams via the project detail view\r\n\r\n**Full Changelog**: https://github.com/LetPeopleWork/Lighthouse/compare/v24.7.28.937...v24.8.3.1040",
            "v24.8.3.1040",
            assets
        )];
    }

    async getProjects(): Promise<Project[]> {
        await this.delay();

        return this.projects;
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

        for (const option of newWorkTrackingSystemConnection.options) {
            if (option.isSecret) {
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

        return { id: 1, name: 'My Team', throughputHistory: 30, featureWIP: 1, workItemQuery: "[System.TeamProject] = \"My Team\"", workItemTypes: ["User Story", "Bug"], workTrackingSystemConnectionId: 12, relationCustomField: '', toDoStates: ["New"], doingStates: ["Active"], doneStates: ["Done"] }
    }

    async updateDefaultTeamSettings(teamSettings: ITeamSettings): Promise<void> {
        console.log(`Updating ${teamSettings.name} Team Settings`);
        await this.delay();
    }

    async getDefaultProjectSettings(): Promise<IProjectSettings> {
        await this.delay();

        return new ProjectSettings(1, "My Project", ["Feature", "Epic"], [new Milestone(1, "Target Date", new Date(this.today + 14 * this.dayMultiplier))], "[System.TeamProject] = \"My Team\"", "[System.TeamProject] = \"My Team\"", false, 15, 85, "", 2, "Microsoft.VSTS.Scheduling.Size");
    }

    async updateDefaultProjectSettings(projecSettings: IProjectSettings): Promise<void> {
        console.log(`Updating ${projecSettings.name} Team Settings`);
        await this.delay();
    }

    async getLighthouseChartData(projectId: number, startDate: Date, sampleRate: number): Promise<ILighthouseChartData> {
        console.log(`Getting Lighthouse Chart for project ${projectId} starting from ${startDate} with sample rate ${sampleRate}`);

        await this.delay();

        const featureData: ILighthouseChartFeatureData[] = [
            new LighthouseChartFeatureData("Feature 1", [new Date("2024-05-17"), new Date("2024-05-28")], [
                new BurndownEntry(new Date("2024-04-08"), 38),
                new BurndownEntry(new Date("2024-04-15"), 32),
                new BurndownEntry(new Date("2024-04-22"), 35),
                new BurndownEntry(new Date("2024-04-29"), 23),
                new BurndownEntry(new Date("2024-05-06"), 15),
            ]),
            new LighthouseChartFeatureData("Feature 2", [], [
                new BurndownEntry(new Date("2024-04-08"), 15),
                new BurndownEntry(new Date("2024-04-15"), 9),
                new BurndownEntry(new Date("2024-04-22"), 5),
                new BurndownEntry(new Date("2024-04-29"), 0),
            ]),
            new LighthouseChartFeatureData("Feature 3", [new Date("2024-05-23"), new Date("2024-06-03")], [
                new BurndownEntry(new Date("2024-04-08"), 41),
                new BurndownEntry(new Date("2024-04-15"), 36),
                new BurndownEntry(new Date("2024-04-22"), 31),
                new BurndownEntry(new Date("2024-04-29"), 19),
                new BurndownEntry(new Date("2024-05-06"), 17),
            ]),
        ];

        const milestones: IMilestone[] = [
            new Milestone(0, "Important Date", new Date("2024-05-04")),
            new Milestone(0, "Customer Visit", new Date("2024-06-01")),
        ];        

        return new LighthouseChartData(featureData, milestones);
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

    recreateTeams(): void {
        this.teams = [
            new Team("Binary Blazers", 0, [], [this.features[0], this.features[3]], 1),
            new Team("Mavericks", 1, [], [this.features[1], this.features[2]], 2),
            new Team("Cyber Sultans", 2, [], [this.features[2]], 1),
            new Team("Tech Eagles", 3, [], [this.features[3]], 2)
        ]
    }

    recreateFeatures(): void {

        const getMileStoneLikelihoods = () => {
            const getRandomNumber = () => {
                const random = Math.random() * (100 - 0) + 0;
                return parseFloat(random.toFixed(2));
            }

            return { 0: getRandomNumber(), 1: getRandomNumber(), 2: getRandomNumber() }
        }

        this.features = [
            new Feature('Feature 1', 0, "https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/1", new Date(), false, { 0: "Release 1.33.7" }, { 0: 10 }, { 0: 15 }, getMileStoneLikelihoods(), [new WhenForecast(50, new Date(this.today + 5 * this.dayMultiplier)), new WhenForecast(70, new Date(this.today + 10 * this.dayMultiplier)), new WhenForecast(85, new Date(this.today + 17 * this.dayMultiplier)), new WhenForecast(95, new Date(this.today + 25 * this.dayMultiplier))]),
            new Feature('Feature 2', 1, "https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/2", new Date(), false, { 1: "Release 42" }, { 1: 5 }, { 1: 5 }, getMileStoneLikelihoods(), [new WhenForecast(50, new Date(this.today + 15 * this.dayMultiplier)), new WhenForecast(70, new Date(this.today + 28 * this.dayMultiplier)), new WhenForecast(85, new Date(this.today + 35 * this.dayMultiplier)), new WhenForecast(95, new Date(this.today + 45 * this.dayMultiplier))]),
            new Feature('Feature 3', 2, "https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/3", new Date(), true, { 2: "Release Codename Daniel" }, { 2: 7, 1: 15 }, { 2: 10, 1: 25 }, getMileStoneLikelihoods(), [new WhenForecast(50, new Date(this.today + 7 * this.dayMultiplier)), new WhenForecast(70, new Date(this.today + 12 * this.dayMultiplier)), new WhenForecast(85, new Date(this.today + 14 * this.dayMultiplier)), new WhenForecast(95, new Date(this.today + 16 * this.dayMultiplier))]),
            new Feature('Feature 4', 3, "https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/4", new Date(), false, { 2: "Release Codename Daniel", 1: "Release 1.33.7" }, { 0: 3, 3: 9 }, { 0: 12, 3: 10 }, getMileStoneLikelihoods(), [new WhenForecast(50, new Date(this.today + 21 * this.dayMultiplier)), new WhenForecast(70, new Date(this.today + 37 * this.dayMultiplier)), new WhenForecast(85, new Date(this.today + 55 * this.dayMultiplier)), new WhenForecast(95, new Date(this.today + 71 * this.dayMultiplier))]),
        ];
    }

    recreateProjects(): void {
        this.projects = [
            new Project("Release 1.33.7", 0, [this.teams[0]], [this.features[0]], this.milestones, this.lastUpdated),
            new Project("Release 42", 1, [this.teams[1]], [this.features[1]], this.milestones, this.lastUpdated),
            new Project("Release Codename Daniel", 2, [this.teams[0], this.teams[1], this.teams[2], this.teams[3]], [this.features[2], this.features[3]], this.milestones, this.lastUpdated),
        ]
    }
}