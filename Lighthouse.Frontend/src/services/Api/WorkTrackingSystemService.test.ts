import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	WriteBackAppliesTo,
	WriteBackTargetValueType,
	WriteBackValueSource,
} from "../../models/WorkTracking/WriteBackMappingDefinition";
import { WorkTrackingSystemService } from "./WorkTrackingSystemService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("WorkTrackingSystemService", () => {
	let workTrackingSystemService: WorkTrackingSystemService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		workTrackingSystemService = new WorkTrackingSystemService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get supported work tracking systems", async () => {
		const mockResponse: IWorkTrackingSystemConnection[] = [
			{
				id: 2,
				name: "Jira",
				workTrackingSystem: "Jira",
				authenticationMethodKey: "jira.cloud",
				additionalFieldDefinitions: [],
				writeBackMappingDefinitions: [],
				workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
				options: [
					{
						key: "apiToken",
						value: "token123",
						isSecret: true,
						isOptional: false,
					},
				],
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const workTrackingSystems =
			await workTrackingSystemService.getWorkTrackingSystems();

		expect(workTrackingSystems).toEqual([
			new WorkTrackingSystemConnection({
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [
					{
						key: "apiToken",
						value: "token123",
						isSecret: true,
						isOptional: false,
					},
				],
				id: 2,
				authenticationMethodKey: "jira.cloud",
				additionalFieldDefinitions: [],
			}),
		]);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/worktrackingsystemconnections/supported",
		);
	});

	it("should validate work tracking system connection", async () => {
		const mockConnection: IWorkTrackingSystemConnection = {
			id: 1,
			name: "Jira",
			workTrackingSystem: "Jira",
			authenticationMethodKey: "jira.cloud",
			additionalFieldDefinitions: [],
			writeBackMappingDefinitions: [],
			workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
			options: [
				{
					key: "apiToken",
					value: "token123",
					isSecret: true,
					isOptional: false,
				},
			],
		};
		mockedAxios.post.mockResolvedValueOnce({ data: true });

		const isValid =
			await workTrackingSystemService.validateWorkTrackingSystemConnection(
				mockConnection,
			);

		expect(isValid).toBe(true);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/worktrackingsystemconnections/validate",
			mockConnection,
		);
	});

	it("should get configured work tracking systems", async () => {
		const mockResponse: IWorkTrackingSystemConnection[] = [
			{
				id: 2,
				name: "Azure DevOps",
				workTrackingSystem: "AzureDevOps",
				authenticationMethodKey: "ado.pat",
				additionalFieldDefinitions: [],
				writeBackMappingDefinitions: [],
				workTrackingSystemGetDataRetrievalDisplayName: () => "WIQL Query",
				options: [
					{
						key: "apiToken",
						value: "adoToken",
						isSecret: true,
						isOptional: false,
					},
				],
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const configuredSystems =
			await workTrackingSystemService.getConfiguredWorkTrackingSystems();

		expect(configuredSystems).toEqual([
			new WorkTrackingSystemConnection({
				name: "Azure DevOps",
				workTrackingSystem: "AzureDevOps",
				options: [
					{
						key: "apiToken",
						value: "adoToken",
						isSecret: true,
						isOptional: false,
					},
				],
				id: 2,
				authenticationMethodKey: "ado.pat",
				additionalFieldDefinitions: [],
			}),
		]);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/worktrackingsystemconnections",
		);
	});

	it("should add a new work tracking system connection", async () => {
		const newConnection: IWorkTrackingSystemConnection = {
			id: 0,
			name: "Jira",
			workTrackingSystem: "Jira",
			authenticationMethodKey: "jira.cloud",
			additionalFieldDefinitions: [],
			writeBackMappingDefinitions: [],
			workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
			options: [
				{
					key: "apiToken",
					value: "token123",
					isSecret: true,
					isOptional: false,
				},
			],
		};

		const mockResponse: IWorkTrackingSystemConnection = {
			...newConnection,
			id: 3,
		};

		mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

		const createdConnection =
			await workTrackingSystemService.addNewWorkTrackingSystemConnection(
				newConnection,
			);

		expect(createdConnection).toEqual(
			new WorkTrackingSystemConnection({
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [
					{
						key: "apiToken",
						value: "token123",
						isSecret: true,
						isOptional: false,
					},
				],
				id: 3,
				authenticationMethodKey: "jira.cloud",
				additionalFieldDefinitions: [],
			}),
		);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/worktrackingsystemconnections",
			newConnection,
		);
	});

	it("should update a work tracking system connection", async () => {
		const updatedConnection: IWorkTrackingSystemConnection = {
			id: 1,
			name: "Jira",
			workTrackingSystem: "Jira",
			authenticationMethodKey: "jira.cloud",
			additionalFieldDefinitions: [],
			writeBackMappingDefinitions: [],
			workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
			options: [
				{
					key: "apiToken",
					value: "updatedToken123",
					isSecret: true,
					isOptional: false,
				},
			],
		};

		mockedAxios.put.mockResolvedValueOnce({ data: updatedConnection });

		const result =
			await workTrackingSystemService.updateWorkTrackingSystemConnection(
				updatedConnection,
			);

		expect(result).toEqual(
			new WorkTrackingSystemConnection({
				name: "Jira",
				workTrackingSystem: "Jira",
				options: [
					{
						key: "apiToken",
						value: "updatedToken123",
						isSecret: true,
						isOptional: false,
					},
				],
				id: 1,
				authenticationMethodKey: "jira.cloud",
				additionalFieldDefinitions: [],
			}),
		);
		expect(mockedAxios.put).toHaveBeenCalledWith(
			"/worktrackingsystemconnections/1",
			updatedConnection,
		);
	});

	it("should delete a work tracking system connection", async () => {
		mockedAxios.delete.mockResolvedValueOnce({});

		await workTrackingSystemService.deleteWorkTrackingSystemConnection(1);

		expect(mockedAxios.delete).toHaveBeenCalledWith(
			"/worktrackingsystemconnections/1",
		);
	});

	it("should deserialize authenticationMethodKey from response", async () => {
		const mockResponse: IWorkTrackingSystemConnection[] = [
			{
				id: 1,
				name: "Jira Cloud",
				workTrackingSystem: "Jira",
				options: [],
				authenticationMethodKey: "jira.cloud",
				additionalFieldDefinitions: [],
				workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
				writeBackMappingDefinitions: [],
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const workTrackingSystems =
			await workTrackingSystemService.getWorkTrackingSystems();

		expect(workTrackingSystems[0].authenticationMethodKey).toBe("jira.cloud");
	});

	describe("writeBackMappingDefinitions enum serialization", () => {
		it("should deserialize string enum values from API response to numeric enums", async () => {
			const mockResponse = [
				{
					id: 1,
					name: "ADO",
					workTrackingSystem: "AzureDevOps",
					options: [],
					authenticationMethodKey: "ado.pat",
					additionalFieldDefinitions: [],
					writeBackMappingDefinitions: [
						{
							id: 1,
							valueSource: "FeatureSize",
							appliesTo: "Portfolio",
							targetFieldReference: "Custom.Size",
							targetValueType: "Date",
							dateFormat: null,
						},
						{
							id: 2,
							valueSource: "ForecastPercentile85",
							appliesTo: "Portfolio",
							targetFieldReference: "Custom.Forecast",
							targetValueType: "FormattedText",
							dateFormat: "yyyy-MM-dd",
						},
					],
				},
			];

			mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

			const systems =
				await workTrackingSystemService.getConfiguredWorkTrackingSystems();

			const mappings = systems[0].writeBackMappingDefinitions;
			expect(mappings).toHaveLength(2);

			expect(mappings[0].valueSource).toBe(WriteBackValueSource.FeatureSize);
			expect(mappings[0].appliesTo).toBe(WriteBackAppliesTo.Portfolio);
			expect(mappings[0].targetValueType).toBe(WriteBackTargetValueType.Date);

			expect(mappings[1].valueSource).toBe(
				WriteBackValueSource.ForecastPercentile85,
			);
			expect(mappings[1].targetValueType).toBe(
				WriteBackTargetValueType.FormattedText,
			);
		});

		it("should serialize numeric enum values to strings when sending to API", async () => {
			const connection: IWorkTrackingSystemConnection = {
				id: 1,
				name: "ADO",
				workTrackingSystem: "AzureDevOps",
				options: [],
				authenticationMethodKey: "ado.pat",
				additionalFieldDefinitions: [],
				writeBackMappingDefinitions: [
					{
						id: -1,
						valueSource: WriteBackValueSource.FeatureSize,
						appliesTo: WriteBackAppliesTo.Portfolio,
						targetFieldReference: "Custom.Size",
						targetValueType: WriteBackTargetValueType.Date,
						dateFormat: null,
					},
				],
				workTrackingSystemGetDataRetrievalDisplayName: () => "WIQL",
			};

			mockedAxios.put.mockResolvedValueOnce({
				data: {
					...connection,
					writeBackMappingDefinitions: [
						{
							id: 5,
							valueSource: "FeatureSize",
							appliesTo: "Portfolio",
							targetFieldReference: "Custom.Size",
							targetValueType: "Date",
							dateFormat: null,
						},
					],
				},
			});

			await workTrackingSystemService.updateWorkTrackingSystemConnection(
				connection,
			);

			const sentPayload = mockedAxios.put.mock.calls[0][1] as {
				writeBackMappingDefinitions: Array<{
					valueSource: string;
					appliesTo: string;
					targetValueType: string;
					dateFormat: string | null;
				}>;
			};
			expect(sentPayload.writeBackMappingDefinitions[0].valueSource).toBe(
				"FeatureSize",
			);
			expect(sentPayload.writeBackMappingDefinitions[0].appliesTo).toBe(
				"Portfolio",
			);
			expect(sentPayload.writeBackMappingDefinitions[0].targetValueType).toBe(
				"Date",
			);
		});

		it("should serialize enums to strings for validate calls", async () => {
			const connection: IWorkTrackingSystemConnection = {
				id: 0,
				name: "ADO",
				workTrackingSystem: "AzureDevOps",
				options: [],
				authenticationMethodKey: "ado.pat",
				additionalFieldDefinitions: [],
				writeBackMappingDefinitions: [
					{
						id: -1,
						valueSource: WriteBackValueSource.WorkItemAgeCycleTime,
						appliesTo: WriteBackAppliesTo.Team,
						targetFieldReference: "Custom.CT",
						targetValueType: WriteBackTargetValueType.Date,
						dateFormat: null,
					},
				],
				workTrackingSystemGetDataRetrievalDisplayName: () => "WIQL",
			};

			mockedAxios.post.mockResolvedValueOnce({ data: true });

			await workTrackingSystemService.validateWorkTrackingSystemConnection(
				connection,
			);

			const sentPayload = mockedAxios.post.mock.calls[0][1] as {
				writeBackMappingDefinitions: Array<{
					valueSource: string;
					appliesTo: string;
					targetValueType: string;
					dateFormat: string | null;
				}>;
			};
			expect(sentPayload.writeBackMappingDefinitions[0].valueSource).toBe(
				"WorkItemAgeCycleTime",
			);
			expect(sentPayload.writeBackMappingDefinitions[0].appliesTo).toBe("Team");
		});

		it("should serialize enums to strings for add calls", async () => {
			const connection: IWorkTrackingSystemConnection = {
				id: 0,
				name: "ADO",
				workTrackingSystem: "AzureDevOps",
				options: [],
				authenticationMethodKey: "ado.pat",
				additionalFieldDefinitions: [],
				writeBackMappingDefinitions: [
					{
						id: -1,
						valueSource: WriteBackValueSource.WorkItemAgeCycleTime,
						appliesTo: WriteBackAppliesTo.Team,
						targetFieldReference: "Custom.Age",
						targetValueType: WriteBackTargetValueType.Date,
						dateFormat: null,
					},
				],
				workTrackingSystemGetDataRetrievalDisplayName: () => "WIQL",
			};

			mockedAxios.post.mockResolvedValueOnce({
				data: {
					...connection,
					id: 10,
					writeBackMappingDefinitions: [
						{
							id: 7,
							valueSource: "WorkItemAgeCycleTime",
							appliesTo: "Team",
							targetFieldReference: "Custom.Age",
							targetValueType: "Date",
							dateFormat: null,
						},
					],
				},
			});

			const created =
				await workTrackingSystemService.addNewWorkTrackingSystemConnection(
					connection,
				);

			// Verify outbound payload has string enums
			const sentPayload = mockedAxios.post.mock.calls[0][1] as {
				writeBackMappingDefinitions: Array<{
					valueSource: string;
					appliesTo: string;
					targetValueType: string;
					dateFormat: string | null;
				}>;
			};
			expect(sentPayload.writeBackMappingDefinitions[0].valueSource).toBe(
				"WorkItemAgeCycleTime",
			);
			expect(sentPayload.writeBackMappingDefinitions[0].appliesTo).toBe("Team");

			// Verify response was deserialized back to numeric enums
			expect(created.writeBackMappingDefinitions[0].valueSource).toBe(
				WriteBackValueSource.WorkItemAgeCycleTime,
			);
			expect(created.writeBackMappingDefinitions[0].appliesTo).toBe(
				WriteBackAppliesTo.Team,
			);
		});

		it("should pass through numeric enum values that are already numeric", async () => {
			const mockResponse = [
				{
					id: 1,
					name: "ADO",
					workTrackingSystem: "AzureDevOps",
					options: [],
					authenticationMethodKey: "ado.pat",
					additionalFieldDefinitions: [],
					writeBackMappingDefinitions: [
						{
							id: 1,
							valueSource: 1,
							appliesTo: 1,
							targetFieldReference: "Custom.Size",
							targetValueType: 0,
							dateFormat: null,
						},
					],
				},
			];

			mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

			const systems =
				await workTrackingSystemService.getConfiguredWorkTrackingSystems();

			const mapping = systems[0].writeBackMappingDefinitions[0];
			expect(mapping.valueSource).toBe(WriteBackValueSource.FeatureSize);
			expect(mapping.appliesTo).toBe(WriteBackAppliesTo.Portfolio);
			expect(mapping.targetValueType).toBe(WriteBackTargetValueType.Date);
		});
	});
});
