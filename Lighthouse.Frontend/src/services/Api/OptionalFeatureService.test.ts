import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { OptionalFeatureService } from "./OptionalFeatureService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("OptionalFeatureService", () => {
	let optionalFeatureService: OptionalFeatureService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		optionalFeatureService = new OptionalFeatureService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get all optional features", async () => {
		const mockResponse = [
			{
				id: 1,
				key: "feature-001",
				name: "Feature 1",
				description: "Description 1",
				enabled: true,
				isPreview: true,
			},
			{
				id: 2,
				key: "feature-002",
				name: "Feature 2",
				description: "Description 2",
				enabled: false,
				isPreview: false,
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const result = await optionalFeatureService.getAllFeatures();

		expect(result).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith("/optionalfeatures");
	});

	it("should get a feature by key", async () => {
		const key = "feature-001";
		const mockResponse = {
			id: 1,
			key,
			name: "Feature 1",
			description: "Description 1",
			enabled: true,
			isPreview: true,
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const result = await optionalFeatureService.getFeatureByKey(key);

		expect(result).toEqual(mockResponse);
		expect(mockedAxios.get).toHaveBeenCalledWith(`/optionalfeatures/${key}`);
	});

	it("should return null if feature by key does not exist", async () => {
		const key = "feature-003";

		mockedAxios.get.mockResolvedValueOnce({ data: null });

		const result = await optionalFeatureService.getFeatureByKey(key);

		expect(result).toBeNull();
		expect(mockedAxios.get).toHaveBeenCalledWith(`/optionalfeatures/${key}`);
	});

	it("should update an optional feature", async () => {
		const feature = {
			id: 1,
			key: "feature-001",
			name: "Feature 1",
			description: "Updated Description",
			enabled: true,
			isPreview: true,
		};

		mockedAxios.post.mockResolvedValueOnce({});

		await optionalFeatureService.updateFeature(feature);

		expect(mockedAxios.post).toHaveBeenCalledWith(
			`/optionalfeatures/${feature.id}`,
			feature,
		);
	});

	it("should throw an error if API call fails", async () => {
		const feature = {
			id: 1,
			key: "feature-001",
			name: "Feature 1",
			description: "Description",
			enabled: true,
			isPreview: true,
		};

		mockedAxios.post.mockRejectedValueOnce(new Error("API error"));

		await expect(optionalFeatureService.updateFeature(feature)).rejects.toThrow(
			"API error",
		);

		expect(mockedAxios.post).toHaveBeenCalledWith(
			`/optionalfeatures/${feature.id}`,
			feature,
		);
	});
});
