import { Delivery, type IDelivery } from "../../models/Delivery";
import {
	type DeliveryMetricsHistory,
	parseDeliveryMetricsHistory,
} from "../../models/Delivery/DeliveryMetricsHistory";
import {
	DeliveryNote,
	type IDeliveryNote,
} from "../../models/Delivery/DeliveryNote";
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
	getNotes(deliveryId: number): Promise<DeliveryNote[]>;
	addNote(deliveryId: number, text: string): Promise<DeliveryNote>;
	updateNote(
		deliveryId: number,
		noteId: number,
		text: string,
	): Promise<DeliveryNote>;
	deleteNote(deliveryId: number, noteId: number): Promise<void>;
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
			return BaseApiService.asArray(
				response.data,
				`/deliveries/portfolio/${portfolioId}`,
			).map((data) => Delivery.fromBackend(data));
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

	async getNotes(deliveryId: number): Promise<DeliveryNote[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IDeliveryNote[]>(
				`/deliveries/${deliveryId}/notes`,
			);
			return response.data.map((note) => DeliveryNote.fromBackend(note));
		});
	}

	async addNote(deliveryId: number, text: string): Promise<DeliveryNote> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IDeliveryNote>(
				`/deliveries/${deliveryId}/notes`,
				{ text },
			);
			return DeliveryNote.fromBackend(response.data);
		});
	}

	async updateNote(
		deliveryId: number,
		noteId: number,
		text: string,
	): Promise<DeliveryNote> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.put<IDeliveryNote>(
				`/deliveries/${deliveryId}/notes/${noteId}`,
				{ text },
			);
			return DeliveryNote.fromBackend(response.data);
		});
	}

	async deleteNote(deliveryId: number, noteId: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete(`/deliveries/${deliveryId}/notes/${noteId}`);
		});
	}
}
