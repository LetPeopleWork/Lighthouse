export interface IDelivery {
	id: number;
	name: string;
	date: string; // ISO date string from backend
	portfolioId: number;
	features: { id: number; name: string }[]; // Simplified feature reference
}

export class Delivery implements IDelivery {
	id!: number;
	name!: string;
	date!: string;
	portfolioId!: number;
	features!: { id: number; name: string }[];

	static fromBackend(data: IDelivery): Delivery {
		const delivery = new Delivery();
		delivery.id = data.id;
		delivery.name = data.name;
		delivery.date = data.date;
		delivery.portfolioId = data.portfolioId;
		delivery.features = data.features || [];
		return delivery;
	}

	getFormattedDate(): string {
		return new Date(this.date).toLocaleDateString();
	}

	getFeatureCount(): number {
		return this.features.length;
	}
}
