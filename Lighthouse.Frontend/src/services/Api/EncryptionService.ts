import {
	type EncryptionKeyState,
	EncryptionKeyStateSchema,
} from "../../models/Encryption/EncryptionKeyState";
import { BaseApiService } from "./BaseApiService";

export interface IEncryptionService {
	getKeyState(): Promise<EncryptionKeyState>;
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
}
