import type {
	IDemoDataScenario,
	IDemoDataService,
} from "../../models/DemoData/IDemoData";
import { BaseApiService } from "./BaseApiService";

export class DemoDataService
	extends BaseApiService
	implements IDemoDataService
{
	async getAvailableScenarios(): Promise<IDemoDataScenario[]> {
		return await this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<IDemoDataScenario[]>("/demo/scenarios");
			return response.data;
		});
	}

	async loadScenario(scenarioId: string): Promise<void> {
		return await this.withErrorHandling(async () => {
			await this.apiService.post(`/demo/scenarios/${scenarioId}/load`);
		});
	}
}
