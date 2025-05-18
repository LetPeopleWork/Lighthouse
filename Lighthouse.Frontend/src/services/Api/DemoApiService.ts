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
import { WhenForecast } from "../../models/Forecasts/WhenForecast";
import {
	type ILighthouseRelease,
	LighthouseRelease,
} from "../../models/LighthouseRelease/LighthouseRelease";
import {
	type ILighthouseReleaseAsset,
	LighthouseReleaseAsset,
} from "../../models/LighthouseRelease/LighthouseReleaseAsset";
import { RunChartData } from "../../models/Metrics/RunChartData";
import type { IOptionalFeature } from "../../models/OptionalFeatures/OptionalFeature";
import type { IPercentileValue } from "../../models/PercentileValue";
import { Milestone } from "../../models/Project/Milestone";
import { Project } from "../../models/Project/Project";
import type { IProjectSettings } from "../../models/Project/ProjectSettings";
import type { StatesCollection } from "../../models/StatesCollection";
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
import type {
	IProjectMetricsService,
	ITeamMetricsService,
} from "./MetricsService";
import type { IOptionalFeatureService } from "./OptionalFeatureService";
import type { IProjectService } from "./ProjectService";
import type { ISettingsService } from "./SettingsService";
import type { ISuggestionService } from "./SuggestionService";
import type { ITeamService } from "./TeamService";
import type { IVersionService } from "./VersionService";
import type { IWorkTrackingSystemService } from "./WorkTrackingSystemService";

let useDelay = true;
let throwError = false;
const dayMultiplier: number = 24 * 60 * 60 * 1000;
const today: number = Date.now();

let milestones = [
	(() => {
		const milestone = new Milestone();
		milestone.id = 0;
		milestone.name = "Milestone 1";
		milestone.date = new Date(today + 14 * dayMultiplier);
		return milestone;
	})(),
	(() => {
		const milestone = new Milestone();
		milestone.id = 1;
		milestone.name = "Milestone 2";
		milestone.date = new Date(today + 28 * dayMultiplier);
		return milestone;
	})(),
	(() => {
		const milestone = new Milestone();
		milestone.id = 2;
		milestone.name = "Milestone 3";
		milestone.date = new Date(today + 90 * dayMultiplier);
		return milestone;
	})(),
];

let features: Feature[] = [];
let projects: Project[] = [];
let teams: Team[] = [];

let teamThroughputs: Record<number, number[]> = {};
let teamFeaturesInProgress: Record<number, string[]> = {};

