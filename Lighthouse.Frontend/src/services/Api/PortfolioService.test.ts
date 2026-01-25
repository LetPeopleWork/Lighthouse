import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { type IPortfolio, Portfolio } from "../../models/Portfolio/Portfolio";
import { createMockProjectSettings } from "../../tests/TestDataProvider";
import { PortfolioService } from "./PortfolioService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("PortfolioService", () => {
	let portfolioService: PortfolioService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		portfolioService = new PortfolioService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get all portfolios", async () => {
		const portfolio = new Portfolio();
		portfolio.name = "Project 1";
		portfolio.id = 1;
		portfolio.lastUpdated = new Date("2023-09-01T12:00:00Z");

		const mockResponse: IPortfolio[] = [portfolio];

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const portfolios = await portfolioService.getPortfolios();

		expect(portfolios).toEqual([portfolio]);
		expect(mockedAxios.get).toHaveBeenCalledWith("/portfolios");
	});

	it("should get a project by id", async () => {
		const project = new Portfolio();
		project.name = "Project 1";
		project.id = 1;
		project.lastUpdated = new Date("2023-09-01T12:00:00Z");

		const mockProject: IPortfolio = project;
		mockedAxios.get.mockResolvedValueOnce({ data: mockProject });

		const mockResponse = await portfolioService.getPortfolio(1);

		expect(mockResponse).toEqual(project);
		expect(mockedAxios.get).toHaveBeenCalledWith("/portfolios/1");
	});

	it("should delete a project by id", async () => {
		mockedAxios.delete.mockResolvedValueOnce({});

		await portfolioService.deletePortfolio(1);

		expect(mockedAxios.delete).toHaveBeenCalledWith("/portfolios/1");
	});

	it("should get project settings by id", async () => {
		const mockSettings = createMockProjectSettings();

		mockedAxios.get.mockResolvedValueOnce({ data: mockSettings });

		const settings = await portfolioService.getPortfolioSettings(1);

		expect(settings).toEqual(mockSettings);
		expect(mockedAxios.get).toHaveBeenCalledWith("/portfolios/1/settings");
	});

	it("should update project settings", async () => {
		const projectSettings = createMockProjectSettings();

		mockedAxios.put.mockResolvedValueOnce({ data: projectSettings });

		const updatedSettings =
			await portfolioService.updatePortfolio(projectSettings);

		expect(updatedSettings).toEqual(projectSettings);
		expect(mockedAxios.put).toHaveBeenCalledWith(
			"/portfolios/1",
			projectSettings,
		);
	});

	it("should create a new project", async () => {
		const newProjectSettings = createMockProjectSettings();

		const mockResponse = createMockProjectSettings();

		mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

		const createdSettings =
			await portfolioService.createPortfolio(newProjectSettings);

		expect(createdSettings).toEqual(mockResponse);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/portfolios",
			newProjectSettings,
		);
	});

	it("should refresh features for a project by id", async () => {
		const mockProject: IPortfolio = new Portfolio();
		mockProject.name = "Project 1";
		mockProject.id = 1;
		mockProject.lastUpdated = new Date("2023-09-01T12:00:00Z");

		mockedAxios.post.mockResolvedValueOnce({ data: mockProject });

		await portfolioService.refreshFeaturesForPortfolio(1);

		expect(mockedAxios.post).toHaveBeenCalledWith("/portfolios/1/refresh");
	});

	it("should refresh features for all portfolios", async () => {
		mockedAxios.post.mockResolvedValueOnce({});

		await portfolioService.refreshFeaturesForAllPortfolios();

		expect(mockedAxios.post).toHaveBeenCalledWith("/portfolios/refresh-all");
	});

	it("should refresh forecasts for a project by id", async () => {
		const project = new Portfolio();
		project.name = "Project 1";
		project.id = 1;
		project.lastUpdated = new Date("2023-09-01T12:00:00Z");

		const mockProject: IPortfolio = project;

		mockedAxios.post.mockResolvedValueOnce({ data: mockProject });

		await portfolioService.refreshForecastsForPortfolio(1);

		expect(mockedAxios.post).toHaveBeenCalledWith("/forecast/1/update");
	});

	it("should validate project settings successfully", async () => {
		const mockProjectSettings = createMockProjectSettings();

		mockedAxios.post.mockResolvedValueOnce({ data: true });

		const isValid =
			await portfolioService.validatePortfolioSettings(mockProjectSettings);

		expect(isValid).toBe(true);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/portfolios/validate",
			mockProjectSettings,
		);
	});

	it("should return false for invalid project settings", async () => {
		const mockProjectSettings = createMockProjectSettings();

		mockedAxios.post.mockResolvedValueOnce({ data: false });

		const isValid =
			await portfolioService.validatePortfolioSettings(mockProjectSettings);

		expect(isValid).toBe(false);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/portfolios/validate",
			mockProjectSettings,
		);
	});
});
