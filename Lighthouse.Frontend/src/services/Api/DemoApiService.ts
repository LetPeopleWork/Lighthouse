import dayjs from "dayjs";
import { LoremIpsum } from "lorem-ipsum";
import type { IDataRetentionSettings } from "../../models/AppSettings/DataRetentionSettings";
import {
	type IRefreshSettings,
	RefreshSettings,
} from "../../models/AppSettings/RefreshSettings";
import { Feature, type IFeature } from "../../models/Feature";
import { HowManyForecast } from "../../models/Forecasts/HowManyForecast";
import { ManualForecast } from "../../models/Forecasts/ManualForecast";
import { RunChartData } from "../../models/Forecasts/RunChartData";
import { WhenForecast } from "../../models/Forecasts/WhenForecast";
import {
	type ILighthouseRelease,
	LighthouseRelease,
} from "../../models/LighthouseRelease/LighthouseRelease";
import {
	type ILighthouseReleaseAsset,
	LighthouseReleaseAsset,
} from "../../models/LighthouseRelease/LighthouseReleaseAsset";
import type { IOptionalFeature } from "../../models/OptionalFeatures/OptionalFeature";
import type { IPercentileValue } from "../../models/PercentileValue";
import { Milestone } from "../../models/Project/Milestone";
import { Project } from "../../models/Project/Project";
import type { IProjectSettings } from "../../models/Project/ProjectSettings";
import { Team } from "../../models/Team/Team";
import type { ITeamSettings } from "../../models/Team/TeamSettings";
import type { IWorkItem } from "../../models/WorkItem";
import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../models/WorkTracking/WorkTrackingSystemConnection";
import type {
	IUpdateStatus,
	IUpdateSubscriptionService,
	UpdateProgress,
	UpdateType,
} from "../UpdateSubscriptionService";
import type { IForecastService } from "./ForecastService";
import type { ILogService } from "./LogService";
import type { IOptionalFeatureService } from "./OptionalFeatureService";
import type { IProjectMetricsService } from "./ProjectMetricsService";
import type { IProjectService } from "./ProjectService";
import type { ISettingsService } from "./SettingsService";
import type { ITeamMetricsService } from "./TeamMetricsService";
import type { ITeamService } from "./TeamService";
import type { IVersionService } from "./VersionService";
import type { IWorkTrackingSystemService } from "./WorkTrackingSystemService";

