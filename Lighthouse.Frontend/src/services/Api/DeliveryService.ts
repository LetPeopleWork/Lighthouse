import { Delivery, type IDelivery } from "../../models/Delivery";
import { BaseApiService } from "./BaseApiService";

export interface IDeliveryService {
	getByPortfolio(portfolioId: number): Promise<Delivery[]>;
	getAll(): Promise<Delivery[]>;
	create(
		portfolioId: number,
		name: string,
		date: Date,
		featureIds: number[],
	): Promise<void>;
	update(
		deliveryId: number,
		name: string,
		date: Date,
		featureIds: number[],
	): Promise<void>;
	delete(deliveryId: number): Promise<void>;
}

export class DeliveryService
	extends BaseApiService
	implements IDeliveryService
{
	async getByPortfolio(portfolioId: number): Promise<Delivery[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IDelivery[]>(
				`/deliveries/portfolio/${portfolioId}`,
			);
			return response.data.map((data) => Delivery.fromBackend(data));
		});
	}

	async getAll(): Promise<Delivery[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IDelivery[]>(`/deliveries`);
			return response.data.map((data) => Delivery.fromBackend(data));
		});
	}

	async create(
		portfolioId: number,
		name: string,
		date: Date,
		featureIds: number[],
	): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post<void>(`/deliveries/portfolio/${portfolioId}`, {
				name,
				date: date.toISOString(),
				featureIds,
			});
		});
	}

	async update(
		deliveryId: number,
		name: string,
		date: Date,
		featureIds: number[],
	): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.put<void>(`/deliveries/${deliveryId}`, {
				name,
				date: date.toISOString(),
				featureIds,
			});
		});
	}

	async delete(deliveryId: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete<void>(`/deliveries/${deliveryId}`);
		});
	}
}
