import {
	type IWorkTrackingSystemConnection,
	WorkTrackingSystemConnection,
} from "../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	type IWriteBackMappingDefinition,
	WriteBackAppliesTo,
	WriteBackTargetValueType,
	WriteBackValueSource,
} from "../../models/WorkTracking/WriteBackMappingDefinition";
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
				this.serializeConnectionForApi(workTrackingConnection),
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
					this.serializeConnectionForApi(newWorkTrackingSystemConnection),
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
				this.serializeConnectionForApi(modifiedConnection),
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
			additionalFieldDefinitions:
				workTrackingSystemConnection.additionalFieldDefinitions ?? [],
			writeBackMappingDefinitions: (
				workTrackingSystemConnection.writeBackMappingDefinitions ?? []
			).map((m) =>
				this.deserializeWriteBackMapping(
					m as unknown as Record<string, unknown>,
				),
			),
		});
	}

	private deserializeWriteBackMapping(
		raw: Record<string, unknown>,
	): IWriteBackMappingDefinition {
		const valueSource = raw.valueSource;
		const appliesTo = raw.appliesTo;
		const targetValueType = raw.targetValueType;

		return {
			id: raw.id as number,
			targetFieldReference: raw.targetFieldReference as string,
			dateFormat: (raw.dateFormat as string | null) ?? null,
			valueSource:
				typeof valueSource === "string"
					? (WriteBackValueSource[
							valueSource as keyof typeof WriteBackValueSource
						] as unknown as WriteBackValueSource)
					: (valueSource as WriteBackValueSource),
			appliesTo:
				typeof appliesTo === "string"
					? (WriteBackAppliesTo[
							appliesTo as keyof typeof WriteBackAppliesTo
						] as unknown as WriteBackAppliesTo)
					: (appliesTo as WriteBackAppliesTo),
			targetValueType:
				typeof targetValueType === "string"
					? (WriteBackTargetValueType[
							targetValueType as keyof typeof WriteBackTargetValueType
						] as unknown as WriteBackTargetValueType)
					: (targetValueType as WriteBackTargetValueType),
		};
	}

	private serializeConnectionForApi(connection: IWorkTrackingSystemConnection) {
		return {
			...connection,
			writeBackMappingDefinitions: (
				connection.writeBackMappingDefinitions ?? []
			).map((m) => ({
				...m,
				valueSource: WriteBackValueSource[m.valueSource],
				appliesTo: WriteBackAppliesTo[m.appliesTo],
				targetValueType: WriteBackTargetValueType[m.targetValueType],
			})),
		};
	}
}
