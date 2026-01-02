import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../models/WorkTracking/WorkTrackingSystemConnection";
import { BaseApiService } from "./BaseApiService";

export interface IWorkTrackingSystemService {
	getWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]>;
	getConfiguredWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]>;
	addNewWorkTrackingSystemConnection(
		newWorkTrackingSystemConnection: IWorkTrackingSystemConnection,
	): Promise<IWorkTrackingSystemConnection>;
	updateWorkTrackingSystemConnection(
		modifiedConnection: IWorkTrackingSystemConnection,
	): Promise<IWorkTrackingSystemConnection>;
	deleteWorkTrackingSystemConnection(connectionId: number): Promise<void>;
	validateWorkTrackingSystemConnection(
		workTrackingConnection: IWorkTrackingSystemConnection,
	): Promise<boolean>;
}

export class WorkTrackingSystemService
	extends BaseApiService
	implements IWorkTrackingSystemService
{
	async getWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.get<
				IWorkTrackingSystemConnection[]
			>("/worktrackingsystemconnections/supported");

			return response.data.map((connection) =>
				this.deserializeWorkTrackingSystemConnection(connection),
			);
		});
	}

	async validateWorkTrackingSystemConnection(
		workTrackingConnection: IWorkTrackingSystemConnection,
	): Promise<boolean> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<boolean>(
				"/worktrackingsystemconnections/validate",
				workTrackingConnection,
			);
			return response.data;
		});
	}

	async getConfiguredWorkTrackingSystems(): Promise<
		IWorkTrackingSystemConnection[]
	> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.get<
				IWorkTrackingSystemConnection[]
			>("/worktrackingsystemconnections");

			return response.data.map((connection) =>
				this.deserializeWorkTrackingSystemConnection(connection),
			);
		});
	}

	async addNewWorkTrackingSystemConnection(
		newWorkTrackingSystemConnection: IWorkTrackingSystemConnection,
	): Promise<IWorkTrackingSystemConnection> {
		return await this.withErrorHandling(async () => {
			const response =
				await this.apiService.post<IWorkTrackingSystemConnection>(
					"/worktrackingsystemconnections",
					newWorkTrackingSystemConnection,
				);

			return this.deserializeWorkTrackingSystemConnection(response.data);
		});
	}

	async updateWorkTrackingSystemConnection(
		modifiedConnection: IWorkTrackingSystemConnection,
	): Promise<IWorkTrackingSystemConnection> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.put<IWorkTrackingSystemConnection>(
				`/worktrackingsystemconnections/${modifiedConnection.id}`,
				modifiedConnection,
			);

			return this.deserializeWorkTrackingSystemConnection(response.data);
		});
	}

	async deleteWorkTrackingSystemConnection(
		connectionId: number,
	): Promise<void> {
		this.withErrorHandling(async () => {
			await this.apiService.delete(
				`/worktrackingsystemconnections/${connectionId}`,
			);
		});
	}

	private deserializeWorkTrackingSystemConnection(
		workTrackingSystemConnection: IWorkTrackingSystemConnection,
	) {
		return new WorkTrackingSystemConnection({
			name: workTrackingSystemConnection.name,
			workTrackingSystem: workTrackingSystemConnection.workTrackingSystem,
			options: workTrackingSystemConnection.options,
			id: workTrackingSystemConnection.id,
			authenticationMethodKey:
				workTrackingSystemConnection.authenticationMethodKey,
			authenticationMethodDisplayName:
				workTrackingSystemConnection.authenticationMethodDisplayName,
			availableAuthenticationMethods:
				workTrackingSystemConnection.availableAuthenticationMethods,
		});
	}
}
