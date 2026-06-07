import { z } from "zod";
import { type ITerminology, TerminologySchema } from "../../models/Terminology";
import { BaseApiService } from "./BaseApiService";

export interface ITerminologyService {
	getAllTerminology(): Promise<ITerminology[]>;
	updateTerminology(terminology: ITerminology[]): Promise<void>;
}

export class TerminologyService
	extends BaseApiService
	implements ITerminologyService
{
	public async getAllTerminology(): Promise<ITerminology[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>("/terminology/all");
			return BaseApiService.parse(z.array(TerminologySchema), response.data);
		});
	}

	public async updateTerminology(terminology: ITerminology[]): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.put("/terminology", terminology);
		});
	}
}
