import { Delivery, type IDelivery } from "../../models/Delivery";
import {
	type DeliveryMetricsHistory,
	parseDeliveryMetricsHistory,
} from "../../models/Delivery/DeliveryMetricsHistory";
import type { Feature } from "../../models/Feature";
import {
	DeliverySelectionMode,
	type IWorkItemRuleCondition,
	type IWorkItemRuleSchema,
} from "../../models/WorkItemRules";
import { BaseApiService } from "./BaseApiService";

export interface IDeliveryUpdateOptions {
	deliveryId: number;
	name: string;
	date: Date;
	featureIds: number[];
	selectionMode?: DeliverySelectionMode;
	rules?: IWorkItemRuleCondition[];
	mode?: "and" | "or";
	concurrencyToken?: string;
}

export interface IDeliveryService {
	getByPortfolio(portfolioId: number): Promise<Delivery[]>;
	create(
		portfolioId: number,
		name: string,
		date: Date,
		featureIds: number[],
		selectionMode?: DeliverySelectionMode,
		rules?: IWorkItemRuleCondition[],
		mode?: "and" | "or",
	): Promise<void>;
	update(options: IDeliveryUpdateOptions): Promise<void>;
	delete(deliveryId: number): Promise<void>;
	getRuleSchema(portfolioId: number): Promise<IWorkItemRuleSchema>;
	validateRules(
		portfolioId: number,
		rules: IWorkItemRuleCondition[],
		mode?: "and" | "or",
	): Promise<Feature[]>;
	getMetricsHistory(deliveryId: number): Promise<DeliveryMetricsHistory>;
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
		rules?: IWorkItemRuleCondition[],
		mode?: "and" | "or",
	): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post<void>(`/deliveries/portfolio/${portfolioId}`, {
				name,
				date: date.toISOString(),
				featureIds,
				selectionMode,
				rules,
				mode,
			});
		});
	}

	async update({
		deliveryId,
		name,
		date,
		featureIds,
		selectionMode = DeliverySelectionMode.Manual,
		rules,
		mode,
		concurrencyToken,
	}: IDeliveryUpdateOptions): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.put<void>(`/deliveries/${deliveryId}`, {
				name,
				date: date.toISOString(),
				featureIds,
				selectionMode,
				rules,
				mode,
				concurrencyToken,
			});
		});
	}

	async delete(deliveryId: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete<void>(`/deliveries/${deliveryId}`);
		});
	}

	async getRuleSchema(portfolioId: number): Promise<IWorkItemRuleSchema> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IWorkItemRuleSchema>(
				`/portfolios/${portfolioId}/delivery-rules/schema`,
			);
			return response.data;
		});
	}

	async validateRules(
		portfolioId: number,
		rules: IWorkItemRuleCondition[],
		mode?: "and" | "or",
	): Promise<Feature[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<Feature[]>(
				`/portfolios/${portfolioId}/delivery-rules/validate`,
				{
					portfolioId,
					rules,
					mode,
				},
			);
			return response.data;
		});
	}

	async getMetricsHistory(deliveryId: number): Promise<DeliveryMetricsHistory> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(
				`/deliveries/${deliveryId}/metrics-history`,
			);
			return parseDeliveryMetricsHistory(response.data);
		});
	}
}
