import * as signalR from "@microsoft/signalr";
import axios, { type AxiosInstance } from "axios";
import { getBackendReadyPromise, getBackendUrl } from "../utils/backendUrl";

export type UpdateType = "Team" | "Features" | "Forecasts";

export type UpdateProgress = "Queued" | "InProgress" | "Completed" | "Failed";

export interface IUpdateStatus {
	updateType: UpdateType;
	id: number;
	status: UpdateProgress;
}

export interface IGlobalUpdateStatus {
	hasActiveUpdates: boolean;
	activeCount: number;
}

export interface IUpdateSubscriptionService {
	getUpdateStatus(
		updateType: UpdateType,
		id: number,
	): Promise<IUpdateStatus | null>;
	getGlobalUpdateStatus(): Promise<IGlobalUpdateStatus>;
	subscribeToAllUpdates(callback: () => void): Promise<void>;
	unsubscribeFromAllUpdates(): Promise<void>;
	subscribeToTeamUpdates(
		teamId: number,
		callback: (status: IUpdateStatus) => void,
	): Promise<void>;
	unsubscribeFromTeamUpdates(teamId: number): Promise<void>;
	subscribeToFeatureUpdates(
		projectId: number,
		callback: (status: IUpdateStatus) => void,
	): Promise<void>;
	unsubscribeFromFeatureUpdates(projectId: number): Promise<void>;
	subscribeToForecastUpdates(
		projectId: number,
		callback: (status: IUpdateStatus) => void,
	): Promise<void>;
	unsubscribeFromForecastUpdates(projectId: number): Promise<void>;
}

export class UpdateSubscriptionService implements IUpdateSubscriptionService {
	private connection!: signalR.HubConnection;
	private isConnected = false;
	private connectionPromise: Promise<void> | null = null;
	private apiService: AxiosInstance;

	constructor() {
		this.apiService = axios.create({ baseURL: getBackendUrl() });

		// Self-initialise once the backend URL is known
		this.connectionPromise = this.connect();
	}

	private async connect(): Promise<void> {
		await getBackendReadyPromise();

		const baseUrl = getBackendUrl();
		this.apiService = axios.create({ baseURL: baseUrl });

		if (this.isConnected) return;

		try {
			this.connection ??= new signalR.HubConnectionBuilder()
				.withUrl(`${baseUrl}/updateNotificationHub`, {
					withCredentials: true,
				})
				.configureLogging(signalR.LogLevel.Information)
				.build();

			await this.connection.start();
			this.isConnected = true;
		} catch (error) {
			console.error("Error starting SignalR connection:", error);
			this.connectionPromise = null;
		}
	}

	private async ensureConnected(): Promise<void> {
		if (this.connectionPromise) {
			await this.connectionPromise;
		}
	}

	public async subscribeToTeamUpdates(
		teamId: number,
		callback: (status: IUpdateStatus) => void,
	): Promise<void> {
		await this.subscribeToUpdate("Team", teamId, callback);
	}

	public async unsubscribeFromTeamUpdates(teamId: number): Promise<void> {
		await this.unsubscribeFromUpdate("Team", teamId);
	}

	public async subscribeToFeatureUpdates(
		projectId: number,
		callback: (status: IUpdateStatus) => void,
	): Promise<void> {
		await this.subscribeToUpdate("Features", projectId, callback);
	}

	public async unsubscribeFromFeatureUpdates(projectId: number): Promise<void> {
		await this.unsubscribeFromUpdate("Features", projectId);
	}

	public async subscribeToForecastUpdates(
		projectId: number,
		callback: (status: IUpdateStatus) => void,
	): Promise<void> {
		await this.subscribeToUpdate("Forecasts", projectId, callback);
	}

	public async unsubscribeFromForecastUpdates(
		projectId: number,
	): Promise<void> {
		await this.unsubscribeFromUpdate("Forecasts", projectId);
	}

	public async getUpdateStatus(
		updateType: UpdateType,
		id: number,
	): Promise<IUpdateStatus | null> {
		await this.ensureConnected();

		try {
			const updateStatus = await this.connection.invoke<IUpdateStatus>(
				"GetUpdateStatus",
				updateType,
				id,
			);
			return updateStatus;
		} catch (err) {
			console.error("Error getting update status:", err);
			return null;
		}
	}

	public async getGlobalUpdateStatus(): Promise<IGlobalUpdateStatus> {
		await this.ensureConnected();

		try {
			const response =
				await this.apiService.get<IGlobalUpdateStatus>("/update/status");
			return response.data;
		} catch (err) {
			console.error("Error getting global update status:", err);
			return { hasActiveUpdates: false, activeCount: 0 };
		}
	}

	public async subscribeToAllUpdates(callback: () => void): Promise<void> {
		await this.ensureConnected();

		try {
			this.connection.on("GlobalUpdateNotification", callback);
			await this.connection.invoke("SubscribeToAllUpdates");
		} catch (err) {
			console.error("Error subscribing to all updates:", err);
		}
	}

	public async unsubscribeFromAllUpdates(): Promise<void> {
		await this.ensureConnected();

		try {
			this.connection.off("GlobalUpdateNotification");
			await this.connection.invoke("UnsubscribeFromAllUpdates");
		} catch (err) {
			console.error("Error unsubscribing from all updates:", err);
		}
	}

	private async subscribeToUpdate(
		updateType: UpdateType,
		id: number,
		callback: (status: IUpdateStatus) => void,
	) {
		await this.ensureConnected();

		const updateKey = `${updateType}_${id}`;

		try {
			this.connection.on(updateKey, callback);
			await this.connection.invoke("SubscribeToUpdate", updateType, id);
		} catch (err) {
			console.error("Error subscribing to update:", err);
		}
	}

	private async unsubscribeFromUpdate(updateType: UpdateType, id: number) {
		await this.ensureConnected();

		const updateKey = `${updateType}_${id}`;

		try {
			this.connection.off(updateKey);
			await this.connection.invoke("UnsubscribeFromUpdate", updateType, id);
		} catch (err) {
			console.error("Error unsubscribing from update:", err);
		}
	}
}
