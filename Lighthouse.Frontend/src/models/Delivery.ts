import type { DeliverySourceUnavailableReason } from "./Delivery/DeliverySource";
import { WhenForecast } from "./Forecasts/WhenForecast";
import {
	DeliverySelectionMode,
	type IWorkItemRuleCondition,
	WorkItemRuleCondition,
} from "./WorkItemRules";

export interface IFeatureLikelihood {
	featureId: number;
	// null when a contributing team has no throughput history, so no forecast exists (ADR-112).
	likelihoodPercentage: number | null;
	teamsWithoutForecast?: string[];
	hasSufficientData?: boolean;
}

export interface IDelivery {
	id: number;
	name: string;
	date: string;
	portfolioId: number;
	features: number[];
	likelihoodPercentage: number | null;
	teamsWithoutForecast?: string[];
	progress: number;
	remainingWork: number;
	totalWork: number;
	featureLikelihoods: IFeatureLikelihood[];
	completionDates: WhenForecast[];
	selectionMode: DeliverySelectionMode;
	sourceKey?: string | null;
	sourceReference?: string | null;
	sourceLastSyncedOn?: string | null;
	sourceUnavailableReason?: DeliverySourceUnavailableReason | null;
	/** Whether this Delivery's forecast is broadcast onto the source it follows. */
	publishForecastToSource?: boolean;
	/** What the source said when it would not take the forecast, and the day it said it. */
	lastPublishRefusedOn?: string | null;
	lastPublishRefusalReason?: string | null;
	rules?: IWorkItemRuleCondition[];
	mode?: "and" | "or";
	concurrencyToken?: string;
	hasSufficientData?: boolean;
	metricSnapshotCount: number;
	/** Decided by the backend on the instance time zone: the browser's day is not the product's day. */
	isOverdue?: boolean;
}

export class Delivery implements IDelivery {
	id!: number;
	name!: string;
	date!: string;
	portfolioId!: number;
	features!: number[];
	likelihoodPercentage!: number | null;
	teamsWithoutForecast!: string[];
	progress!: number;
	remainingWork!: number;
	totalWork!: number;
	featureLikelihoods!: IFeatureLikelihood[];
	completionDates!: WhenForecast[];
	selectionMode!: DeliverySelectionMode;
	sourceKey!: string | null;
	sourceReference!: string | null;
	sourceLastSyncedOn!: string | null;
	sourceUnavailableReason!: DeliverySourceUnavailableReason | null;
	publishForecastToSource!: boolean;
	lastPublishRefusedOn!: string | null;
	lastPublishRefusalReason!: string | null;
	rules?: WorkItemRuleCondition[];
	mode?: "and" | "or";
	concurrencyToken?: string;
	hasSufficientData!: boolean;
	metricSnapshotCount!: number;
	isOverdue!: boolean;

	static fromBackend(data: IDelivery): Delivery {
		const delivery = new Delivery();
		delivery.id = data.id;
		delivery.name = data.name;
		delivery.date = data.date;
		delivery.portfolioId = data.portfolioId;
		delivery.features = data.features || [];
		delivery.likelihoodPercentage = data.likelihoodPercentage ?? null;
		delivery.teamsWithoutForecast = data.teamsWithoutForecast ?? [];
		delivery.progress = data.progress || 0;
		delivery.remainingWork = data.remainingWork || 0;
		delivery.totalWork = data.totalWork || 0;
		delivery.featureLikelihoods = data.featureLikelihoods || [];
		delivery.hasSufficientData = data.hasSufficientData ?? true;
		delivery.metricSnapshotCount = data.metricSnapshotCount ?? 0;
		delivery.isOverdue = data.isOverdue ?? false;
		delivery.selectionMode = data.selectionMode ?? DeliverySelectionMode.Manual;
		delivery.sourceKey = data.sourceKey ?? null;
		delivery.sourceReference = data.sourceReference ?? null;
		delivery.sourceLastSyncedOn = data.sourceLastSyncedOn ?? null;
		delivery.sourceUnavailableReason = data.sourceUnavailableReason ?? null;
		delivery.publishForecastToSource = data.publishForecastToSource ?? false;
		delivery.lastPublishRefusedOn = data.lastPublishRefusedOn ?? null;
		delivery.lastPublishRefusalReason = data.lastPublishRefusalReason ?? null;
		delivery.rules = data.rules?.map((r) =>
			WorkItemRuleCondition.fromBackend(r),
		);
		delivery.mode = data.mode?.toLowerCase() === "or" ? "or" : "and";
		delivery.concurrencyToken = data.concurrencyToken;

		delivery.completionDates = (data.completionDates || []).map(
			(forecastData) => WhenForecast.fromBackend(forecastData),
		);

		return delivery;
	}

	getFormattedDate(): string {
		return new Date(this.date).toLocaleDateString(undefined, {
			timeZone: "UTC",
		});
	}

	getFeatureCount(): number {
		return this.features.length;
	}

	getLikelihoodLevel(): "risky" | "realistic" | "likely" | "certain" {
		// No forecast is not the same as a bad one, but "risky" is the honest fallback for a caller
		// that insists on a level - it must not read as certainty (ADR-112).
		if (this.likelihoodPercentage === null) return "risky";
		if (this.likelihoodPercentage < 50) return "risky";
		if (this.likelihoodPercentage < 70) return "realistic";
		if (this.likelihoodPercentage < 85) return "likely";
		return "certain";
	}

	getFeatureLikelihood(featureId: number): number {
		const featureLikelihood = this.featureLikelihoods.find(
			(fl) => fl.featureId === featureId,
		);
		return featureLikelihood?.likelihoodPercentage ?? 0;
	}
}
