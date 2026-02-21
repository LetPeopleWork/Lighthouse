import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ConfigurationExport } from "../../models/Configuration/ConfigurationExport";
import { ConfigurationService } from "./ConfigurationService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

// Mock the document methods needed for file download
const mockCreateElement = vi.fn();
const mockAppendChild = vi.fn();
const mockClick = vi.fn();
const mockRemove = vi.fn();

// Mock URL methods
const mockCreateObjectURL = vi.fn();
const mockRevokeObjectURL = vi.fn();

describe("ConfigurationService", () => {
	let configurationService: ConfigurationService;
	let originalCreateElement: typeof document.createElement;
	let originalAppendChild: typeof document.body.appendChild;
	let originalRemoveChild: typeof document.body.removeChild;
	let originalCreateObjectURL: typeof globalThis.URL.createObjectURL;
	let originalRevokeObjectURL: typeof globalThis.URL.revokeObjectURL;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		configurationService = new ConfigurationService();

		// Save original functions
		originalCreateElement = document.createElement;
		originalAppendChild = document.body.appendChild;
		originalRemoveChild = document.body.removeChild;
		originalCreateObjectURL = globalThis.URL.createObjectURL;
		originalRevokeObjectURL = globalThis.URL.revokeObjectURL;

		// Mock document functions
		document.createElement = mockCreateElement;
		document.body.appendChild = mockAppendChild;

		// Mock URL functions
		globalThis.URL.createObjectURL = mockCreateObjectURL;
		globalThis.URL.revokeObjectURL = mockRevokeObjectURL;

		// Setup mock link element
		mockCreateElement.mockReturnValue({
			setAttribute: vi.fn(),
			click: mockClick,
			remove: mockRemove,
		});

		// Mock createObjectURL to return a fake URL
		mockCreateObjectURL.mockReturnValue("blob:mock-url");
	});

	afterEach(() => {
		vi.resetAllMocks();

		// Restore original functions
		document.createElement = originalCreateElement;
		document.body.appendChild = originalAppendChild;
		document.body.removeChild = originalRemoveChild;
		globalThis.URL.createObjectURL = originalCreateObjectURL;
		globalThis.URL.revokeObjectURL = originalRevokeObjectURL;
	});

	it("should export configuration and download it", async () => {
		const mockResponseData = new Blob(['{"config": "test"}'], {
			type: "application/json",
		});
		const mockResponse = {
			data: mockResponseData,
			headers: {
				"content-disposition": 'attachment; filename="lighthouse-config.json"',
			},
		};

		mockedAxios.get.mockResolvedValueOnce(mockResponse);

		await configurationService.exportConfiguration();

		expect(mockedAxios.get).toHaveBeenCalledWith("/configuration/export", {
			responseType: "blob",
		});
		expect(mockCreateObjectURL).toHaveBeenCalledWith(mockResponseData);
		expect(mockCreateElement).toHaveBeenCalledWith("a");
		expect(mockAppendChild).toHaveBeenCalled();
		expect(mockClick).toHaveBeenCalled();
		expect(mockRemove).toHaveBeenCalled();
		expect(mockRevokeObjectURL).toHaveBeenCalledWith("blob:mock-url");
	});

	it("should use default filename if content-disposition header is missing", async () => {
		const mockResponseData = new Blob(['{"config": "test"}'], {
			type: "application/json",
		});
		const mockResponse = {
			data: mockResponseData,
			headers: {}, // No content-disposition header
		};

		mockedAxios.get.mockResolvedValueOnce(mockResponse);

		let downloadFileName = "";
		mockCreateElement.mockReturnValue({
			setAttribute: (name: string, value: string) => {
				if (name === "download") {
					downloadFileName = value;
				}
			},
			click: mockClick,
			remove: mockRemove,
		});

		await configurationService.exportConfiguration();

		expect(downloadFileName).toBe("Lighthouse_Configuration.json");
	});

	it("should clear configuration", async () => {
		mockedAxios.delete.mockResolvedValueOnce({});

		await configurationService.clearConfiguration();

		expect(mockedAxios.delete).toHaveBeenCalledWith("/configuration/clear");
	});

	it("should validate configuration and return validation data", async () => {
		// Mock validation response data
		const mockValidationResponse = {
			data: {
				workTrackingSystems: [
					{ id: 1, name: "AzureDevOps", status: "New", errorMessage: "" },
				],
				teams: [{ id: 2, name: "Team A", status: "New", errorMessage: "" }],
				projects: [
					{ id: 3, name: "Project X", status: "New", errorMessage: "" },
				],
			},
		};

		mockedAxios.post.mockResolvedValueOnce(mockValidationResponse);

		const mockConfigurationExport: ConfigurationExport = {
			workTrackingSystems: [
				{
					id: 1,
					name: "AzureDevOps",
					workTrackingSystem: "AzureDevOps",
					options: [],
					authenticationMethodKey: "ado.pat",
					additionalFieldDefinitions: [],
					workTrackingSystemGetDataRetrievalDisplayName: () => "WIQL Query",
				},
			],
			teams: [
				{
					id: 2,
					name: "Team A",
					throughputHistory: 0,
					useFixedDatesForThroughput: false,
					throughputHistoryStartDate: new Date(),
					throughputHistoryEndDate: new Date(),
					featureWIP: 0,
					parentOverrideAdditionalFieldDefinitionId: null,
					automaticallyAdjustFeatureWIP: false,
					dataRetrievalValue: "",
					workItemTypes: [],
					toDoStates: [],
					doingStates: [],
					doneStates: [],
					tags: [],
					workTrackingSystemConnectionId: 0,
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
					blockedStates: ["Waiting for Peter"],
					blockedTags: ["Blocked", "On Hold"],
					doneItemsCutoffDays: 0,
					processBehaviourChartBaselineStartDate: null,
					processBehaviourChartBaselineEndDate: null,
					estimationAdditionalFieldDefinitionId: null,
					estimationUnit: null,
					useNonNumericEstimation: false,
					estimationCategoryValues: [],
				},
			],
			projects: [
				{
					id: 3,
					name: "Project X",
					involvedTeams: [],
					overrideRealChildCountStates: [],
					usePercentileToCalculateDefaultAmountOfWorkItems: false,
					defaultAmountOfWorkItemsPerFeature: 0,
					defaultWorkItemPercentile: 0,
					percentileHistoryInDays: 90,
					dataRetrievalValue: "",
					workItemTypes: [],
					toDoStates: [],
					doingStates: [],
					doneStates: [],
					tags: [],
					workTrackingSystemConnectionId: 0,
					serviceLevelExpectationProbability: 0,
					serviceLevelExpectationRange: 0,
					systemWIPLimit: 0,
					parentOverrideAdditionalFieldDefinitionId: null,
					sizeEstimateAdditionalFieldDefinitionId: null,
					featureOwnerAdditionalFieldDefinitionId: null,
					blockedStates: ["Waiting for Peter"],
					blockedTags: ["Blocked", "On Hold"],
					doneItemsCutoffDays: 0,
					processBehaviourChartBaselineStartDate: null,
					processBehaviourChartBaselineEndDate: null,
					estimationAdditionalFieldDefinitionId: null,
					estimationUnit: null,
					useNonNumericEstimation: false,
					estimationCategoryValues: [],
				},
			],
		};

		const result = await configurationService.validateConfiguration(
			mockConfigurationExport,
		);

		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/configuration/validate",
			mockConfigurationExport,
		);

		expect(result).toEqual(mockValidationResponse.data);
		expect(result.workTrackingSystems).toHaveLength(1);
		expect(result.workTrackingSystems[0].name).toBe("AzureDevOps");
		expect(result.teams).toHaveLength(1);
		expect(result.teams[0].name).toBe("Team A");
		expect(result.projects).toHaveLength(1);
		expect(result.projects[0].name).toBe("Project X");
	});
});
