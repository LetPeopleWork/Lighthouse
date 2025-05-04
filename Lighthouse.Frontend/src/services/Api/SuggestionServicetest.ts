import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
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
});
