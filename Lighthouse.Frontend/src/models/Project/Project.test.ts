import { Feature } from "../Feature";
import { WhenForecast } from "../Forecasts/WhenForecast";
import { Team } from "../Team/Team";
import { Milestone } from "./Milestone";
import { Project } from "./Project";

describe("Project Class", () => {
	let project: Project;
	let name: string;
	let id: number;
	let involvedTeams: Team[];
	let features: Feature[];
	let lastUpdated: Date;

	beforeEach(() => {
		name = "New Project";
		id = 1;
		involvedTeams = [
			new Team(
				"Team A",
				1,
				[],
				[],
				2,
				new Date(),
				false,
				new Date(new Date().setDate(new Date().getDate() - [1].length)),
				new Date(),
			),
			new Team(
				"Team B",
				2,
				[],
				[],
				1,
				new Date(),
				false,
				new Date(new Date().setDate(new Date().getDate() - [1].length)),
				new Date(),
			),
		];
		lastUpdated = new Date("2023-07-11");

		const milestone = new Milestone(
			0,
			"Milestone 1",
			new Date(Date.now() + 14 * 24 * 60 * 60),
		);

		const feature1 = new Feature(
			"Feature 1",
			1,
			"FTR-1",
			"",
			"Unknown",
			new Date("2023-07-10"),
			false,
			{ 1: name },
			{ 1: 10, 2: 20 },
			{ 1: 10, 2: 20 },
			{ 0: 88.7 },
			[new WhenForecast(0.8, new Date("2023-08-01"))],
			null,
			"ToDo",
			new Date("2023-07-01"),
			new Date("2023-07-10"),
			9,
			10,
		);
		const feature2 = new Feature(
			"Feature 2",
			2,
			"FTR-2",
			"",
			"Unknown",
			new Date("2023-07-09"),
			true,
			{ 1: name },
			{ 1: 5, 2: 15 },
			{ 1: 5, 2: 15 },
			{ 0: 54.3 },
			[new WhenForecast(0.6, new Date("2023-09-01"))],
			null,
			"Doing",
			new Date("2023-07-01"),
			new Date("2023-07-09"),
			8,
			9,
		);

		features = [feature1, feature2];
		project = new Project(
			name,
			id,
			involvedTeams,
			features,
			[milestone],
			lastUpdated,
		);
	});

	it("should create an instance of Project correctly", () => {
		expect(project.name).toBe(name);
		expect(project.id).toBe(id);
		expect(project.involvedTeams).toEqual(involvedTeams);
		expect(project.lastUpdated).toBe(lastUpdated);
		expect(project.features).toEqual(features);
	});

	it("should return correct total remaining work", () => {
		const expectedRemainingWork = 10 + 20 + 5 + 15;
		expect(project.remainingWork).toBe(expectedRemainingWork);
	});

	it("should return correct number of remaining features", () => {
		expect(project.remainingFeatures).toBe(2);
	});
});
