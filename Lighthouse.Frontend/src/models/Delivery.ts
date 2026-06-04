import { WhenForecast } from "./Forecasts/WhenForecast";
import {
	DeliverySelectionMode,
	type IWorkItemRuleCondition,
	WorkItemRuleCondition,
} from "./WorkItemRules";

export interface IFeatureLikelihood {
	featureId: number;
	likelihoodPercentage: number;
	hasSufficientData?: boolean;
}

export interface IDelivery {
	id: number;
	name: string;
	date: string;
	portfolioId: number;
	features: number[];
	likelihoodPercentage: number;
	progress: number;
	remainingWork: number;
	totalWork: number;
	featureLikelihoods: IFeatureLikelihood[];
	completionDates: WhenForecast[];
	selectionMode: DeliverySelectionMode;
	rules?: IWorkItemRuleCondition[];
	mode?: "and" | "or";
	concurrencyToken?: string;
	hasSufficientData?: boolean;
	metricSnapshotCount: number;
}

export class Delivery implements IDelivery {
	id!: number;
	name!: string;
	date!: string;
	portfolioId!: number;
	features!: number[];
	likelihoodPercentage!: number;
	progress!: number;
	remainingWork!: number;
	totalWork!: number;
	featureLikelihoods!: IFeatureLikelihood[];
	completionDates!: WhenForecast[];
	selectionMode!: DeliverySelectionMode;
	rules?: WorkItemRuleCondition[];
	mode?: "and" | "or";
	concurrencyToken?: string;
	hasSufficientData!: boolean;
	metricSnapshotCount!: number;

	static fromBackend(data: IDelivery): Delivery {
		const delivery = new Delivery();
		delivery.id = data.id;
		delivery.name = data.name;
		delivery.date = data.date;
		delivery.portfolioId = data.portfolioId;
		delivery.features = data.features || [];
		delivery.likelihoodPercentage = data.likelihoodPercentage;
		delivery.progress = data.progress || 0;
		delivery.remainingWork = data.remainingWork || 0;
		delivery.totalWork = data.totalWork || 0;
		delivery.featureLikelihoods = data.featureLikelihoods || [];
		delivery.hasSufficientData = data.hasSufficientData ?? true;
		delivery.metricSnapshotCount = data.metricSnapshotCount ?? 0;
		delivery.selectionMode = data.selectionMode ?? DeliverySelectionMode.Manual;
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
