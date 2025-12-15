export interface IDelivery {
	id: number;
	name: string;
	date: string; // ISO date string from backend
	portfolioId: number;
	features: { id: number; name: string }[]; // Simplified feature reference
	likelihoodPercentage: number; // Probability of delivery completion on time
}

export class Delivery implements IDelivery {
	id!: number;
	name!: string;
	date!: string;
	portfolioId!: number;
	features!: { id: number; name: string }[];
	likelihoodPercentage!: number;

	static fromBackend(data: IDelivery): Delivery {
		const delivery = new Delivery();
		delivery.id = data.id;
		delivery.name = data.name;
		delivery.date = data.date;
		delivery.portfolioId = data.portfolioId;
		delivery.features = data.features || [];
		delivery.likelihoodPercentage = data.likelihoodPercentage;
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
}
