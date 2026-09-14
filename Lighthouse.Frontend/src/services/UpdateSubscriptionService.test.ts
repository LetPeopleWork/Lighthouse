import * as signalR from "@microsoft/signalr";
import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	type IUpdateStatus,
	UpdateSubscriptionService,
} from "./UpdateSubscriptionService";

vi.mock("@microsoft/signalr");
vi.mock("axios");
vi.mock("../utils/backendUrl", () => ({
	getBackendReadyPromise: () => Promise.resolve(),
	getBackendUrl: () => "/api",
}));
const mockedAxios = vi.mocked(axios, true);

/** Helper: create and wait for the auto-connecting service */
const createService = async (): Promise<UpdateSubscriptionService> => {
	const svc = new UpdateSubscriptionService();
	// Flush the internal connect() micro-task chain
	await Promise.resolve();
	await Promise.resolve();
	return svc;
};

describe("UpdateSubscriptionService", () => {
	let service: UpdateSubscriptionService;
	let mockConnection: signalR.HubConnection;

	beforeEach(async () => {
		mockConnection = {
			start: vi.fn().mockResolvedValue(undefined),
			on: vi.fn(),
			off: vi.fn(),
			invoke: vi.fn(),
			stop: vi.fn(),
		} as unknown as signalR.HubConnection;
		const withUrlMock = vi.fn().mockReturnValue({
			configureLogging: vi.fn().mockReturnValue({
				build: vi.fn().mockReturnValue(mockConnection),
			}),
		});
		signalR.HubConnectionBuilder.prototype.withUrl = withUrlMock;

		mockedAxios.create.mockReturnThis();

		service = await createService();
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it("should initialize the connection", async () => {
		expect(mockConnection.start).toHaveBeenCalled();
	});

	it("should subscribe to team updates", async () => {
		const callback = vi.fn();

		await service.subscribeToTeamUpdates(1, callback);
		expect(mockConnection.on).toHaveBeenCalledWith("Team_1", callback);
		expect(mockConnection.invoke).toHaveBeenCalledWith(
			"SubscribeToUpdate",
			"Team",
			1,
		);
	});

	it("should unsubscribe from team updates", async () => {
		await service.unsubscribeFromTeamUpdates(1);

		expect(mockConnection.off).toHaveBeenCalledWith("Team_1");
		expect(mockConnection.invoke).toHaveBeenCalledWith(
			"UnsubscribeFromUpdate",
			"Team",
			1,
		);
	});

	it("should get update status", async () => {
		const mockStatus: IUpdateStatus = {
			updateType: "Team",
			id: 1,
			status: "Completed",
		};
		(mockConnection.invoke as import("@vitest/spy").Mock).mockResolvedValue(
			mockStatus,
		);

		const status = await service.getUpdateStatus("Team", 1);
		expect(status).toEqual(mockStatus);
		expect(mockConnection.invoke).toHaveBeenCalledWith(
			"GetUpdateStatus",
			"Team",
			1,
		);
	});

	it("should handle errors when getting update status", async () => {
		(mockConnection.invoke as import("@vitest/spy").Mock).mockRejectedValue(
			new Error("Test error"),
		);

		const status = await service.getUpdateStatus("Team", 1);
		expect(status).toBeNull();
		expect(mockConnection.invoke).toHaveBeenCalledWith(
			"GetUpdateStatus",
			"Team",
			1,
		);
	});

	it("should get global update status successfully", async () => {
		const mockResponse = {
			data: { hasActiveUpdates: true, activeCount: 2 },
		};
		mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

		const result = await service.getGlobalUpdateStatus();

		expect(result).toEqual({
			data: { hasActiveUpdates: true, activeCount: 2 },
		});
		expect(mockedAxios.get).toHaveBeenCalledWith("/update/status");
	});

	// Epic #5511 slice 02. The route is the whole contract here; nothing else in the browser knows
	// where the task list lives.
	it("asks the instance for its task list and hands back what it says", async () => {
		const tasks = [
			{
				updateType: "Team",
				id: 7,
				name: "Lagunitas",
				status: "InProgress",
			},
		];
		mockedAxios.get.mockResolvedValueOnce({ data: tasks });

		const result = await service.getRunningTasks();

		expect(mockedAxios.get).toHaveBeenCalledWith("/update/tasks");
		expect(result).toEqual(tasks);
	});

	// Deliberately unlike getGlobalUpdateStatus above, which swallows and reports an idle instance. An
	// instance that cannot say what it is doing is not an instance doing nothing, and the popover has to
	// be able to tell those apart.
	it("lets a failed read surface rather than reporting an idle instance", async () => {
		mockedAxios.get.mockRejectedValueOnce(new Error("API error"));

		await expect(service.getRunningTasks()).rejects.toThrow("API error");
	});

	it("should handle errors when getting global update status", async () => {
		mockedAxios.get.mockRejectedValue(new Error("API error"));

		const result = await service.getGlobalUpdateStatus();

		expect(result).toEqual({ hasActiveUpdates: false, activeCount: 0 });
		expect(mockedAxios.get).toHaveBeenCalledWith("/update/status");
	});
});
