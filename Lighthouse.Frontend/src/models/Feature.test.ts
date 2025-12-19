import { Feature } from "./Feature";
import { WhenForecast } from "./Forecasts/WhenForecast";

describe("Feature Class", () => {
	let feature: Feature;
	let name: string;
	let id: number;
	let referenceId: string;
	let lastUpdated: Date;
	let remainingWork: { [key: number]: number };
	let totalWork: { [key: number]: number };
	let forecasts: WhenForecast[];

	beforeEach(() => {
		name = "New Feature";
		id = 1;
		referenceId = "FTR-1";
		lastUpdated = new Date("2023-07-11");
		remainingWork = { 1: 10, 2: 20, 3: 30 };
		totalWork = { 1: 15, 2: 20, 3: 45 };
		forecasts = [
			WhenForecast.new(0.8, new Date("2023-08-01")),
			WhenForecast.new(0.6, new Date("2023-09-01")),
		];

		feature = new Feature();
		feature.name = name;
		feature.id = id;
		feature.referenceId = referenceId;
		feature.lastUpdated = lastUpdated;
		feature.remainingWork = remainingWork;
		feature.totalWork = totalWork;
		feature.forecasts = forecasts;
	});

	it("should create an instance of Feature correctly", () => {
		expect(feature.name).toBe(name);
		expect(feature.id).toBe(id);
		expect(feature.referenceId).toBe(referenceId);
		expect(feature.lastUpdated).toBe(lastUpdated);
		expect(feature.remainingWork).toEqual(remainingWork);
		expect(feature.forecasts).toEqual(forecasts);
	});

	it("should return correct remaining work for a team", () => {
		expect(feature.getRemainingWorkForTeam(1)).toBe(10);
		expect(feature.getRemainingWorkForTeam(2)).toBe(20);
		expect(feature.getRemainingWorkForTeam(3)).toBe(30);
		expect(feature.getRemainingWorkForTeam(4)).toBe(0);
	});

	it("should return correct total remaining work", () => {
		expect(feature.getRemainingWorkForFeature()).toBe(60);
	});

	it("should return correct total work for a team", () => {
		expect(feature.getTotalWorkForTeam(1)).toBe(15);
		expect(feature.getTotalWorkForTeam(2)).toBe(20);
		expect(feature.getTotalWorkForTeam(3)).toBe(45);
		expect(feature.getTotalWorkForTeam(4)).toBe(0);
	});

	it("should return correct total work", () => {
		expect(feature.getTotalWorkForFeature()).toBe(80);
	});
});
