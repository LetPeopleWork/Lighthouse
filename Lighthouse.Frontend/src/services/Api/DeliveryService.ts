import { z } from "zod";
import { Delivery, type IDelivery } from "../../models/Delivery";
import {
	ArchivedDelivery,
	ArchivedDeliverySchema,
} from "../../models/Delivery/ArchivedDelivery";
import {
	type DeliveryMetricsHistory,
	parseDeliveryMetricsHistory,
} from "../../models/Delivery/DeliveryMetricsHistory";
import {
	DeliveryNote,
	type IDeliveryNote,
} from "../../models/Delivery/DeliveryNote";
import {
	DeliverySourceOptionSchema,
	DeliverySourcePreviewSchema,
	DeliverySourceSchema,
	type IDeliverySource,
	type IDeliverySourceOption,
	type IDeliverySourcePreview,
} from "../../models/Delivery/DeliverySource";
import type { Feature } from "../../models/Feature";
import {
	DeliverySelectionMode,
	type IWorkItemRuleCondition,
	type IWorkItemRuleSchema,
} from "../../models/WorkItemRules";
import { BaseApiService } from "./BaseApiService";

export interface IDeliveryCreateOptions {
	portfolioId: number;
	name: string;
	date: Date;
	featureIds: number[];
	selectionMode?: DeliverySelectionMode;
	sourceKey?: string;
	sourceReference?: string;
	rules?: IWorkItemRuleCondition[];
	mode?: "and" | "or";
}

/**
 * Asking for manual selection is also how a Delivery stops following a source: the server drops the
 * binding and keeps whatever name, date and Features the source last gave it.
 */
export interface IDeliveryUpdateOptions {
	deliveryId: number;
	name: string;
	date: Date;
	featureIds: number[];
	selectionMode?: DeliverySelectionMode;
	sourceKey?: string;
	sourceReference?: string;
	rules?: IWorkItemRuleCondition[];
	mode?: "and" | "or";
	concurrencyToken?: string;
}

/**
 * A Portfolio's Deliveries, with the retired ones kept apart from the ones still running. They
 * arrive separated rather than flagged because they are worked out from different things - the
 * running ones from the Features as they stand now, the retired ones from what was written down
 * when they closed.
 */
export interface IPortfolioDeliveries {
	active: Delivery[];
	archived: ArchivedDelivery[];
}

const PortfolioDeliveriesSchema = z.object({
	active: z.array(z.custom<IDelivery>()),
	archived: z.array(ArchivedDeliverySchema),
});

export interface IDeliveryService {
	getByPortfolio(portfolioId: number): Promise<IPortfolioDeliveries>;
	create(options: IDeliveryCreateOptions): Promise<void>;
	update(options: IDeliveryUpdateOptions): Promise<void>;
	delete(deliveryId: number): Promise<void>;
	archive(deliveryId: number, concurrencyToken?: string): Promise<void>;
	unarchive(deliveryId: number, concurrencyToken?: string): Promise<void>;
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
	/**
	 * Three answers that all look like "nothing" and none of which may be turned into another. An
	 * empty source list means this connection has nothing a date could be bound to, which is the
	 * ordinary case and the reason the tab stays hidden. A refusal on a source key means the key is
	 * unknown or the Release behind it is gone — somebody has to go and fix a setting. A preview
	 * that resolves with no Features means the Release exists and has nothing tagged against it yet.
	 * Collapsing the refusal into the empty list is the tempting one, and it silently tells a user
	 * their configuration is fine when it is not.
	 */
	getDeliverySources(portfolioId: number): Promise<IDeliverySource[]>;
	getDeliverySourceOptions(
		portfolioId: number,
		sourceKey: string,
	): Promise<IDeliverySourceOption[]>;
	previewDeliverySource(
		portfolioId: number,
		sourceKey: string,
		sourceReference: string,
	): Promise<IDeliverySourcePreview>;
}

export class DeliveryService
	extends BaseApiService
	implements IDeliveryService
{
	async getByPortfolio(portfolioId: number): Promise<IPortfolioDeliveries> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(
				`/deliveries/portfolio/${portfolioId}`,
			);
			const parsed = BaseApiService.parse(
				PortfolioDeliveriesSchema,
				response.data,
			);

			return {
				active: parsed.active.map((data) => Delivery.fromBackend(data)),
				archived: parsed.archived.map((data) =>
					ArchivedDelivery.fromParsed(data),
				),
			};
		});
	}

	async create({
		portfolioId,
		name,
		date,
		featureIds,
		selectionMode = DeliverySelectionMode.Manual,
		sourceKey,
		sourceReference,
		rules,
		mode,
	}: IDeliveryCreateOptions): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post<void>(`/deliveries/portfolio/${portfolioId}`, {
				name,
				date: date.toISOString(),
				featureIds,
				selectionMode,
				sourceKey,
				sourceReference,
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
		sourceKey,
		sourceReference,
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
				sourceKey,
				sourceReference,
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

	async archive(deliveryId: number, concurrencyToken?: string): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post<void>(`/deliveries/${deliveryId}/archive`, {
				concurrencyToken,
			});
		});
	}

	async unarchive(
		deliveryId: number,
		concurrencyToken?: string,
	): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post<void>(`/deliveries/${deliveryId}/unarchive`, {
				concurrencyToken,
			});
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

	async getDeliverySources(portfolioId: number): Promise<IDeliverySource[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(
				`/portfolios/${portfolioId}/delivery-sources`,
			);
			return BaseApiService.parse(z.array(DeliverySourceSchema), response.data);
		});
	}

	async getDeliverySourceOptions(
		portfolioId: number,
		sourceKey: string,
	): Promise<IDeliverySourceOption[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(
				`/portfolios/${portfolioId}/delivery-sources/${sourceKey}/options`,
			);
			return BaseApiService.parse(
				z.array(DeliverySourceOptionSchema),
				response.data,
			);
		});
	}

	async previewDeliverySource(
		portfolioId: number,
		sourceKey: string,
		sourceReference: string,
	): Promise<IDeliverySourcePreview> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<unknown>(
				`/portfolios/${portfolioId}/delivery-sources/${sourceKey}/preview`,
				{ sourceReference },
			);
			return BaseApiService.parse(DeliverySourcePreviewSchema, response.data);
		});
	}
}
