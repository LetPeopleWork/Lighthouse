import { WhenForecast } from "./Forecasts/WhenForecast";

export interface IFeatureLikelihood {
	featureId: number;
	likelihoodPercentage: number;
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

	static fromBackend(data: IDelivery): Delivery {
		console.log("Mapping delivery from backend:", data);

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

		delivery.completionDates = (data.completionDates || []).map(
			(forecastData) => WhenForecast.fromBackend(forecastData),
		);

		return delivery;
	}

	getFormattedDate(): string {
		return new Date(this.date).toLocaleDateString();
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