export class DemoApiService
	implements
		IForecastService,
		ILogService,
		IProjectService,
		ISettingsService,
		ITeamService,
		IVersionService,
		IWorkTrackingSystemService,
		IOptionalFeatureService,
		IUpdateSubscriptionService,
		ITeamMetricsService,
		IProjectMetricsService
{
	private readonly useDelay: boolean;
	private readonly throwError: boolean;
	private readonly lastUpdated = new Date("06/23/2024 12:41");
	private readonly dayMultiplier: number = 24 * 60 * 60 * 1000;
	private readonly today: number = Date.now();

	private readonly subscribers: Map<string, (status: IUpdateStatus) => void> =
		new Map();

	private milestones = [
		new Milestone(
			0,
			"Milestone 1",
			new Date(this.today + 14 * this.dayMultiplier),
		),
		new Milestone(
			1,
			"Milestone 2",
			new Date(this.today + 28 * this.dayMultiplier),
		),
		new Milestone(
			2,
			"Milestone 3",
			new Date(this.today + 90 * this.dayMultiplier),
		),
	];

	private features: Feature[] = [];
	private projects: Project[] = [];
	private teams: Team[] = [];

	private teamThroughputs: Record<number, number[]> = {};
	private teamFeaturesInProgress: Record<number, string[]> = {};

	private dataRetentionSettings: IDataRetentionSettings = {
		maxStorageTimeInDays: 90,
	};

	private projectSettings: IProjectSettings[] = [
		{
			id: 0,
			name: "Release 1.33.7",
			workItemTypes: ["Feature", "Epic"],
			milestones: this.milestones,
			workItemQuery: '[System.TeamProject] = "My Team"',
			unparentedItemsQuery: '[System.TeamProject] = "My Team"',
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 15,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: "",
			workTrackingSystemConnectionId: 2,
			sizeEstimateField: "customfield_10037",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			overrideRealChildCountStates: ["Analysis In Progress"],
			involvedTeams: [],
		},
		{
			id: 1,
			name: "Release 42",
			workItemTypes: ["Feature", "Epic"],
			milestones: this.milestones,
			workItemQuery: '[System.TeamProject] = "My Team"',
			unparentedItemsQuery: '[System.TeamProject] = "My Team"',
			usePercentileToCalculateDefaultAmountOfWorkItems: true,
			defaultAmountOfWorkItemsPerFeature: 15,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: '[System.TeamProject] = "My Team"',
			workTrackingSystemConnectionId: 2,
			sizeEstimateField: "customfield_10037",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			overrideRealChildCountStates: ["Analysis In Progress"],
			involvedTeams: [],
		},
		{
			id: 2,
			name: "Release Codename Daniel",
			workItemTypes: ["Feature", "Epic"],
			milestones: this.milestones,
			workItemQuery: '[System.TeamProject] = "My Team"',
			unparentedItemsQuery: '[System.TeamProject] = "My Team"',
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 15,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: "",
			workTrackingSystemConnectionId: 2,
			sizeEstimateField: "customfield_10037",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			overrideRealChildCountStates: ["Analysis In Progress"],
			involvedTeams: [],
		},
	];

	private readonly optionalFeatures = [
		{
			id: 0,
			key: "CycleTimeScatterPlot",
			enabled: true,
			isPreview: true,
			name: "Cycle Time Scatterplot",
			description: "Shows Cycle Time Scatterplot for a team",
		},
		{
			id: 1,
			key: "SomeOtherFeature",
			enabled: false,
			isPreview: false,
			name: "Some Other Feature",
			description: "Feature that is longer in Preview already",
		},
	];

	constructor(useDelay: boolean, throwError = false) {
		this.useDelay = useDelay;
		this.throwError = throwError;

		this.recreateFeatures();
		this.recreateTeams();
		this.recreateProjects();

		for (const projectSetting of this.projectSettings) {
			projectSetting.involvedTeams = [
				this.teams[0],
				this.teams[1],
				this.teams[2],
				this.teams[3],
			];
		}
	}
	async getCycleTimePercentiles(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]> {
		console.log(
			`Getting Cycle Time Percentiles for Team ${teamId} between ${startDate} - ${endDate}`,
		);
		await this.delay();

		return [
			{ percentile: 50, value: 5 },
			{ percentile: 70, value: 7 },
			{ percentile: 85, value: 10 },
			{ percentile: 95, value: 12 },
		];
	}
	async getCycleTimeData(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IWorkItem[]> {
		console.log(
			`Getting Cycle Time Data for Team ${teamId} between ${startDate} - ${endDate}`,
		);

		await this.delay();

		const items: IWorkItem[] = [];
		let counter = 0;

		const numberOfItems = Math.floor(Math.random() * (10 - 3 + 1)) + 3;
		for (let i = 0; i < numberOfItems; i++) {
			const workItem = this.generateWorkItem(counter++);
			workItem.workItemReference = `WI-${counter}`;

			items.push(workItem);
		}
		return items;
	}

	async initialize(): Promise<void> {
		await this.delay();
		console.log("Initialized Update Subscription Service");
	}

	private getKey(id: number, type: UpdateType): string {
		return `${id}-${type}`;
	}

	async notifyAboutUpdate(
		updateType: UpdateType,
		id: number,
		progress: UpdateProgress,
	): Promise<void> {
		const key = this.getKey(id, updateType);
		const callback = this.subscribers.get(key);

		const status = {
			updateType: updateType,
			id: id,
			status: progress,
		};

		if (callback) {
			callback(status);
		}
	}

	async getUpdateStatus(
		updateType: UpdateType,
		id: number,
	): Promise<IUpdateStatus | null> {
		console.log(`Getting update Status for ${updateType} and ID ${id}`);
		return null;
	}

	async subscribeToUpdates(
		id: number,
		type: UpdateType,
		callback: (status: IUpdateStatus) => void,
	): Promise<void> {
		const key = this.getKey(id, type);
		this.subscribers.set(key, callback);
	}

	async unSubscribeFromUpdates(id: number, type: UpdateType): Promise<void> {
		const key = this.getKey(id, type);
		this.subscribers.delete(key);
	}

	async subscribeToTeamUpdates(
		teamId: number,
		callback: (status: IUpdateStatus) => void,
	): Promise<void> {
		await this.subscribeToUpdates(teamId, "Team", callback);
	}

	async unsubscribeFromTeamUpdates(teamId: number): Promise<void> {
		await this.unSubscribeFromUpdates(teamId, "Team");
	}

	async subscribeToFeatureUpdates(
		projectId: number,
		callback: (status: IUpdateStatus) => void,
	): Promise<void> {
		await this.subscribeToUpdates(projectId, "Features", callback);
	}

	async unsubscribeFromFeatureUpdates(projectId: number): Promise<void> {
		await this.unSubscribeFromUpdates(projectId, "Features");
	}

	async subscribeToForecastUpdates(
		projectId: number,
		callback: (status: IUpdateStatus) => void,
	): Promise<void> {
		await this.subscribeToUpdates(projectId, "Forecasts", callback);
	}

	async unsubscribeFromForecastUpdates(projectId: number): Promise<void> {
		await this.unSubscribeFromUpdates(projectId, "Forecasts");
	}

	async getAllFeatures(): Promise<IOptionalFeature[]> {
		await this.delay();

		return this.optionalFeatures;
	}

	async getFeatureByKey(key: string): Promise<IOptionalFeature | null> {
		await this.delay();

		const feature = this.optionalFeatures.find(
			(feature) => feature.key === key,
		);
		return feature || null;
	}

	async updateFeature(feature: IOptionalFeature): Promise<void> {
		await this.delay();

		const featureIndex = this.optionalFeatures.findIndex(
			(f) => f.key === feature.key,
		);

		if (featureIndex >= 0) {
			this.optionalFeatures.splice(featureIndex, 1);
			this.optionalFeatures.splice(featureIndex, 0, feature);
		}
	}

	async updateTeamData(teamId: number): Promise<void> {
		console.log(`Updating Throughput for Team ${teamId}`);

		this.notifyAboutUpdate("Team", teamId, "Queued");
		await this.delay();

		const team = await this.getTeam(teamId);

		await this.delay();
		if (team) {
			team.lastUpdated = new Date();
			await this.delay();
		}

		this.notifyAboutUpdate("Team", teamId, "Completed");
	}

	async updateForecast(teamId: number): Promise<void> {
		console.log(`Updating Forecast for Team ${teamId}`);

		await this.delay();
	}

	async runManualForecast(
		teamId: number,
		remainingItems: number,
		targetDate: Date,
	): Promise<ManualForecast> {
		console.log(
			`Updating Forecast for Team ${teamId}: How Many: ${remainingItems} - When: ${targetDate}`,
		);
		await this.delay();

		const howManyForecasts = [
			new HowManyForecast(50, 42),
			new HowManyForecast(70, 31),
			new HowManyForecast(85, 12),
			new HowManyForecast(95, 7),
		];

		const whenForecasts = [
			new WhenForecast(50, dayjs().add(2, "days").toDate()),
			new WhenForecast(70, dayjs().add(5, "days").toDate()),
			new WhenForecast(85, dayjs().add(9, "days").toDate()),
			new WhenForecast(95, dayjs().add(12, "days").toDate()),
		];

		const likelihood = Math.round(Math.random() * 10000) / 100;

		return new ManualForecast(
			remainingItems,
			targetDate,
			whenForecasts,
			howManyForecasts,
			likelihood,
		);
	}

	async getTeams(): Promise<Team[]> {
		await this.delay();

		return this.teams;
	}

	async getTeam(id: number): Promise<Team | null> {
		console.log(`Getting Team with id ${id}`);
		const teams = await this.getTeams();
		const team = teams.find((team) => team.id === id);
		return team || null;
	}

	async deleteTeam(id: number): Promise<void> {
		console.log(`'Deleting' Team with id ${id}`);
		await this.delay();
	}

	async getTeamSettings(id: number): Promise<ITeamSettings> {
		console.log(`Getting Settings for team ${id}`);

		await this.delay();

		return {
			id: 1,
			name: "My Team",
			throughputHistory: 30,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(
				Date.now() - 30 * 24 * 60 * 60 * 1000,
			),
			throughputHistoryEndDate: new Date(),
			featureWIP: 1,
			workItemQuery: '[System.TeamProject] = "My Team"',
			workItemTypes: ["User Story", "Bug"],
			workTrackingSystemConnectionId: 12,
			relationCustomField: "",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			automaticallyAdjustFeatureWIP: false,
		};
	}

	async validateTeamSettings(teamSettings: ITeamSettings): Promise<boolean> {
		console.log(`Validating Team ${teamSettings.name}`);

		await this.delay();

		return Math.random() >= 0.5;
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

		const projectSettings = this.projectSettings.find(
			(project) => project.id === id,
		);

		if (projectSettings) {
			return projectSettings;
		}

		return this.projectSettings[0];
	}

	async validateProjectSettings(
		projectSettings: IProjectSettings,
	): Promise<boolean> {
		console.log(`Validating Project ${projectSettings.name}`);
		await this.delay();
		return Math.random() >= 0.5;
	}

	async updateProject(
		projectSettings: IProjectSettings,
	): Promise<IProjectSettings> {
		console.log(`Updating Project ${projectSettings.name}`);

		this.projectSettings[projectSettings.id] = projectSettings;

		this.milestones = projectSettings.milestones;
		this.recreateFeatures();
		this.recreateProjects();
		this.recreateTeams();

		return projectSettings;
	}

	async createProject(
		projectSettings: IProjectSettings,
	): Promise<IProjectSettings> {
		console.log(`Creating Project ${projectSettings.name}`);

		await this.delay();
		return projectSettings;
	}

	async getProject(id: number): Promise<Project | null> {
		console.log(`Getting Project with id ${id}`);
		const projects = await this.getProjects();
		const project = projects.find((project) => project.id === id);
		return project || null;
	}

	async refreshFeaturesForProject(id: number): Promise<void> {
		console.log(`Refreshing Features for Project with id ${id}`);
		this.notifyAboutUpdate("Features", id, "Queued");
		await this.delay();
		const project = await this.getProject(id);

		if (project) {
			project.lastUpdated = new Date();
			await this.delay();
		}

		this.notifyAboutUpdate("Features", id, "Completed");
	}

	async refreshForecastsForProject(id: number): Promise<void> {
		console.log(`Refreshing Forecasts for Project with id ${id}`);
		this.notifyAboutUpdate("Forecasts", id, "Queued");
		await this.delay();

		const project = await this.getProject(id);

		if (project) {
			project.lastUpdated = new Date();
			await this.delay();
		}

		this.notifyAboutUpdate("Forecasts", id, "Completed");
	}

	async deleteProject(id: number): Promise<void> {
		console.log(`'Deleting' Project with id ${id}`);
		await this.delay();
	}

	async getCurrentVersion(): Promise<string> {
		await this.delay();
		return "DEMO VERSION";
	}

	async isUpdateAvailable(): Promise<boolean> {
		await this.delay();
		return true;
	}

	async getNewReleases(): Promise<ILighthouseRelease[]> {
		await this.delay();

		const assets: ILighthouseReleaseAsset[] = [
			new LighthouseReleaseAsset(
				"Lighthouse_v24.8.3.1040_linux-x64.zip",
				"https://github.com/LetPeopleWork/Lighthouse/releases/download/v24.8.3.1040/Lighthouse_v24.8.3.1040_linux-x64.zip",
			),
			new LighthouseReleaseAsset(
				"Lighthouse_v24.8.3.1040_osx-x64.zip",
				"https://github.com/LetPeopleWork/Lighthouse/releases/download/v24.8.3.1040/Lighthouse_v24.8.3.1040_osx-x64.zip",
			),
			new LighthouseReleaseAsset(
				"Lighthouse_v24.8.3.1040_win-x64.zip",
				"https://github.com/LetPeopleWork/Lighthouse/releases/download/v24.8.3.1040/Lighthouse_v24.8.3.1040_win-x64.zip",
			),
		];

		return [
			new LighthouseRelease(
				"Lighthouse v24.8.3.1040",
				"https://github.com/LetPeopleWork/Lighthouse/releases/tag/v24.8.3.1040",
				"# Highlights\r\n- This release adds interactive tutorials for various pages\r\n- Possibility to adjust milestones via the project view\r\n- Possibility to adjust Feature WIP of involved teams via the project detail view\r\n\r\n**Full Changelog**: https://github.com/LetPeopleWork/Lighthouse/compare/v24.7.28.937...v24.8.3.1040",
				"v24.8.3.1040",
				assets,
			),
		];
	}

	async getProjects(): Promise<Project[]> {
		await this.delay();

		return this.projects;
	}

	async getWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]> {
		await this.delay();

		return [
			new WorkTrackingSystemConnection(
				"New Azure DevOps Connection",
				"AzureDevOps",
				[
					{
						key: "Azure DevOps Url",
						value: "",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "Personal Access Token",
						value: "",
						isSecret: true,
						isOptional: false,
					},
				],
				null,
			),
			new WorkTrackingSystemConnection(
				"New Jira Connection",
				"Jira",
				[
					{ key: "Jira Url", value: "", isSecret: false, isOptional: false },
					{ key: "Username", value: "", isSecret: false, isOptional: true },
					{ key: "Api Token", value: "", isSecret: true, isOptional: false },
				],
				null,
			),
		];
	}

	async getConfiguredWorkTrackingSystems(): Promise<
		IWorkTrackingSystemConnection[]
	> {
		await this.delay();

		return [
			new WorkTrackingSystemConnection(
				"My ADO Connection",
				"AzureDevOps",
				[
					{
						key: "Azure DevOps Url",
						value: "https://dev.azure.com/letpeoplework",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "Personal Access Token",
						value: "",
						isSecret: true,
						isOptional: false,
					},
				],
				12,
			),
			new WorkTrackingSystemConnection(
				"My Jira Connection",
				"Jira",
				[
					{
						key: "Jira Url",
						value: "https://letpeoplework.atlassian.com",
						isSecret: false,
						isOptional: false,
					},
					{
						key: "Username",
						value: "superuser@letpeople.work",
						isSecret: false,
						isOptional: true,
					},
					{ key: "Api Token", value: "", isSecret: true, isOptional: false },
				],
				42,
			),
		];
	}

	async addNewWorkTrackingSystemConnection(
		newWorkTrackingSystemConnection: IWorkTrackingSystemConnection,
	): Promise<IWorkTrackingSystemConnection> {
		await this.delay();

		newWorkTrackingSystemConnection.id = 12;

		for (const option of newWorkTrackingSystemConnection.options) {
			if (option.isSecret) {
				option.value = "";
			}
		}

		return newWorkTrackingSystemConnection;
	}

	async updateWorkTrackingSystemConnection(
		modifiedConnection: IWorkTrackingSystemConnection,
	): Promise<IWorkTrackingSystemConnection> {
		await this.delay();

		return modifiedConnection;
	}

	async deleteWorkTrackingSystemConnection(
		connectionId: number,
	): Promise<void> {
		console.log(`Deleting Work Tracking Connection with id ${connectionId}`);
		await this.delay();
	}

	async validateWorkTrackingSystemConnection(
		connection: IWorkTrackingSystemConnection,
	): Promise<boolean> {
		console.log(`Validating connection for ${connection.name}`);
		await this.delay();
		return Math.random() >= 0.5;
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
				min: 2,
			},
			wordsPerSentence: {
				max: 10,
				min: 2,
			},
		});

		return lorem.generateParagraphs(7);
	}

	async downloadLogs(): Promise<void> {
		console.log("Downloading Logs");
	}

	async getRefreshSettings(settingName: string): Promise<IRefreshSettings> {
		console.log(`Getting ${settingName} refresh settings`);

		await this.delay();

		return new RefreshSettings(10, 20, 30);
	}

	async updateRefreshSettings(
		settingName: string,
		refreshSettings: IRefreshSettings,
	): Promise<void> {
		console.log(`Update ${settingName} refresh settings: ${refreshSettings}`);

		await this.delay();
	}

	async getDefaultTeamSettings(): Promise<ITeamSettings> {
		await this.delay();

		return {
			id: 1,
			name: "My Team",
			throughputHistory: 30,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			featureWIP: 1,
			workItemQuery: '[System.TeamProject] = "My Team"',
			workItemTypes: ["User Story", "Bug"],
			workTrackingSystemConnectionId: 12,
			relationCustomField: "",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			automaticallyAdjustFeatureWIP: false,
		};
	}

	async updateDefaultTeamSettings(teamSettings: ITeamSettings): Promise<void> {
		console.log(`Updating ${teamSettings.name} Team Settings`);
		await this.delay();
	}

	async getDefaultProjectSettings(): Promise<IProjectSettings> {
		await this.delay();

		return {
			id: 1,
			name: "My Project",
			workItemTypes: ["Feature", "Epic"],
			milestones: [
				new Milestone(
					1,
					"Target Date",
					new Date(this.today + 14 * this.dayMultiplier),
				),
			],
			workItemQuery: '[System.TeamProject] = "My Team"',
			unparentedItemsQuery: '[System.TeamProject] = "My Team"',
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 15,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: "",
			workTrackingSystemConnectionId: 2,
			sizeEstimateField: "Microsoft.VSTS.Scheduling.Size",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			overrideRealChildCountStates: ["Analysis In Progress"],
			involvedTeams: [],
		};
	}

	async updateDefaultProjectSettings(
		projecSettings: IProjectSettings,
	): Promise<void> {
		console.log(`Updating ${projecSettings.name} Team Settings`);
		await this.delay();
	}

	async getDataRetentionSettings(): Promise<IDataRetentionSettings> {
		await this.delay();
		return this.dataRetentionSettings;
	}

	async updateDataRetentionSettings(
		dataRetentionSettings: IDataRetentionSettings,
	): Promise<void> {
		await this.delay();
		this.dataRetentionSettings = dataRetentionSettings;
	}

	delay() {
		if (this.throwError) {
			throw new Error("Simulated Error");
		}

		if (this.useDelay) {
			const randomDelay: number = Math.random() * 1000;
			return new Promise((resolve) => setTimeout(resolve, randomDelay));
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
		const throughput1 = this.generateThroughput();
		const throughput2 = this.generateThroughput();
		const throughput3 = this.generateThroughput();
		const throughput4 = this.generateThroughput();

		this.teamThroughputs = {
			0: throughput1,
			1: throughput2,
			2: throughput3,
			3: throughput4,
		};

		this.teamFeaturesInProgress = {
			0: ["FTR-1", "FTR-3"],
			1: ["FTR-2", "FTR-3"],
			2: ["FTR-3"],
			3: ["FTR-4"],
		};

		this.teams = [
			new Team(
				"Binary Blazers",
				0,
				[],
				[this.features[0], this.features[3]],
				1,
				new Date(),
				false,
				new Date(new Date().setDate(new Date().getDate() - throughput1.length)),
				new Date(),
			),
			new Team(
				"Mavericks",
				1,
				[],
				[this.features[1], this.features[2]],
				2,
				new Date(),
				false,
				new Date(new Date().setDate(new Date().getDate() - throughput2.length)),
				new Date(),
			),
			new Team(
				"Cyber Sultans",
				2,
				[],
				[this.features[2]],
				1,
				new Date(),
				true,
				new Date(new Date().setDate(new Date().getDate() - throughput3.length)),
				new Date(),
			),
			new Team(
				"Tech Eagles",
				3,
				[],
				[this.features[3]],
				2,
				new Date(),
				false,
				new Date(new Date().setDate(new Date().getDate() - throughput4.length)),
				new Date(),
			),
		];
	}

	async getThroughput(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		console.log(
			`Getting Throughput for Team ${teamId} and Dates ${startDate} - ${endDate}`,
		);
		await this.delay();

		const rawThroughput = this.teamThroughputs[teamId];
		const totalThroughput = rawThroughput.reduce(
			(sum, items) => sum + items,
			0,
		);
		return new RunChartData(
			rawThroughput,
			rawThroughput.length,
			totalThroughput,
		);
	}

	async getWorkInProgressOverTime(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		console.log(
			`Getting Work In Progress over time for Team ${teamId} and Dates ${startDate} - ${endDate}`,
		);

		await this.delay();

		// Generate an array of random WIP numbers for each day in the date range
		const rawWIP: number[] = [];
		const startTimestamp = startDate.getTime();
		const endTimestamp = endDate.getTime();
		const oneDay = 24 * 60 * 60 * 1000;

		// Generate a data point for each day in the range (inclusive)
		for (
			let timestamp = startTimestamp;
			timestamp <= endTimestamp;
			timestamp += oneDay
		) {
			// Generate random WIP between 1 and 8
			const wip = Math.floor(Math.random() * 8) + 1;
			rawWIP.push(wip);
		}

		return new RunChartData(rawWIP, rawWIP.length, 0);
	}

	async getFeaturesInProgress(teamId: number): Promise<IWorkItem[]> {
		console.log(`Getting Features in Progress for Team ${teamId}`);
		await this.delay();

		const featuresInProgress: IWorkItem[] = [];
		let counter = 0;

		for (const item in this.teamFeaturesInProgress[teamId]) {
			const workItem = this.generateWorkItem(counter++);
			workItem.workItemReference = item;

			featuresInProgress.push(workItem);
		}
		return featuresInProgress;
	}

	async getInProgressItems(teamId: number): Promise<IWorkItem[]> {
		console.log(`Getting Items in Progress for Team ${teamId}`);
		await this.delay();

		const items: IWorkItem[] = [];
		let counter = 0;

		const numberOfItems = Math.floor(Math.random() * (10 - 3 + 1)) + 3;
		for (let i = 0; i < numberOfItems; i++) {
			const workItem = this.generateWorkItem(counter++);
			const lorem = new LoremIpsum({
				wordsPerSentence: {
					max: 10,
					min: 1,
				},
			});
			const nameLength = Math.floor(Math.random() * 9) + 1; // 1-4 words
			workItem.workItemReference = `WI-${counter}`;
			workItem.name = lorem.generateWords(nameLength);

			items.push(workItem);
		}
		return items;
	}

	generateWorkItem(id: number): IWorkItem {
		// Random date between now and 30 days ago
		const getRandomDate = (maxDaysAgo: number) => {
			const today = new Date();
			const daysAgo = Math.floor(Math.random() * (maxDaysAgo + 1));
			const date = new Date(today);
			date.setDate(today.getDate() - daysAgo);
			return date;
		};

		// Generate random work item
		const startedDate = getRandomDate(30);
		// Closed date must be after start date (or same day)
		const daysAfterStart = Math.floor(
			Math.random() * (30 - (30 - startedDate.getDate()) + 1),
		);
		const closedDate = new Date(startedDate);
		closedDate.setDate(startedDate.getDate() + daysAfterStart);

		return {
			name: `Work Item ${id}`,
			id: id,
			workItemReference: `WI-${id}`,
			url: `https://example.com/work-items/${id}`,
			state: "In Progress",
			stateCategory: "Doing",
			type: "Feature",
			workItemAge: Math.floor(Math.random() * (19 - 3 + 1)) + 3,
			startedDate,
			closedDate,
			cycleTime: daysAfterStart + 1,
		};
	}

	recreateFeatures(): void {
		const getMileStoneLikelihoods = () => {
			const getRandomNumber = () => {
				const random = Math.random() * (100 - 0) + 0;
				return Number.parseFloat(random.toFixed(2));
			};

			return {
				0: getRandomNumber(),
				1: getRandomNumber(),
				2: getRandomNumber(),
			};
		};

		this.features = [
			new Feature(
				"Feature 1",
				0,
				"FTR-1",
				"",
				"ToDo",
				new Date(),
				false,
				{ 0: "Release 1.33.7" },
				{ 0: 10 },
				{ 0: 15 },
				getMileStoneLikelihoods(),
				[
					new WhenForecast(50, new Date(this.today + 5 * this.dayMultiplier)),
					new WhenForecast(70, new Date(this.today + 10 * this.dayMultiplier)),
					new WhenForecast(85, new Date(this.today + 17 * this.dayMultiplier)),
					new WhenForecast(95, new Date(this.today + 25 * this.dayMultiplier)),
				],
				"https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/1",
				"ToDo",
				new Date(this.today - 10 * this.dayMultiplier),
				new Date(this.today),
				10,
				10,
			),
			new Feature(
				"Feature 2",
				1,
				"FTR-2",
				"",
				"Doing",
				new Date(),
				false,
				{ 1: "Release 42" },
				{ 1: 5 },
				{ 1: 5 },
				getMileStoneLikelihoods(),
				[
					new WhenForecast(50, new Date(this.today + 15 * this.dayMultiplier)),
					new WhenForecast(70, new Date(this.today + 28 * this.dayMultiplier)),
					new WhenForecast(85, new Date(this.today + 35 * this.dayMultiplier)),
					new WhenForecast(95, new Date(this.today + 45 * this.dayMultiplier)),
				],
				"https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/2",
				"Doing",
				new Date(this.today - 15 * this.dayMultiplier),
				new Date(this.today),
				15,
				15,
			),
			new Feature(
				"Feature 3",
				2,
				"FTR-3",
				"",
				"Done",
				new Date(),
				true,
				{ 2: "Release Codename Daniel" },
				{ 2: 7, 1: 15 },
				{ 2: 10, 1: 25 },
				getMileStoneLikelihoods(),
				[
					new WhenForecast(50, new Date(this.today + 7 * this.dayMultiplier)),
					new WhenForecast(70, new Date(this.today + 12 * this.dayMultiplier)),
					new WhenForecast(85, new Date(this.today + 14 * this.dayMultiplier)),
					new WhenForecast(95, new Date(this.today + 16 * this.dayMultiplier)),
				],
				"https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/3",
				"Done",
				new Date(this.today - 20 * this.dayMultiplier),
				new Date(this.today - 5 * this.dayMultiplier),
				15,
				20,
			),
			new Feature(
				"Feature 4",
				3,
				"FTR-4",
				"",
				"Unknown",
				new Date(),
				false,
				{ 2: "Release Codename Daniel", 1: "Release 1.33.7" },
				{ 0: 3, 3: 9 },
				{ 0: 12, 3: 10 },
				getMileStoneLikelihoods(),
				[
					new WhenForecast(50, new Date(this.today + 21 * this.dayMultiplier)),
					new WhenForecast(70, new Date(this.today + 37 * this.dayMultiplier)),
					new WhenForecast(85, new Date(this.today + 55 * this.dayMultiplier)),
					new WhenForecast(95, new Date(this.today + 71 * this.dayMultiplier)),
				],
				"https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/4",
				"Unknown",
				new Date(this.today - 5 * this.dayMultiplier),
				new Date(this.today),
				5,
				5,
			),
		];
	}

	recreateProjects(): void {
		this.projects = [
			new Project(
				"Release 1.33.7",
				0,
				[this.teams[0]],
				[this.features[0]],
				this.milestones,
				this.lastUpdated,
			),
			new Project(
				"Release 42",
				1,
				[this.teams[1]],
				[this.features[1]],
				this.milestones,
				this.lastUpdated,
			),
			new Project(
				"Release Codename Daniel",
				2,
				[this.teams[0], this.teams[1], this.teams[2], this.teams[3]],
				[this.features[2], this.features[3]],
				this.milestones,
				this.lastUpdated,
			),
		];
	}

	// Project Metrics Service Implementation
	async getThroughputForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		console.log(
			`Getting Throughput for Project ${projectId} and Dates ${startDate} - ${endDate}`,
		);
		await this.delay();

		// For project, we'll combine the throughput of all teams in the project
		const project = await this.getProject(projectId);
		if (!project) {
			return new RunChartData([], 0, 0);
		}

		// Get throughput for each team in the project and combine
		const teams = project.involvedTeams;
		const combinedThroughput: number[] = [];
		let totalHistory = 0;
		let totalSum = 0;

		for (const team of teams) {
			const teamThroughput = this.teamThroughputs[team.id];
			if (teamThroughput) {
				// If combinedThroughput is empty, initialize it with the first team's data
				if (combinedThroughput.length === 0) {
					combinedThroughput.push(...teamThroughput);
					totalHistory = teamThroughput.length;
				} else {
					// Otherwise, sum the throughputs for each day
					// (assuming they have the same length, which is a simplification)
					for (
						let i = 0;
						i < Math.min(combinedThroughput.length, teamThroughput.length);
						i++
					) {
						combinedThroughput[i] += teamThroughput[i];
					}
				}

				totalSum += teamThroughput.reduce((sum, val) => sum + val, 0);
			}
		}

		return new RunChartData(combinedThroughput, totalHistory, totalSum);
	}

	async getFeaturesInProgressOverTimeForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		console.log(
			`Getting Features In Progress over time for Project ${projectId} and Dates ${startDate} - ${endDate}`,
		);

		await this.delay();

		// Generate an array of random WIP numbers for each day in the date range
		const rawWIP: number[] = [];
		const startTimestamp = startDate.getTime();
		const endTimestamp = endDate.getTime();
		const oneDay = 24 * 60 * 60 * 1000;

		// Generate a data point for each day in the range (inclusive)
		for (
			let timestamp = startTimestamp;
			timestamp <= endTimestamp;
			timestamp += oneDay
		) {
			// Generate random WIP between 1 and 5 (features are typically fewer than regular items)
			const wip = Math.floor(Math.random() * 5) + 1;
			rawWIP.push(wip);
		}

		// Calculate total features in progress (sum of the daily values)
		const total = rawWIP.reduce((sum, val) => sum + val, 0);

		return new RunChartData(rawWIP, rawWIP.length, total);
	}

	async getInProgressFeaturesForProject(
		projectId: number,
	): Promise<IFeature[]> {
		console.log(`Getting In Progress Features for Project ${projectId}`);
		await this.delay();

		// Get the project
		const project = await this.getProject(projectId);
		if (!project) {
			return [];
		}

		// Return the project's features that are in progress
		const inProgressFeatures = project.features.filter(
			(feature) => feature.stateCategory !== "Doing",
		);

		return inProgressFeatures;
	}

	async getCycleTimePercentilesForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]> {
		console.log(
			`Getting Cycle Time Percentiles for Project ${projectId} between ${startDate} - ${endDate}`,
		);
		await this.delay();

		// For a project, cycle times might be higher than for individual teams
		return [
			{ percentile: 50, value: 8 },
			{ percentile: 70, value: 12 },
			{ percentile: 85, value: 18 },
			{ percentile: 95, value: 25 },
		];
	}

	async getCycleTimeDataForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeature[]> {
		console.log(
			`Getting Cycle Time Data for Project ${projectId} between ${startDate} - ${endDate}`,
		);

		await this.delay();

		const features: IFeature[] = [];
		let counter = 0;

		const numberOfItems = Math.floor(Math.random() * (8 - 3 + 1)) + 3;
		for (let i = 0; i < numberOfItems; i++) {
			const workItem = this.generateWorkItem(counter++);

			// Create a feature based on the work item
			const projects = {};
			const remainingWork = {};
			const totalWork = {};
			const milestoneLikelihood = {};

			const feature = new Feature(
				`Feature ${counter}`,
				workItem.id,
				`P-FTR-${counter}`,
				workItem.state,
				"Feature",
				new Date(),
				false,
				projects,
				remainingWork,
				totalWork,
				milestoneLikelihood,
				[],
				workItem.url,
				workItem.stateCategory,
				workItem.startedDate,
				workItem.closedDate,
				workItem.cycleTime,
				workItem.workItemAge,
			);

			features.push(feature);
		}
		return features;
	}
}
