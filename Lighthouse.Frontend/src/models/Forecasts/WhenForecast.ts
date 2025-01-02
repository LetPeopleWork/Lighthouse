import type { IForecast } from "./IForecast";

export interface IWhenForecast extends IForecast {
	expectedDate: Date;
}

export class WhenForecast implements IWhenForecast {
	probability!: number;
	expectedDate!: Date;

	constructor(probability: number, expectedDate: Date) {
		if (probability < 0 || probability > 100) {
			throw new RangeError("Probability must be between 0 and 100.");
		}

		if (Number.isNaN(expectedDate.getTime())) {
			throw new Error("Invalid date.");
		}

		this.probability = probability;
		this.expectedDate = expectedDate;
	}
}
