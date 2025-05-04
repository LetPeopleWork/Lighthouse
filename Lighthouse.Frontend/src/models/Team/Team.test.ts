import { Feature } from "../Feature";
import { WhenForecast } from "../Forecasts/WhenForecast";
import { Project } from "../Project/Project";
import { Team } from "./Team";

describe("Team Class", () => {
	let team: Team;
	let name: string;
	let id: number;
	let projects: Project[];
	let features: Feature[];

	beforeEach(() => {
		name = "Team A";
		id = 1;

		const project1 = (() => {
			const project = new Project();
			project.name = "Project 1";
			project.id = 1;
			project.lastUpdated = new Date("2023-07-11");
			return project;
		})();
		const project2 = (() => {
			const project = new Project();
			project.name = "Project 2";
			project.id = 2;
			project.lastUpdated = new Date("2023-07-10");
			return project;
		})();
		projects = [project1, project2];

		const feature1 = (() => {
			const feature = new Feature();
			feature.name = "Feature 1";
			feature.id = 1;
			feature.workItemReference = "FTR-1";
			feature.stateCategory = "ToDo";
			feature.lastUpdated = new Date("2023-07-10");
			feature.isUsingDefaultFeatureSize = false;
			feature.projects = { 1: "Project 1" };
			feature.remainingWork = { 1: 10, 2: 20 };
			feature.totalWork = { 1: 10, 2: 20 };
			feature.milestoneLikelihood = { 0: 88.7 };
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
			feature.workItemReference = "FTR-2";
			feature.stateCategory = "Doing";
			feature.lastUpdated = new Date("2023-07-09");
			feature.isUsingDefaultFeatureSize = true;
			feature.projects = { 2: "Project 2" };
			feature.remainingWork = { 1: 5, 2: 15 };
			feature.totalWork = { 1: 5, 2: 15 };
			feature.milestoneLikelihood = { 0: 54.3 };
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

		team = (() => {
			const team = new Team();
			team.name = name;
			team.id = id;
			team.projects = projects;
			team.features = features;
			team.featureWip = 1;
			team.lastUpdated = new Date();
			team.useFixedDatesForThroughput = false;
			team.throughputStartDate = new Date(
				new Date().setDate(new Date().getDate() - [1].length),
			);
			team.throughputEndDate = new Date();
			return team;
		})();
	});

	it("should create an instance of Team correctly", () => {
		expect(team.name).toBe(name);
		expect(team.id).toBe(id);
		expect(team.projects).toEqual(projects);
		expect(team.features).toEqual(features);
	});

	it("should return correct total remaining work for the team", () => {
		const expectedRemainingWork = 10 + 5;
		expect(team.remainingWork).toBe(expectedRemainingWork);
	});

	it("should return correct number of remaining features", () => {
		expect(team.remainingFeatures).toBe(2);
	});
});
