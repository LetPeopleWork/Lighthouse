import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { type IProject, Project } from "../../models/Project/Project";
import { createMockProjectSettings } from "../../tests/TestDataProvider";
import { ProjectService } from "./ProjectService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("ProjectService", () => {
	let projectService: ProjectService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		projectService = new ProjectService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get all projects", async () => {
		const project = new Project();
		project.name = "Project 1";
		project.id = 1;
		project.lastUpdated = new Date("2023-09-01T12:00:00Z");

		const mockResponse: IProject[] = [project];

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const projects = await projectService.getProjects();

		expect(projects).toEqual([project]);
		expect(mockedAxios.get).toHaveBeenCalledWith("/projects");
	});

	it("should get a project by id", async () => {
		const project = new Project();
		project.name = "Project 1";
		project.id = 1;
		project.lastUpdated = new Date("2023-09-01T12:00:00Z");

		const mockProject: IProject = project;
		mockedAxios.get.mockResolvedValueOnce({ data: mockProject });

		const mockResponse = await projectService.getProject(1);

		expect(mockResponse).toEqual(project);
		expect(mockedAxios.get).toHaveBeenCalledWith("/projects/1");
	});

	it("should delete a project by id", async () => {
		mockedAxios.delete.mockResolvedValueOnce({});

		await projectService.deleteProject(1);

		expect(mockedAxios.delete).toHaveBeenCalledWith("/projects/1");
	});

	it("should get project settings by id", async () => {
		const mockSettings = createMockProjectSettings();

		mockedAxios.get.mockResolvedValueOnce({ data: mockSettings });

		const settings = await projectService.getProjectSettings(1);

		expect(settings).toEqual(mockSettings);
		expect(mockedAxios.get).toHaveBeenCalledWith("/projects/1/settings");
	});

	it("should update project settings", async () => {
		const projectSettings = createMockProjectSettings();

		mockedAxios.put.mockResolvedValueOnce({ data: projectSettings });

		const updatedSettings = await projectService.updateProject(projectSettings);

		expect(updatedSettings).toEqual(projectSettings);
		expect(mockedAxios.put).toHaveBeenCalledWith(
			"/projects/1",
			projectSettings,
		);
	});

	it("should create a new project", async () => {
		const newProjectSettings = createMockProjectSettings();

		const mockResponse = createMockProjectSettings();

		mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

		const createdSettings =
			await projectService.createProject(newProjectSettings);

		expect(createdSettings).toEqual(mockResponse);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/projects",
			newProjectSettings,
		);
	});

	it("should refresh features for a project by id", async () => {
		const mockProject: IProject = new Project();
		mockProject.name = "Project 1";
		mockProject.id = 1;
		mockProject.lastUpdated = new Date("2023-09-01T12:00:00Z");

		mockedAxios.post.mockResolvedValueOnce({ data: mockProject });

		await projectService.refreshFeaturesForProject(1);

		expect(mockedAxios.post).toHaveBeenCalledWith("/projects/refresh/1");
	});

	it("should refresh features for all projects", async () => {
		mockedAxios.post.mockResolvedValueOnce({});

		await projectService.refreshFeaturesForAllProjects();

		expect(mockedAxios.post).toHaveBeenCalledWith("/projects/refresh-all");
	});

	it("should refresh forecasts for a project by id", async () => {
		const project = new Project();
		project.name = "Project 1";
		project.id = 1;
		project.lastUpdated = new Date("2023-09-01T12:00:00Z");

		const mockProject: IProject = project;

		mockedAxios.post.mockResolvedValueOnce({ data: mockProject });

		await projectService.refreshForecastsForProject(1);

		expect(mockedAxios.post).toHaveBeenCalledWith("/forecast/update/1");
	});

	it("should validate project settings successfully", async () => {
		const mockProjectSettings = createMockProjectSettings();

		mockedAxios.post.mockResolvedValueOnce({ data: true });

		const isValid =
			await projectService.validateProjectSettings(mockProjectSettings);

		expect(isValid).toBe(true);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/projects/validate",
			mockProjectSettings,
		);
	});

	it("should return false for invalid project settings", async () => {
		const mockProjectSettings = createMockProjectSettings();

		mockedAxios.post.mockResolvedValueOnce({ data: false });

		const isValid =
			await projectService.validateProjectSettings(mockProjectSettings);

		expect(isValid).toBe(false);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/projects/validate",
			mockProjectSettings,
		);
	});
});
