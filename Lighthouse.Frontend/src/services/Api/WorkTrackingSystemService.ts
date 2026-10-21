import { z } from "zod";
import {
	type IConnectionValidationPayload,
	type IConnectionValidationResult,
	readConnectionValidation,
} from "../../models/WorkTracking/ConnectionValidationResult";
import {
	ConnectionDefaultNames,
	type EstimationDefaultName,
	type FieldDefaultName,
	type IWorkTrackingSystemConnection,
	isConnectionDefaultName,
	isEstimationDefaultName,
	WorkTrackingSystemConnection,
} from "../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	type IWriteBackMappingDefinition,
	WriteBackAppliesTo,
	WriteBackTargetValueType,
	WriteBackValueSource,
} from "../../models/WorkTracking/WriteBackMappingDefinition";
import { BaseApiService } from "./BaseApiService";

const FieldDefaultSchema = z.number().nullable().optional();

const EstimationDefaultSchema = z
	.object({
		additionalFieldDefinitionId: z
			.number()
			.nullish()
			.transform((id) => id ?? null),
		unit: z
			.string()
			.nullish()
			.transform((unit) => unit ?? null),
		useNonNumericEstimation: z
			.boolean()
			.nullish()
			.transform((nonNumeric) => nonNumeric ?? false),
		estimationCategoryValues: z
			.array(z.string())
			.nullish()
			.transform((values) => values ?? []),
	})
	.nullable()
	.optional();

const ConnectionDefaultsSchema = z.object({
	defaults: z
		.object(
			Object.fromEntries(
				ConnectionDefaultNames.map((name) => [
					name,
					isEstimationDefaultName(name)
						? EstimationDefaultSchema
						: FieldDefaultSchema,
				]),
			) as Record<FieldDefaultName, typeof FieldDefaultSchema> &
				Record<EstimationDefaultName, typeof EstimationDefaultSchema>,
		)
		.nullable()
		.optional(),
	supportedDefaults: z.array(z.string()).nullable().optional(),
});

/**
 * The answer to the question asked before a save that changes what Teams and Portfolios fetch: update
 * them now, or leave them to their next scheduled update. A save that asked no question passes no
 * choice at all, which is how the two are told apart. additionalFieldsChanged is false when only a
 * followed default changed.
 */
export interface ISaveConnectionChoice {
	startUpdates: boolean;
	additionalFieldsChanged: boolean;
}

export interface IWorkTrackingSystemService {
	getWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]>;
	getConfiguredWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]>;
	addNewWorkTrackingSystemConnection(
		newWorkTrackingSystemConnection: IWorkTrackingSystemConnection,
	): Promise<IWorkTrackingSystemConnection>;
	updateWorkTrackingSystemConnection(
		modifiedConnection: IWorkTrackingSystemConnection,
		choice?: ISaveConnectionChoice,
	): Promise<IWorkTrackingSystemConnection>;
	deleteWorkTrackingSystemConnection(connectionId: number): Promise<void>;
	validateWorkTrackingSystemConnection(
		workTrackingConnection: IWorkTrackingSystemConnection,
	): Promise<IConnectionValidationResult>;
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

			return BaseApiService.asArray(
				response.data,
				"/worktrackingsystemconnections/supported",
			).map((connection) =>
				this.deserializeWorkTrackingSystemConnection(connection),
			);
		});
	}

	validateWorkTrackingSystemConnection(
		workTrackingConnection: IWorkTrackingSystemConnection,
	): Promise<IConnectionValidationResult> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<
				boolean | IConnectionValidationPayload
			>(
				"/worktrackingsystemconnections/validate",
				this.serializeConnectionForApi(workTrackingConnection),
			);

			return readConnectionValidation(response.data);
		});
	}

	async getConfiguredWorkTrackingSystems(): Promise<
		IWorkTrackingSystemConnection[]
	> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.get<
				IWorkTrackingSystemConnection[]
			>("/worktrackingsystemconnections");

			return BaseApiService.asArray(
				response.data,
				"/worktrackingsystemconnections",
			).map((connection) =>
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
		choice?: ISaveConnectionChoice,
	): Promise<IWorkTrackingSystemConnection> {
		const holdUpdatesBack =
			choice?.startUpdates === false ? "?startUpdates=false" : "";
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.put<IWorkTrackingSystemConnection>(
				`/worktrackingsystemconnections/${modifiedConnection.id}${holdUpdatesBack}`,
				this.serializeConnectionForApi(modifiedConnection),
			);

			return this.deserializeWorkTrackingSystemConnection(response.data);
		});
	}

	deleteWorkTrackingSystemConnection(connectionId: number): Promise<void> {
		this.withErrorHandling(async () => {
			await this.apiService.delete(
				`/worktrackingsystemconnections/${connectionId}`,
			);
		}).catch((error: unknown) => {
			console.error("Error deleting the connection:", error);
		});
		return Promise.resolve();
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
			requiresReconnect: workTrackingSystemConnection.requiresReconnect,
			concurrencyToken: workTrackingSystemConnection.concurrencyToken,
			usedBy: workTrackingSystemConnection.usedBy,
			...this.deserializeDefaults(workTrackingSystemConnection),
		});
	}

	private deserializeDefaults(connection: IWorkTrackingSystemConnection) {
		const { defaults, supportedDefaults } = BaseApiService.parse(
			ConnectionDefaultsSchema,
			connection,
		);
		// A server newer than this page may offer a default the page has no row for yet.
		return {
			defaults: defaults ?? undefined,
			supportedDefaults: supportedDefaults?.filter(isConnectionDefaultName),
		};
	}

	private deserializeWriteBackMapping(
		raw: Record<string, unknown>,
	): IWriteBackMappingDefinition {
		const valueSource = raw.valueSource;
		const appliesTo = raw.appliesTo;
		const targetValueType = raw.targetValueType;

		return {
			id: raw.id as number,
			additionalFieldDefinitionId:
				(raw.additionalFieldDefinitionId as number | null) ?? null,
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
				valueSource: this.nameOfValueSource(m.valueSource),
				appliesTo: WriteBackAppliesTo[m.appliesTo],
				targetValueType: WriteBackTargetValueType[m.targetValueType],
			})),
		};
	}

	/**
	 * The wire carries the member name, so a source this build does not know has no name to send. That
	 * happens when the page was loaded from a bundle older than the server - a long-lived tab across an
	 * upgrade is enough.
	 *
	 * Refusing to save is the point. The name would otherwise be sent as undefined, which JSON drops,
	 * which the server reads as the first member of the enum: a field set up to receive a forecasted
	 * start date would silently begin receiving work item age, from an edit the admin made to something
	 * else entirely on the same screen.
	 */
	private nameOfValueSource(valueSource: WriteBackValueSource): string {
		const name = WriteBackValueSource[valueSource];

		if (name === undefined) {
			throw new Error(
				"This page cannot save a sync mapping it does not recognise. Reload the page to pick up the current version, then try again.",
			);
		}

		return name;
	}
}
