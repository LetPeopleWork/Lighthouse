import { Delivery, type IDelivery } from "../../models/Delivery";
import {
	DeliverySelectionMode,
	type IDeliveryRuleCondition,
	type IDeliveryRuleSchema,
} from "../../models/DeliveryRules";
import type { Feature } from "../../models/Feature";
import { BaseApiService } from "./BaseApiService";

export interface IDeliveryService {
	getByPortfolio(portfolioId: number): Promise<Delivery[]>;
	create(
		portfolioId: number,
		name: string,
		date: Date,
		featureIds: number[],
		selectionMode?: DeliverySelectionMode,
		rules?: IDeliveryRuleCondition[],
	): Promise<void>;
	update(
		deliveryId: number,
		name: string,
		date: Date,
		featureIds: number[],
		selectionMode?: DeliverySelectionMode,
		rules?: IDeliveryRuleCondition[],
	): Promise<void>;
	delete(deliveryId: number): Promise<void>;
	getRuleSchema(portfolioId: number): Promise<IDeliveryRuleSchema>;
	validateRules(
		portfolioId: number,
		rules: IDeliveryRuleCondition[],
	): Promise<Feature[]>;
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

	async create(
		portfolioId: number,
		name: string,
		date: Date,
		featureIds: number[],
		selectionMode: DeliverySelectionMode = DeliverySelectionMode.Manual,
		rules?: IDeliveryRuleCondition[],
	): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post<void>(`/deliveries/portfolio/${portfolioId}`, {
				name,
				date: date.toISOString(),
				featureIds,
				selectionMode,
				rules,
			});
		});
	}

	async update(
		deliveryId: number,
		name: string,
		date: Date,
		featureIds: number[],
		selectionMode: DeliverySelectionMode = DeliverySelectionMode.Manual,
		rules?: IDeliveryRuleCondition[],
	): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.put<void>(`/deliveries/${deliveryId}`, {
				name,
				date: date.toISOString(),
				featureIds,
				selectionMode,
				rules,
			});
		});
	}

	async delete(deliveryId: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete<void>(`/deliveries/${deliveryId}`);
		});
	}

	async getRuleSchema(portfolioId: number): Promise<IDeliveryRuleSchema> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IDeliveryRuleSchema>(
				`/portfolios/${portfolioId}/delivery-rules/schema`,
			);
			return response.data;
		});
	}

	async validateRules(
		portfolioId: number,
		rules: IDeliveryRuleCondition[],
	): Promise<Feature[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<Feature[]>(
				`/portfolios/${portfolioId}/delivery-rules/validate`,
				{
					portfolioId,
					rules,
				},
			);
			return response.data;
		});
	}
}
