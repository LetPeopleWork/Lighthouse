import type { ITerminology } from "../../models/Terminology";
import { BaseApiService } from "./BaseApiService";

export interface ITerminologyService {
	getTerminology(): Promise<ITerminology>;
}

export class TerminologyService
	extends BaseApiService
	implements ITerminologyService
{
	public async getTerminology(): Promise<ITerminology> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ITerminology>("/terminology");
			return response.data;
		});
	}
}
