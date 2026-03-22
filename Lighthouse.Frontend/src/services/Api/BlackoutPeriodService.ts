import type { IBlackoutPeriod } from "../../models/BlackoutPeriod";
import { BaseApiService } from "./BaseApiService";

export interface IBlackoutPeriodService {
	getAll(): Promise<IBlackoutPeriod[]>;
	create(period: Omit<IBlackoutPeriod, "id">): Promise<IBlackoutPeriod>;
	update(
		id: number,
		period: Omit<IBlackoutPeriod, "id">,
	): Promise<IBlackoutPeriod>;
	delete(id: number): Promise<void>;
}

export class BlackoutPeriodService
	extends BaseApiService
	implements IBlackoutPeriodService
{
	public async getAll(): Promise<IBlackoutPeriod[]> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<IBlackoutPeriod[]>("/blackout-periods");
			return response.data;
		});
	}

	public async create(
		period: Omit<IBlackoutPeriod, "id">,
	): Promise<IBlackoutPeriod> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IBlackoutPeriod>(
				"/blackout-periods",
				period,
			);
			return response.data;
		});
	}

	public async update(
		id: number,
		period: Omit<IBlackoutPeriod, "id">,
	): Promise<IBlackoutPeriod> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.put<IBlackoutPeriod>(
				`/blackout-periods/${id}`,
				period,
			);
			return response.data;
		});
	}

	public async delete(id: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete(`/blackout-periods/${id}`);
		});
	}
}
