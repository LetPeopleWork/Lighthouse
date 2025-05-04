import { BaseApiService } from "./BaseApiService";

export interface ISuggestionService {
	getTags(): Promise<string[]>;
	getWorkItemTypesForTeams(): Promise<string[]>;
	getWorkItemTypesForProjects(): Promise<string[]>;
}

export class SuggestionService
	extends BaseApiService
	implements ISuggestionService
{
	getTags(): Promise<string[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<string[]>("/suggestions/tags");

			return response.data;
		});
	}

	getWorkItemTypesForTeams(): Promise<string[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<string[]>(
				"/suggestions/workitemtypes/teams",
			);

			return response.data;
		});
	}

	getWorkItemTypesForProjects(): Promise<string[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<string[]>(
				"/suggestions/workitemtypes/projects",
			);

			return response.data;
		});
	}
}
