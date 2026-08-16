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
	checkSecrets(): Promise<SecretReadabilityReport>;
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

	// A read, and it asks like one. What tells an operator this changes nothing is that it is the same
	// kind of request as opening the page, not a promise made in the button's label.
	async checkSecrets(): Promise<SecretReadabilityReport> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(
				"/encryption/secrets",
			);
			return BaseApiService.parse(SecretReadabilityReportSchema, response.data);
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
