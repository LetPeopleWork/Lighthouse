import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../models/WorkTracking/WorkTrackingSystemConnection";
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
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const workTrackingSystems =
			await workTrackingSystemService.getWorkTrackingSystems();

		expect(workTrackingSystems[0].authenticationMethodKey).toBe("jira.cloud");
	});
});
