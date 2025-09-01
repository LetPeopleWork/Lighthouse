import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../models/Feature";
import { FeatureService } from "./FeatureService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("FeatureService", () => {
	let featureService: FeatureService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		featureService = new FeatureService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get parent features by reference ids", async () => {
		// Arrange
		const referenceIds = ["FTR-1", "FTR-2"];
		const date = new Date();

		// Mock feature data similar to how Feature objects would be structured
		const mockResponse: IFeature[] = [
			{
				name: "Feature 1",
				id: 1,
				referenceId: "FTR-1",
				state: "In Progress",
				type: "Feature",
				size: 12,
				lastUpdated: date,
				isUsingDefaultFeatureSize: false,
				owningTeam: "",
				parentWorkItemReference: "",
				projects: { 1: "Project A" },
				remainingWork: { 1: 5 },
				totalWork: { 1: 10 },
				milestoneLikelihood: { 1: 85.5 },
				forecasts: [
					{
						probability: 0.85,
						expectedDate: date,
					},
				],
				url: "https://example.com/feature/1",
				stateCategory: "Doing",
				startedDate: date,
				closedDate: new Date(),
				cycleTime: 5,
				workItemAge: 10,
				getRemainingWorkForFeature: () => 5,
				getRemainingWorkForTeam: () => 5,
				getTotalWorkForFeature: () => 10,
				getTotalWorkForTeam: () => 10,
				getMilestoneLikelihood: () => 85.5,
				isBlocked: false,
			},
			{
				name: "Feature 2",
				id: 2,
				referenceId: "FTR-2",
				state: "In Progress",
				type: "Feature",
				size: 12,
				owningTeam: "",
				lastUpdated: date,
				isUsingDefaultFeatureSize: true,
				parentWorkItemReference: "",
				projects: { 1: "Project A" },
				remainingWork: { 1: 3 },
				totalWork: { 1: 8 },
				milestoneLikelihood: { 1: 90.2 },
				forecasts: [
					{
						probability: 0.9,
						expectedDate: date,
					},
				],
				url: "https://example.com/feature/2",
				stateCategory: "Doing",
				startedDate: date,
				closedDate: new Date(),
				cycleTime: 3,
				workItemAge: 7,
				getRemainingWorkForFeature: () => 3,
				getRemainingWorkForTeam: () => 3,
				getTotalWorkForFeature: () => 8,
				getTotalWorkForTeam: () => 8,
				getMilestoneLikelihood: () => 90.2,
				isBlocked: false,
			},
		];

		// Setup the mock to return our data
		const expectedUrl =
			"/features/parent?parentFeatureReferenceIds=FTR-1&parentFeatureReferenceIds=FTR-2";
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		// Act
		const features = await featureService.getParentFeatures(referenceIds);

		// Assert
		expect(features).toHaveLength(2);
		expect(features[0].referenceId).toBe("FTR-1");
		expect(features[1].referenceId).toBe("FTR-2");
		expect(mockedAxios.get).toHaveBeenCalledWith(expectedUrl);
	});
});
