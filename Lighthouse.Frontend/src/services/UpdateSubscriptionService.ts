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

/**
 * The update types that reach the task list. Wider than UpdateType, which names only the three a
 * detail page ever subscribes to - deletes go through the same queue and an operator watching the
 * instance sees them too.
 */
export type UpdateTaskType = UpdateType | "TeamDelete" | "PortfolioDelete";

/** One piece of work the instance has admitted: what it is, what it is called, and where it has got to. */
export interface IUpdateTask {
	updateType: UpdateTaskType;
	id: number;
	name: string;
	status: UpdateProgress;
	/** The entity holding the lane this one is waiting for. Absent for work that is running. */
	waitingBehind?: string | null;
	/**
	 * How long the work has been in the state `status` names, measured by the instance. Running counts
	 * from when it started, waiting from when it was admitted, so one number reads correctly either way.
	 *
	 * It arrives already computed because the browser's clock is not the instance's, and the two can be
	 * minutes apart; subtracting a server timestamp from a local `new Date()` would give two people
	 * looking at one instance different answers. Absent when the moment behind it was never recorded,
	 * which happens to anything admitted by a replica still on an older build mid-upgrade.
	 */
	elapsedMs?: number | null;
}

export interface IUpdateSubscriptionService {
	getUpdateStatus(
		updateType: UpdateType,
		id: number,
	): Promise<IUpdateStatus | null>;
	getGlobalUpdateStatus(): Promise<IGlobalUpdateStatus>;
	getRunningTasks(): Promise<IUpdateTask[]>;
	cancelTask(updateType: UpdateTaskType, id: number): Promise<void>;
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

const GLOBAL_UPDATES = "GlobalUpdateNotification";

export class UpdateSubscriptionService implements IUpdateSubscriptionService {
	private connection!: signalR.HubConnection;
	private isConnected = false;
	private connectionPromise: Promise<void> | null = null;
	private apiService: AxiosInstance;

	/**
	 * What this client has asked the server to send it. The server keeps subscriptions per connection, so
	 * a reconnect starts with none of them — and every caller here subscribed once, on mount, and will
	 * never ask again. Without this the page survives a reconnect holding handlers the server no longer
	 * sends anything to, which looks exactly like an instance with nothing to report.
	 */
	private readonly joinedGroups = new Set<string>();

	constructor() {
		this.apiService = axios.create({
			baseURL: `${getBackendUrl()}/latest`,
		});

		// Self-initialise once the backend URL is known
		this.connectionPromise = this.connect();
	}

	private async connect(): Promise<void> {
		await getBackendReadyPromise();

		const baseUrl = getBackendUrl();
		this.apiService = axios.create({ baseURL: `${baseUrl}/latest` });

		if (this.isConnected) return;

		try {
			if (!this.connection) {
				this.connection = new signalR.HubConnectionBuilder()
					.withUrl(`${baseUrl}/updateNotificationHub`, {
						withCredentials: true,
					})
					.withAutomaticReconnect()
					.configureLogging(signalR.LogLevel.Information)
					.build();

				// A backend restart is an ordinary event — it is what every Lighthouse update looks like
				// from here — and it must not leave this client believing it is still connected. Without
				// these two the flag stays true, every later call goes to a dead socket, and the failure
				// surfaces as a console error nobody reads while the page quietly stops updating.
				this.connection.onclose(() => {
					this.isConnected = false;
					this.connectionPromise = null;
				});

				this.connection.onreconnected(() => {
					this.isConnected = true;
					void this.rejoinGroups();
				});
			}

			await this.connection.start();
			this.isConnected = true;
			await this.rejoinGroups();
		} catch (error) {
			console.error("Error starting SignalR connection:", error);
			this.isConnected = false;
			// Cleared so the next caller tries again. Leaving the settled promise in place would make
			// ensureConnected return instantly for the life of the page after one failed connect.
			this.connectionPromise = null;
		}
	}

	private async rejoinGroups(): Promise<void> {
		for (const group of this.joinedGroups) {
			try {
				if (group === GLOBAL_UPDATES) {
					await this.connection.invoke("SubscribeToAllUpdates");
				} else {
					const separator = group.lastIndexOf("_");
					await this.connection.invoke(
						"SubscribeToUpdate",
						group.slice(0, separator),
						Number(group.slice(separator + 1)),
					);
				}
			} catch (err) {
				console.error("Error restoring a subscription after reconnect:", err);
			}
		}
	}

	private async ensureConnected(): Promise<void> {
		if (!this.connectionPromise && !this.isConnected) {
			this.connectionPromise = this.connect();
		}

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

	public async getRunningTasks(): Promise<IUpdateTask[]> {
		await this.ensureConnected();

		const response = await this.apiService.get<IUpdateTask[]>("/update/tasks");
		return response.data;
	}

	/**
	 * Asks the instance to stop a refresh. Accepted whatever state the work is in, including gone - the row
	 * was drawn before it was clicked, so "it finished while you were reading" is ordinary rather than an
	 * error worth putting in front of somebody.
	 */
	public async cancelTask(
		updateType: UpdateTaskType,
		id: number,
	): Promise<void> {
		await this.ensureConnected();

		await this.apiService.post(`/update/tasks/${updateType}/${id}/cancel`);
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
			this.connection.on(GLOBAL_UPDATES, callback);
			this.joinedGroups.add(GLOBAL_UPDATES);
			await this.connection.invoke("SubscribeToAllUpdates");
		} catch (err) {
			console.error("Error subscribing to all updates:", err);
		}
	}

	public async unsubscribeFromAllUpdates(): Promise<void> {
		await this.ensureConnected();

		try {
			this.connection.off(GLOBAL_UPDATES);
			this.joinedGroups.delete(GLOBAL_UPDATES);
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
			this.joinedGroups.add(updateKey);
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
			this.joinedGroups.delete(updateKey);
			await this.connection.invoke("UnsubscribeFromUpdate", updateType, id);
		} catch (err) {
			console.error("Error unsubscribing from update:", err);
		}
	}
}
