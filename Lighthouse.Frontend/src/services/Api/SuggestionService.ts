import type { StatesCollection } from "../../models/StatesCollection";
import { BaseApiService } from "./BaseApiService";

export interface ISuggestionService {
	getTags(): Promise<string[]>;
	getWorkItemTypesForTeams(): Promise<string[]>;
	getWorkItemTypesForProjects(): Promise<string[]>;
	getStatesForTeams(): Promise<StatesCollection>;
	getStatesForProjects(): Promise<StatesCollection>;
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

	getStatesForTeams(): Promise<StatesCollection> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<StatesCollection>(
				"/suggestions/states/teams",
			);

			return response.data;
		});
	}

	getStatesForProjects(): Promise<StatesCollection> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<StatesCollection>(
				"/suggestions/states/projects",
			);

			return response.data;
		});
	}
}
