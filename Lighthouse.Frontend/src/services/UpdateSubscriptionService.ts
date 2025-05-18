import * as signalR from "@microsoft/signalr";

export type UpdateType = "Team" | "Features" | "Forecasts";

export type UpdateProgress = "Queued" | "InProgress" | "Completed" | "Failed";

export interface IUpdateStatus {
	updateType: UpdateType;
	id: number;
	status: UpdateProgress;
}

export interface IUpdateSubscriptionService {
	initialize(): Promise<void>;
	getUpdateStatus(
		updateType: UpdateType,
		id: number,
	): Promise<IUpdateStatus | null>;
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
	private readonly baseUrl: string = "/api";

	private connection!: signalR.HubConnection;
	private isConnected = false;
	private isConnecting = false;
	private connectionPromise: Promise<void> | null = null;

	public constructor() {
		if (import.meta.env.VITE_API_BASE_URL !== undefined) {
			this.baseUrl = import.meta.env.VITE_API_BASE_URL;
		}
	}

	public async initialize(): Promise<void> {
		if (this.isConnected) {
			return;
		}

		if (this.isConnecting) {
			if (this.connectionPromise) {
				return this.connectionPromise;
			}
			throw new Error("Connection promise is null");
		}

		this.isConnecting = true;

		this.connection ??= new signalR.HubConnectionBuilder()
			.withUrl(`${this.baseUrl}/updateNotificationHub`, {
				withCredentials: true,
			})
			.configureLogging(signalR.LogLevel.Information)
			.build();

		this.connectionPromise = this.connection
			.start()
			.then(() => {
				this.isConnected = true;
				this.isConnecting = false;
			})
			.catch((error) => {
				console.error("Error starting SignalR connection:", error);
				this.isConnecting = false;
				this.connectionPromise = null;
			});

		return this.connectionPromise;
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

	private async subscribeToUpdate(
		updateType: UpdateType,
		id: number,
		callback: (status: IUpdateStatus) => void,
	) {
		await this.initialize();

		const updateKey = `${updateType}_${id}`;

		try {
			this.connection.on(updateKey, callback);
			await this.connection.invoke("SubscribeToUpdate", updateType, id);
		} catch (err) {
			console.error("Error subscribing to update:", err);
		}
	}

	private async unsubscribeFromUpdate(updateType: UpdateType, id: number) {
		await this.initialize();

		const updateKey = `${updateType}_${id}`;

		try {
			this.connection.off(updateKey);
			await this.connection.invoke("UnsubscribeFromUpdate", updateType, id);
		} catch (err) {
			console.error("Error unsubscribing from update:", err);
		}
	}
}
