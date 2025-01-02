import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { type IProject, Project } from "../../models/Project/Project";
import type { IProjectSettings } from "../../models/Project/ProjectSettings";
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
		const mockResponse: IProject[] = [
			new Project("Project 1", 1, [], [], [], new Date("2023-09-01T12:00:00Z")),
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const projects = await projectService.getProjects();

		expect(projects).toEqual([
			new Project("Project 1", 1, [], [], [], new Date("2023-09-01T12:00:00Z")),
		]);
		expect(mockedAxios.get).toHaveBeenCalledWith("/projects");
	});

	it("should get a project by id", async () => {
		const mockProject: IProject = new Project(
			"Project 1",
			1,
			[],
			[],
			[],
			new Date("2023-09-01T12:00:00Z"),
		);

		mockedAxios.get.mockResolvedValueOnce({ data: mockProject });

		const project = await projectService.getProject(1);

		expect(project).toEqual(
			new Project("Project 1", 1, [], [], [], new Date("2023-09-01T12:00:00Z")),
		);
		expect(mockedAxios.get).toHaveBeenCalledWith("/projects/1");
	});

	it("should delete a project by id", async () => {
		mockedAxios.delete.mockResolvedValueOnce({});

		await projectService.deleteProject(1);

		expect(mockedAxios.delete).toHaveBeenCalledWith("/projects/1");
	});

	it("should get project settings by id", async () => {
		const mockSettings: IProjectSettings = {
			id: 1,
			name: "ProjectSetting",
			workItemTypes: ["Epic"],
			milestones: [],
			workItemQuery: "Query",
			unparentedItemsQuery: "Unparented Query",
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 10,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: "",
			workTrackingSystemConnectionId: 0,
			sizeEstimateField: "Size",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			overrideRealChildCountStates: [""],
			involvedTeams: [],
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockSettings });

		const settings = await projectService.getProjectSettings(1);

		expect(settings).toEqual(mockSettings);
		expect(mockedAxios.get).toHaveBeenCalledWith("/projects/1/settings");
	});

	it("should update project settings", async () => {
		const projectSettings: IProjectSettings = {
			id: 1,
			name: "ProjectSetting",
			workItemTypes: ["Epic"],
			milestones: [],
			workItemQuery: "Query",
			unparentedItemsQuery: "Unparented Query",
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 10,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: "",
			workTrackingSystemConnectionId: 0,
			sizeEstimateField: "Size",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			overrideRealChildCountStates: [""],
			involvedTeams: [],
		};

		mockedAxios.put.mockResolvedValueOnce({ data: projectSettings });

		const updatedSettings = await projectService.updateProject(projectSettings);

		expect(updatedSettings).toEqual(projectSettings);
		expect(mockedAxios.put).toHaveBeenCalledWith(
			"/projects/1",
			projectSettings,
		);
	});

	it("should create a new project", async () => {
		const newProjectSettings: IProjectSettings = {
			id: 1,
			name: "ProjectSetting",
			workItemTypes: ["Epic"],
			milestones: [],
			workItemQuery: "Query",
			unparentedItemsQuery: "Unparented Query",
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 10,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: "",
			workTrackingSystemConnectionId: 0,
			sizeEstimateField: "Size",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			overrideRealChildCountStates: [""],
			involvedTeams: [],
		};

		const mockResponse: IProjectSettings = {
			id: 2,
			name: "ProjectSetting",
			workItemTypes: ["Epic"],
			milestones: [],
			workItemQuery: "Query",
			unparentedItemsQuery: "Unparented Query",
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 10,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: "",
			workTrackingSystemConnectionId: 0,
			sizeEstimateField: "Size",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			overrideRealChildCountStates: [""],
			involvedTeams: [],
		};

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
		const mockProject: IProject = new Project(
			"Project 1",
			1,
			[],
			[],
			[],
			new Date("2023-09-01T12:00:00Z"),
		);

		mockedAxios.post.mockResolvedValueOnce({ data: mockProject });

		await projectService.refreshFeaturesForProject(1);

		expect(mockedAxios.post).toHaveBeenCalledWith("/projects/refresh/1");
	});

	it("should refresh forecasts for a project by id", async () => {
		const mockProject: IProject = new Project(
			"Project 1",
			1,
			[],
			[],
			[],
			new Date("2023-09-01T12:00:00Z"),
		);

		mockedAxios.post.mockResolvedValueOnce({ data: mockProject });

		await projectService.refreshForecastsForProject(1);

		expect(mockedAxios.post).toHaveBeenCalledWith("/forecast/update/1");
	});

	it("should validate project settings successfully", async () => {
		const mockProjectSettings: IProjectSettings = {
			id: 1,
			name: "Project A",
			workItemTypes: ["Epic"],
			milestones: [],
			workItemQuery: "Query",
			unparentedItemsQuery: "Unparented Query",
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 10,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: "",
			workTrackingSystemConnectionId: 0,
			sizeEstimateField: "Size",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			overrideRealChildCountStates: [""],
			involvedTeams: [],
		};

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
		const mockProjectSettings: IProjectSettings = {
			id: 1,
			name: "Project A",
			workItemTypes: ["Epic"],
			milestones: [],
			workItemQuery: "Query",
			unparentedItemsQuery: "Unparented Query",
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 10,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: "",
			workTrackingSystemConnectionId: 0,
			sizeEstimateField: "Size",
			toDoStates: ["New"],
			doingStates: ["Active"],
			doneStates: ["Done"],
			overrideRealChildCountStates: [""],
			involvedTeams: [],
		};

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
