import { IForecast } from "./IForecast";

export interface IHowManyForecast extends IForecast {
    expectedItems: number
}

export class HowManyForecast implements IHowManyForecast{
    expectedItems: number;
    probability: number;

    constructor(probability: number, expectedItems: number){
        this.probability = probability;
        this.expectedItems = expectedItems;
    }
}