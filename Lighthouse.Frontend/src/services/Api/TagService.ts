import { BaseApiService } from "./BaseApiService";

export interface ITagService {
	getTags(): Promise<string[]>;
}

export class TagService extends BaseApiService implements ITagService {
	getTags(): Promise<string[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<string[]>("/tags");

			return response.data;
		});
	}
}
