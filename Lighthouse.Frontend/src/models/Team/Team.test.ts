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

		const project1 = new Project(
			"Project 1",
			1,
			[],
			[],
			[],
			new Date("2023-07-11"),
		);
		const project2 = new Project(
			"Project 2",
			2,
			[],
			[],
			[],
			new Date("2023-07-10"),
		);
		projects = [project1, project2];

		const feature1 = new Feature(
			"Feature 1",
			1,
			"FTR-1",
			"",
			"Unknown",
			new Date("2023-07-10"),
			false,
			{ 1: "Project 1" },
			{ 1: 10, 2: 20 },
			{ 1: 10, 2: 20 },
			{},
			[new WhenForecast(0.8, new Date("2023-08-01"))],
		);
		const feature2 = new Feature(
			"Feature 2",
			2,
			"FTR-2",
			"",
			"Unknown",
			new Date("2023-07-09"),
			true,
			{ 2: "Project 2" },
			{ 1: 5, 2: 15 },
			{ 1: 5, 2: 15 },
			{},
			[new WhenForecast(0.6, new Date("2023-09-01"))],
		);
		features = [feature1, feature2];

		team = new Team(
			name,
			id,
			projects,
			features,
			1,
			["FTR-1"],
			new Date(),
			[1],
			false,
			new Date(new Date().setDate(new Date().getDate() - [1].length)),
			new Date(),
		);
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
