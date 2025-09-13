import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IDemoDataScenario } from "../../models/DemoData/IDemoData";
import { DemoDataService } from "./DemoDataService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("DemoDataService", () => {
	let demoDataService: DemoDataService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		demoDataService = new DemoDataService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	describe("getAvailableScenarios", () => {
		it("should get available demo scenarios", async () => {
			const mockScenarios: IDemoDataScenario[] = [
				{
					id: "small-startup",
					title: "Small Startup",
					description: "A basic setup with minimal teams and projects",
					isPremium: false,
				},
				{
					id: "enterprise-basic",
					title: "Enterprise Basic",
					description: "A mid-sized organization with multiple teams",
					isPremium: true,
				},
			];

			mockedAxios.get.mockResolvedValueOnce({ data: mockScenarios });

			const scenarios = await demoDataService.getAvailableScenarios();

			expect(scenarios).toEqual(mockScenarios);
			expect(scenarios).toHaveLength(2);
			expect(scenarios[0].title).toEqual("Small Startup");
			expect(scenarios[1].title).toEqual("Enterprise Basic");
			expect(mockedAxios.get).toHaveBeenCalledWith("/demo/scenarios");
		});

		it("should handle empty scenarios list", async () => {
			const mockScenarios: IDemoDataScenario[] = [];
			mockedAxios.get.mockResolvedValueOnce({ data: mockScenarios });

			const scenarios = await demoDataService.getAvailableScenarios();

			expect(scenarios).toEqual([]);
			expect(scenarios).toHaveLength(0);
			expect(mockedAxios.get).toHaveBeenCalledWith("/demo/scenarios");
		});
	});

	describe("loadScenario", () => {
		it("should load a specific scenario", async () => {
			const scenarioId = "small-startup";
			mockedAxios.post.mockResolvedValueOnce({ data: {} });

			await demoDataService.loadScenario(scenarioId);

			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/demo/scenarios/${scenarioId}/load`,
			);
		});

		it("should handle loading premium scenario", async () => {
			const scenarioId = "enterprise-basic";
			mockedAxios.post.mockResolvedValueOnce({ data: {} });

			await demoDataService.loadScenario(scenarioId);

			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/demo/scenarios/${scenarioId}/load`,
			);
		});

		it("should handle scenario loading with special characters in ID", async () => {
			const scenarioId = "mega-corp-2024";
			mockedAxios.post.mockResolvedValueOnce({ data: {} });

			await demoDataService.loadScenario(scenarioId);

			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/demo/scenarios/${scenarioId}/load`,
			);
		});
	});

	describe("loadAllScenarios", () => {
		it("should load all scenarios", async () => {
			mockedAxios.post.mockResolvedValueOnce({ data: {} });

			await demoDataService.loadAllScenarios();

			expect(mockedAxios.post).toHaveBeenCalledWith("/demo/scenarios/load-all");
		});

		it("should handle multiple calls to load all scenarios", async () => {
			mockedAxios.post.mockResolvedValueOnce({ data: {} });
			mockedAxios.post.mockResolvedValueOnce({ data: {} });

			await demoDataService.loadAllScenarios();
			await demoDataService.loadAllScenarios();

			expect(mockedAxios.post).toHaveBeenCalledTimes(2);
			expect(mockedAxios.post).toHaveBeenNthCalledWith(
				1,
				"/demo/scenarios/load-all",
			);
			expect(mockedAxios.post).toHaveBeenNthCalledWith(
				2,
				"/demo/scenarios/load-all",
			);
		});
	});

	describe("error handling", () => {
		it("should handle network errors in getAvailableScenarios", async () => {
			const networkError = new Error("Network Error");
			mockedAxios.get.mockRejectedValueOnce(networkError);

			await expect(demoDataService.getAvailableScenarios()).rejects.toThrow(
				"Network Error",
			);
			expect(mockedAxios.get).toHaveBeenCalledWith("/demo/scenarios");
		});

		it("should handle errors in loadScenario", async () => {
			const scenarioId = "invalid-scenario";
			const apiError = new Error("Scenario not found");
			mockedAxios.post.mockRejectedValueOnce(apiError);

			await expect(demoDataService.loadScenario(scenarioId)).rejects.toThrow(
				"Scenario not found",
			);
			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/demo/scenarios/${scenarioId}/load`,
			);
		});

		it("should handle errors in loadAllScenarios", async () => {
			const apiError = new Error("Server Error");
			mockedAxios.post.mockRejectedValueOnce(apiError);

			await expect(demoDataService.loadAllScenarios()).rejects.toThrow(
				"Server Error",
			);
			expect(mockedAxios.post).toHaveBeenCalledWith("/demo/scenarios/load-all");
		});
	});
});