function generateWorkItem(id: number): IWorkItem {
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

function delay() {
	if (throwError) {
		throw new Error("Simulated Error");
	}

	if (useDelay) {
		const randomDelay: number = Math.random() * 1000;
		return new Promise((resolve) => setTimeout(resolve, randomDelay));
	}

	return Promise.resolve();
}

export class DemoTeamMetricsService implements ITeamMetricsService {
	async getCycleTimePercentiles(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]> {
		console.log(
			`Getting Cycle Time Percentiles for Team ${teamId} between ${startDate} - ${endDate}`,
		);
		await delay();

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

		await delay();

		const items: IWorkItem[] = [];
		let counter = 0;

		const numberOfItems = Math.floor(Math.random() * (10 - 3 + 1)) + 3;
		for (let i = 0; i < numberOfItems; i++) {
			const workItem = generateWorkItem(counter++);
			workItem.workItemReference = `WI-${counter}`;

			items.push(workItem);
		}
		return items;
	}

	async getThroughput(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		console.log(
			`Getting Throughput for Team ${teamId} and Dates ${startDate} - ${endDate}`,
		);
		await delay();

		const rawThroughput = teamThroughputs[teamId];
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

	async getStartedItems(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		console.log(
			`Getting Started Items for Team ${teamId} and Dates ${startDate} - ${endDate}`,
		);
		await delay();

		const oneDay = 24 * 60 * 60 * 1000; // milliseconds in a day
		const daysBetween =
			Math.floor((endDate.getTime() - startDate.getTime()) / oneDay) + 1; // +1 to include both start and end dates
		const valuePerUnitOfTime = Array(daysBetween)
			.fill(0)
			.map(() => Math.floor(Math.random() * 5) + 1);
		const total = valuePerUnitOfTime.reduce((sum, value) => sum + value, 0);
		return new RunChartData(valuePerUnitOfTime, 30, total);
	}

	async getWorkInProgressOverTime(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		console.log(
			`Getting Work In Progress over time for Team ${teamId} and Dates ${startDate} - ${endDate}`,
		);

		await delay();

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
		await delay();

		const featuresInProgress: IWorkItem[] = [];
		let counter = 0;

		for (const item in teamFeaturesInProgress[teamId]) {
			const workItem = generateWorkItem(counter++);
			workItem.workItemReference = item;

			featuresInProgress.push(workItem);
		}
		return featuresInProgress;
	}

	async getInProgressItems(teamId: number): Promise<IWorkItem[]> {
		console.log(`Getting Items in Progress for Team ${teamId}`);
		await delay();

		const items: IWorkItem[] = [];
		let counter = 0;

		const numberOfItems = Math.floor(Math.random() * (10 - 3 + 1)) + 3;
		for (let i = 0; i < numberOfItems; i++) {
			const workItem = generateWorkItem(counter++);
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
}

export class DemoProjectMetricsService implements IProjectMetricsService {
	async getThroughput(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		console.log(
			`Getting Throughput for Project ${id} and Dates ${startDate} - ${endDate}`,
		);
		await delay();

		// For project, we'll combine the throughput of all teams in the project
		const project = projects.find((project) => project.id === id);
		if (!project) {
			return new RunChartData([], 0, 0);
		}

		// Get throughput for each team in the project and combine
		const teams = project.involvedTeams;
		const combinedThroughput: number[] = [];
		let totalHistory = 0;
		let totalSum = 0;

		for (const team of teams) {
			const teamThroughput = teamThroughputs[team.id];
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

	async getStartedItems(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		console.log(
			`Getting Started Items for Team ${teamId} and Dates ${startDate} - ${endDate}`,
		);
		await delay();

		const oneDay = 24 * 60 * 60 * 1000; // milliseconds in a day
		const daysBetween =
			Math.floor((endDate.getTime() - startDate.getTime()) / oneDay) + 1; // +1 to include both start and end dates
		const valuePerUnitOfTime = Array(daysBetween)
			.fill(0)
			.map(() => Math.floor(Math.random() * 5) + 1);
		const total = valuePerUnitOfTime.reduce((sum, value) => sum + value, 0);
		return new RunChartData(valuePerUnitOfTime, 30, total);
	}

	async getWorkInProgressOverTime(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		console.log(
			`Getting Features In Progress over time for Project ${id} and Dates ${startDate} - ${endDate}`,
		);

		await delay();

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

	async getInProgressItems(id: number): Promise<IFeature[]> {
		console.log(`Getting In Progress Features for Project ${id}`);
		await delay();

		// Get the project
		const project = projects.find((project) => project.id === id);
		if (!project) {
			return [];
		}

		// Return the project's features that are in progress
		const inProgressFeatures = project.features.filter(
			(feature) => feature.stateCategory !== "Doing",
		);

		return inProgressFeatures;
	}

	async getCycleTimePercentiles(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]> {
		console.log(
			`Getting Cycle Time Percentiles for Project ${id} between ${startDate} - ${endDate}`,
		);
		await delay();

		// For a project, cycle times might be higher than for individual teams
		return [
			{ percentile: 50, value: 8 },
			{ percentile: 70, value: 12 },
			{ percentile: 85, value: 18 },
			{ percentile: 95, value: 25 },
		];
	}

	async getCycleTimeData(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeature[]> {
		console.log(
			`Getting Cycle Time Data for Project ${id} between ${startDate} - ${endDate}`,
		);

		await delay();

		const features: IFeature[] = [];
		let counter = 0;

		const numberOfItems = Math.floor(Math.random() * (8 - 3 + 1)) + 3;
		for (let i = 0; i < numberOfItems; i++) {
			const workItem = generateWorkItem(counter++);

			// Create a feature based on the work item
			const projects = {};
			const remainingWork = {};
			const totalWork = {};
			const milestoneLikelihood = {};

			const feature = new Feature();
			feature.name = `Feature ${counter}`;
			feature.id = counter;
			feature.workItemReference = `FTR-${counter}`;
			feature.state = workItem.state;
			feature.type = "Feature";
			feature.workItemAge = workItem.workItemAge;
			feature.startedDate = workItem.startedDate;
			feature.closedDate = workItem.closedDate;
			feature.cycleTime = workItem.cycleTime;
			feature.projects = projects;
			feature.remainingWork = remainingWork;
			feature.totalWork = totalWork;
			feature.milestoneLikelihood = milestoneLikelihood;
			feature.url = workItem.url ?? "";
			feature.stateCategory = workItem.stateCategory;

			features.push(feature);
		}
		return features;
	}
}

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
		ISuggestionService
{
	private readonly subscribers: Map<string, (status: IUpdateStatus) => void> =
		new Map();

	private dataRetentionSettings: IDataRetentionSettings = {
		maxStorageTimeInDays: 90,
	};

	private projectSettings: IProjectSettings[] = [
		{
			id: 0,
			name: "Release 1.33.7",
			workItemTypes: ["Feature", "Epic"],
			milestones: milestones,
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
			tags: [],
			overrideRealChildCountStates: ["Analysis In Progress"],
			involvedTeams: [],
			serviceLevelExpectationProbability: 75,
			serviceLevelExpectationRange: 35,
		},
		{
			id: 1,
			name: "Release 42",
			workItemTypes: ["Feature", "Epic"],
			milestones: milestones,
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
			tags: [],
			overrideRealChildCountStates: ["Analysis In Progress"],
			involvedTeams: [],
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 18,
		},
		{
			id: 2,
			name: "Release Codename Daniel",
			workItemTypes: ["Feature", "Epic"],
			milestones: milestones,
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
			tags: [],
			overrideRealChildCountStates: ["Analysis In Progress"],
			involvedTeams: [],
			serviceLevelExpectationProbability: 90,
			serviceLevelExpectationRange: 45,
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

	constructor(useDelayParam: boolean, throwErrorParam = false) {
		useDelay = useDelayParam;
		throwError = throwErrorParam;

		this.recreateFeatures();
		this.recreateTeams();
		this.recreateProjects();

		for (const projectSetting of this.projectSettings) {
			projectSetting.involvedTeams = [teams[0], teams[1], teams[2], teams[3]];
		}
	}

	async initialize(): Promise<void> {
		await delay();
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
		await delay();

		return this.optionalFeatures;
	}

	async getFeatureByKey(key: string): Promise<IOptionalFeature | null> {
		await delay();

		const feature = this.optionalFeatures.find(
			(feature) => feature.key === key,
		);
		return feature || null;
	}

	async updateFeature(feature: IOptionalFeature): Promise<void> {
		await delay();

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
		await delay();

		const team = await this.getTeam(teamId);

		await delay();
		if (team) {
			team.lastUpdated = new Date();
			await delay();
		}

		this.notifyAboutUpdate("Team", teamId, "Completed");
	}

	async updateForecast(teamId: number): Promise<void> {
		console.log(`Updating Forecast for Team ${teamId}`);

		await delay();
	}

	async runManualForecast(
		teamId: number,
		remainingItems: number,
		targetDate: Date,
	): Promise<ManualForecast> {
		console.log(
			`Updating Forecast for Team ${teamId}: How Many: ${remainingItems} - When: ${targetDate}`,
		);
		await delay();

		const howManyForecasts = [
			new HowManyForecast(50, 42),
			new HowManyForecast(70, 31),
			new HowManyForecast(85, 12),
			new HowManyForecast(95, 7),
		];

		const whenForecasts = [
			WhenForecast.new(50, dayjs().add(2, "days").toDate()),
			WhenForecast.new(70, dayjs().add(5, "days").toDate()),
			WhenForecast.new(85, dayjs().add(9, "days").toDate()),
			WhenForecast.new(95, dayjs().add(12, "days").toDate()),
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
		await delay();

		return teams;
	}

	async getTeam(id: number): Promise<Team | null> {
		console.log(`Getting Team with id ${id}`);
		const teams = await this.getTeams();
		const team = teams.find((team) => team.id === id);
		return team || null;
	}

	async deleteTeam(id: number): Promise<void> {
		console.log(`'Deleting' Team with id ${id}`);
		await delay();
	}

	async getTeamSettings(id: number): Promise<ITeamSettings> {
		console.log(`Getting Settings for team ${id}`);

		await delay();

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
			tags: [],
			automaticallyAdjustFeatureWIP: false,
			serviceLevelExpectationProbability: 75,
			serviceLevelExpectationRange: 35,
		};
	}

	async validateTeamSettings(teamSettings: ITeamSettings): Promise<boolean> {
		console.log(`Validating Team ${teamSettings.name}`);

		await delay();

		return Math.random() >= 0.5;
	}

	async updateTeam(teamSettings: ITeamSettings): Promise<ITeamSettings> {
		console.log(`Updating Team ${teamSettings.name}`);

		await delay();
		return teamSettings;
	}

	async createTeam(teamSettings: ITeamSettings): Promise<ITeamSettings> {
		console.log(`Creating Team ${teamSettings.name}`);

		await delay();
		return teamSettings;
	}

	async getProjectSettings(id: number): Promise<IProjectSettings> {
		console.log(`Getting Settings for Project ${id}`);

		await delay();

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
		await delay();
		return Math.random() >= 0.5;
	}

	async updateProject(
		projectSettings: IProjectSettings,
	): Promise<IProjectSettings> {
		console.log(`Updating Project ${projectSettings.name}`);

		this.projectSettings[projectSettings.id] = projectSettings;

		milestones = projectSettings.milestones;
		this.recreateFeatures();
		this.recreateProjects();
		this.recreateTeams();

		return projectSettings;
	}

	async createProject(
		projectSettings: IProjectSettings,
	): Promise<IProjectSettings> {
		console.log(`Creating Project ${projectSettings.name}`);

		await delay();
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
		await delay();
		const project = await this.getProject(id);

		if (project) {
			project.lastUpdated = new Date();
			await delay();
		}

		this.notifyAboutUpdate("Features", id, "Completed");
	}

	async refreshForecastsForProject(id: number): Promise<void> {
		console.log(`Refreshing Forecasts for Project with id ${id}`);
		this.notifyAboutUpdate("Forecasts", id, "Queued");
		await delay();

		const project = await this.getProject(id);

		if (project) {
			project.lastUpdated = new Date();
			await delay();
		}

		this.notifyAboutUpdate("Forecasts", id, "Completed");
	}

	async deleteProject(id: number): Promise<void> {
		console.log(`'Deleting' Project with id ${id}`);
		await delay();
	}

	async getCurrentVersion(): Promise<string> {
		await delay();
		return "DEMO VERSION";
	}

	async isUpdateAvailable(): Promise<boolean> {
		await delay();
		return true;
	}

	async getNewReleases(): Promise<ILighthouseRelease[]> {
		await delay();

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
		await delay();

		return projects;
	}

	async getWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]> {
		await delay();

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
		await delay();

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

	async getTags(): Promise<string[]> {
		await delay();

		return ["GCZ", "VfL", "Barca"];
	}

	async getWorkItemTypesForTeams(): Promise<string[]> {
		await delay();

		return ["User Story", "Bug"];
	}

	async getWorkItemTypesForProjects(): Promise<string[]> {
		await delay();

		return ["Epic"];
	}

	async getStatesForTeams(): Promise<StatesCollection> {
		await delay();

		return {
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
		};
	}

	async getStatesForProjects(): Promise<StatesCollection> {
		await delay();

		return {
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
		};
	}

	async addNewWorkTrackingSystemConnection(
		newWorkTrackingSystemConnection: IWorkTrackingSystemConnection,
	): Promise<IWorkTrackingSystemConnection> {
		await delay();

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
		await delay();

		return modifiedConnection;
	}

	async deleteWorkTrackingSystemConnection(
		connectionId: number,
	): Promise<void> {
		console.log(`Deleting Work Tracking Connection with id ${connectionId}`);
		await delay();
	}

	async validateWorkTrackingSystemConnection(
		connection: IWorkTrackingSystemConnection,
	): Promise<boolean> {
		console.log(`Validating connection for ${connection.name}`);
		await delay();
		return Math.random() >= 0.5;
	}

	async getLogLevel(): Promise<string> {
		await delay();
		return "Information";
	}

	async getSupportedLogLevels(): Promise<string[]> {
		await delay();

		return ["Debug", "Information", "Warning", "Error"];
	}

	async setLogLevel(logLevel: string): Promise<void> {
		console.log(`Setting log level to ${logLevel}`);
		await delay();
	}

	async getLogs(): Promise<string> {
		await delay();

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

		await delay();

		return new RefreshSettings(10, 20, 30);
	}

	async updateRefreshSettings(
		settingName: string,
		refreshSettings: IRefreshSettings,
	): Promise<void> {
		console.log(`Update ${settingName} refresh settings: ${refreshSettings}`);

		await delay();
	}

	async getDefaultTeamSettings(): Promise<ITeamSettings> {
		await delay();

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
			tags: [],
			automaticallyAdjustFeatureWIP: false,
			serviceLevelExpectationProbability: 75,
			serviceLevelExpectationRange: 35,
		};
	}

	async updateDefaultTeamSettings(teamSettings: ITeamSettings): Promise<void> {
		console.log(`Updating ${teamSettings.name} Team Settings`);
		await delay();
	}

	async getDefaultProjectSettings(): Promise<IProjectSettings> {
		await delay();

		return {
			id: 1,
			name: "My Project",
			workItemTypes: ["Feature", "Epic"],
			milestones: [
				(() => {
					const milestone = new Milestone();
					milestone.id = 1;
					milestone.name = "Target Date";
					milestone.date = new Date(today + 14 * dayMultiplier);
					return milestone;
				})(),
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
			tags: [],
			overrideRealChildCountStates: ["Analysis In Progress"],
			involvedTeams: [],
			serviceLevelExpectationProbability: 75,
			serviceLevelExpectationRange: 35,
		};
	}

	async updateDefaultProjectSettings(
		projecSettings: IProjectSettings,
	): Promise<void> {
		console.log(`Updating ${projecSettings.name} Team Settings`);
		await delay();
	}

	async getDataRetentionSettings(): Promise<IDataRetentionSettings> {
		await delay();
		return this.dataRetentionSettings;
	}

	async updateDataRetentionSettings(
		dataRetentionSettings: IDataRetentionSettings,
	): Promise<void> {
		await delay();
		this.dataRetentionSettings = dataRetentionSettings;
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

		teamThroughputs = {
			0: throughput1,
			1: throughput2,
			2: throughput3,
			3: throughput4,
		};

		teamFeaturesInProgress = {
			0: ["FTR-1", "FTR-3"],
			1: ["FTR-2", "FTR-3"],
			2: ["FTR-3"],
			3: ["FTR-4"],
		};

		const teamBinaryBlazers = new Team();
		teamBinaryBlazers.id = 0;
		teamBinaryBlazers.name = "Binary Blazers";
		teamBinaryBlazers.features = [features[0], features[3]];
		teamBinaryBlazers.featureWip = 1;
		teamBinaryBlazers.tags = ["Task Force"];
		teamBinaryBlazers.serviceLevelExpectationProbability = 75;
		teamBinaryBlazers.serviceLevelExpectationRange = 11;

		const teamMavericks = new Team();
		teamMavericks.id = 1;
		teamMavericks.name = "Mavericks";
		teamMavericks.features = [features[1], features[2]];
		teamMavericks.featureWip = 2;

		const teamCyberSultans = new Team();
		teamCyberSultans.id = 2;
		teamCyberSultans.name = "Cyber Sultans";
		teamCyberSultans.features = [features[2]];
		teamCyberSultans.featureWip = 1;
		teamCyberSultans.tags = ["Task Force"];

		const teamTechEagles = new Team();
		teamTechEagles.id = 3;
		teamTechEagles.name = "Tech Eagles";
		teamTechEagles.features = [features[3]];
		teamTechEagles.featureWip = 2;
		teamTechEagles.tags = ["New"];

		teams = [
			teamBinaryBlazers,
			teamMavericks,
			teamCyberSultans,
			teamTechEagles,
		];
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

		const feature1 = new Feature();
		feature1.name = "Feature 1";
		feature1.id = 0;
		feature1.workItemReference = "FTR-1";
		feature1.state = "ToDo";
		feature1.projects = { 0: "Release 1.33.7" };
		feature1.remainingWork = { 0: 10 };
		feature1.totalWork = { 0: 15 };
		feature1.milestoneLikelihood = getMileStoneLikelihoods();
		feature1.forecasts = [
			WhenForecast.new(50, new Date(today + 5 * dayMultiplier)),
			WhenForecast.new(70, new Date(today + 10 * dayMultiplier)),
			WhenForecast.new(85, new Date(today + 17 * dayMultiplier)),
			WhenForecast.new(95, new Date(today + 25 * dayMultiplier)),
		];
		feature1.url =
			"https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/1";

		const feature2 = new Feature();
		feature2.name = "Feature 2";
		feature2.id = 1;
		feature2.workItemReference = "FTR-2";
		feature2.state = "Doing";
		feature2.projects = { 1: "Release 42" };
		feature2.remainingWork = { 1: 5 };
		feature2.totalWork = { 1: 5 };
		feature2.milestoneLikelihood = getMileStoneLikelihoods();
		feature2.forecasts = [
			WhenForecast.new(50, new Date(today + 15 * dayMultiplier)),
			WhenForecast.new(70, new Date(today + 28 * dayMultiplier)),
			WhenForecast.new(85, new Date(today + 35 * dayMultiplier)),
			WhenForecast.new(95, new Date(today + 45 * dayMultiplier)),
		];
		feature2.url =
			"https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/2";

		const feature3 = new Feature();
		feature3.name = "Feature 3";
		feature3.id = 2;
		feature3.workItemReference = "FTR-3";
		feature3.state = "Done";
		feature3.projects = { 2: "Release Codename Daniel" };
		feature3.remainingWork = { 2: 7, 1: 15 };
		feature3.totalWork = { 2: 10, 1: 25 };
		feature3.milestoneLikelihood = getMileStoneLikelihoods();
		feature3.forecasts = [
			WhenForecast.new(50, new Date(today + 7 * dayMultiplier)),
			WhenForecast.new(70, new Date(today + 12 * dayMultiplier)),
			WhenForecast.new(85, new Date(today + 14 * dayMultiplier)),
			WhenForecast.new(95, new Date(today + 16 * dayMultiplier)),
		];
		feature3.url =
			"https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/3";

		const feature4 = new Feature();
		feature4.name = "Feature 4";
		feature4.id = 3;
		feature4.workItemReference = "FTR-4";
		feature4.state = "Unknown";
		feature4.projects = { 2: "Release Codename Daniel", 1: "Release 1.33.7" };
		feature4.remainingWork = { 2: 3, 1: 5 };
		feature4.totalWork = { 2: 5, 1: 10 };
		feature4.milestoneLikelihood = getMileStoneLikelihoods();
		feature4.forecasts = [
			WhenForecast.new(50, new Date(today + 21 * dayMultiplier)),
			WhenForecast.new(70, new Date(today + 37 * dayMultiplier)),
			WhenForecast.new(85, new Date(today + 55 * dayMultiplier)),
			WhenForecast.new(95, new Date(today + 71 * dayMultiplier)),
		];

		features = [feature1, feature2, feature3, feature4];
	}

	recreateProjects(): void {
		const project1 = new Project();
		project1.name = "Release 1.33.7";
		project1.id = 0;
		project1.involvedTeams = [teams[0]];
		project1.features = [features[0]];
		project1.milestones = milestones;
		project1.tags = ["Urgent"];

		const project2 = new Project();
		project2.name = "Release 42";
		project2.id = 1;
		project2.involvedTeams = [teams[1]];
		project2.features = [features[1]];
		project2.milestones = milestones;
		project2.tags = ["New", "Important Customer"];

		const project3 = new Project();
		project3.name = "Release Codename Daniel";
		project3.id = 2;
		project3.involvedTeams = [teams[2], teams[3]];
		project3.features = [features[2], features[3]];
		project3.milestones = milestones;

		projects = [project1, project2, project3];
	}
}
