import {
	type EncryptionKeyState,
	EncryptionKeyStateSchema,
} from "../../models/Encryption/EncryptionKeyState";
import {
	type SecretReadabilityReport,
	SecretReadabilityReportSchema,
} from "../../models/Encryption/SecretReadabilityReport";
import { BaseApiService } from "./BaseApiService";

export interface IEncryptionService {
	getKeyState(): Promise<EncryptionKeyState>;
	rotateKey(): Promise<SecretReadabilityReport>;
	reEncryptSecrets(): Promise<SecretReadabilityReport>;
}

export class EncryptionService
	extends BaseApiService
	implements IEncryptionService
{
	async getKeyState(): Promise<EncryptionKeyState> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>("/encryption");
			return BaseApiService.parse(EncryptionKeyStateSchema, response.data);
		});
	}

	// Two calls rather than one with a flag, because the two have different preconditions and different
	// outcomes: an operator who pressed one and got the other would reasonably believe an exposure had
	// been contained when it had not.
	async rotateKey(): Promise<SecretReadabilityReport> {
		return await this.report("/encryption/rotate");
	}

	async reEncryptSecrets(): Promise<SecretReadabilityReport> {
		return await this.report("/encryption/reencrypt");
	}

	private async report(route: string): Promise<SecretReadabilityReport> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.post<unknown>(route);
			return BaseApiService.parse(SecretReadabilityReportSchema, response.data);
		});
	}
}
