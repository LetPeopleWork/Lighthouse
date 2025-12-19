import { Feature } from "../Feature";
import { WhenForecast } from "../Forecasts/WhenForecast";
import { Team } from "../Team/Team";
import { Portfolio } from "./Portfolio";

describe("Project Class", () => {
	let project: Portfolio;
	let name: string;
	let id: number;
	let involvedTeams: Team[];
	let features: Feature[];
	let lastUpdated: Date;

	beforeEach(() => {
		name = "New Project";
		id = 1;
		involvedTeams = [
			(() => {
				const team = new Team();
				team.name = "Team A";
				team.id = 1;
				team.projects = [];
				team.features = [];
				team.featureWip = 2;
				team.lastUpdated = new Date();
				team.useFixedDatesForThroughput = false;
				team.throughputStartDate = new Date(
					new Date().setDate(new Date().getDate() - [1].length),
				);
				team.throughputEndDate = new Date();
				return team;
			})(),
			(() => {
				const team = new Team();
				team.name = "Team B";
				team.id = 2;
				team.projects = [];
				team.features = [];
				team.featureWip = 1;
				team.lastUpdated = new Date();
				team.useFixedDatesForThroughput = false;
				team.throughputStartDate = new Date(
					new Date().setDate(new Date().getDate() - [1].length),
				);
				team.throughputEndDate = new Date();
				return team;
			})(),
		];
		lastUpdated = new Date("2023-07-11");

		const feature1 = (() => {
			const feature = new Feature();
			feature.name = "Feature 1";
			feature.id = 1;
			feature.referenceId = "FTR-1";
			feature.stateCategory = "ToDo";
			feature.lastUpdated = new Date("2023-07-10");
			feature.isUsingDefaultFeatureSize = false;
			feature.projects = [{ id: 1, name: name }];
			feature.remainingWork = { 1: 10, 2: 20 };
			feature.totalWork = { 1: 10, 2: 20 };
			feature.forecasts = [
				(() => {
					const forecast = new WhenForecast();
					forecast.probability = 0.8;
					forecast.expectedDate = new Date("2023-08-01");
					return forecast;
				})(),
			];
			feature.startedDate = new Date("2023-07-01");
			feature.closedDate = new Date("2023-07-10");
			feature.cycleTime = 9;
			feature.workItemAge = 10;
			return feature;
		})();
		const feature2 = (() => {
			const feature = new Feature();
			feature.name = "Feature 2";
			feature.id = 2;
			feature.referenceId = "FTR-2";
			feature.stateCategory = "Doing";
			feature.lastUpdated = new Date("2023-07-09");
			feature.isUsingDefaultFeatureSize = true;
			feature.projects = [{ id: 1, name: name }];
			feature.remainingWork = { 1: 5, 2: 15 };
			feature.totalWork = { 1: 5, 2: 15 };
			feature.forecasts = [
				(() => {
					const forecast = new WhenForecast();
					forecast.probability = 0.6;
					forecast.expectedDate = new Date("2023-09-01");
					return forecast;
				})(),
			];
			feature.startedDate = new Date("2023-07-01");
			feature.closedDate = new Date("2023-07-09");
			feature.cycleTime = 8;
			feature.workItemAge = 9;
			return feature;
		})();

		features = [feature1, feature2];
		project = (() => {
			const project = new Portfolio();
			project.name = name;
			project.id = id;
			project.involvedTeams = involvedTeams;
			project.features = features;
			project.lastUpdated = lastUpdated;
			return project;
		})();
	});

	it("should create an instance of Project correctly", () => {
		expect(project.name).toBe(name);
		expect(project.id).toBe(id);
		expect(project.involvedTeams).toEqual(involvedTeams);
		expect(project.lastUpdated).toBe(lastUpdated);
		expect(project.features).toEqual(features);
	});

	it("should return correct number of remaining features", () => {
		expect(project.remainingFeatures).toBe(2);
	});
});
