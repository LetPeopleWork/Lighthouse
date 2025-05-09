import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { StatesCollection } from "../../models/StatesCollection";
import { SuggestionService } from "./SuggestionService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("SuggestionService", () => {
	let tagService: SuggestionService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		tagService = new SuggestionService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get tags", async () => {
		const mockTags = ["Tag1", "Tag2", "Tag3"];

		mockedAxios.get.mockResolvedValueOnce({ data: mockTags });

		const result = await tagService.getTags();

		expect(result).toEqual(mockTags);
		expect(result).toHaveLength(3);
		expect(result[0]).toBe("Tag1");
		expect(result[1]).toBe("Tag2");
		expect(result[2]).toBe("Tag3");
		expect(mockedAxios.get).toHaveBeenCalledWith("/suggestions/tags");
	});

	it("should handle errors when getting tags", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(tagService.getTags()).rejects.toThrow(errorMessage);
	});

	it("should get work item types for teams", async () => {
		const mockWorkItemTypes = ["User Story", "Bug", "Task"];

		mockedAxios.get.mockResolvedValueOnce({ data: mockWorkItemTypes });

		const result = await tagService.getWorkItemTypesForTeams();

		expect(result).toEqual(mockWorkItemTypes);
		expect(result).toHaveLength(3);
		expect(result[0]).toBe("User Story");
		expect(result[1]).toBe("Bug");
		expect(result[2]).toBe("Task");
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/suggestions/workitemtypes/teams",
		);
	});

	it("should handle errors when getting work item types for teams", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(tagService.getWorkItemTypesForTeams()).rejects.toThrow(
			errorMessage,
		);
	});

	it("should get work item types for projects", async () => {
		const mockWorkItemTypes = ["Epic", "Feature", "Initiative"];

		mockedAxios.get.mockResolvedValueOnce({ data: mockWorkItemTypes });

		const result = await tagService.getWorkItemTypesForProjects();

		expect(result).toEqual(mockWorkItemTypes);
		expect(result).toHaveLength(3);
		expect(result[0]).toBe("Epic");
		expect(result[1]).toBe("Feature");
		expect(result[2]).toBe("Initiative");
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/suggestions/workitemtypes/projects",
		);
	});

	it("should handle errors when getting work item types for projects", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(tagService.getWorkItemTypesForProjects()).rejects.toThrow(
			errorMessage,
		);
	});

	it("should get states for teams", async () => {
		const mockStates: StatesCollection = {
			toDoStates: ["New", "Proposed", "To Do"],
			doingStates: ["Active", "In Progress"],
			doneStates: ["Done", "Closed"],
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockStates });

		const result = await tagService.getStatesForTeams();

		expect(result).toEqual(mockStates);
		expect(result.toDoStates).toHaveLength(3);
		expect(result.doingStates).toHaveLength(2);
		expect(result.doneStates).toHaveLength(2);
		expect(mockedAxios.get).toHaveBeenCalledWith("/suggestions/states/teams");
	});

	it("should handle errors when getting states for teams", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(tagService.getStatesForTeams()).rejects.toThrow(errorMessage);
	});

	it("should get states for projects", async () => {
		const mockStates: StatesCollection = {
			toDoStates: ["Backlog", "Proposed", "Ready"],
			doingStates: ["In Development", "In Testing"],
			doneStates: ["Released", "Completed"],
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockStates });

		const result = await tagService.getStatesForProjects();

		expect(result).toEqual(mockStates);
		expect(result.toDoStates).toHaveLength(3);
		expect(result.doingStates).toHaveLength(2);
		expect(result.doneStates).toHaveLength(2);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/suggestions/states/projects",
		);
	});

	it("should handle errors when getting states for projects", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(tagService.getStatesForProjects()).rejects.toThrow(
			errorMessage,
		);
	});
});
