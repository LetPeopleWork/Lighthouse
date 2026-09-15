import { BaseApiService } from "./BaseApiService";

/**
 * What Lighthouse is willing to say about one connection's credential. `Unknown` is the state of a
 * connection nothing has been observed about, and it is deliberately not rendered as healthy:
 * claiming a credential works because nothing has disproved it is how the icon this replaced came to
 * mislead.
 */
export type ConnectionHealthState =
	| "Unknown"
	| "Healthy"
	| "Unreachable"
	| "AuthenticationFailed";

export interface IConnectionHealth {
	connectionId: number;
	connectionName: string;
	workTrackingSystem: string;
	state: ConnectionHealthState;
	/** The sentence an administrator reads. Written by the connector; absent while the state is `Unknown`. */
	message?: string | null;
	/** When the state was observed. Absent while the state is `Unknown`. */
	observedAt?: string | null;
}

export interface IConnectionHealthService {
	getHealth(): Promise<IConnectionHealth[]>;
	testConnection(connectionId: number): Promise<IConnectionHealth>;
}

export class ConnectionHealthService
	extends BaseApiService
	implements IConnectionHealthService
{
	async getHealth(): Promise<IConnectionHealth[]> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<IConnectionHealth[]>("/connectionhealth");

			return response.data;
		});
	}

	async testConnection(connectionId: number): Promise<IConnectionHealth> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IConnectionHealth>(
				`/connectionhealth/${connectionId}/test`,
			);

			return response.data;
		});
	}
}
