import type { IForecast } from "./IForecast";

export interface IHowManyForecast extends IForecast {
	value: number;
}

export class HowManyForecast implements IHowManyForecast {
	value: number;
	probability: number;

	constructor(probability: number, expectedItems: number) {
		this.probability = probability;
		this.value = expectedItems;
	}
}
